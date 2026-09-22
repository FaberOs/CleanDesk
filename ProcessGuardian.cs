using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;

namespace CleanDesk
{
    public class ProcessGroupItem : INotifyPropertyChanged
    {
        public string ProcessKey { get; set; }
        public string DisplayName { get; set; }
        public string ExecutableName { get; set; }
        public string Glyph { get; set; }

        private int _count;
        public int Count
        {
            get { return _count; }
            set 
            { 
                if (_count != value) 
                { 
                    _count = value; 
                    OnPropertyChanged("Count"); 
                    OnPropertyChanged("FormattedCount"); 
                    OnPropertyChanged("IsActive"); 
                    OnPropertyChanged("KillVisibility");
                } 
            }
        }

        public int InstanceCount
        {
            get { return _count; }
        }

        private long _totalMemoryBytes;
        public long TotalMemoryBytes
        {
            get { return _totalMemoryBytes; }
            set { if (_totalMemoryBytes != value) { _totalMemoryBytes = value; OnPropertyChanged("TotalMemoryBytes"); OnPropertyChanged("FormattedMemory"); } }
        }

        private bool _isRunaway;
        public bool IsRunaway
        {
            get { return _isRunaway; }
            set 
            { 
                if (_isRunaway != value) 
                { 
                    _isRunaway = value; 
                    OnPropertyChanged("IsRunaway"); 
                    OnPropertyChanged("AlertBadgeVisibility");
                } 
            }
        }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get { return _isSelected; }
            set { if (_isSelected != value) { _isSelected = value; OnPropertyChanged("IsSelected"); } }
        }

        public bool IsActive
        {
            get { return Count > 0; }
        }

        public Visibility KillVisibility
        {
            get { return IsActive ? Visibility.Visible : Visibility.Collapsed; }
        }

        public Visibility AlertBadgeVisibility
        {
            get { return IsRunaway ? Visibility.Visible : Visibility.Collapsed; }
        }

        public string AlertBadgeText
        {
            get { return Strings.IsSpanish ? "BUCLE" : "RUNAWAY"; }
        }

        public string FormattedCount
        {
            get { return Strings.ProcessInstances(Count); }
        }

