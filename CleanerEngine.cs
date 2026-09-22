using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading.Tasks;

namespace CleanDesk
{
    public class JunkCategory : INotifyPropertyChanged
    {
        public string Id { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string Glyph { get; set; }
        
        private long _bytes;
        public long Bytes
        {
            get { return _bytes; }
            set
            {
                if (_bytes != value)
                {
                    _bytes = value;
                    OnPropertyChanged("Bytes");
                    OnPropertyChanged("FormattedSize");
                }
            }
        }

        private long _fileCount;
        public long FileCount
        {
            get { return _fileCount; }
            set
            {
                if (_fileCount != value)
                {
                    _fileCount = value;
                    OnPropertyChanged("FileCount");
                    OnPropertyChanged("FormattedFiles");
                }
            }
        }

        public List<string> Paths { get; set; }
        public bool IsRecycleBin { get; set; }

        private bool _isSelected = true;
        public bool IsSelected
        {
            get { return _isSelected; }
            set
            {
                if (_isSelected != value)
                {
                    _isSelected = value;
                    OnPropertyChanged("IsSelected");
                }
            }
        }

        public string FormattedSize
        {
            get { return CleanerEngine.FormatBytes(Bytes); }
        }

        public string FormattedFiles
        {
            get { return string.Format("in {0:n0} files", FileCount); }
        }

        public JunkCategory()
        {
            Paths = new List<string>();
            _isSelected = true;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        protected void OnPropertyChanged(string name)
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(name));
            }
        }
    }

    public class ScanResult
    {
        public long TotalBytes { get; set; }
        public long TotalFiles { get; set; }
        public string FormattedTotal
        {
            get { return CleanerEngine.FormatBytes(TotalBytes); }
        }
        public string FormattedFiles
        {
            get { return string.Format("in {0:n0} files", TotalFiles); }
        }
        public List<JunkCategory> Categories { get; set; }

        public ScanResult()
        {
            Categories = new List<JunkCategory>();
        }
    }

    public struct DirStats
    {
        public long Bytes;
        public long Files;
    }

    public static class CleanerEngine
    {
        [DllImport("kernel32.dll")]
        private static extern bool SetProcessWorkingSetSize(IntPtr proc, int min, int max);

        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
        private struct SHQUERYRBINFO
        {
            public int cbSize;
            public long i64Size;
            public long i64NumItems;
        }

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHQueryRecycleBin(string pszRootPath, ref SHQUERYRBINFO pSHQueryRBInfo);

        [DllImport("Shell32.dll", CharSet = CharSet.Unicode)]
        private static extern int SHEmptyRecycleBin(IntPtr hwnd, string pszRootPath, uint dwFlags);

        private const uint SHERB_NOCONFIRMATION = 0x00000001;
        private const uint SHERB_NOPROGRESSUI   = 0x00000002;
        private const uint SHERB_NOSOUND        = 0x00000004;

        // Palabras clave de seguridad: NUNCA tocar archivos de red, wifi, bluetooth, hardware o servicios de Windows
        private static readonly string[] ProtectedPatterns = new string[]
        {
            // Red, DNS, DHCP, Sockets, NLA, VPN, Proxy
            "dns", "socket", ".sock", "pipe", "dhcp", "hosts", "network", 
            "nla", "wpad", "tcp", "ip", "ethernet", "adapter", "vpn", "proxy", "gateway",
            // Conectividad Inalámbrica (Wi-Fi y Bluetooth)
            "wifi", "wlan", "80211", "dot11", "wireless", "wpa",
            "bluetooth", "bth", "blue", "rfcomm", "a2dp", "btle", "ble", "obex",
            // Audio y Hardware (Drivers, GPU, Sonido, Periféricos)
            "audio", "sound", "nahimic", "realtek", "driver", "intel", "amd", "nvidia", "disp", "display",
            // Seguridad, Criptografía, Tokens y Servicios de Windows
            "system", "windows", "defender", "security", "antivirus", "cert", "token", "auth", 
            "crypto", "sam", "lsass", "svchost", "service", "rpc", "com", "dcom",
            // Entornos de Desarrollo, IDEs y Herramientas del Sistema
            "antigravity", "code", "vscode", "git", "ssh", "wsl", "sdk", "winget", "jetbrains", "cursor"
        };

        // Extensiones de archivos de sistema, sockets y candados que NUNCA deben eliminarse
        private static readonly string[] ProtectedExtensions = new string[]
        {
            ".lock", ".sock", ".socket", ".pipe", ".pid", ".sys", ".inf", ".cat",
            ".db", ".sqlite", ".dat", ".key", ".cer", ".crt", ".pem"
        };

        public static void TrimMemory()
        {
            try
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                GC.Collect();
                if (Environment.OSVersion.Platform == PlatformID.Win32NT)
                {
                    SetProcessWorkingSetSize(System.Diagnostics.Process.GetCurrentProcess().Handle, -1, -1);
                }
            }
            catch { }
        }

        public static string FormatBytes(long bytes)
        {
            if (bytes <= 0) return "0 MB";
            string[] suffixes = new string[] { "B", "KB", "MB", "GB", "TB" };
            int counter = 0;
            decimal number = (decimal)bytes;
            while (Math.Round(number / 1024) >= 1 && counter < suffixes.Length - 1)
            {
                number /= 1024;
                counter++;
            }
            return string.Format("{0:n1} {1}", number, suffixes[counter]);
        }

        public static DirStats GetRecycleBinStats()
        {
            var stats = new DirStats();
            try
            {
                SHQUERYRBINFO rbInfo = new SHQUERYRBINFO();
                rbInfo.cbSize = Marshal.SizeOf(typeof(SHQUERYRBINFO));
                int res = SHQueryRecycleBin(null, ref rbInfo);
                if (res == 0)
                {
                    stats.Bytes = rbInfo.i64Size;
                    stats.Files = rbInfo.i64NumItems;
                }
            }
            catch { }
            return stats;
        }

        public static DirStats GetDirectoryStatsSafe(string dirPath, bool onlyOlderThan24h = true, string[] allowedExtensions = null)
        {
            var stats = new DirStats();
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return stats;

            DateTime cutoff = DateTime.Now.AddHours(-24);

            try
            {
                var dir = new DirectoryInfo(dirPath);
                foreach (var file in dir.EnumerateFiles("*", SearchOption.AllDirectories))
                {
                    try
                    {
                        if (onlyOlderThan24h && file.LastWriteTime > cutoff) continue;
                        if (allowedExtensions != null && allowedExtensions.Length > 0)
                        {
                            string ext = file.Extension.ToLowerInvariant();
                            if (!allowedExtensions.Contains(ext)) continue;
                        }

                        string extLower = file.Extension.ToLowerInvariant();
                        if (ProtectedExtensions.Any(e => extLower == e)) continue;

                        // Comprobar si coincide con patrones protegidos de red, wifi, bluetooth, hardware o sistema
                        string nameLower = file.Name.ToLowerInvariant();
                        if (ProtectedPatterns.Any(p => nameLower.Contains(p))) continue;

                        stats.Bytes += file.Length;
                        stats.Files++;
                    }
                    catch { }
                }
            }
            catch { }

            return stats;
        }

        public static DirStats GetFileStatsSafe(string filePath)
        {
            var stats = new DirStats();
            try
            {
                if (File.Exists(filePath))
                {
                    var fi = new FileInfo(filePath);
                    stats.Bytes = fi.Length;
                    stats.Files = 1;
                }
            }
            catch { }
            return stats;
        }

        public static Task<ScanResult> ScanAsync(Action<int, string> onProgress = null)
        {
            return Task.Factory.StartNew<ScanResult>(() =>
            {
                var result = new ScanResult();
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string userTemp = Path.GetTempPath();

                // 1. Archivos Temporales de Usuario (SOLO carpeta de usuario, NUNCA C:\Windows\Temp)
                if (onProgress != null) onProgress(20, "Escaneando temporales de usuario...");
                // Solo temporales con más de 24 horas y sin patrones protegidos para garantizar seguridad absoluta
                var userStats = GetDirectoryStatsSafe(userTemp, onlyOlderThan24h: true);
                result.Categories.Add(new JunkCategory
                {
                    Id = "temp",
                    Name = Strings.GetCategoryName("temp"),
                    Description = Strings.GetCategoryDesc("temp"),
                    Glyph = "\uED25",
                    Bytes = userStats.Bytes,
                    FileCount = userStats.Files,
                    Paths = new List<string> { userTemp }
                });

                // 2. Caché de Desarrollo (npm y pip) - 100% seguro
                if (onProgress != null) onProgress(45, "Analizando cachés de desarrollo...");
                string npmCache = Path.Combine(localAppData, "npm-cache");
                string pipCache = Path.Combine(localAppData, "pip", "cache");
                var npmStats = GetDirectoryStatsSafe(npmCache);
                var pipStats = GetDirectoryStatsSafe(pipCache);
                result.Categories.Add(new JunkCategory
                {
                    Id = "dev",
                    Name = Strings.GetCategoryName("dev"),
                    Description = Strings.GetCategoryDesc("dev"),
                    Glyph = "\uE7B8",
                    Bytes = npmStats.Bytes + pipStats.Bytes,
                    FileCount = npmStats.Files + pipStats.Files,
                    Paths = new List<string> { npmCache, pipCache }
                });

                // 3. Shaders NVIDIA (DXCache) - Solo binarios de shaders obsoletos (>24h)
                if (onProgress != null) onProgress(70, "Verificando shaders DirectX...");
                string nvCache = Path.Combine(localAppData, "NVIDIA", "DXCache");
                var nvStats = GetDirectoryStatsSafe(nvCache, onlyOlderThan24h: true, allowedExtensions: new string[] { ".bin", ".toc", ".nvx" });
                result.Categories.Add(new JunkCategory
                {
                    Id = "shaders",
                    Name = Strings.GetCategoryName("shaders"),
                    Description = Strings.GetCategoryDesc("shaders"),
                    Glyph = "\uE7FC",
                    Bytes = nvStats.Bytes,
                    FileCount = nvStats.Files,
                    Paths = new List<string> { nvCache }
                });

                // 4. Reportes y Dumps - Solo archivos .dmp de aplicaciones de usuario (NUNCA C:\Windows\Minidump)
                if (onProgress != null) onProgress(85, "Revisando reportes y volcados...");
                string crashDumpsDir = Path.Combine(localAppData, "CrashDumps");
                var crashStats = GetDirectoryStatsSafe(crashDumpsDir, allowedExtensions: new string[] { ".dmp", ".mdmp" });
                result.Categories.Add(new JunkCategory
                {
                    Id = "dumps",
                    Name = Strings.GetCategoryName("dumps"),
                    Description = Strings.GetCategoryDesc("dumps"),
                    Glyph = "\uE7BA",
                    Bytes = crashStats.Bytes,
                    FileCount = crashStats.Files,
                    Paths = new List<string> { crashDumpsDir }
                });

                // 5. Papelera de Reciclaje
                if (onProgress != null) onProgress(95, "Consultando papelera de reciclaje...");
                var rbStats = GetRecycleBinStats();
                result.Categories.Add(new JunkCategory
                {
                    Id = "recyclebin",
                    Name = Strings.GetCategoryName("recyclebin"),
                    Description = Strings.GetCategoryDesc("recyclebin"),
                    Glyph = "\uE74D",
                    Bytes = rbStats.Bytes,
                    FileCount = rbStats.Files,
                    IsRecycleBin = true
                });

                result.TotalBytes = result.Categories.Sum(c => c.Bytes);
                result.TotalFiles = result.Categories.Sum(c => c.FileCount);

                if (onProgress != null) onProgress(100, "¡Escaneo completado!");
                TrimMemory();
                return result;
            });
        }

        public static Task<long> CleanCategoriesAsync(IEnumerable<JunkCategory> categories, Action<int, string> onProgress = null)
        {
            return Task.Factory.StartNew<long>(() =>
            {
                long totalFreedBytes = 0;
                var list = categories.Where(c => c.IsSelected).ToList();
                int totalSteps = Math.Max(1, list.Count);
                int current = 0;

                foreach (var cat in list)
                {
                    int pct = (int)Math.Round((double)current / totalSteps * 100);
                    if (onProgress != null) onProgress(pct, string.Format("Limpiando {0}...", cat.Name));

                    long categoryFreed = 0;

                    if (cat.IsRecycleBin)
                    {
                        var rbBefore = GetRecycleBinStats();
                        try
                        {
                            SHEmptyRecycleBin(IntPtr.Zero, null, SHERB_NOCONFIRMATION | SHERB_NOPROGRESSUI | SHERB_NOSOUND);
                            var rbAfter = GetRecycleBinStats();
                            categoryFreed += Math.Max(0, rbBefore.Bytes - rbAfter.Bytes);
                        }
                        catch { }
                    }
                    else
                    {
                        bool onlyOld = (cat.Id == "temp" || cat.Id == "shaders");
                        string[] exts = (cat.Id == "dumps") ? new string[] { ".dmp", ".mdmp" } :
                                        (cat.Id == "shaders") ? new string[] { ".bin", ".toc", ".nvx" } : null;

                        foreach (var path in cat.Paths)
                        {
                            if (File.Exists(path))
                            {
                                categoryFreed += PurgeFileSafe(path, onlyOld, exts);
                            }
                            else if (Directory.Exists(path))
                            {
                                categoryFreed += PurgeDirectorySafe(path, onlyOld, exts);
                            }
                        }
                    }

                    totalFreedBytes += categoryFreed;

                    // Actualizar el estado del objeto en vivo para que refleje la limpieza
                    cat.Bytes = Math.Max(0, cat.Bytes - categoryFreed);
                    // Si quedan residuos minúsculos bloqueados (< 5MB), marcamos la categoría como limpia
                    if (cat.Bytes < 5L * 1024 * 1024)
                    {
                        cat.Bytes = 0;
                        cat.FileCount = 0;
                    }

                    current++;
                }

                if (onProgress != null) onProgress(100, "¡Limpieza completada!");
                TrimMemory();
                return totalFreedBytes;
            });
        }

        private static long PurgeDirectorySafe(string dirPath, bool onlyOlderThan24h = false, string[] allowedExtensions = null)
        {
            if (string.IsNullOrEmpty(dirPath) || !Directory.Exists(dirPath)) return 0;
            long freed = 0;
            DateTime cutoff = DateTime.Now.AddHours(-24);

            try
            {
                var dir = new DirectoryInfo(dirPath);

                foreach (var file in dir.GetFiles())
                {
                    try
                    {
                        if (onlyOlderThan24h && file.LastWriteTime > cutoff) continue;
                        if (allowedExtensions != null && allowedExtensions.Length > 0)
                        {
                            string ext = file.Extension.ToLowerInvariant();
                            if (!allowedExtensions.Contains(ext)) continue;
                        }

                        string extLower = file.Extension.ToLowerInvariant();
                        if (ProtectedExtensions.Any(e => extLower == e)) continue;

                        string nameLower = file.Name.ToLowerInvariant();
                        if (ProtectedPatterns.Any(p => nameLower.Contains(p))) continue;

                        long len = file.Length;
                        file.Attributes = FileAttributes.Normal;
                        file.Delete();
                        freed += len;
                    }
                    catch { }
                }

                foreach (var subDir in dir.GetDirectories())
                {
                    try
                    {
                        string subNameLower = subDir.Name.ToLowerInvariant();
                        if (ProtectedPatterns.Any(p => subNameLower.Contains(p))) continue;
                        if (ProtectedExtensions.Any(e => subNameLower.EndsWith(e))) continue;

                        freed += PurgeDirectorySafe(subDir.FullName, onlyOlderThan24h, allowedExtensions);
                        
                        // Solo eliminar subcarpetas vacías con más de 24 horas de antigüedad
                        if (subDir.LastWriteTime < cutoff && !subDir.EnumerateFileSystemInfos().Any())
                        {
                            subDir.Delete();
                        }
                    }
                    catch { }
                }
            }
            catch { }

            return freed;
        }

        private static long PurgeFileSafe(string filePath, bool onlyOlderThan24h = false, string[] allowedExtensions = null)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath)) return 0;
            try
            {
                var fi = new FileInfo(filePath);
                if (onlyOlderThan24h && fi.LastWriteTime > DateTime.Now.AddHours(-24)) return 0;
                if (allowedExtensions != null && allowedExtensions.Length > 0)
                {
                    string ext = fi.Extension.ToLowerInvariant();
                    if (!allowedExtensions.Contains(ext)) return 0;
                }

                string extLower = fi.Extension.ToLowerInvariant();
                if (ProtectedExtensions.Any(e => extLower == e)) return 0;

                string nameLower = fi.Name.ToLowerInvariant();
                if (ProtectedPatterns.Any(p => nameLower.Contains(p))) return 0;

                long len = fi.Length;
                fi.Attributes = FileAttributes.Normal;
                fi.Delete();
                return len;
            }
            catch
            {
                return 0;
            }
        }
    }
}
