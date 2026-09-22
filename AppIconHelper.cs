using System;
using System.IO;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace CleanDesk
{
    public static class AppIconHelper
    {
        private static Icon _cachedIcon = null;
        private static ImageSource _cachedImageSource = null;

        [DllImport("user32.dll", CharSet = CharSet.Auto)]
        private static extern bool DestroyIcon(IntPtr handle);

        public static Icon GetAppIcon()
        {
            if (_cachedIcon != null)
            {
                return _cachedIcon;
            }

            try
            {
                // 1. Intentar cargar desde app.ico en el directorio base
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "app.ico");
                if (File.Exists(icoPath))
                {
                    using (var fs = new FileStream(icoPath, FileMode.Open, FileAccess.Read, FileShare.Read))
                    {
                        _cachedIcon = new Icon(fs);
                        return _cachedIcon;
                    }
                }

                // 2. Extraer del ensamblado .exe en ejecución
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (File.Exists(exePath))
                {
                    Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (exeIcon != null)
                    {
                        _cachedIcon = exeIcon;
                        return _cachedIcon;
                    }
                }
            }
            catch
            {
                // Fallback dinámico si no existe en disco
            }

            // 3. Generar dinámicamente el icono en memoria
            _cachedIcon = GenerateCleanDeskIcon(32);
            return _cachedIcon;
        }

        public static ImageSource GetAppImageSource()
        {
            if (_cachedImageSource != null)
            {
                return _cachedImageSource;
            }

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "app.ico");
                if (File.Exists(icoPath))
                {
                    var decoder = BitmapDecoder.Create(
                        new Uri(icoPath, UriKind.Absolute),
                        BitmapCreateOptions.None,
                        BitmapCacheOption.OnLoad
                    );
                    if (decoder.Frames.Count > 0)
                    {
                        BitmapFrame bestFrame = decoder.Frames[0];
                        foreach (var frame in decoder.Frames)
                        {
                            if (frame.PixelWidth >= 48 && frame.PixelWidth <= 128)
                            {
                                bestFrame = frame;
                                break;
                            }
                        }
                        _cachedImageSource = bestFrame;
                        return _cachedImageSource;
                    }
                }

                // 2. Extraer del ensamblado .exe en ejecución
                string exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                if (File.Exists(exePath))
                {
                    Icon exeIcon = Icon.ExtractAssociatedIcon(exePath);
                    if (exeIcon != null)
                    {
                        using (var bmp = exeIcon.ToBitmap())
                        {
                            IntPtr hBitmap = bmp.GetHbitmap();
                            try
                            {
                                _cachedImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                                    hBitmap,
                                    IntPtr.Zero,
                                    System.Windows.Int32Rect.Empty,
                                    BitmapSizeOptions.FromEmptyOptions()
                                );
                                _cachedImageSource.Freeze();
                                return _cachedImageSource;
                            }
                            finally
                            {
                                DeleteObject(hBitmap);
                            }
                        }
                    }
                }
            }
            catch
            {
                // Fallback dinámico si no existe en disco
            }

            try
            {
                using (var bmp = GenerateCleanDeskBitmap(64))
                {
                    IntPtr hBitmap = bmp.GetHbitmap();
                    try
                    {
                        _cachedImageSource = System.Windows.Interop.Imaging.CreateBitmapSourceFromHBitmap(
                            hBitmap,
                            IntPtr.Zero,
                            System.Windows.Int32Rect.Empty,
                            BitmapSizeOptions.FromEmptyOptions()
                        );
                        _cachedImageSource.Freeze();
                        return _cachedImageSource;
                    }
                    finally
                    {
                        DeleteObject(hBitmap);
                    }
                }
            }
            catch
            {
                return null;
            }
        }

        [DllImport("gdi32.dll")]
        private static extern bool DeleteObject(IntPtr hObject);

        public static Icon GenerateCleanDeskIcon(int size)
        {
            using (var bmp = GenerateCleanDeskBitmap(size))
            {
                IntPtr hIcon = bmp.GetHicon();
                try
                {
                    // Clonar el icono para que persista tras destruir el handle de GDI
                    Icon tempIcon = Icon.FromHandle(hIcon);
                    return (Icon)tempIcon.Clone();
                }
                finally
                {
                    DestroyIcon(hIcon);
                }
            }
        }

        public static Bitmap GenerateCleanDeskBitmap(int size)
        {
            var bmp = new Bitmap(size, size, System.Drawing.Imaging.PixelFormat.Format32bppArgb);
            using (var g = Graphics.FromImage(bmp))
            {
                g.SmoothingMode = SmoothingMode.AntiAlias;
                g.InterpolationMode = InterpolationMode.HighQualityBicubic;
                g.PixelOffsetMode = PixelOffsetMode.HighQuality;

                float pad = size * 0.05f;
                float rectSize = size - (pad * 2f);
                float radius = size * 0.24f;

                // 1. Squircle de fondo redondeado con degradado Fluent Blue
                using (var gp = CreateRoundedRectanglePath(new RectangleF(pad, pad, rectSize, rectSize), radius))
                {
                    using (var brush = new System.Drawing.Drawing2D.LinearGradientBrush(
                        new PointF(0, 0),
                        new PointF(size, size),
                        System.Drawing.Color.FromArgb(255, 30, 114, 254),   // #1E72FE Fluent Accent
                        System.Drawing.Color.FromArgb(255, 10, 50, 160)))   // #0A32A0 Deep Ocean Blue
                    {
                        g.FillPath(brush, gp);
                    }

                    // Resplandor superior sutil
                    using (var pen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(100, 255, 255, 255), Math.Max(1.0f, size * 0.03f)))
                    {
                        g.DrawPath(pen, gp);
                    }
                }

                // 2. Mango de la Escoba (Largo, blanco, diagonal a 45 grados)
                float handleThickness = Math.Max(2.2f, size * 0.095f);
                using (var handlePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 255, 255, 255), handleThickness))
                {
                    handlePen.StartCap = LineCap.Round;
                    handlePen.EndCap = LineCap.Round;
                    g.DrawLine(handlePen, size * 0.74f, size * 0.24f, size * 0.44f, size * 0.54f);
                }

                // 3. Collar Dorado (Ferrule / Abrazadera de unión)
                float collarThickness = Math.Max(2.6f, size * 0.125f);
                using (var collarPen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(255, 245, 158, 11), collarThickness)) // #F59E0B
                {
                    collarPen.StartCap = LineCap.Round;
                    collarPen.EndCap = LineCap.Round;
                    g.DrawLine(collarPen, size * 0.49f, size * 0.49f, size * 0.39f, size * 0.59f);
                }

                // 4. Cabezal de Cerdas en Abanico (Bristles Fan)
                using (var bristlesBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255, 255)))
                {
                    PointF[] bristlePts = new PointF[]
                    {
                        new PointF(size * 0.47f, size * 0.51f),
                        new PointF(size * 0.39f, size * 0.59f),
                        new PointF(size * 0.16f, size * 0.74f),
                        new PointF(size * 0.22f, size * 0.84f),
                        new PointF(size * 0.32f, size * 0.84f),
                        new PointF(size * 0.42f, size * 0.76f),
                        new PointF(size * 0.47f, size * 0.65f)
                    };
                    g.FillPolygon(bristlesBrush, bristlePts);
                }

                // Ranuras de separación de cerdas (Definen claramente la escoba)
                using (var groovePen = new System.Drawing.Pen(System.Drawing.Color.FromArgb(200, 10, 50, 160), Math.Max(1.0f, size * 0.035f)))
                {
                    g.DrawLine(groovePen, size * 0.42f, size * 0.56f, size * 0.22f, size * 0.82f);
                    g.DrawLine(groovePen, size * 0.44f, size * 0.54f, size * 0.30f, size * 0.83f);
                    g.DrawLine(groovePen, size * 0.46f, size * 0.52f, size * 0.38f, size * 0.77f);
                }

                // 5. Destello Mágico Primario (4-Point Diamond Sparkle) en esquina superior derecha
                using (var sparkleBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 74, 222, 128))) // #4ADE80 Emerald
                {
                    float cx = size * 0.78f;
                    float cy = size * 0.22f;
                    float spR = size * 0.14f;
                    float spIn = size * 0.035f;

                    PointF[] spPts = new PointF[]
                    {
                        new PointF(cx, cy - spR),
                        new PointF(cx + spIn, cy - spIn),
                        new PointF(cx + spR, cy),
                        new PointF(cx + spIn, cy + spIn),
                        new PointF(cx, cy + spR),
                        new PointF(cx - spIn, cy + spIn),
                        new PointF(cx - spR, cy),
                        new PointF(cx - spIn, cy - spIn)
                    };
                    g.FillPolygon(sparkleBrush, spPts);
                }

                // Núcleo blanco del destello
                using (var coreBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255, 255)))
                {
                    float cx = size * 0.78f;
                    float cy = size * 0.22f;
                    float coreR = size * 0.04f;
                    g.FillEllipse(coreBrush, cx - coreR, cy - coreR, coreR * 2f, coreR * 2f);

                    // 6. Destello Mágico Secundario (Blanco puro suave en zona izquierda)
                    float cx2 = size * 0.24f;
                    float cy2 = size * 0.34f;
                    float spR2 = size * 0.08f;
                    float spIn2 = size * 0.02f;

                    PointF[] spPts2 = new PointF[]
                    {
                        new PointF(cx2, cy2 - spR2),
                        new PointF(cx2 + spIn2, cy2 - spIn2),
                        new PointF(cx2 + spR2, cy2),
                        new PointF(cx2 + spIn2, cy2 + spIn2),
                        new PointF(cx2, cy2 + spR2),
                        new PointF(cx2 - spIn2, cy2 + spIn2),
                        new PointF(cx2 - spR2, cy2),
                        new PointF(cx2 - spIn2, cy2 - spIn2)
                    };
                    g.FillPolygon(coreBrush, spPts2);
                }
            }

            return bmp;
        }

        private static GraphicsPath CreateRoundedRectanglePath(RectangleF rect, float radius)
        {
            var path = new GraphicsPath();
            float d = radius * 2f;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }

        public static void EnsureIconFiles()
        {
            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string icoPath = Path.Combine(baseDir, "app.ico");
                if (!File.Exists(icoPath))
                {
                    using (var fs = new FileStream(icoPath, FileMode.Create, FileAccess.Write))
                    {
                        Icon ico = GenerateCleanDeskIcon(32);
                        ico.Save(fs);
                    }
                }
            }
            catch
            {
                // Silencioso
            }
        }

        public static void UpdateDesktopShortcut()
        {
            try
            {
                string desktopPath = Environment.GetFolderPath(Environment.SpecialFolder.DesktopDirectory);
                string shortcutPath = Path.Combine(desktopPath, "CleanDesk.lnk");
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;
                string exePath = Path.Combine(baseDir, "CleanDesk.exe");
                if (!File.Exists(exePath))
                {
                    exePath = System.Reflection.Assembly.GetExecutingAssembly().Location;
                }

                string icoPath = Path.Combine(baseDir, "app.ico");
                if (!File.Exists(icoPath))
                {
                    string rootCandidate = Path.GetFullPath(Path.Combine(baseDir, @"..\..\app.ico"));
                    if (File.Exists(rootCandidate)) icoPath = rootCandidate;
                }

                // 1. Sincronizar binario a la raíz si se ejecuta desde bin\Release
                try
                {
                    string projectRootExe = Path.GetFullPath(Path.Combine(baseDir, @"..\..\CleanDesk.exe"));
                    if (File.Exists(projectRootExe) && !string.Equals(exePath, projectRootExe, StringComparison.OrdinalIgnoreCase))
                    {
                        File.Copy(exePath, projectRootExe, true);
                    }
                }
                catch { }

                // 2. Recrear el acceso directo desde cero para forzar a Windows Explorer a invalidar la caché
                try
                {
                    if (File.Exists(shortcutPath))
                    {
                        File.Delete(shortcutPath);
                    }
                }
                catch { }

                Type shellType = Type.GetTypeFromProgID("WScript.Shell");
                if (shellType != null)
                {
                    dynamic shell = Activator.CreateInstance(shellType);
                    dynamic shortcut = shell.CreateShortcut(shortcutPath);
                    shortcut.TargetPath = exePath;
                    shortcut.WorkingDirectory = Path.GetDirectoryName(exePath);
                    shortcut.IconLocation = File.Exists(icoPath) ? (icoPath + ",0") : (exePath + ",0");
                    shortcut.Description = "CleanDesk - Limpiador y Gestor Dev";
                    shortcut.Save();
                }

                // 3. Limpiar scripts obsoletos del escritorio ahora integrados en CleanDesk
                try
                {
                    string kp = Path.Combine(desktopPath, "killerport.bat");
                    if (File.Exists(kp)) File.Delete(kp);
                    string mn = Path.Combine(desktopPath, "Matar_Node.bat");
                    if (File.Exists(mn)) File.Delete(mn);
                    string cp = Path.Combine(desktopPath, "Compilar_CleanDesk.bat");
                    if (File.Exists(cp)) File.Delete(cp);
                    string le = Path.Combine(desktopPath, "Limpiar_Escritorio.bat");
                    if (File.Exists(le)) File.Delete(le);
                }
                catch { }

                // 4. Notificar al shell de Windows Explorer para refrescar iconos
                SHChangeNotify(0x08000000, 0, IntPtr.Zero, IntPtr.Zero);
                try
                {
                    IntPtr pShortcut = Marshal.StringToHGlobalUni(shortcutPath);
                    SHChangeNotify(0x00002000, 0x0005, pShortcut, IntPtr.Zero);
                    Marshal.FreeHGlobal(pShortcut);
                }
                catch { }
            }
            catch
            {
                // Silencioso
            }
        }

        [DllImport("shell32.dll")]
        private static extern void SHChangeNotify(int wEventId, int uFlags, IntPtr dwItem1, IntPtr dwItem2);
    }

    public class CleanDeskTrayRenderer : System.Windows.Forms.ToolStripProfessionalRenderer
    {
        private static readonly System.Drawing.Color _bgMenu = System.Drawing.Color.FromArgb(255, 26, 32, 44);         // #1A202C Windows 11 Acrylic
        private static readonly System.Drawing.Color _borderMenu = System.Drawing.Color.FromArgb(255, 45, 55, 72);      // #2D3748
        private static readonly System.Drawing.Color _hoverBg = System.Drawing.Color.FromArgb(255, 42, 53, 71);         // #2A3547 Fluent Hover Pill
        private static readonly System.Drawing.Color _hoverBorder = System.Drawing.Color.FromArgb(255, 59, 130, 246);   // #3B82F6 Subtle Blue Accent Edge
        private static readonly System.Drawing.Color _hoverDangerBg = System.Drawing.Color.FromArgb(255, 220, 38, 38);   // #DC2626 Red Hover for Exit
        private static readonly System.Drawing.Color _separator = System.Drawing.Color.FromArgb(255, 45, 55, 72);       // #2D3748
        private static readonly System.Drawing.Color _textPrimary = System.Drawing.Color.FromArgb(255, 248, 250, 252);  // #F8FAFC
        private static readonly System.Drawing.Color _textMuted = System.Drawing.Color.FromArgb(255, 100, 116, 139);    // #64748B
        private static readonly System.Drawing.Color _textDanger = System.Drawing.Color.FromArgb(255, 248, 113, 113);   // #F87171 Soft Red
        private static readonly System.Drawing.Color _textHeader = System.Drawing.Color.FromArgb(255, 96, 165, 250);    // #60A5FA Electric Cyan

        public CleanDeskTrayRenderer() : base(new CleanDeskColorTable())
        {
        }

        protected override void OnRenderToolStripBackground(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using (var brush = new System.Drawing.SolidBrush(_bgMenu))
            {
                e.Graphics.FillRectangle(brush, e.AffectedBounds);
            }
        }

        protected override void OnRenderToolStripBorder(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            using (var pen = new System.Drawing.Pen(_borderMenu, 1f))
            {
                var rect = new System.Drawing.Rectangle(0, 0, e.ToolStrip.Width - 1, e.ToolStrip.Height - 1);
                e.Graphics.DrawRectangle(pen, rect);
            }
        }

        protected override void OnRenderImageMargin(System.Windows.Forms.ToolStripRenderEventArgs e)
        {
            // Sin margen izquierdo retro de iconos
        }

        protected override void OnRenderMenuItemBackground(System.Windows.Forms.ToolStripItemRenderEventArgs e)
        {
            if (!e.Item.Enabled) return;

            e.Graphics.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.AntiAlias;
            if (e.Item.Selected)
            {
                bool isDanger = "Danger".Equals(e.Item.Tag);
                var rect = new System.Drawing.Rectangle(4, 2, e.Item.Width - 8, e.Item.Height - 4);
                using (var path = CreateRoundedRect(rect, 4))
                {
                    using (var brush = new System.Drawing.SolidBrush(isDanger ? _hoverDangerBg : _hoverBg))
                    {
                        e.Graphics.FillPath(brush, path);
                    }
                    using (var pen = new System.Drawing.Pen(isDanger ? _hoverDangerBg : _hoverBorder, 1f))
                    {
                        e.Graphics.DrawPath(pen, path);
                    }
                }
            }
        }

        protected override void OnRenderSeparator(System.Windows.Forms.ToolStripSeparatorRenderEventArgs e)
        {
            int y = e.Item.Height / 2;
            using (var pen = new System.Drawing.Pen(_separator, 1f))
            {
                e.Graphics.DrawLine(pen, 12, y, e.Item.Width - 12, y);
            }
        }

        protected override void OnRenderItemText(System.Windows.Forms.ToolStripItemTextRenderEventArgs e)
        {
            e.Graphics.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;

            System.Drawing.Color textColor = _textPrimary;
            if (!e.Item.Enabled)
            {
                textColor = _textMuted;
            }
            else if ("Danger".Equals(e.Item.Tag))
            {
                textColor = e.Item.Selected ? System.Drawing.Color.White : _textDanger;
            }
            else if ("Header".Equals(e.Item.Tag))
            {
                textColor = _textHeader;
            }

            var textRect = new System.Drawing.Rectangle(14, e.TextRectangle.Y, e.Item.Width - 28, e.TextRectangle.Height);

            System.Windows.Forms.TextRenderer.DrawText(
                e.Graphics,
                e.Text,
                e.TextFont,
                textRect,
                textColor,
                System.Windows.Forms.TextFormatFlags.Left | System.Windows.Forms.TextFormatFlags.VerticalCenter | System.Windows.Forms.TextFormatFlags.SingleLine
            );
        }

        private static System.Drawing.Drawing2D.GraphicsPath CreateRoundedRect(System.Drawing.Rectangle rect, int radius)
        {
            var path = new System.Drawing.Drawing2D.GraphicsPath();
            int d = radius * 2;
            path.AddArc(rect.X, rect.Y, d, d, 180, 90);
            path.AddArc(rect.Right - d, rect.Y, d, d, 270, 90);
            path.AddArc(rect.Right - d, rect.Bottom - d, d, d, 0, 90);
            path.AddArc(rect.X, rect.Bottom - d, d, d, 90, 90);
            path.CloseFigure();
            return path;
        }
    }

    public class CleanDeskColorTable : System.Windows.Forms.ProfessionalColorTable
    {
        public override System.Drawing.Color MenuBorder { get { return System.Drawing.Color.FromArgb(255, 45, 55, 72); } }
        public override System.Drawing.Color ToolStripDropDownBackground { get { return System.Drawing.Color.FromArgb(255, 26, 32, 44); } }
        public override System.Drawing.Color ImageMarginGradientBegin { get { return System.Drawing.Color.FromArgb(255, 26, 32, 44); } }
        public override System.Drawing.Color ImageMarginGradientMiddle { get { return System.Drawing.Color.FromArgb(255, 26, 32, 44); } }
        public override System.Drawing.Color ImageMarginGradientEnd { get { return System.Drawing.Color.FromArgb(255, 26, 32, 44); } }
        public override System.Drawing.Color SeparatorDark { get { return System.Drawing.Color.FromArgb(255, 45, 55, 72); } }
        public override System.Drawing.Color SeparatorLight { get { return System.Drawing.Color.Transparent; } }
        public override System.Drawing.Color MenuItemSelected { get { return System.Drawing.Color.FromArgb(255, 42, 53, 71); } }
        public override System.Drawing.Color MenuItemBorder { get { return System.Drawing.Color.FromArgb(255, 59, 130, 246); } }
    }
}
