using System;
using System.Globalization;

namespace CleanDesk
{
    public static class Strings
    {
        public static bool IsSpanish { get; private set; }

        static Strings()
        {
            string lang = CultureInfo.CurrentUICulture.TwoLetterISOLanguageName.ToLowerInvariant();
            IsSpanish = (lang == "es");
        }

        public static void ToggleLanguage()
        {
            IsSpanish = !IsSpanish;
        }

        public static string AppTitle
        {
            get { return "CleanDesk"; }
        }

        public static string ReadyToScanTitle
        {
            get { return IsSpanish ? "Limpieza del Sistema" : "System Cleaner"; }
        }

        public static string ReadyToScanDesc
        {
            get { return IsSpanish ? "Analiza tu PC para encontrar archivos temporales y caché obsoleta." : "Scan your PC to find temporary files and outdated cache."; }
        }

        public static string ScanPC
        {
            get { return IsSpanish ? "Escanear PC" : "Scan PC"; }
        }

        public static string RescanClean
        {
            get { return IsSpanish ? "Reescanear" : "Rescan"; }
        }

        public static string TemporaryFilesFound
        {
            get { return IsSpanish ? "Archivos temporales detectados" : "Temporary files found"; }
        }

        public static string SystemClean
        {
            get { return IsSpanish ? "Sistema limpio y optimizado" : "System clean & optimized"; }
        }

        public static string InFiles(long count)
        {
            return IsSpanish ? string.Format("en {0:n0} archivos", count) : string.Format("in {0:n0} files", count);
        }

        public static string FreeUpSpaceDesc
        {
            get { return IsSpanish ? "Libera espacio y mantén tu PC funcionando fluido." : "Free up space and keep your PC running smoothly."; }
        }

        public static string SystemCleanDesc
        {
            get { return IsSpanish ? "No se encontraron archivos innecesarios. Tu equipo está al 100%." : "No unnecessary files found. Your PC is running at peak performance."; }
        }

        public static string CleanNow
        {
            get { return IsSpanish ? "Limpiar ahora" : "Clean now"; }
        }

        public static string Details
        {
            get { return IsSpanish ? "Detalles" : "Details"; }
        }

        public static string CloseDetails
        {
            get { return IsSpanish ? "Ocultar detalles" : "Close details"; }
        }

        public static string TemporaryFiles
        {
            get { return IsSpanish ? "Archivos temporales" : "Temporary files"; }
        }

        public static string ScanningTitle
        {
            get { return IsSpanish ? "Escaneando archivos temporales" : "Scanning temporary files"; }
        }

        public static string ScanningDesc
        {
            get { return IsSpanish ? "Buscando archivos que pueden eliminarse con seguridad..." : "Checking your system for files that can be safely removed..."; }
        }

        public static string ScanningNote
        {
            get { return IsSpanish ? "Esto puede tomar unos momentos." : "This may take a few moments."; }
        }

        public static string CleaningTitle
        {
            get { return IsSpanish ? "Limpiando archivos temporales" : "Cleaning temporary files"; }
        }

        public static string CleaningDesc
        {
            get { return IsSpanish ? "Liberando espacio en disco y optimizando..." : "Freeing up disk space and optimizing system..."; }
        }

        public static string SpaceRecovered
        {
            get { return IsSpanish ? "¡Espacio recuperado!" : "Space recovered!"; }
        }

        public static string CleanedStats(string formatted)
        {
            return IsSpanish ? string.Format("{0} liberados", formatted) : string.Format("{0} cleaned", formatted);
        }

        public static string ReadyToGo
        {
            get { return IsSpanish ? "Tu PC está más limpio y listo para usar." : "Your PC is cleaner and ready to go."; }
        }

        public static string MenuThemeLight
        {
            get { return IsSpanish ? "Cambiar a Modo Claro" : "Switch to Light Mode"; }
        }

        public static string MenuThemeDark
        {
            get { return IsSpanish ? "Cambiar a Modo Oscuro" : "Switch to Dark Mode"; }
        }

        public static string MenuCompact
        {
            get { return IsSpanish ? "Vista Compacta" : "Compact View"; }
        }

        public static string MenuFull
        {
            get { return IsSpanish ? "Vista Completa" : "Full View"; }
        }

        public static string MenuRescan
        {
            get { return IsSpanish ? "Volver a Escanear" : "Scan Again"; }
        }

        public static string MenuLang
        {
            get { return IsSpanish ? "Switch to English" : "Cambiar a Español"; }
        }

        public static string MenuClose
        {
            get { return IsSpanish ? "Cerrar Widget" : "Close Widget"; }
        }

        public static string MenuHideToTray
        {
            get { return IsSpanish ? "Ocultar en la bandeja" : "Hide to Tray"; }
        }

        public static string MenuExitApp
        {
            get { return IsSpanish ? "Salir de CleanDesk" : "Exit CleanDesk"; }
        }

        public static string TrayShow
        {
            get { return IsSpanish ? "Mostrar CleanDesk" : "Show CleanDesk"; }
        }

        public static string TrayHide
        {
            get { return IsSpanish ? "Ocultar CleanDesk" : "Hide CleanDesk"; }
        }

        public static string TrayScanPC
        {
            get { return IsSpanish ? "Escanear Archivos Temporales" : "Scan Temporary Files"; }
        }

        public static string TrayKillPorts
        {
            get { return IsSpanish ? "Liberar Puertos Dev Ocupados" : "Kill Busy Dev Ports"; }
        }

        public static string TrayKillTasks
        {
            get { return IsSpanish ? "Terminar Bucles Dev en RAM" : "Kill Runaway Dev Tasks"; }
        }

