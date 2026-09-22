using System;
using System.IO;
using System.Reflection;
using System.Drawing;
using System.Windows.Forms;
using System.Runtime.InteropServices;
using Microsoft.Win32;
using System.Diagnostics;

namespace CleanDeskInstaller
{
    static class Program
    {
        [STAThread]
        static void Main(string[] args)
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            bool isSilent = false;
            foreach (var arg in args)
            {
                if (arg.Equals("/S", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/SILENT", StringComparison.OrdinalIgnoreCase) ||
                    arg.Equals("/VERYSILENT", StringComparison.OrdinalIgnoreCase))
                {
                    isSilent = true;
                }
            }

            if (isSilent)
            {
                InstallerLogic.ExecuteInstall(
                    InstallerLogic.GetDefaultInstallDir(),
                    createDesktop: true,
                    createStartMenu: true,
                    addToStartup: true,
                    launchAfter: false
                );
                return;
            }

            Application.Run(new InstallerForm());
        }
    }

    public class InstallerForm : Form
    {
        private TextBox txtInstallPath;
        private Button btnBrowse;
        private CheckBox chkDesktop;
        private CheckBox chkStartMenu;
        private CheckBox chkStartup;
        private CheckBox chkLaunch;
        private ProgressBar progressBar;
        private Label lblStatus;
        private Button btnInstall;
        private Button btnCancel;
        private bool _isCompleted = false;

        public InstallerForm()
        {
            InitUI();
        }

