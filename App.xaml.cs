using System;
using System.IO;
using System.Windows;

namespace CleanDesk
{
    public partial class App : Application
    {
        public App()
        {
            AppDomain.CurrentDomain.UnhandledException += (s, e) =>
            {
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "cleandesk_crash.log"), e.ExceptionObject.ToString()); } catch { }
            };
            this.DispatcherUnhandledException += (s, e) =>
            {
                try { File.WriteAllText(Path.Combine(Path.GetTempPath(), "cleandesk_crash.log"), e.Exception.ToString()); } catch { }
            };
        }

    }
}
