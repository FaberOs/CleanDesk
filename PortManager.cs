using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media;

namespace CleanDesk
{
    public class PortItem : INotifyPropertyChanged
    {
        private int _portNumber;
        public int PortNumber
        {
            get { return _portNumber; }
            set { if (_portNumber != value) { _portNumber = value; OnPropertyChanged("PortNumber"); OnPropertyChanged("FormattedPort"); } }
        }

        private int _processId;
        public int ProcessId
        {
            get { return _processId; }
            set { if (_processId != value) { _processId = value; OnPropertyChanged("ProcessId"); OnPropertyChanged("FormattedPid"); } }
        }

        private string _processName;
        public string ProcessName
        {
            get { return _processName; }
            set 
            { 
                if (_processName != value) 
                { 
                    _processName = value; 
                    OnPropertyChanged("ProcessName"); 
                    OnPropertyChanged("StatusText"); 
                    OnPropertyChanged("DisplayName");
                    OnPropertyChanged("DisplayDetails");
                } 
            }
        }

        private string _description;
        public string Description
        {
            get { return _description; }
            set 
            { 
                if (_description != value) 
                { 
                    _description = value; 
                    OnPropertyChanged("Description"); 
                    OnPropertyChanged("DisplayDetails");
                } 
            }
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
                    OnPropertyChanged("StatusText"); 
                    OnPropertyChanged("StatusLabel");
                    OnPropertyChanged("DisplayName");
                    OnPropertyChanged("DisplayDetails");
                    OnPropertyChanged("KillVisibility");
                    OnPropertyChanged("StatusBgBrush");
                    OnPropertyChanged("StatusFgBrush");
                } 
            }
        }

        private bool _isSelected;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        }

        public string FormattedPort
        {
            get { return string.Format(":{0}", PortNumber); }
        }

        public string FormattedPid
        {
            get { return ProcessId > 0 ? string.Format("PID {0}", ProcessId) : ""; }
        }

        public string DisplayName
        {
            get 
            { 
                if (!IsActive) return Strings.PortFree;
                return string.IsNullOrEmpty(ProcessName) ? string.Format("PID {0}", ProcessId) : ProcessName;
            }
        }

        public string DisplayDetails
        {
            get
            {
                if (!IsActive)
                {
                    return string.IsNullOrEmpty(Description) ? "" : Description;
                }
                if (!string.IsNullOrEmpty(Description) && ProcessId > 0)
                {
                    return string.Format("{0} • PID {1}", Description, ProcessId);
                }
                if (ProcessId > 0) return string.Format("PID {0}", ProcessId);
                return Description ?? "";
            }
        }

        public string StatusLabel
        {
            get { return IsActive ? Strings.PortBusy : Strings.PortFree; }
        }

        public string StatusText
        {
            get
            {
                if (!IsActive) return Strings.PortFree;
                return string.IsNullOrEmpty(ProcessName) ? string.Format("PID {0}", ProcessId) : string.Format("{0} ({1})", ProcessName, ProcessId);
            }
        }

        public Visibility KillVisibility
        {
            get { return IsActive ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Brush StatusBgBrush
        {
            get 
            { 
                return IsActive 
                    ? (Brush)new SolidColorBrush(Color.FromArgb(45, 239, 68, 68)) 
                    : (Brush)new SolidColorBrush(Color.FromArgb(35, 34, 197, 94)); 
            }
        }

        public Brush StatusFgBrush
        {
            get 
            { 
                return IsActive 
                    ? (Brush)new SolidColorBrush(Color.FromRgb(239, 68, 68)) 
                    : (Brush)new SolidColorBrush(Color.FromRgb(34, 197, 94)); 
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public static class PortManager
    {
        #region Win32 API GetExtendedTcpTable

        private const int AF_INET = 2; // IPv4
        private const int TCP_TABLE_OWNER_PID_ALL = 5;
        private const uint MIB_TCP_STATE_LISTEN = 2;

        [StructLayout(LayoutKind.Sequential)]
        private struct MIB_TCPROW_OWNER_PID
        {
            public uint state;
            public uint localAddr;
            public byte localPort1;
            public byte localPort2;
            public byte localPort3;
            public byte localPort4;
            public uint remoteAddr;
            public byte remotePort1;
            public byte remotePort2;
            public byte remotePort3;
            public byte remotePort4;
            public uint owningPid;
        }

        [DllImport("iphlpapi.dll", SetLastError = true)]
        private static extern uint GetExtendedTcpTable(
            IntPtr pTcpTable,
            ref int pdwSize,
            bool bOrder,
            int ulAf,
            int tableClass,
            uint reserved);

        #endregion

        // Puertos comunes para desarrolladores (incluyendo 3000-3006 de killerport.bat)
        public static readonly int[] DefaultDevPorts = new int[]
        {
            3000, 3001, 3002, 3003, 3004, 3005, 3006, // React, Next.js, Node dev
            4200,                                      // Angular
            5000, 5001,                                // Flask, ASP.NET Core
            5173, 5174,                                // Vite, Vue, Svelte
            8000, 8080, 8081,                          // Django, Spring, PHP
            9000                                       // FastAPI, PHP-FPM, SonarQube
        };

        public static string GetPortDescription(int port)
        {
            switch (port)
            {
                case 3000: return "React / Next.js";
                case 3001: return "Dev Server 2";
                case 3002: return "Dev Server 3";
                case 3003: return "Dev Server 4";
                case 3004: return "Dev Server 5";
                case 3005: return "Dev Server 6";
                case 3006: return "Dev Server 7";
                case 4200: return "Angular Dev";
                case 5000: return "Flask / ASP.NET";
                case 5001: return "ASP.NET HTTPS";
                case 5173: return "Vite / Vue / Svelte";
                case 5174: return "Vite Alt";
                case 8000: return "Django / Python";
                case 8080: return "Spring / Tomcat / Web";
                case 8081: return "Web Alt / Proxy";
                case 9000: return "FastAPI / PHP-FPM";
                default:   return "Custom Dev Port";
            }
        }

        public static Dictionary<int, int> GetListeningPorts()
        {
            var result = new Dictionary<int, int>();
            int bufferSize = 0;

            uint ret = GetExtendedTcpTable(IntPtr.Zero, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
            IntPtr tcpTablePtr = Marshal.AllocHGlobal(bufferSize);

            try
            {
                ret = GetExtendedTcpTable(tcpTablePtr, ref bufferSize, true, AF_INET, TCP_TABLE_OWNER_PID_ALL, 0);
                if (ret == 0)
                {
                    int rowCount = Marshal.ReadInt32(tcpTablePtr);
                    IntPtr rowPtr = (IntPtr)((long)tcpTablePtr + 4);
                    int rowSize = Marshal.SizeOf(typeof(MIB_TCPROW_OWNER_PID));

                    for (int i = 0; i < rowCount; i++)
                    {
                        var row = (MIB_TCPROW_OWNER_PID)Marshal.PtrToStructure(rowPtr, typeof(MIB_TCPROW_OWNER_PID));
                        if (row.state == MIB_TCP_STATE_LISTEN)
                        {
                            ushort port = (ushort)((row.localPort1 << 8) + row.localPort2);
                            int pid = (int)row.owningPid;
                            if (!result.ContainsKey((int)port))
                            {
                                result[(int)port] = pid;
                            }
                        }
                        rowPtr = (IntPtr)((long)rowPtr + rowSize);
                    }
                }
            }
            catch { }
            finally
            {
                Marshal.FreeHGlobal(tcpTablePtr);
            }

            // Fallback con netstat si no se detectó nada
            if (result.Count == 0)
            {
                try
                {
                    var netstatPorts = QueryNetstatListeningPorts();
                    foreach (var kvp in netstatPorts)
                    {
                        result[kvp.Key] = kvp.Value;
                    }
                }
                catch { }
            }

            return result;
        }

        private static Dictionary<int, int> QueryNetstatListeningPorts()
        {
            var dict = new Dictionary<int, int>();
            var psi = new ProcessStartInfo
            {
                FileName = "netstat.exe",
                Arguments = "-ano -p tcp",
                CreateNoWindow = true,
                UseShellExecute = false,
                RedirectStandardOutput = true
            };

            using (var proc = Process.Start(psi))
            {
                if (proc != null)
                {
                    string output = proc.StandardOutput.ReadToEnd();
                    proc.WaitForExit(2000);
                    using (var reader = new StringReader(output))
                    {
                        string line;
                        while ((line = reader.ReadLine()) != null)
                        {
                            line = line.Trim();
                            if (line.StartsWith("TCP", StringComparison.OrdinalIgnoreCase) && line.IndexOf("LISTENING", StringComparison.OrdinalIgnoreCase) >= 0)
                            {
                                string[] parts = line.Split(new char[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                if (parts.Length >= 5)
                                {
                                    string localAddr = parts[1];
                                    string pidStr = parts[parts.Length - 1];
                                    int colonIdx = localAddr.LastIndexOf(':');
                                    int port, pid;
                                    if (colonIdx >= 0 && int.TryParse(localAddr.Substring(colonIdx + 1), out port) && int.TryParse(pidStr, out pid))
                                    {
                                        if (!dict.ContainsKey(port)) dict[port] = pid;
                                    }
                                }
                            }
                        }
                    }
                }
            }
            return dict;
        }

        public static string ResolveProcessName(int pid)
        {
            if (pid <= 4) return "System";
            try
            {
                var proc = Process.GetProcessById(pid);
                return proc.ProcessName + ".exe";
            }
            catch
            {
                return "Unknown";
            }
        }

        public static Task<List<PortItem>> ScanPortsAsync(IEnumerable<int> customPorts = null)
        {
            return Task.Factory.StartNew<List<PortItem>>(() =>
            {
                var portsToInspect = new HashSet<int>(DefaultDevPorts);
                if (customPorts != null)
                {
                    foreach (var p in customPorts)
                    {
                        if (p > 0 && p <= 65535) portsToInspect.Add(p);
                    }
                }

                var listening = GetListeningPorts();
                var list = new List<PortItem>();

                foreach (var port in portsToInspect.OrderBy(p => p))
                {
                    bool isBusy = listening.ContainsKey(port);
                    int pid = isBusy ? listening[port] : 0;
                    string procName = isBusy ? ResolveProcessName(pid) : "";

                    list.Add(new PortItem
                    {
                        PortNumber = port,
                        ProcessId = pid,
                        ProcessName = procName,
                        Description = GetPortDescription(port),
                        IsActive = isBusy,
                        IsSelected = isBusy // Por defecto preseleccionar ocupados para matar de golpe
                    });
                }

                return list;
            });
        }

        public static Task<bool> KillPortAsync(int port, int pid)
        {
            return Task.Factory.StartNew<bool>(() =>
            {
                if (pid <= 4) return false;
                try
                {
                    var psi = new ProcessStartInfo
                    {
                        FileName = "taskkill.exe",
                        Arguments = string.Format("/F /PID {0} /T", pid),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit(2500);
                            CleanerEngine.TrimMemory();
                            return proc.ExitCode == 0;
                        }
                    }
                }
                catch { }

                try
                {
                    var p = Process.GetProcessById(pid);
                    p.Kill();
                    CleanerEngine.TrimMemory();
                    return true;
                }
                catch { }

                return false;
            });
        }

        public static Task<int> KillSelectedPortsAsync(IEnumerable<PortItem> items)
        {
            return Task.Factory.StartNew<int>(() =>
            {
                int killed = 0;
                var busySelected = items.Where(i => i.IsActive && i.IsSelected && i.ProcessId > 4).ToList();

                foreach (var item in busySelected)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "taskkill.exe",
                            Arguments = string.Format("/F /PID {0} /T", item.ProcessId),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (var proc = Process.Start(psi))
                        {
                            if (proc != null)
                            {
                                proc.WaitForExit(2000);
                                killed++;
                            }
                        }
                    }
                    catch { }
                }

                CleanerEngine.TrimMemory();
                return killed;
            });
        }
    }
}