        private void InitUI()
        {
            this.Text = "Instalador de CleanDesk - Versión 1.0";
            this.Size = new Size(540, 480);
            this.FormBorderStyle = FormBorderStyle.FixedDialog;
            this.MaximizeBox = false;
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(26, 32, 44);     // Fluent Dark Background
            this.ForeColor = Color.FromArgb(248, 250, 252);
            this.Font = new Font("Segoe UI", 9.25f, FontStyle.Regular);

            try
            {
                using (var icoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("app.ico"))
                {
                    if (icoStream != null) this.Icon = new Icon(icoStream);
                }
            }
            catch { }

            // Header Banner
            var pnlHeader = new Panel
            {
                Dock = DockStyle.Top,
                Height = 85,
                BackColor = Color.FromArgb(30, 41, 59)
            };

            var picIcon = new PictureBox
            {
                Location = new Point(20, 15),
                Size = new Size(54, 54),
                SizeMode = PictureBoxSizeMode.Zoom
            };
            try
            {
                if (this.Icon != null) picIcon.Image = this.Icon.ToBitmap();
            }
            catch { }

            var lblTitle = new Label
            {
                Text = "CleanDesk Widget",
                Font = new Font("Segoe UI", 14f, FontStyle.Bold),
                ForeColor = Color.FromArgb(96, 165, 250), // Electric Blue
                Location = new Point(88, 16),
                AutoSize = true
            };

            var lblSubtitle = new Label
            {
                Text = "Limpiador dev, gestión de puertos y guardián de RAM para Windows",
                Font = new Font("Segoe UI", 8.75f, FontStyle.Regular),
                ForeColor = Color.FromArgb(148, 163, 184),
                Location = new Point(89, 45),
                AutoSize = true
            };

            pnlHeader.Controls.Add(picIcon);
            pnlHeader.Controls.Add(lblTitle);
            pnlHeader.Controls.Add(lblSubtitle);
            this.Controls.Add(pnlHeader);

            // Path Section
            int y = 105;
            var lblDest = new Label
            {
                Text = "Carpeta de instalación de CleanDesk:",
                Location = new Point(24, y),
                AutoSize = true,
                Font = new Font("Segoe UI", 9f, FontStyle.Bold)
            };
            this.Controls.Add(lblDest);

            y += 24;
            txtInstallPath = new TextBox
            {
                Text = InstallerLogic.GetDefaultInstallDir(),
                Location = new Point(24, y),
                Size = new Size(385, 25),
                BackColor = Color.FromArgb(15, 23, 42),
                ForeColor = Color.White,
                BorderStyle = BorderStyle.FixedSingle
            };
            this.Controls.Add(txtInstallPath);

            btnBrowse = new Button
            {
                Text = "Examinar...",
                Location = new Point(418, y - 1),
                Size = new Size(88, 27),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnBrowse.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btnBrowse.Click += (s, e) =>
            {
                using (var fbd = new FolderBrowserDialog())
                {
                    fbd.SelectedPath = txtInstallPath.Text;
                    if (fbd.ShowDialog() == DialogResult.OK)
                    {
                        txtInstallPath.Text = Path.Combine(fbd.SelectedPath, "CleanDesk");
                    }
                }
            };
            this.Controls.Add(btnBrowse);

            // Checkboxes Options
            y += 38;
            var grpOptions = new GroupBox
            {
                Text = " Opciones de instalación ",
                ForeColor = Color.FromArgb(203, 213, 225),
                Location = new Point(24, y),
                Size = new Size(482, 142)
            };

            chkDesktop = new CheckBox
            {
                Text = "Crear acceso directo en el Escritorio",
                Checked = true,
                Location = new Point(16, 26),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            chkStartMenu = new CheckBox
            {
                Text = "Crear acceso directo en el Menú de Inicio (Start Menu)",
                Checked = true,
                Location = new Point(16, 52),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            chkStartup = new CheckBox
            {
                Text = "Iniciar CleanDesk automáticamente al arrancar Windows (Startup)",
                Checked = true,
                Location = new Point(16, 78),
                AutoSize = true,
                Cursor = Cursors.Hand
            };
            chkLaunch = new CheckBox
            {
                Text = "Iniciar CleanDesk Widget al finalizar la instalación",
                Checked = true,
                Location = new Point(16, 104),
                AutoSize = true,
                Cursor = Cursors.Hand
            };

            grpOptions.Controls.Add(chkDesktop);
            grpOptions.Controls.Add(chkStartMenu);
            grpOptions.Controls.Add(chkStartup);
            grpOptions.Controls.Add(chkLaunch);
            this.Controls.Add(grpOptions);

            // Progress & Status
            y += 152;
            progressBar = new ProgressBar
            {
                Location = new Point(24, y),
                Size = new Size(482, 10),
                Style = ProgressBarStyle.Continuous,
                Value = 0,
                Visible = false
            };
            this.Controls.Add(progressBar);

            y += 16;
            lblStatus = new Label
            {
                Text = "Listo para instalar la versión 1.0",
                Location = new Point(24, y),
                AutoSize = true,
                ForeColor = Color.FromArgb(148, 163, 184),
                Font = new Font("Segoe UI", 8.5f)
            };
            this.Controls.Add(lblStatus);

            // Bottom Buttons
            btnInstall = new Button
            {
                Text = "Instalar CleanDesk",
                Location = new Point(265, 395),
                Size = new Size(140, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(37, 99, 235), // Primary Blue
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9.5f, FontStyle.Bold),
                Cursor = Cursors.Hand
            };
            btnInstall.FlatAppearance.BorderSize = 0;
            btnInstall.Click += BtnInstall_Click;
            this.Controls.Add(btnInstall);

            btnCancel = new Button
            {
                Text = "Cancelar",
                Location = new Point(415, 395),
                Size = new Size(91, 34),
                FlatStyle = FlatStyle.Flat,
                BackColor = Color.FromArgb(51, 65, 85),
                ForeColor = Color.White,
                Cursor = Cursors.Hand
            };
            btnCancel.FlatAppearance.BorderColor = Color.FromArgb(71, 85, 105);
            btnCancel.Click += (s, e) => this.Close();
            this.Controls.Add(btnCancel);
        }

        private void BtnInstall_Click(object sender, EventArgs e)
        {
            if (_isCompleted)
            {
                this.Close();
                return;
            }

            string targetDir = txtInstallPath.Text.Trim();
            if (string.IsNullOrEmpty(targetDir))
            {
                MessageBox.Show("Por favor especifica una ruta válida.", "CleanDesk", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            btnInstall.Enabled = false;
            btnBrowse.Enabled = false;
            txtInstallPath.Enabled = false;
            chkDesktop.Enabled = false;
            chkStartMenu.Enabled = false;
            chkStartup.Enabled = false;
            chkLaunch.Enabled = false;
            progressBar.Visible = true;
            progressBar.Value = 25;
            lblStatus.Text = "Cerrando instancias previas...";
            Application.DoEvents();

            try
            {
                progressBar.Value = 50;
                lblStatus.Text = "Extrayendo binarios y recursos de CleanDesk...";
                Application.DoEvents();

                InstallerLogic.ExecuteInstall(
                    targetDir,
                    chkDesktop.Checked,
                    chkStartMenu.Checked,
                    chkStartup.Checked,
                    chkLaunch.Checked
                );

                progressBar.Value = 100;
                lblStatus.ForeColor = Color.FromArgb(74, 222, 128); // Green
                lblStatus.Text = "¡Instalación completada exitosamente!";
                btnInstall.Text = "Finalizar";
                btnInstall.BackColor = Color.FromArgb(22, 163, 74);
                btnInstall.Enabled = true;
                btnCancel.Visible = false;
                _isCompleted = true;
            }
            catch (Exception ex)
            {
                progressBar.Visible = false;
                lblStatus.ForeColor = Color.FromArgb(248, 113, 113);
                lblStatus.Text = "Error: " + ex.Message;
                btnInstall.Enabled = true;
                btnInstall.Text = "Reintentar";
            }
        }
    }

    public static class InstallerLogic
    {
        public static string GetDefaultInstallDir()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "Programs", "CleanDesk");
        }

        public static void ExecuteInstall(string installDir, bool createDesktop, bool createStartMenu, bool addToStartup, bool launchAfter)
        {
            // 1. Cerrar procesos previos
            try
            {
                foreach (var p in Process.GetProcessesByName("CleanDesk"))
                {
                    try { p.Kill(); p.WaitForExit(2000); } catch { }
                }
            }
            catch { }

            // 2. Crear directorio destino
            if (!Directory.Exists(installDir))
            {
                Directory.CreateDirectory(installDir);
            }

            // 3. Extraer recursos incrustados
            ExtractResource("CleanDesk.exe", Path.Combine(installDir, "CleanDesk.exe"));
            ExtractResource("CleanDesk.exe.config", Path.Combine(installDir, "CleanDesk.exe.config"));
            ExtractResource("app.ico", Path.Combine(installDir, "app.ico"));

            string exePath = Path.Combine(installDir, "CleanDesk.exe");
            string icoPath = Path.Combine(installDir, "app.ico");

            // 4. Crear desinstalador
            CreateUninstaller(installDir);

            // 5. Registrar en Windows 'Agregar o quitar programas'
            RegisterUninstallEntry(installDir, exePath, icoPath);

            // 6. Accesos directos
            Type shellType = Type.GetTypeFromProgID("WScript.Shell");
            if (shellType != null)
            {
                dynamic shell = Activator.CreateInstance(shellType);

                // Escritorio
                string desktopLnk = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory), "CleanDesk.lnk");
                if (createDesktop)
                {
                    if (File.Exists(desktopLnk)) try { File.Delete(desktopLnk); } catch { }
                    dynamic s = shell.CreateShortcut(desktopLnk);
                    s.TargetPath = exePath;
                    s.WorkingDirectory = installDir;
                    s.IconLocation = icoPath + ",0";
                    s.Description = "CleanDesk - Limpiador y Gestor Dev";
                    s.Save();
                }
                else if (File.Exists(desktopLnk))
                {
                    try { File.Delete(desktopLnk); } catch { }
                }

                // Menú Inicio
                string startMenuDir = Environment.GetFolderPath(Environment.SpecialFolder.Programs);
                string startMenuLnk = Path.Combine(startMenuDir, "CleanDesk.lnk");
                if (createStartMenu)
                {
                    if (File.Exists(startMenuLnk)) try { File.Delete(startMenuLnk); } catch { }
                    dynamic s = shell.CreateShortcut(startMenuLnk);
                    s.TargetPath = exePath;
                    s.WorkingDirectory = installDir;
                    s.IconLocation = icoPath + ",0";
                    s.Description = "CleanDesk - Limpiador y Gestor Dev";
                    s.Save();
                }
                else if (File.Exists(startMenuLnk))
                {
                    try { File.Delete(startMenuLnk); } catch { }
                }
            }

            // 7. Startup Registry (único registro sin duplicar en Startup folder)
            string runKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
            string approvedKey = @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run";
            using (var key = Registry.CurrentUser.OpenSubKey(runKey, true))
            {
                if (key != null)
                {
                    if (addToStartup)
                    {
                        key.SetValue("CleanDesk", "\"" + exePath + "\"");
                    }
                    else
                    {
                        try { key.DeleteValue("CleanDesk", false); } catch { }
                    }
                }
            }

            // Marcar Enabled en StartupApproved si se seleccionó
            if (addToStartup)
            {
                using (var key = Registry.CurrentUser.OpenSubKey(approvedKey, true))
                {
                    if (key != null)
                    {
                        byte[] enabledBytes = new byte[] { 2, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 };
                        key.SetValue("CleanDesk", enabledBytes, RegistryValueKind.Binary);
                    }
                }
            }

            // Eliminar de carpeta Startup si existía previamente para evitar duplicados
            string startupDir = Environment.GetFolderPath(Environment.SpecialFolder.Startup);
            string oldStartupLnk = Path.Combine(startupDir, "CleanDesk.lnk");
            if (File.Exists(oldStartupLnk)) try { File.Delete(oldStartupLnk); } catch { }

            // 8. Refrescar caché del shell de Windows
            SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);

            // 9. Iniciar si se solicitó
            if (launchAfter)
            {
                try
                {
                    Process.Start(new ProcessStartInfo
                    {
                        FileName = exePath,
                        WorkingDirectory = installDir
                    });
                }
                catch { }
            }
        }

