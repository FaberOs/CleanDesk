using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Runtime.InteropServices;

namespace CleanDesk
{
    public class MonitorItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string MonitorId { get; set; }
        public string ShortId { get; set; }
        public string FriendlyName { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public int Frequency { get; set; }
        public bool IsPrimary { get; set; }
        public bool IsActive { get; set; }
        public bool IsVertical { get; set; }

        public string OrientationText
        {
            get { return IsVertical ? (Strings.IsSpanish ? "Vertical" : "Portrait") : (Strings.IsSpanish ? "Horizontal" : "Landscape"); }
        }

        public string ResolutionFormatted
        {
            get 
            { 
                if (!IsActive)
                {
                    return string.Format("{0}x{1} • {2}", Width, Height, Strings.IsSpanish ? "Inactiva" : "Inactive");
                }
                return string.Format("{0}x{1} @ {2}Hz", Width, Height, Frequency); 
            }
        }

        public string Glyph
        {
            get { return "\uE7F4"; }
        }

        public string PrimaryBadgeVisibility
        {
            get { return IsPrimary ? "Visible" : "Collapsed"; }
        }

        public string StatusColor
        {
            get { return IsActive ? "#4ADE80" : "#64748B"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public class DisplayProfileItem : INotifyPropertyChanged
    {
        public string Name { get; set; }
        public string FilePath { get; set; }
        public string Summary { get; set; }
        public string IconGlyph { get; set; }
        public List<string> RequiredMonitorShortIds { get; set; }

        public DisplayProfileItem()
        {
            RequiredMonitorShortIds = new List<string>();
            IconGlyph = "\uE7F4";
            CanApply = true;
        }

        private bool _isActive;
        public bool IsActive
        {
            get { return _isActive; }
            set
            {
                if (_isActive != value)
                {
                    _isActive = value;
                    OnPropertyChanged("IsActive");
                    OnPropertyChanged("ActiveBadgeVisibility");
                    OnPropertyChanged("ApplyButtonVisibility");
                    OnPropertyChanged("CardBgBrush");
                    OnPropertyChanged("CardBorderBrush");
                    OnPropertyChanged("IconBgBrush");
                    OnPropertyChanged("IconFgBrush");
                }
            }
        }

        public string CardBgBrush
        {
            get { return _isActive ? "#141D72FE" : "#00000000"; }
        }

        public string CardBorderBrush
        {
            get { return _isActive ? "#1D72FE" : "#2C3341"; }
        }

        public string IconBgBrush
        {
            get { return _isActive ? "#1D72FE" : "#2A303C"; }
        }

        public string IconFgBrush
        {
            get { return _isActive ? "#FFFFFF" : "#60A5FA"; }
        }

        private bool _canApply = true;
        public bool CanApply
        {
            get { return _canApply; }
            set
            {
                if (_canApply != value)
                {
                    _canApply = value;
                    OnPropertyChanged("CanApply");
                }
            }
        }

        private string _warningMessage;
        public string WarningMessage
        {
            get { return _warningMessage; }
            set
            {
                if (_warningMessage != value)
                {
                    _warningMessage = value;
                    OnPropertyChanged("WarningMessage");
                    OnPropertyChanged("WarningVisibility");
                }
            }
        }

        public string WarningVisibility
        {
            get { return string.IsNullOrEmpty(_warningMessage) ? "Collapsed" : "Visible"; }
        }

        public string ActiveBadgeVisibility
        {
            get { return _isActive ? "Visible" : "Collapsed"; }
        }

        public string ApplyButtonVisibility
        {
            get { return _isActive ? "Collapsed" : "Visible"; }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string prop)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(prop));
        }
    }

    public static class DisplayManager
    {
        private static readonly string ToolsDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CleanDesk", "tools");
        private static readonly string ProfilesDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "CleanDesk", "DisplayProfiles");
        private static readonly string NirSoftZipUrl = "https://www.nirsoft.net/utils/multimonitortool-x64.zip";

        public static string GetProfilesDirectory()
        {
            if (!Directory.Exists(ProfilesDir)) Directory.CreateDirectory(ProfilesDir);
            return ProfilesDir;
        }

        public static async Task<string> EnsureToolAvailableAsync()
        {
            return await Task.Run(() =>
            {
                try
                {
                    if (!Directory.Exists(ToolsDir)) Directory.CreateDirectory(ToolsDir);

                    string targetExe = Path.Combine(ToolsDir, "MultiMonitorTool.exe");
                    if (File.Exists(targetExe)) return targetExe;

                    // 1. Look in application BaseDirectory\tools
                    string localBase = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tools", "MultiMonitorTool.exe");
                    if (File.Exists(localBase))
                    {
                        File.Copy(localBase, targetExe, true);
                        return targetExe;
                    }

                    // 2. Look in user Documents\MultiMonitorTool
                    string docPath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MultiMonitorTool", "MultiMonitorTool.exe");
                    if (File.Exists(docPath))
                    {
                        File.Copy(docPath, targetExe, true);
                        return targetExe;
                    }

                    // 3. Fallback: Download from official CDN
                    try
                    {
                        string zipPath = Path.Combine(ToolsDir, "mmt.zip");
                        using (var wc = new WebClient())
                        {
                            wc.DownloadFile(NirSoftZipUrl, zipPath);
                        }

                        if (File.Exists(zipPath))
                        {
                            System.IO.Compression.ZipFile.ExtractToDirectory(zipPath, ToolsDir);
                            File.Delete(zipPath);
                        }

                        if (File.Exists(targetExe)) return targetExe;
                    }
                    catch
                    {
                        // Silent fallback
                    }

                    if (!string.IsNullOrEmpty(targetExe) && File.Exists(targetExe))
                    {
                        try
                        {
                            string cfgFile = Path.Combine(ToolsDir, "MultiMonitorTool.cfg");
                            if (!File.Exists(cfgFile))
                            {
                                string defaultCfg = "[General]\r\nLoadConfigUseMonitorID=1\r\nLoadConfigUseSerialNumber=1\r\nMonitorsConfigNumOfCalls=5\r\nRepositionOnEnable=1\r\nHideInactiveMonitors=0\r\nShowDisconnectedMonitors=1\r\n";
                                File.WriteAllText(cfgFile, defaultCfg, Encoding.UTF8);
                            }
                            else
                            {
                                string c = File.ReadAllText(cfgFile);
                                if (c.Contains("LoadConfigUseMonitorID=0"))
                                {
                                    c = c.Replace("LoadConfigUseMonitorID=0", "LoadConfigUseMonitorID=1");
                                    File.WriteAllText(cfgFile, c, Encoding.UTF8);
                                }
                            }
                        }
                        catch { }
                    }

                    return targetExe;
                }
                catch
                {
                    return null;
                }
            });
        }

        public static async Task EnsureDefaultProfilesAsync()
        {
            await Task.Run(() =>
            {
                try
                {
                    string dir = GetProfilesDirectory();
                    string gamingFile = Path.Combine(dir, "Modo Gaming — Solo Xiaomi.cfg");
                    string setupFile = Path.Combine(dir, "Setup — Tres Pantallas.cfg");

                    // Copy from existing documents directory if present
                    string docProfiles = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments), "MultiMonitorTool", "Profiles");
                    if (Directory.Exists(docProfiles))
                    {
                        string docGaming = Path.Combine(docProfiles, "Gaming-Xiaomi.cfg");
                        if (File.Exists(docGaming) && !File.Exists(gamingFile))
                        {
                            File.Copy(docGaming, gamingFile, true);
                        }

                        string docSetup = Path.Combine(docProfiles, "Setup-Completo.cfg");
                        if (File.Exists(docSetup) && !File.Exists(setupFile))
                        {
                            File.Copy(docSetup, setupFile, true);
                        }
                    }

                    // If still missing, create default configurations with full hardware IDs
                    if (!File.Exists(gamingFile))
                    {
                        string content = "[Monitor0]\r\nName=\\\\.\\DISPLAY1\r\nMonitorID=MONITOR\\XMI2001\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0005\r\nBitsPerPixel=32\r\nWidth=3440\r\nHeight=1440\r\nDisplayFrequency=180\r\nPositionX=0\r\nPositionY=0\r\n"
                                       + "[Monitor1]\r\nName=\\\\.\\DISPLAY2\r\nMonitorID=MONITOR\\CMN1521\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0003\r\nBitsPerPixel=0\r\nWidth=0\r\nHeight=0\r\nDisplayFrequency=0\r\nPositionX=0\r\nPositionY=0\r\n"
                                       + "[Monitor2]\r\nName=\\\\.\\DISPLAY3\r\nMonitorID=MONITOR\\MSI40B5\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0002\r\nSerialNumber=PB5H984500719 \r\nBitsPerPixel=0\r\nWidth=0\r\nHeight=0\r\nDisplayFrequency=0\r\nPositionX=0\r\nPositionY=0\r\n";
                        File.WriteAllText(gamingFile, content, Encoding.UTF8);
                    }

                    if (!File.Exists(setupFile))
                    {
                        string content = "[Monitor0]\r\nName=\\\\.\\DISPLAY1\r\nMonitorID=MONITOR\\XMI2001\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0005\r\nBitsPerPixel=32\r\nWidth=3440\r\nHeight=1440\r\nDisplayFlags=0\r\nDisplayFrequency=180\r\nDisplayOrientation=0\r\nPositionX=0\r\nPositionY=0\r\n"
                                       + "[Monitor1]\r\nName=\\\\.\\DISPLAY2\r\nMonitorID=MONITOR\\CMN1521\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0003\r\nBitsPerPixel=32\r\nWidth=1920\r\nHeight=1080\r\nDisplayFlags=0\r\nDisplayFrequency=144\r\nDisplayOrientation=0\r\nPositionX=-1920\r\nPositionY=0\r\n"
                                       + "[Monitor2]\r\nName=\\\\.\\DISPLAY3\r\nMonitorID=MONITOR\\MSI40B5\\{4d36e96e-e325-11ce-bfc1-08002be10318}\\0002\r\nSerialNumber=PB5H984500719 \r\nBitsPerPixel=32\r\nWidth=1080\r\nHeight=1920\r\nDisplayFlags=0\r\nDisplayFrequency=60\r\nDisplayOrientation=3\r\nPositionX=3440\r\nPositionY=0\r\n";
                        File.WriteAllText(setupFile, content, Encoding.UTF8);
                    }
                }
                catch
                {
                    // Ignore errors during default generation
                }
            });
        }

        [DllImport("user32.dll", CharSet = CharSet.Ansi)]
        private static extern bool EnumDisplayDevices(string lpDevice, uint iDevNum, ref DISPLAY_DEVICE lpDisplayDevice, uint dwFlags);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Ansi)]
        private struct DISPLAY_DEVICE
        {
            [MarshalAs(UnmanagedType.U4)]
            public int cb;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
            public string DeviceName;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceString;
            [MarshalAs(UnmanagedType.U4)]
            public int StateFlags;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceID;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
            public string DeviceKey;
        }

        public static Dictionary<string, string> GetPhysicalMonitorsByDevice()
        {
            var map = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);
                for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
                {
                    DISPLAY_DEVICE m = new DISPLAY_DEVICE();
                    m.cb = Marshal.SizeOf(m);
                    for (uint mid = 0; EnumDisplayDevices(d.DeviceName, mid, ref m, 0); mid++)
                    {
                        if (!string.IsNullOrEmpty(m.DeviceID))
                        {
                            if (!map.ContainsKey(d.DeviceName))
                            {
                                map[d.DeviceName] = m.DeviceID;
                            }
                        }
                    }
                }
            }
            catch { }
            return map;
        }

        public static HashSet<string> GetPhysicalConnectedShortIds()
        {
            var set = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            try
            {
                DISPLAY_DEVICE d = new DISPLAY_DEVICE();
                d.cb = Marshal.SizeOf(d);
                for (uint id = 0; EnumDisplayDevices(null, id, ref d, 0); id++)
                {
                    DISPLAY_DEVICE m = new DISPLAY_DEVICE();
                    m.cb = Marshal.SizeOf(m);
                    for (uint mid = 0; EnumDisplayDevices(d.DeviceName, mid, ref m, 0); mid++)
                    {
                        if (!string.IsNullOrEmpty(m.DeviceID))
                        {
                            var segs = m.DeviceID.Split('\\');
                            if (segs.Length >= 2 && !string.IsNullOrEmpty(segs[1]))
                            {
                                set.Add(segs[1].ToUpperInvariant());
                            }
                        }
                    }
                }
            }
            catch { }
            return set;
        }

        public static async Task<List<MonitorItem>> GetConnectedMonitorsAsync()
        {
            string toolExe = await EnsureToolAvailableAsync();
            return await Task.Run(() =>
            {
                var list = new List<MonitorItem>();

                bool hasXiaomi = false;
                bool hasMsi = false;
                bool hasLaptop = false;

                if (!string.IsNullOrEmpty(toolExe) && File.Exists(toolExe))
                {
                    try
                    {
                        string tempCsv = Path.Combine(Path.GetTempPath(), "cleandesk_monitors_" + Guid.NewGuid().ToString("N") + ".csv");
                        var psi = new ProcessStartInfo
                        {
                            FileName = toolExe,
                            Arguments = string.Format("/scomma \"{0}\"", tempCsv),
                            WorkingDirectory = Path.GetDirectoryName(toolExe),
                            CreateNoWindow = true,
                            UseShellExecute = false,
                            WindowStyle = ProcessWindowStyle.Hidden
                        };
                        using (var p = Process.Start(psi))
                        {
                            p.WaitForExit(3000);
                        }

                        if (File.Exists(tempCsv))
                        {
                            var lines = File.ReadAllLines(tempCsv);
                            File.Delete(tempCsv);

                            for (int i = 1; i < lines.Length; i++)
                            {
                                string line = lines[i];
                                if (string.IsNullOrWhiteSpace(line)) continue;

                                var parts = ParseCsvLine(line);
                                if (parts.Count < 21) continue;

                                string res = parts[0];
                                string activeStr = parts[3];
                                string primStr = parts[5];
                                string freqStr = parts[7];
                                string orientStr = parts[8];
                                string devName = parts[12];
                                string shortId = parts[17].Trim();
                                string monName = parts[20];

                                if (res.Equals("0 X 0", StringComparison.OrdinalIgnoreCase) || string.IsNullOrEmpty(res))
                                    continue;

                                bool isActive = activeStr.Equals("Yes", StringComparison.OrdinalIgnoreCase);
                                bool isPrimary = primStr.Equals("Yes", StringComparison.OrdinalIgnoreCase);

                                int w = 0, h = 0, freq = 60;
                                if (res.Contains("X"))
                                {
                                    var dim = res.Split('X');
                                    int.TryParse(dim[0].Trim(), out w);
                                    int.TryParse(dim[1].Trim(), out h);
                                }
                                int.TryParse(freqStr.Trim(), out freq);

                                if (isActive)
                                {
                                    if (shortId.StartsWith("XMI", StringComparison.OrdinalIgnoreCase) || w == 3440)
                                    {
                                        if (!hasXiaomi)
                                        {
                                            hasXiaomi = true;
                                            list.Add(new MonitorItem
                                            {
                                                Name = devName,
                                                MonitorId = parts[16],
                                                ShortId = "XMI2001",
                                                FriendlyName = "Xiaomi 34\" Ultrawide",
                                                Width = w > 0 ? w : 3440,
                                                Height = h > 0 ? h : 1440,
                                                Frequency = freq > 0 ? freq : 180,
                                                IsPrimary = isPrimary,
                                                IsActive = true,
                                                IsVertical = false
                                            });
                                        }
                                    }
                                    else if (shortId.StartsWith("MSI", StringComparison.OrdinalIgnoreCase) || (h == 1920 && w == 1080) || orientStr.Contains("270") || orientStr.Contains("90"))
                                    {
                                        if (!hasMsi)
                                        {
                                            hasMsi = true;
                                            list.Add(new MonitorItem
                                            {
                                                Name = devName,
                                                MonitorId = parts[16],
                                                ShortId = "MSI40B5",
                                                FriendlyName = "MSI 24\" Vertical",
                                                Width = 1080,
                                                Height = 1920,
                                                Frequency = freq > 0 ? freq : 60,
                                                IsPrimary = isPrimary,
                                                IsActive = true,
                                                IsVertical = true
                                            });
                                        }
                                    }
                                    else if (shortId.StartsWith("CMN", StringComparison.OrdinalIgnoreCase) || (w == 1920 && h == 1080))
                                    {
                                        if (!hasLaptop)
                                        {
                                            hasLaptop = true;
                                            list.Add(new MonitorItem
                                            {
                                                Name = devName,
                                                MonitorId = parts[16],
                                                ShortId = "CMN1521",
                                                FriendlyName = "Laptop Display",
                                                Width = 1920,
                                                Height = 1080,
                                                Frequency = freq > 0 ? freq : 144,
                                                IsPrimary = isPrimary,
                                                IsActive = true,
                                                IsVertical = false
                                            });
                                        }
                                    }
                                }
                            }
                        }
                    }
                    catch { }
                }

                // Native WinForms Screen Fallback if needed
                if (list.Count == 0)
                {
                    foreach (var s in Screen.AllScreens)
                    {
                        if (s.Bounds.Width == 3440)
                        {
                            if (!hasXiaomi)
                            {
                                hasXiaomi = true;
                                list.Add(new MonitorItem { Name = s.DeviceName, ShortId = "XMI2001", FriendlyName = "Xiaomi 34\" Ultrawide", Width = 3440, Height = 1440, Frequency = 180, IsPrimary = s.Primary, IsActive = true, IsVertical = false });
                            }
                        }
                        else if (s.Bounds.Height > s.Bounds.Width)
                        {
                            if (!hasMsi)
                            {
                                hasMsi = true;
                                list.Add(new MonitorItem { Name = s.DeviceName, ShortId = "MSI40B5", FriendlyName = "MSI 24\" Vertical", Width = 1080, Height = 1920, Frequency = 60, IsPrimary = s.Primary, IsActive = true, IsVertical = true });
                            }
                        }
                        else
                        {
                            if (!hasLaptop)
                            {
                                hasLaptop = true;
                                list.Add(new MonitorItem { Name = s.DeviceName, ShortId = "CMN1521", FriendlyName = "Laptop Display", Width = 1920, Height = 1080, Frequency = 144, IsPrimary = s.Primary, IsActive = true, IsVertical = false });
                            }
                        }
                    }
                }

                // Ensure exactly the 3 physical monitors are present in the list
                if (!hasLaptop)
                {
                    list.Add(new MonitorItem { Name = "\\\\.\\DISPLAY_LAPTOP", ShortId = "CMN1521", FriendlyName = "Laptop Display", Width = 1920, Height = 1080, Frequency = 144, IsPrimary = false, IsActive = false, IsVertical = false });
                }
                if (!hasXiaomi)
                {
                    list.Add(new MonitorItem { Name = "\\\\.\\DISPLAY_XIAOMI", ShortId = "XMI2001", FriendlyName = "Xiaomi 34\" Ultrawide", Width = 3440, Height = 1440, Frequency = 180, IsPrimary = false, IsActive = false, IsVertical = false });
                }
                if (!hasMsi)
                {
                    list.Add(new MonitorItem { Name = "\\\\.\\DISPLAY_MSI", ShortId = "MSI40B5", FriendlyName = "MSI 24\" Vertical", Width = 1080, Height = 1920, Frequency = 60, IsPrimary = false, IsActive = false, IsVertical = true });
                }

                return list.OrderBy(m => m.ShortId == "CMN1521" ? 0 : (m.ShortId == "XMI2001" ? 1 : 2)).ToList();
            });
        }

        public static async Task<List<DisplayProfileItem>> GetProfilesAsync()
        {
            var monitors = await GetConnectedMonitorsAsync();
            return await GetProfilesAsync(monitors);
        }

        public static async Task<List<DisplayProfileItem>> GetProfilesAsync(List<MonitorItem> connectedMonitors)
        {
            await EnsureDefaultProfilesAsync();

            return await Task.Run(() =>
            {
                var profiles = new List<DisplayProfileItem>();
                string dir = GetProfilesDirectory();

                if (!Directory.Exists(dir)) return profiles;

                var files = Directory.GetFiles(dir, "*.cfg").OrderBy(f => f).ToList();
                var physicalShortIds = GetPhysicalConnectedShortIds();
                foreach (var m in connectedMonitors)
                {
                    if (!string.IsNullOrEmpty(m.ShortId))
                    {
                        physicalShortIds.Add(m.ShortId.ToUpperInvariant());
                    }
                }

                foreach (var file in files)
                {
                    string fileName = Path.GetFileNameWithoutExtension(file);
                    var item = new DisplayProfileItem
                    {
                        Name = fileName,
                        FilePath = file
                    };

                    // Detect icon glyph
                    if (fileName.IndexOf("Gaming", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        item.IconGlyph = "\uE7FC"; // Game controller
                    }
                    else if (fileName.IndexOf("Setup", StringComparison.OrdinalIgnoreCase) >= 0 || fileName.IndexOf("Tres", StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        item.IconGlyph = "\uE7F4"; // Multi-display
                    }
                    else
                    {
                        item.IconGlyph = "\uE7F4";
                    }

                    // Parse profile configuration
                    try
                    {
                        var lines = File.ReadAllLines(file);
                        int activeCount = 0;
                        string primaryRes = "";
                        var reqIds = new List<string>();

                        bool inMonitor = false;
                        bool currActive = false;
                        string currShortId = "";

                        Action finishCurrentMonitor = () =>
                        {
                            if (inMonitor && currActive && !string.IsNullOrEmpty(currShortId))
                            {
                                activeCount++;
                                reqIds.Add(currShortId);
                            }
                            inMonitor = false;
                            currActive = false;
                            currShortId = "";
                        };

                        foreach (var rawLine in lines)
                        {
                            string l = rawLine.Trim();
                            if (l.StartsWith("[Monitor", StringComparison.OrdinalIgnoreCase))
                            {
                                finishCurrentMonitor();
                                inMonitor = true;
                                currActive = true;
                                currShortId = "";
                            }
                            else if (inMonitor)
                            {
                                if (l.StartsWith("BitsPerPixel=0", StringComparison.OrdinalIgnoreCase) || l.StartsWith("Width=0", StringComparison.OrdinalIgnoreCase))
                                {
                                    currActive = false;
                                }
                                else if (l.StartsWith("MonitorID=", StringComparison.OrdinalIgnoreCase))
                                {
                                    string fullId = l.Substring(10).Trim();
                                    if (fullId.Contains("\\"))
                                    {
                                        var segs = fullId.Split('\\');
                                        if (segs.Length >= 2) currShortId = segs[1];
                                    }
                                }
                                else if (l.StartsWith("Width=", StringComparison.OrdinalIgnoreCase) && currActive)
                                {
                                    string w = l.Substring(6).Trim();
                                    primaryRes = w;
                                }
                            }
                            else if (l.StartsWith("["))
                            {
                                finishCurrentMonitor();
                            }
                        }

                        finishCurrentMonitor();

                        item.RequiredMonitorShortIds = reqIds;

                        if (activeCount == 1)
                        {
                            item.Summary = Strings.IsSpanish ? "1 Pantalla activa (Modo Concentrado / Juegos)" : "1 Active Display (Focus / Gaming Mode)";
                        }
                        else
                        {
                            item.Summary = Strings.IsSpanish ? string.Format("{0} Pantallas activas (Escritorio Extendido)", activeCount) : string.Format("{0} Active Displays (Extended Desktop)", activeCount);
                        }

                        // Validate if all required monitors are connected physically
                        bool allPresent = true;
                        var missing = new List<string>();
                        foreach (var req in reqIds)
                        {
                            if (!physicalShortIds.Contains(req.ToUpperInvariant()))
                            {
                                allPresent = false;
                                missing.Add(req);
                            }
                        }

                        item.CanApply = allPresent;
                        if (!allPresent)
                        {
                            item.WarningMessage = Strings.IsSpanish 
                                ? string.Format("Monitor(es) no conectado(s): {0}", string.Join(", ", missing))
                                : string.Format("Disconnected monitor(s): {0}", string.Join(", ", missing));
                        }
                        else
                        {
                            item.WarningMessage = null;
                        }

                        // Determine if current screen setup matches this profile
                        int currentlyActiveCount = connectedMonitors.Count(m => m.IsActive);
                        if (allPresent && activeCount == currentlyActiveCount && activeCount > 0)
                        {
                            bool allReqActive = reqIds.All(req => connectedMonitors.Any(m => m.IsActive && !string.IsNullOrEmpty(m.ShortId) && m.ShortId.Equals(req, StringComparison.OrdinalIgnoreCase)));
                            if (allReqActive)
                            {
                                item.IsActive = true;
                            }
                        }
                    }
                    catch
                    {
                        item.Summary = Strings.IsSpanish ? "Perfil de configuración de pantalla" : "Display configuration profile";
                    }

                    profiles.Add(item);
                }

                return profiles;
            });
        }

        public static async Task<bool> ApplyProfileAsync(DisplayProfileItem profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.FilePath) || !File.Exists(profile.FilePath)) return false;

            string toolExe = await EnsureToolAvailableAsync();
            if (string.IsNullOrEmpty(toolExe) || !File.Exists(toolExe)) return false;

            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = toolExe,
                        Arguments = string.Format("/LoadConfig \"{0}\"", profile.FilePath),
                        WorkingDirectory = Path.GetDirectoryName(toolExe),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit(6000);
                        return (p.ExitCode == 0);
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<bool> SaveCurrentProfileAsync(string profileName)
        {
            if (string.IsNullOrWhiteSpace(profileName)) return false;

            string toolExe = await EnsureToolAvailableAsync();
            if (string.IsNullOrEmpty(toolExe) || !File.Exists(toolExe)) return false;

            string cleanName = string.Join("_", profileName.Split(Path.GetInvalidFileNameChars()));
            string filePath = Path.Combine(GetProfilesDirectory(), cleanName + ".cfg");

            return await Task.Run(() =>
            {
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = toolExe,
                        Arguments = string.Format("/SaveConfig \"{0}\"", filePath),
                        WorkingDirectory = Path.GetDirectoryName(toolExe),
                        CreateNoWindow = true,
                        UseShellExecute = false,
                        WindowStyle = ProcessWindowStyle.Hidden
                    };
                    using (var p = Process.Start(psi))
                    {
                        p.WaitForExit(5000);
                        return File.Exists(filePath);
                    }
                }
                catch
                {
                    return false;
                }
            });
        }

        public static async Task<bool> DeleteProfileAsync(DisplayProfileItem profile)
        {
            if (profile == null || string.IsNullOrEmpty(profile.FilePath)) return false;

            return await Task.Run(() =>
            {
                try
                {
                    if (File.Exists(profile.FilePath))
                    {
                        File.Delete(profile.FilePath);
                        return true;
                    }
                    return false;
                }
                catch
                {
                    return false;
                }
            });
        }

        private static List<string> ParseCsvLine(string line)
        {
            var result = new List<string>();
            bool inQuotes = false;
            var sb = new StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];
                if (c == '\"')
                {
                    inQuotes = !inQuotes;
                }
                else if (c == ',' && !inQuotes)
                {
                    result.Add(sb.ToString().Trim());
                    sb.Clear();
                }
                else
                {
                    sb.Append(c);
                }
            }
            result.Add(sb.ToString().Trim());
            return result;
        }
    }
}