        public string FormattedMemory
        {
            get { return CleanerEngine.FormatBytes(TotalMemoryBytes); }
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null) PropertyChanged(this, new PropertyChangedEventArgs(name));
        }
    }

    public class ProcessGuardianReport
    {
        public int TotalInstances { get; set; }
        public long TotalMemoryBytes { get; set; }
        public bool HasRunawayAlert { get; set; }
        public List<ProcessGroupItem> Groups { get; set; }

        public string FormattedTotalMemory
        {
            get { return CleanerEngine.FormatBytes(TotalMemoryBytes); }
        }

        public ProcessGuardianReport()
        {
            Groups = new List<ProcessGroupItem>();
        }
    }

    public static class ProcessGuardian
    {
        // Grupos de procesos típicos que agentes de IA o loops dev sobrecargan
        private static readonly Dictionary<string, Tuple<string, string, string>> KnownTargets = 
            new Dictionary<string, Tuple<string, string, string>>(StringComparer.OrdinalIgnoreCase)
        {
            // key -> Item1: DisplayName, Item2: ExeName, Item3: Glyph
            { "node",        Tuple.Create("Node.js Workers",      "node.exe",        "\uE74C") },
            { "pwsh",        Tuple.Create("PowerShell Core",      "pwsh.exe",        "\uE756") },
            { "powershell",  Tuple.Create("Windows PowerShell",   "powershell.exe",  "\uE756") },
            { "cmd",         Tuple.Create("Command Prompt (CMD)", "cmd.exe",         "\uE756") },
            { "python",      Tuple.Create("Python Workers",       "python.exe",      "\uE7B8") },
            { "pythonw",     Tuple.Create("Python GUI / Server",  "pythonw.exe",     "\uE7B8") },
            { "git",         Tuple.Create("Git Subprocesses",     "git.exe",         "\uE943") },
            { "conhost",     Tuple.Create("Console Windows Host", "conhost.exe",     "\uE756") },
            { "ruby",        Tuple.Create("Ruby Processes",       "ruby.exe",        "\uE74C") }
        };

        public static Task<ProcessGuardianReport> ScanProcessesAsync()
        {
            return Task.Factory.StartNew<ProcessGuardianReport>(() =>
            {
                var report = new ProcessGuardianReport();
                var groupsDict = new Dictionary<string, ProcessGroupItem>(StringComparer.OrdinalIgnoreCase);

                // Inicializar categorías conocidas
                foreach (var kvp in KnownTargets)
                {
                    groupsDict[kvp.Key] = new ProcessGroupItem
                    {
                        ProcessKey = kvp.Key,
                        DisplayName = kvp.Value.Item1,
                        ExecutableName = kvp.Value.Item2,
                        Glyph = kvp.Value.Item3,
                        Count = 0,
                        TotalMemoryBytes = 0,
                        IsRunaway = false,
                        IsSelected = true
                    };
                }

                try
                {
                    var processes = Process.GetProcesses();
                    foreach (var proc in processes)
                    {
                        try
                        {
                            string pName = proc.ProcessName.ToLowerInvariant();
                            if (groupsDict.ContainsKey(pName))
                            {
                                var item = groupsDict[pName];
                                item.Count++;
                                item.TotalMemoryBytes += proc.WorkingSet64;
                            }
                        }
                        catch { }
                    }
                }
                catch { }

                // Evaluar alertas de bucle o sobrecarga (runaway)
                foreach (var item in groupsDict.Values)
                {
                    // Alerta si hay más de 4 instancias (típico desmadre de agentes de IA) o más de 350 MB de RAM
                    if (item.Count >= 4 || item.TotalMemoryBytes >= 350L * 1024 * 1024)
                    {
                        item.IsRunaway = true;
                        report.HasRunawayAlert = true;
                    }
                    else
                    {
                        item.IsRunaway = false;
                    }

                    report.TotalInstances += item.Count;
                    report.TotalMemoryBytes += item.TotalMemoryBytes;
                }

                // Ordenar: primero los activos ordenados por RAM descendente, luego inactivos
                report.Groups = groupsDict.Values
                    .OrderByDescending(g => g.IsActive)
                    .ThenByDescending(g => g.TotalMemoryBytes)
                    .ToList();

                return report;
            });
        }

        public static Task<bool> KillGroupAsync(string executableName)
        {
            return Task.Factory.StartNew<bool>(() =>
            {
                try
                {
                    // Usar taskkill /F /IM <name> /T para erradicar procesos y árbol de hijos (igual que Matar_Node.bat)
                    var psi = new ProcessStartInfo
                    {
                        FileName = "taskkill.exe",
                        Arguments = string.Format("/F /IM {0} /T", executableName),
                        CreateNoWindow = true,
                        UseShellExecute = false
                    };
                    using (var proc = Process.Start(psi))
                    {
                        if (proc != null)
                        {
                            proc.WaitForExit(3000);
                        }
                    }

                    // Forzar limpieza con Stop-Process por si quedó algún remanente huérfano
                    string shortName = executableName.Replace(".exe", "");
                    var procList = Process.GetProcessesByName(shortName);
                    foreach (var p in procList)
                    {
                        try { p.Kill(); } catch { }
                    }

                    CleanerEngine.TrimMemory();
                    return true;
                }
                catch
                {
                    return false;
                }
            });
        }

        public static Task<int> KillAllRunawaysAsync(IEnumerable<ProcessGroupItem> groups)
        {
            return Task.Factory.StartNew<int>(() =>
            {
                int killedGroups = 0;
                var activeTargets = groups.Where(g => g.IsActive && g.IsSelected).ToList();

                foreach (var g in activeTargets)
                {
                    try
                    {
                        var psi = new ProcessStartInfo
                        {
                            FileName = "taskkill.exe",
                            Arguments = string.Format("/F /IM {0} /T", g.ExecutableName),
                            CreateNoWindow = true,
                            UseShellExecute = false
                        };
                        using (var proc = Process.Start(psi))
                        {
                            if (proc != null)
                            {
                                proc.WaitForExit(2500);
                                killedGroups++;
                            }
                        }
                    }
                    catch { }
                }

                CleanerEngine.TrimMemory();
                return killedGroups;
            });
        }
    }
}