        private static void ExtractResource(string resourceName, string outputPath)
        {
            var asm = Assembly.GetExecutingAssembly();
            using (var stream = asm.GetManifestResourceStream(resourceName))
            {
                if (stream == null) return;
                using (var fs = new FileStream(outputPath, FileMode.Create, FileAccess.Write, FileShare.None))
                {
                    stream.CopyTo(fs);
                }
            }
        }

        private static void CreateUninstaller(string installDir)
        {
            string uninstallCmd = Path.Combine(installDir, "uninstall.bat");
            string content = string.Format(@"@echo off
taskkill /F /IM CleanDesk.exe 2>nul
powershell -NoProfile -Command ""Remove-Item -Path '$([Environment]::GetFolderPath('Desktop'))\CleanDesk.lnk' -Force -ErrorAction SilentlyContinue; Remove-Item -Path '$([Environment]::GetFolderPath('Programs'))\CleanDesk.lnk' -Force -ErrorAction SilentlyContinue; Remove-Item -Path '$([Environment]::GetFolderPath('Startup'))\CleanDesk.lnk' -Force -ErrorAction SilentlyContinue; Remove-ItemProperty -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Run' -Name 'CleanDesk' -ErrorAction SilentlyContinue; Remove-Item -Path 'HKCU:\Software\Microsoft\Windows\CurrentVersion\Uninstall\CleanDesk' -Recurse -Force -ErrorAction SilentlyContinue; [Win32.ShellNotification]::SHChangeNotify(0x08000000, 0, [IntPtr]::Zero, [IntPtr]::Zero)"" 2>nul
timeout /t 1 >nul
cd ..
rmdir /s /q ""{0}"" 2>nul
", installDir);
            File.WriteAllText(uninstallCmd, content);
        }

        private static void RegisterUninstallEntry(string installDir, string exePath, string icoPath)
        {
            try
            {
                string unKey = @"Software\Microsoft\Windows\CurrentVersion\Uninstall\CleanDesk";
                using (var key = Registry.CurrentUser.CreateSubKey(unKey))
                {
                    if (key != null)
                    {
                        key.SetValue("DisplayName", "CleanDesk Widget");
                        key.SetValue("DisplayVersion", "1.0.0");
                        key.SetValue("Publisher", "FaberOs");
                        key.SetValue("DisplayIcon", icoPath);
                        key.SetValue("UninstallString", Path.Combine(installDir, "uninstall.bat"));
                        key.SetValue("InstallLocation", installDir);
                        key.SetValue("EstimatedSize", 1024, RegistryValueKind.DWord);
                    }
                }
            }
            catch { }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }
}
