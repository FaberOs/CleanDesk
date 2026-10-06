using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace CleanDesk
{
    public class EmulatorItem : INotifyPropertyChanged
    {
        public string Name { get; set; }           // e.g. "Arrenta_-_Medium_Phone"
        public string DisplayName { get; set; }    // e.g. "Arrenta - Medium Phone"
        public string TargetApi { get; set; }      // e.g. "Android 36"
        public string Abi { get; set; }            // e.g. "x86_64"
        public string RamConfigured { get; set; }  // e.g. "4096 MB"
        public string DeviceSerial { get; set; }   // e.g. "emulator-5554"
        public int Pid { get; set; }               // Process ID of qemu-system-x86_64.exe
        public long RamUsedBytes { get; set; }     // WorkingSet64 in bytes
        public long MemoryUsageMb
        {
            get { return RamUsedBytes / (1024 * 1024); }
        }

        private bool _isBusy;
        public bool IsBusy
        {
            get { return _isBusy || _isStarting; }
            set
            {
                if (_isBusy != value)
                {
                    _isBusy = value;
                    OnPropertyChanged("IsBusy");
                    OnPropertyChanged("CanExecuteAction");
                }
            }
        }

        private string _statusText;
        public string StatusText
        {
            get { return _statusText; }
            set
            {
                if (_statusText != value)
                {
                    _statusText = value;
                    OnPropertyChanged("StatusText");
                    OnPropertyChanged("Subtitle");
                }
            }
        }

        private bool _isOnline;
        public bool IsOnline
        {
            get { return _isOnline; }
            set
            {
                if (_isOnline != value)
                {
                    _isOnline = value;
                    OnPropertyChanged("IsOnline");
                    NotifyUiProperties();
                }
            }
        }

        private bool _isStarting;
        public bool IsStarting
        {
            get { return _isStarting; }
            set
            {
                if (_isStarting != value)
                {
                    _isStarting = value;
                    OnPropertyChanged("IsStarting");
                    NotifyUiProperties();
                }
            }
        }

        public string Glyph
        {
            get { return "\uE8EA"; } // Mobile phone glyph in Segoe Fluent / MDL2
        }

        public string FormattedRamUsed
        {
            get
            {
                if (RamUsedBytes <= 0) return "";
                double gb = RamUsedBytes / (1024.0 * 1024.0 * 1024.0);
                if (gb >= 1.0)
                {
                    return string.Format("{0:0.00} GB RAM", gb);
                }
                double mb = RamUsedBytes / (1024.0 * 1024.0);
                return string.Format("{0:0} MB RAM", mb);
            }
        }

        public string Subtitle
        {
            get
            {
                if (!string.IsNullOrEmpty(_statusText))
                {
                    return _statusText;
                }
                if (IsOnline)
                {
                    string ram = !string.IsNullOrEmpty(FormattedRamUsed) ? FormattedRamUsed : (Strings.IsSpanish ? "Activo" : "Online");
                    if (!string.IsNullOrEmpty(DeviceSerial))
                    {
                        return string.Format("{0} • {1}", DeviceSerial, ram);
                    }
                    return ram;
                }
                if (IsStarting)
                {
                    return Strings.IsSpanish ? "Iniciando máquina virtual..." : "Starting virtual machine...";
                }

                string apiInfo = !string.IsNullOrEmpty(TargetApi) ? TargetApi : "Android";
                string ramCfg = !string.IsNullOrEmpty(RamConfigured) ? RamConfigured : "";
                if (!string.IsNullOrEmpty(ramCfg))
                {
                    return string.Format("{0} • {1}", apiInfo, ramCfg);
                }
                return apiInfo;
            }
        }

        public string StatusBadgeText
        {
            get
            {
                if (IsOnline) return Strings.IsSpanish ? "ACTIVO" : "ONLINE";
                if (IsStarting) return Strings.IsSpanish ? "INICIANDO" : "STARTING";
                return Strings.IsSpanish ? "DETENIDO" : "STOPPED";
            }
        }

        public string StatusDotColor
        {
            get
            {
                if (IsOnline) return "#4ADE80";
                if (IsStarting) return "#FBBF24";
                return "#64748B";
            }
        }

        public string StatusBadgeBg
        {
            get
            {
                if (IsOnline) return "#143E2C";
                if (IsStarting) return "#3D2B14";
                return "#2A303C";
            }
        }

        public string StatusBadgeBorder
        {
            get
            {
                if (IsOnline) return "#4ADE80";
                if (IsStarting) return "#FBBF24";
                return "#353D4C";
            }
        }

        public string StatusBadgeFg
        {
            get
            {
                if (IsOnline) return "#4ADE80";
                if (IsStarting) return "#FBBF24";
                return "#94A3B8";
            }
        }

        public string ActionButtonText
        {
            get
            {
                if (IsOnline) return Strings.IsSpanish ? "Detener" : "Stop";
                if (IsStarting) return Strings.IsSpanish ? "Iniciando..." : "Starting...";
                return Strings.IsSpanish ? "Iniciar" : "Start";
            }
        }

        public string ActionButtonIcon
        {
            get
            {
                if (IsOnline) return "\uE71A"; // Stop square
                if (IsStarting) return "\uE72C"; // Refresh spinner
                return "\uE768"; // Play triangle
            }
        }

        public bool CanExecuteAction
        {
            get { return !IsStarting && !_isBusy; }
        }

        public void NotifyUiProperties()
        {
            OnPropertyChanged("Subtitle");
            OnPropertyChanged("FormattedRamUsed");
            OnPropertyChanged("StatusBadgeText");
            OnPropertyChanged("StatusDotColor");
            OnPropertyChanged("StatusBadgeBg");
            OnPropertyChanged("StatusBadgeBorder");
            OnPropertyChanged("StatusBadgeFg");
            OnPropertyChanged("ActionButtonText");
            OnPropertyChanged("ActionButtonIcon");
            OnPropertyChanged("CanExecuteAction");
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class EmulatorScanResult
    {
        public bool SdkFound { get; set; }
        public string SdkPath { get; set; }
        public string EmulatorExePath { get; set; }
        public string AdbExePath { get; set; }
        public List<EmulatorItem> Emulators { get; set; }
        public int RunningCount { get; set; }
        public long TotalRamUsedBytes { get; set; }

        public EmulatorScanResult()
        {
            Emulators = new List<EmulatorItem>();
        }
    }

    public static class EmulatorManager
    {
        private static string _cachedEmulatorExe = null;
        private static string _cachedAdbExe = null;
        private static string _cachedSdkPath = null;
        private static readonly object _lock = new object();

        public static bool IsSdkAvailable()
        {
            string emulatorExe, adbExe, sdkPath;
            return DetectSdkTools(out emulatorExe, out adbExe, out sdkPath);
        }

        public static bool DetectSdkTools(out string emulatorExe, out string adbExe, out string sdkPath)
        {
            lock (_lock)
            {
                if (!string.IsNullOrEmpty(_cachedEmulatorExe) && File.Exists(_cachedEmulatorExe) &&
                    !string.IsNullOrEmpty(_cachedAdbExe) && File.Exists(_cachedAdbExe))
                {
                    emulatorExe = _cachedEmulatorExe;
                    adbExe = _cachedAdbExe;
                    sdkPath = _cachedSdkPath;
                    return true;
                }

                // 1. Check environment variables
                string envHome = Environment.GetEnvironmentVariable("ANDROID_HOME");
                string envSdkRoot = Environment.GetEnvironmentVariable("ANDROID_SDK_ROOT");
                string localApp = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string defaultSdk = Path.Combine(localApp, "Android", "Sdk");

                var candidates = new List<string>();
                if (!string.IsNullOrEmpty(envHome)) candidates.Add(envHome);
                if (!string.IsNullOrEmpty(envSdkRoot)) candidates.Add(envSdkRoot);
                candidates.Add(defaultSdk);

                // Common Android Studio SDK locations
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), "Android", "Android Studio"));
                candidates.Add(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "AppData", "Local", "Android", "Sdk"));

                foreach (var dir in candidates)
                {
                    if (string.IsNullOrEmpty(dir) || !Directory.Exists(dir)) continue;

                    string emu = Path.Combine(dir, "emulator", "emulator.exe");
                    string adb = Path.Combine(dir, "platform-tools", "adb.exe");

                    if (File.Exists(emu) && File.Exists(adb))
                    {
                        _cachedEmulatorExe = emu;
                        _cachedAdbExe = adb;
                        _cachedSdkPath = dir;
                        emulatorExe = emu;
                        adbExe = adb;
                        sdkPath = dir;
                        return true;
                    }
                }

                // 2. Check PATH
                string pathAdb = FindExecutableInPath("adb.exe");
                string pathEmu = FindExecutableInPath("emulator.exe");

                if (!string.IsNullOrEmpty(pathAdb) && !string.IsNullOrEmpty(pathEmu))
                {
                    _cachedEmulatorExe = pathEmu;
                    _cachedAdbExe = pathAdb;
                    _cachedSdkPath = Path.GetDirectoryName(Path.GetDirectoryName(pathAdb));
                    emulatorExe = pathEmu;
                    adbExe = pathAdb;
                    sdkPath = _cachedSdkPath;
                    return true;
                }

                if (!string.IsNullOrEmpty(pathAdb))
                {
                    // Fallback: check sibling directory from adb.exe
                    string dir = Path.GetDirectoryName(Path.GetDirectoryName(pathAdb));
                    string emu = Path.Combine(dir, "emulator", "emulator.exe");
                    if (File.Exists(emu))
                    {
                        _cachedEmulatorExe = emu;
                        _cachedAdbExe = pathAdb;
                        _cachedSdkPath = dir;
                        emulatorExe = emu;
                        adbExe = pathAdb;
                        sdkPath = dir;
                        return true;
                    }
                }

                emulatorExe = null;
                adbExe = null;
                sdkPath = null;
                return false;
            }
        }

        private static string FindExecutableInPath(string exeName)
        {
            try
            {
                string pathEnv = Environment.GetEnvironmentVariable("PATH");
                if (string.IsNullOrEmpty(pathEnv)) return null;

                foreach (string p in pathEnv.Split(';'))
                {
                    string trimmed = p.Trim();
                    if (string.IsNullOrEmpty(trimmed)) continue;
                    try
                    {
                        string full = Path.Combine(trimmed, exeName);
                        if (File.Exists(full)) return full;
                    }
                    catch { }
                }
            }
            catch { }
            return null;
        }

        public static async Task<EmulatorScanResult> ScanEmulatorsAsync()
        {
            return await Task.Run(() =>
            {
                var result = new EmulatorScanResult();
                string emulatorExe, adbExe, sdkPath;
                bool hasSdk = DetectSdkTools(out emulatorExe, out adbExe, out sdkPath);

                result.SdkFound = hasSdk;
                result.SdkPath = sdkPath;
                result.EmulatorExePath = emulatorExe;
                result.AdbExePath = adbExe;

                // 1. Gather all AVDs from emulator CLI or ~/.android/avd/*.ini
                var avdNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

                if (hasSdk)
                {
                    try
                    {
                        string avdOutput = RunProcessCaptured(emulatorExe, "-list-avds", 4000);
                        if (!string.IsNullOrEmpty(avdOutput))
                        {
                            foreach (string line in avdOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                            {
                                string trimmed = line.Trim();
                                if (!string.IsNullOrEmpty(trimmed) && !trimmed.StartsWith("INFO", StringComparison.OrdinalIgnoreCase))
                                {
                                    avdNames.Add(trimmed);
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Fallback / complement from ~/.android/avd/*.ini
                string userAvdDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".android", "avd");
                if (Directory.Exists(userAvdDir))
                {
                    try
                    {
                        var inis = Directory.GetFiles(userAvdDir, "*.ini");
                        foreach (var ini in inis)
                        {
                            string avdName = GetFileNameWithoutEmptyExtension(ini);
                            if (!string.IsNullOrEmpty(avdName))
                            {
                                avdNames.Add(avdName);
                            }
                        }
                    }
                    catch { }
                }

                // 2. Query Running Devices via ADB
                var runningDevices = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase); // serial -> avdName
                if (hasSdk)
                {
                    try
                    {
                        string adbDevices = RunProcessCaptured(adbExe, "devices -l", 4000);
                        if (!string.IsNullOrEmpty(adbDevices))
                        {
                            var lines = adbDevices.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                            foreach (var line in lines)
                            {
                                var m = Regex.Match(line, @"^(emulator-\d+)\s+device");
                                if (m.Success)
                                {
                                    string serial = m.Groups[1].Value;
                                    string avdNameOutput = RunProcessCaptured(adbExe, string.Format("-s {0} emu avd name", serial), 3000);
                                    if (!string.IsNullOrEmpty(avdNameOutput))
                                    {
                                        string firstLine = avdNameOutput.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
                                        if (!string.IsNullOrEmpty(firstLine) && !firstLine.Equals("OK", StringComparison.OrdinalIgnoreCase))
                                        {
                                            runningDevices[serial] = firstLine.Trim();
                                            avdNames.Add(firstLine.Trim());
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // 3. Inspect Running QEMU processes to correlate PIDs and RAM (WorkingSet)
                var qemuProcesses = new Dictionary<string, Tuple<int, long>>(StringComparer.OrdinalIgnoreCase); // avdName -> (PID, WorkingSetBytes)
                try
                {
                    using (var searcher = new ManagementObjectSearcher("SELECT ProcessId, CommandLine, WorkingSetSize FROM Win32_Process WHERE Name LIKE 'qemu%' OR Name LIKE 'emulator%'"))
                    {
                        foreach (ManagementObject obj in searcher.Get())
                        {
                            try
                            {
                                int pid = Convert.ToInt32(obj["ProcessId"]);
                                string cmd = obj["CommandLine"] as string;
                                long mem = 0;
                                if (obj["WorkingSetSize"] != null)
                                {
                                    mem = Convert.ToInt64(obj["WorkingSetSize"]);
                                }

                                if (!string.IsNullOrEmpty(cmd))
                                {
                                    var match = Regex.Match(cmd, @"-avd\s+[""']?([^""'\s]+)[""']?", RegexOptions.IgnoreCase);
                                    if (match.Success)
                                    {
                                        string name = match.Groups[1].Value.Trim();
                                        qemuProcesses[name] = new Tuple<int, long>(pid, mem);
                                        avdNames.Add(name);
                                    }
                                }
                            }
                            catch { }
                        }
                    }
                }
                catch
                {
                    // Fallback to Process.GetProcessesByName
                    try
                    {
                        var procs = Process.GetProcessesByName("qemu-system-x86_64");
                        if (procs != null && procs.Length > 0 && runningDevices.Count == 1)
                        {
                            var singleAvd = runningDevices.Values.FirstOrDefault();
                            if (!string.IsNullOrEmpty(singleAvd))
                            {
                                qemuProcesses[singleAvd] = new Tuple<int, long>(procs[0].Id, procs[0].WorkingSet64);
                            }
                        }
                    }
                    catch { }
                }

                // 4. Build EmulatorItem list with metadata
                long totalRamUsed = 0;
                int runningCount = 0;

                foreach (var avd in avdNames.OrderBy(a => a))
                {
                    var item = new EmulatorItem
                    {
                        Name = avd,
                        DisplayName = FormatDisplayName(avd)
                    };

                    // Read ini metadata if exists
                    ReadAvdConfig(userAvdDir, avd, item);

                    // Check if online via ADB or QEMU process
                    string matchingSerial = runningDevices.FirstOrDefault(kvp => kvp.Value.Equals(avd, StringComparison.OrdinalIgnoreCase)).Key;
                    Tuple<int, long> qemuInfo;
                    bool hasQemu = qemuProcesses.TryGetValue(avd, out qemuInfo);

                    if (!string.IsNullOrEmpty(matchingSerial) || hasQemu)
                    {
                        item.IsOnline = true;
                        item.DeviceSerial = matchingSerial;
                        if (hasQemu)
                        {
                            item.Pid = qemuInfo.Item1;
                            item.RamUsedBytes = qemuInfo.Item2;
                            totalRamUsed += item.RamUsedBytes;
                        }
                        runningCount++;
                    }
                    else
                    {
                        item.IsOnline = false;
                        item.DeviceSerial = null;
                        item.Pid = 0;
                        item.RamUsedBytes = 0;
                    }

                    item.NotifyUiProperties();
                    result.Emulators.Add(item);
                }

                result.RunningCount = runningCount;
                result.TotalRamUsedBytes = totalRamUsed;
                return result;
            });
        }

        private static void ReadAvdConfig(string userAvdDir, string avdName, EmulatorItem item)
        {
            try
            {
                string iniFile = Path.Combine(userAvdDir, avdName + ".ini");
                string avdFolderPath = null;

                if (File.Exists(iniFile))
                {
                    foreach (var line in File.ReadAllLines(iniFile))
                    {
                        if (line.StartsWith("path=", StringComparison.OrdinalIgnoreCase))
                        {
                            avdFolderPath = line.Substring(5).Trim();
                        }
                        else if (line.StartsWith("target=", StringComparison.OrdinalIgnoreCase))
                        {
                            string t = line.Substring(7).Trim();
                            item.TargetApi = FormatTargetApi(t);
                        }
                    }
                }

                if (string.IsNullOrEmpty(avdFolderPath))
                {
                    avdFolderPath = Path.Combine(userAvdDir, avdName + ".avd");
                }

                string configFile = Path.Combine(avdFolderPath, "config.ini");
                if (File.Exists(configFile))
                {
                    foreach (var line in File.ReadAllLines(configFile))
                    {
                        if (line.StartsWith("avd.ini.displayname=", StringComparison.OrdinalIgnoreCase))
                        {
                            string disp = line.Substring(20).Trim();
                            if (!string.IsNullOrEmpty(disp)) item.DisplayName = disp;
                        }
                        else if (line.StartsWith("target=", StringComparison.OrdinalIgnoreCase) && string.IsNullOrEmpty(item.TargetApi))
                        {
                            item.TargetApi = FormatTargetApi(line.Substring(7).Trim());
                        }
                        else if (line.StartsWith("abi.type=", StringComparison.OrdinalIgnoreCase))
                        {
                            item.Abi = line.Substring(9).Trim();
                        }
                        else if (line.StartsWith("hw.ramSize=", StringComparison.OrdinalIgnoreCase))
                        {
                            string ram = line.Substring(11).Trim();
                            item.RamConfigured = ram + " MB";
                        }
                    }
                }
            }
            catch { }
        }

        private static string FormatDisplayName(string avdName)
        {
            if (string.IsNullOrEmpty(avdName)) return "";
            return avdName.Replace("_-_", " - ").Replace('_', ' ');
        }

        private static string FormatTargetApi(string target)
        {
            if (string.IsNullOrEmpty(target)) return "Android";
            if (target.StartsWith("android-", StringComparison.OrdinalIgnoreCase))
            {
                string api = target.Substring(8);
                return "API " + api;
            }
            return target;
        }

        public static async Task<bool> StartEmulatorAsync(EmulatorItem item, bool coldBoot = false, bool wipeData = false)
        {
            if (item == null) return false;
            return await StartEmulatorAsync(item.Name, coldBoot, wipeData);
        }

        public static async Task<bool> StartEmulatorAsync(string avdName, bool coldBoot = false, bool wipeData = false)
        {
            return await Task.Run(() =>
            {
                string emulatorExe, adbExe, sdkPath;
                if (!DetectSdkTools(out emulatorExe, out adbExe, out sdkPath)) return false;

                try
                {
                    var args = new StringBuilder();
                    args.AppendFormat("-avd \"{0}\"", avdName);

                    if (coldBoot)
                    {
                        args.Append(" -no-snapshot-load");
                    }
                    if (wipeData)
                    {
                        args.Append(" -wipe-data");
                    }

                    var psi = new ProcessStartInfo
                    {
                        FileName = emulatorExe,
                        Arguments = args.ToString(),
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetDirectoryName(emulatorExe)
                    };

                    Process.Start(psi);
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<bool> StopEmulatorAsync(EmulatorItem item)
        {
            if (item == null) return false;
            return await StopEmulatorAsync(item.DeviceSerial, item.Pid);
        }

        public static async Task<bool> StopEmulatorAsync(string serial, int pid = 0)
        {
            return await Task.Run(() =>
            {
                string emulatorExe, adbExe, sdkPath;
                DetectSdkTools(out emulatorExe, out adbExe, out sdkPath);

                bool killedGracefully = false;
                if (!string.IsNullOrEmpty(adbExe) && !string.IsNullOrEmpty(serial))
                {
                    try
                    {
                        RunProcessCaptured(adbExe, string.Format("-s {0} emu kill", serial), 4000);
                        killedGracefully = true;
                    }
                    catch { }
                }

                // If PID was recorded and still alive after 2 seconds, enforce termination
                if (pid > 0)
                {
                    try
                    {
                        Task.Delay(1500).Wait();
                        var p = Process.GetProcessById(pid);
                        if (p != null && !p.HasExited)
                        {
                            p.Kill();
                        }
                    }
                    catch { }
                }

                return killedGracefully;
            });
        }

        public static async Task<int> StopAllEmulatorsAsync()
        {
            var scan = await ScanEmulatorsAsync();
            return await StopAllEmulatorsAsync(scan.Emulators);
        }

        public static async Task<int> StopAllEmulatorsAsync(IEnumerable<EmulatorItem> emulators)
        {
            return await Task.Run(async () =>
            {
                int stoppedCount = 0;
                var running = emulators.Where(e => e.IsOnline).ToList();

                foreach (var emu in running)
                {
                    bool ok = await StopEmulatorAsync(emu.DeviceSerial, emu.Pid);
                    if (ok) stoppedCount++;
                }

                return stoppedCount;
            });
        }

        public static async Task<bool> RebootEmulatorAsync(EmulatorItem item)
        {
            if (item == null || string.IsNullOrEmpty(item.DeviceSerial)) return false;
            return await RebootEmulatorAsync(item.DeviceSerial);
        }

        public static async Task<bool> RebootEmulatorAsync(string serial)
        {
            return await Task.Run(() =>
            {
                string emulatorExe, adbExe, sdkPath;
                if (!DetectSdkTools(out emulatorExe, out adbExe, out sdkPath)) return false;

                try
                {
                    RunProcessCaptured(adbExe, string.Format("-s {0} reboot", serial), 4000);
                    return true;
                }
                catch { return false; }
            });
        }

        private static string RunProcessCaptured(string exe, string args, int timeoutMs)
        {
            try
            {
                var psi = new ProcessStartInfo
                {
                    FileName = exe,
                    Arguments = args,
                    UseShellExecute = false,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    CreateNoWindow = true
                };

                using (var p = Process.Start(psi))
                {
                    if (p.WaitForExit(timeoutMs))
                    {
                        return p.StandardOutput.ReadToEnd();
                    }
                    else
                    {
                        try { p.Kill(); } catch { }
                    }
                }
            }
            catch { }
            return null;
        }

        private static string GetFileNameWithoutEmptyExtension(string path)
        {
            try
            {
                return Path.GetFileNameWithoutExtension(path);
            }
            catch { return null; }
        }
    }
}