        // Navigation Tabs
        public static string TabClean
        {
            get { return IsSpanish ? "Limpieza" : "Clean"; }
        }

        public static string TabPorts
        {
            get { return IsSpanish ? "Puertos" : "Ports"; }
        }

        public static string TabTasks
        {
            get { return IsSpanish ? "Tareas" : "Tasks"; }
        }

        // KillerPort Strings
        public static string PortFree
        {
            get { return IsSpanish ? "LIBRE" : "FREE"; }
        }

        public static string PortBusy
        {
            get { return IsSpanish ? "OCUPADO" : "BUSY"; }
        }

        public static string KillSelectedPorts
        {
            get { return IsSpanish ? "Matar Ocupados" : "Kill Busy"; }
        }

        public static string CustomPortPlaceholder
        {
            get { return IsSpanish ? "Puerto (ej. 8080)" : "Port (e.g. 8080)"; }
        }

        public static string AddPort
        {
            get { return IsSpanish ? "Agregar" : "Add"; }
        }

        public static string AllPortsFree
        {
            get { return IsSpanish ? "Todos los puertos dev están libres" : "All dev ports are free"; }
        }

        public static string BusyPortsCount(int n)
        {
            return IsSpanish ? string.Format("{0} puerto(s) en uso", n) : string.Format("{0} port(s) in use", n);
        }

        // Process Guardian Strings
        public static string ProcessInstances(int n)
        {
            return IsSpanish ? string.Format("{0} instancia(s)", n) : string.Format("{0} instance(s)", n);
        }

        public static string RunawayAlertTitle
        {
            get { return IsSpanish ? "Bucle o Sobrecarga Detectada" : "Runaway Loop Detected"; }
        }

        public static string RunawayAlertDesc
        {
            get { return IsSpanish ? "Subshells huérfanas o alto consumo de RAM." : "Orphan subshells or high RAM usage found."; }
        }

        public static string KillAllRunaways
        {
            get { return IsSpanish ? "Matar Bucles" : "Kill Runaways"; }
        }

        public static string KillGroup
        {
            get { return IsSpanish ? "Matar" : "Kill"; }
        }

        public static string NoRunawayDetected
        {
            get { return IsSpanish ? "Sin bucles de procesos dev activos" : "No runaway dev processes detected"; }
        }

        public static string TotalDevRam(string mem, int count)
        {
            return IsSpanish ? string.Format("{0} RAM en {1} procesos dev", mem, count) : string.Format("{0} RAM in {1} dev processes", mem, count);
        }

        // Category localized names and descriptions
        public static string GetCategoryName(string id)
        {
            switch (id)
            {
                case "temp": return IsSpanish ? "Archivos Temporales" : "Temporary Files";
                case "dev": return IsSpanish ? "Caché de Desarrollo" : "Developer Cache";
                case "shaders": return IsSpanish ? "Shaders DirectX" : "DirectX Shaders";
                case "dumps": return IsSpanish ? "Reportes y Volcados" : "Reports & Dumps";
                case "recyclebin": return IsSpanish ? "Papelera de Reciclaje" : "Recycle Bin";
                default: return id;
            }
        }

        public static string GetCategoryDesc(string id)
        {
            switch (id)
            {
                case "temp": return IsSpanish ? "%TEMP% y temporales de Windows" : "%TEMP% and Windows temp";
                case "dev": return IsSpanish ? "Paquetes de npm y pip" : "npm and pip packages";
                case "shaders": return IsSpanish ? "Caché de shaders obsoletos NVIDIA" : "Outdated NVIDIA shader cache";
                case "dumps": return IsSpanish ? "Volcados de error y diagnóstico" : "Crash memory dumps & errors";
                case "recyclebin": return IsSpanish ? "Elementos eliminados en disco" : "Deleted files on disk";
                default: return "";
            }
        }

        // Display Manager Localized Strings
        public static string TabDisplays
        {
            get { return IsSpanish ? "Pantallas" : "Displays"; }
        }

        public static string DisplaysTitle
        {
            get { return IsSpanish ? "Gestor de Pantallas" : "Display Manager"; }
        }

        public static string DisplaysSubtitle
        {
            get { return IsSpanish ? "Perfiles rápidos y disposición física" : "Quick presets & physical layout"; }
        }

        public static string ConnectedMonitors
        {
            get { return IsSpanish ? "Monitores Conectados" : "Connected Displays"; }
        }

        public static string DisplayProfiles
        {
            get { return IsSpanish ? "Perfiles" : "Presets"; }
        }

        public static string SaveCurrentProfile
        {
            get { return IsSpanish ? "Guardar actual" : "Save current"; }
        }

        public static string EnterProfileNamePlaceholder
        {
            get { return IsSpanish ? "Nombre del perfil..." : "Profile name..."; }
        }

        public static string SaveBtn
        {
            get { return IsSpanish ? "Guardar" : "Save"; }
        }

        public static string CancelBtn
        {
            get { return IsSpanish ? "Cancelar" : "Cancel"; }
        }

        public static string ActiveBadge
        {
            get { return IsSpanish ? "ACTIVO" : "ACTIVE"; }
        }

        public static string ApplyBtn
        {
            get { return IsSpanish ? "Aplicar" : "Apply"; }
        }

        public static string DeleteBtn
        {
            get { return IsSpanish ? "Eliminar" : "Delete"; }
        }

        public static string MissingMonitors
        {
            get { return IsSpanish ? "Faltan monitores desconectados" : "Missing disconnected monitors"; }
        }
    }
}

