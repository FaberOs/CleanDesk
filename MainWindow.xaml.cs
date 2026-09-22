using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;
using Microsoft.Win32;

namespace CleanDesk
{
    public enum WidgetViewState
    {
        ReadyToScan,
        FullIdle,
        Compact,
        Scanning,
        Complete
    }

    public enum WidgetTab
    {
        Clean,
        Ports,
        Tasks
    }

    public partial class MainWindow : Window
    {
        private WidgetViewState _currentState = WidgetViewState.ReadyToScan;
        private WidgetTab _currentTab = WidgetTab.Clean;
        private bool _isLightMode = false;
        private bool _isReviewOpen = false;
        private List<JunkCategory> _categories = new List<JunkCategory>();

        private ObservableCollection<PortItem> _portsList = new ObservableCollection<PortItem>();
        private ObservableCollection<ProcessGroupItem> _processGroups = new ObservableCollection<ProcessGroupItem>();
        private HashSet<int> _customPorts = new HashSet<int>();
        private bool _isScanningClean = false;
        private bool _isCleaningNow = false;
        private bool _isProcessingCompactClean = false;
        private bool _isScanningPorts = false;
        private bool _isKillingPorts = false;
        private bool _isScanningTasks = false;
        private bool _isKillingTasks = false;
        private bool _isSwitchingCompactFeature = false;
        private bool _isSwitchingTab = false;
        private int _compactFeatureIndex = 0; // 0 = Clean, 1 = Ports, 2 = Tasks

        private System.Windows.Forms.NotifyIcon _notifyIcon = null;
        private System.Windows.Forms.ContextMenuStrip _trayMenu = null;
        private bool _isExplicitExit = false;

        private DispatcherTimer _arcTimer;
        private double _currentArcProgress = 0;
        private double _targetArcProgress = 0;
        private double _baseTop = double.NaN;

        public MainWindow()
        {
            InitializeComponent();
            Loaded += MainWindow_Loaded;
            SizeChanged += MainWindow_SizeChanged;
            SystemEvents.UserPreferenceChanged += SystemEvents_UserPreferenceChanged;
            Closed += MainWindow_Closed;
        }

        protected override void OnClosing(System.ComponentModel.CancelEventArgs e)
        {
            if (!_isExplicitExit)
            {
                e.Cancel = true;
                HideWidget();
                return;
            }
            DisposeNotifyIcon();
            base.OnClosing(e);
        }

        private void MainWindow_Closed(object sender, EventArgs e)
        {
            if (_arcTimer != null)
            {
                _arcTimer.Stop();
            }
            SystemEvents.UserPreferenceChanged -= SystemEvents_UserPreferenceChanged;
            DisposeNotifyIcon();
        }

        private void SystemEvents_UserPreferenceChanged(object sender, UserPreferenceChangedEventArgs e)
        {
            Dispatcher.Invoke(new Action(() =>
            {
                ApplySystemTheme();
            }));
        }

        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            InitArcTimer();

            try
            {
                this.Icon = AppIconHelper.GetAppImageSource();
                AppIconHelper.EnsureIconFiles();
                AppIconHelper.UpdateDesktopShortcut();
                InitNotifyIcon();
            }
            catch { }

            var workArea = SystemParameters.WorkArea;
            this.Left = Math.Max(16, workArea.Right - this.ActualWidth - 24);
            this.Top = Math.Max(16, workArea.Bottom - this.ActualHeight - 24);
            _baseTop = this.Top;

            ApplySystemTheme();
            ApplyLocalization();
            SetViewState(WidgetViewState.ReadyToScan, animate: false);
        }

        private void MainWindow_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            if (!IsLoaded) return;
            var workArea = SystemParameters.WorkArea;

            // Mantener anclaje inferior sobre la barra de tareas solo si sobrepasa la pantalla
            if (this.Top + this.ActualHeight > workArea.Bottom - 16)
            {
                this.Top = Math.Max(workArea.Top + 16, workArea.Bottom - this.ActualHeight - 16);
            }
            if (this.Top < workArea.Top + 16)
            {
                this.Top = workArea.Top + 16;
            }
        }

        private void Window_MouseDown(object sender, MouseButtonEventArgs e)
        {
            if (e.ChangedButton == MouseButton.Left)
            {
                this.DragMove();
                if (!_isReviewOpen)
                {
                    _baseTop = this.Top;
                }
                else
                {
                    _baseTop = this.Top;
                }
            }
        }

        #region Theme Engine (Mockups 1 & 2)

        private static bool DetectSystemLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(@"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize"))
                {
                    if (key != null)
                    {
                        object val = key.GetValue("AppsUseLightTheme");
                        if (val is int) return (int)val == 1;
                    }
                }
            }
            catch { }
            return false;
        }

        private void ApplySystemTheme()
        {
            ApplyTheme(DetectSystemLightTheme());
        }

        private void ApplyTheme(bool lightMode)
        {
            _isLightMode = lightMode;

            if (lightMode)
            {
                // Mockup 1: Light Mode
                SetColor("BrushWindowBg", "#FFFFFF");
                SetColor("BrushWindowBorder", "#E2E8F0");
                SetColor("BrushTextPrimary", "#0F172A");
                SetColor("BrushTextSecondary", "#64748B");
                SetColor("BrushTextMuted", "#94A3B8");
                SetColor("BrushAccent", "#1D72FE");
                SetColor("BrushAccentHover", "#1562DF");
                SetColor("BrushReviewBg", "#F1F5F9");
                SetColor("BrushReviewHover", "#E2E8F0");
                SetColor("BrushReviewText", "#0F172A");
                SetColor("BrushSubtleHover", "#10000000");

                SetColor("BrushMenuBg", "#FFFFFF");
                SetColor("BrushMenuBorder", "#E2E8F0");
                SetColor("BrushMenuItemHover", "#F1F5F9");

                SetColor("BrushRingTrack", "#E2E8F0");
                SetColor("BrushSuccessBg", "#DCFCE7");
                SetColor("BrushSuccessCheck", "#16A34A");
                SetColor("BrushSuccessSparkle", "#86EFAC");
                SetColor("BrushFolderGlow", "#0F2563EB");

                // Ports & Guardian status colors (Light Mode)
                SetColor("BrushDanger", "#DC2626");
                SetColor("BrushDangerHover", "#B91C1C");
                SetColor("BrushDangerBg", "#FEE2E2");
                SetColor("BrushWarning", "#D97706");
                SetColor("BrushWarningBg", "#FEF3C7");
                SetColor("BrushPortFreeBg", "#DCFCE7");
                SetColor("BrushPortFreeText", "#16A34A");

                WindowShadow.Color = (Color)ColorConverter.ConvertFromString("#64748B");
                WindowShadow.Opacity = 0.18;
            }
            else
            {
                // Mockup 2: Dark Mode
                SetColor("BrushWindowBg", "#1E232D");
                SetColor("BrushWindowBorder", "#2C3341");
                SetColor("BrushTextPrimary", "#FFFFFF");
                SetColor("BrushTextSecondary", "#94A3B8");
                SetColor("BrushTextMuted", "#64748B");
                SetColor("BrushAccent", "#1D72FE");
                SetColor("BrushAccentHover", "#1562DF");
                SetColor("BrushReviewBg", "#2A303C");
                SetColor("BrushReviewHover", "#353D4C");
                SetColor("BrushReviewText", "#F1F5F9");
                SetColor("BrushSubtleHover", "#22FFFFFF");

                SetColor("BrushMenuBg", "#242A36");
                SetColor("BrushMenuBorder", "#333C4E");
                SetColor("BrushMenuItemHover", "#2E3646");

                SetColor("BrushRingTrack", "#2B3241");
                SetColor("BrushSuccessBg", "#143E2C");
                SetColor("BrushSuccessCheck", "#4ADE80");
                SetColor("BrushSuccessSparkle", "#34D399");
                SetColor("BrushFolderGlow", "#153B82F6");

                // Ports & Guardian status colors (Dark Mode)
                SetColor("BrushDanger", "#F87171");
                SetColor("BrushDangerHover", "#EF4444");
                SetColor("BrushDangerBg", "#3E1B24");
                SetColor("BrushWarning", "#FBBF24");
                SetColor("BrushWarningBg", "#3D2B14");
                SetColor("BrushPortFreeBg", "#133E2B");
                SetColor("BrushPortFreeText", "#34D399");

                WindowShadow.Color = (Color)ColorConverter.ConvertFromString("#000000");
                WindowShadow.Opacity = 0.38;
            }
        }

        private void SetColor(string resourceKey, string hex)
        {
            this.Resources[resourceKey] = new SolidColorBrush((Color)ColorConverter.ConvertFromString(hex));
        }

        #endregion

        #region Multi-Language Engine

        private void ApplyLocalization()
        {
            long selectedBytes = (_categories != null) ? _categories.Where(c => c.IsSelected).Sum(c => c.Bytes) : 0;
            if (selectedBytes == 0)
            {
                TxtSubtitleFound.Text = Strings.SystemClean;
                TxtFreeUpDesc.Text = Strings.SystemCleanDesc;
            }
            else
            {
                TxtSubtitleFound.Text = Strings.TemporaryFilesFound;
                TxtFreeUpDesc.Text = Strings.FreeUpSpaceDesc;
            }

            TxtReadyTitle.Text = Strings.ReadyToScanTitle;
            TxtReadyDesc.Text = Strings.ReadyToScanDesc;
            TxtScanPCBtn.Text = Strings.ScanPC;
            BtnRescanClean.ToolTip = Strings.RescanClean;

            TxtCleanNowBtn.Text = Strings.CleanNow;
            TxtReviewBtn.Text = _isReviewOpen ? Strings.CloseDetails : Strings.Details;
            UpdateCompactUI();

            TxtScanTitle.Text = Strings.ScanningTitle;
            TxtScanDescription.Text = Strings.ScanningDesc;
            TxtScanNote.Text = Strings.ScanningNote;

            TxtSpaceRecoveredTitle.Text = Strings.SpaceRecovered;
            TxtSpaceRecoveredDesc.Text = Strings.ReadyToGo;

            // Navigation tabs
            TabBtnClean.Content = Strings.TabClean;
            TabBtnPorts.Content = Strings.TabPorts;
            TabBtnTasks.Content = Strings.TabTasks;

            // Ports tab
            TxtPortsHeaderTitle.Text = Strings.IsSpanish ? "Puertos Dev en Escucha" : "Listening Dev Ports";
            TxtKillAllBusyBtn.Text = Strings.KillSelectedPorts;
            TxtAddPortBtn.Text = Strings.AddPort;
            TxtCustomPortPlaceholder.Text = Strings.CustomPortPlaceholder;

            // Tasks tab
            TxtTasksHeaderTitle.Text = Strings.IsSpanish ? "Procesos Dev y RAM" : "Dev Processes & RAM";
            TxtAlertBannerTitle.Text = Strings.RunawayAlertTitle;
            TxtAlertBannerDesc.Text = Strings.RunawayAlertDesc;
            TxtKillRunawaysBtn.Text = Strings.KillAllRunaways;

            // Actualizar textos de categorías
            if (_categories != null)
            {
                foreach (var cat in _categories)
                {
                    cat.Name = Strings.GetCategoryName(cat.Id);
                    cat.Description = Strings.GetCategoryDesc(cat.Id);
                }
                ItemsCategories.ItemsSource = null;
                ItemsCategories.ItemsSource = _categories;
            }
        }

        #endregion

        #region State Machine & Organic Animations

        private FrameworkElement GetStateElement(WidgetViewState state)
        {
            switch (state)
            {
                case WidgetViewState.ReadyToScan: return StateReadyToScan;
                case WidgetViewState.FullIdle: return StateFullIdle;
                case WidgetViewState.Compact: return CompactWidgetContainer;
                case WidgetViewState.Scanning: return StateScanning;
                case WidgetViewState.Complete: return StateComplete;
                default: return StateReadyToScan;
            }
        }

        private TranslateTransform GetStateTransform(WidgetViewState state)
        {
            switch (state)
            {
                case WidgetViewState.ReadyToScan: return TransReadyToScan;
                case WidgetViewState.FullIdle: return TransFullIdle;
                case WidgetViewState.Compact: return TransCompact;
                case WidgetViewState.Scanning: return TransScanning;
                case WidgetViewState.Complete: return TransComplete;
                default: return TransReadyToScan;
            }
        }

        private void SetViewState(WidgetViewState newState, bool animate = true)
        {
            if (_currentState == newState && IsLoaded) return;
            var oldState = _currentState;
            _currentState = newState;

            var oldElem = GetStateElement(oldState);
            var newElem = GetStateElement(newState);
            var newTrans = GetStateTransform(newState);

            if (!animate || !IsLoaded)
            {
                StateReadyToScan.Visibility = (newState == WidgetViewState.ReadyToScan) ? Visibility.Visible : Visibility.Collapsed;
                StateFullIdle.Visibility = (newState == WidgetViewState.FullIdle) ? Visibility.Visible : Visibility.Collapsed;
                CompactWidgetContainer.Visibility = (newState == WidgetViewState.Compact) ? Visibility.Visible : Visibility.Collapsed;
                StateScanning.Visibility = (newState == WidgetViewState.Scanning) ? Visibility.Visible : Visibility.Collapsed;
                StateComplete.Visibility = (newState == WidgetViewState.Complete) ? Visibility.Visible : Visibility.Collapsed;
                ActionButtonsRow.Visibility = (newState == WidgetViewState.FullIdle) ? Visibility.Visible : Visibility.Collapsed;

                StateReadyToScan.Opacity = (newState == WidgetViewState.ReadyToScan) ? 1 : 0;
                StateFullIdle.Opacity = (newState == WidgetViewState.FullIdle) ? 1 : 0;
                CompactWidgetContainer.Opacity = (newState == WidgetViewState.Compact) ? 1 : 0;
                StateScanning.Opacity = (newState == WidgetViewState.Scanning) ? 1 : 0;
                StateComplete.Opacity = (newState == WidgetViewState.Complete) ? 1 : 0;

                if (newState == WidgetViewState.Compact)
                {
                    this.Width = 330;
                    NavigationBarBorder.Visibility = Visibility.Collapsed;
                    FullContentContainer.Visibility = Visibility.Collapsed;
                    BtnExpandCompact.Visibility = Visibility.Visible;
                    PanelReviewDrawer.Visibility = Visibility.Collapsed;
                    _isReviewOpen = false;
                    SwitchCompactFeature(_compactFeatureIndex, animate: false);
                }
                else
                {
                    this.Width = 396;
                    NavigationBarBorder.Visibility = Visibility.Visible;
                    FullContentContainer.Visibility = Visibility.Visible;
                    BtnExpandCompact.Visibility = Visibility.Collapsed;
                }
                return;
            }

            // Animación suave de ancho entre vista Completa y Compacta
            double targetWidth = (newState == WidgetViewState.Compact) ? 330 : 396;
            if (Math.Abs(this.Width - targetWidth) > 1)
            {
                var animW = new DoubleAnimation(targetWidth, TimeSpan.FromMilliseconds(260))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                this.BeginAnimation(Window.WidthProperty, animW);
            }

            // Si se pasa a compacto, cerrar panel de detalles y restaurar baseTop
            if (newState == WidgetViewState.Compact)
            {
                NavigationBarBorder.Visibility = Visibility.Collapsed;
                FullContentContainer.Visibility = Visibility.Collapsed;
                BtnExpandCompact.Visibility = Visibility.Visible;
                PanelReviewDrawer.Visibility = Visibility.Collapsed;
                _isReviewOpen = false;
                SwitchCompactFeature(_compactFeatureIndex, animate: false);
                if (!double.IsNaN(_baseTop))
                {
                    this.Top = _baseTop;
                }
            }
            else
            {
                NavigationBarBorder.Visibility = Visibility.Visible;
                FullContentContainer.Visibility = Visibility.Visible;
                BtnExpandCompact.Visibility = Visibility.Collapsed;
            }

            // Animación de fila de botones de acción (solo visible en FullIdle)
            if (newState == WidgetViewState.FullIdle)
            {
                ActionButtonsRow.Visibility = Visibility.Visible;
                var animBtn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                ActionButtonsRow.BeginAnimation(UIElement.OpacityProperty, animBtn);
            }
            else
            {
                var animBtn = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                animBtn.Completed += (s, e) => ActionButtonsRow.Visibility = Visibility.Collapsed;
                ActionButtonsRow.BeginAnimation(UIElement.OpacityProperty, animBtn);
            }

            // Fade out suave del estado anterior
            if (oldElem != null && oldElem != newElem)
            {
                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(130))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var capturedOld = oldElem;
                fadeOut.Completed += (s, e) =>
                {
                    capturedOld.Visibility = Visibility.Collapsed;
                };
                capturedOld.BeginAnimation(UIElement.OpacityProperty, fadeOut);
            }

            // Fade in y deslizamiento suave del nuevo estado
            newElem.Visibility = Visibility.Visible;
            var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(250))
            {
                EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
            };
            newElem.BeginAnimation(UIElement.OpacityProperty, fadeIn);

            if (newTrans != null)
            {
                newTrans.Y = 8;
                var slideIn = new DoubleAnimation(8, 0, TimeSpan.FromMilliseconds(250))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                newTrans.BeginAnimation(TranslateTransform.YProperty, slideIn);
            }

            // Efecto de pop orgánico con BackEase en StateComplete
            if (newState == WidgetViewState.Complete)
            {
                var scaleAnim = new DoubleAnimation(0.4, 1.0, TimeSpan.FromMilliseconds(420))
                {
                    EasingFunction = new BackEase { Amplitude = 0.4, EasingMode = EasingMode.EaseOut }
                };
                ScaleCompleteBadge.BeginAnimation(ScaleTransform.ScaleXProperty, scaleAnim);
                ScaleCompleteBadge.BeginAnimation(ScaleTransform.ScaleYProperty, scaleAnim);

                var sparkleAnim = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(380))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                Sparkle1.BeginAnimation(UIElement.OpacityProperty, sparkleAnim);
                Sparkle2.BeginAnimation(UIElement.OpacityProperty, sparkleAnim);
                Sparkle3.BeginAnimation(UIElement.OpacityProperty, sparkleAnim);
                Sparkle4.BeginAnimation(UIElement.OpacityProperty, sparkleAnim);
            }

            // Mantener anclaje en pantalla
            this.UpdateLayout();
            var workArea = SystemParameters.WorkArea;
            if (this.Top + this.ActualHeight > workArea.Bottom - 16)
            {
                this.Top = Math.Max(workArea.Top + 16, workArea.Bottom - this.ActualHeight - 16);
            }
        }

        #endregion

        #region Progress Ring Engine (Smooth 60 FPS Arc)

        private void InitArcTimer()
        {
            if (_arcTimer != null) return;
            _arcTimer = new DispatcherTimer { Interval = TimeSpan.FromMilliseconds(16) };
            _arcTimer.Tick += (s, e) =>
            {
                double diff = _targetArcProgress - _currentArcProgress;
                if (Math.Abs(diff) < 0.4)
                {
                    _currentArcProgress = _targetArcProgress;
                    _arcTimer.Stop();
                }
                else
                {
                    _currentArcProgress += diff * 0.22;
                }

                TxtScanPercent.Text = string.Format("{0}%", (int)Math.Round(_currentArcProgress));
                DrawArcGeometry(_currentArcProgress);
            };
        }

        private void SetProgressArc(int percentage)
        {
            _targetArcProgress = Math.Max(0, Math.Min(100, percentage));
            if (_arcTimer == null) InitArcTimer();
            if (!_arcTimer.IsEnabled) _arcTimer.Start();
        }

        private void ResetProgressArc()
        {
            if (_arcTimer != null) _arcTimer.Stop();
            _currentArcProgress = 0;
            _targetArcProgress = 0;
            TxtScanPercent.Text = "0%";
            ProgressArc.Data = null;
        }

        private void DrawArcGeometry(double percentage)
        {
            if (percentage <= 0.5)
            {
                ProgressArc.Data = null;
                return;
            }

            double angle = Math.Min(359.99, (percentage / 100.0) * 360.0);
            double rad = (angle - 90.0) * (Math.PI / 180.0);
            double radius = 32.0;
            Point center = new Point(40, 40);
            Point start = new Point(40, 8);
            Point end = new Point(center.X + radius * Math.Cos(rad), center.Y + radius * Math.Sin(rad));

            var figure = new PathFigure
            {
                StartPoint = start,
                IsClosed = false
            };
            figure.Segments.Add(new ArcSegment
            {
                Point = end,
                Size = new Size(radius, radius),
                IsLargeArc = angle > 180.0,
                SweepDirection = SweepDirection.Clockwise
            });

            var geo = new PathGeometry();
            geo.Figures.Add(figure);
            ProgressArc.Data = geo;
        }

        private void StartRotateAnimation(UIElement element)
        {
            if (element == null) return;
            var rotate = new RotateTransform();
            element.RenderTransform = rotate;
            element.RenderTransformOrigin = new Point(0.5, 0.5);

            var anim = new DoubleAnimation(0, 360, TimeSpan.FromMilliseconds(750))
            {
                RepeatBehavior = RepeatBehavior.Forever
            };
            rotate.BeginAnimation(RotateTransform.AngleProperty, anim);
        }

        private void StopRotateAnimation(UIElement element)
        {
            if (element == null) return;
            var rotate = element.RenderTransform as RotateTransform;
            if (rotate != null)
            {
                rotate.BeginAnimation(RotateTransform.AngleProperty, null);
            }
            element.RenderTransform = null;
        }

        #endregion

        #region Scanning & Cleaning

        private async Task TriggerScanAsync()
        {
            if (_isScanningClean || _isCleaningNow || _isProcessingCompactClean) return;
            _isScanningClean = true;

            BtnScanPC.IsEnabled = false;
            BtnRescanClean.IsEnabled = false;
            BtnCleanNow.IsEnabled = false;
            BtnReview.IsEnabled = false;
            StartRotateAnimation(IcoRescanClean);

            ResetProgressArc();
            SetViewState(WidgetViewState.Scanning);
            TxtScanTitle.Text = Strings.ScanningTitle;
            TxtScanDescription.Text = Strings.ScanningDesc;
            SetProgressArc(10);

            try
            {
                var scanResult = await CleanerEngine.ScanAsync((pct, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        SetProgressArc(pct);
                    }));
                });

                _categories = scanResult.Categories;
                foreach (var cat in _categories)
                {
                    cat.Name = Strings.GetCategoryName(cat.Id);
                    cat.Description = Strings.GetCategoryDesc(cat.Id);
                }
                ItemsCategories.ItemsSource = null;
                ItemsCategories.ItemsSource = _categories;

                UpdateStats();
            }
            catch
            {
                TxtTotalSize.Text = "0 MB";
            }
            finally
            {
                _isScanningClean = false;
                BtnScanPC.IsEnabled = true;
                BtnRescanClean.IsEnabled = true;
                BtnReview.IsEnabled = true;
                StopRotateAnimation(IcoRescanClean);
                SetViewState(WidgetViewState.FullIdle);
                UpdateStats();
            }
        }

        private void UpdateStats()
        {
            long selectedBytes = _categories.Where(c => c.IsSelected).Sum(c => c.Bytes);
            long selectedFiles = _categories.Where(c => c.IsSelected).Sum(c => c.FileCount);

            string formattedSize = CleanerEngine.FormatBytes(selectedBytes);
            TxtTotalSize.Text = formattedSize;
            TxtFileCount.Text = Strings.InFiles(selectedFiles);

            if (selectedBytes == 0)
            {
                TxtSubtitleFound.Text = Strings.SystemClean;
                TxtFreeUpDesc.Text = Strings.SystemCleanDesc;
            }
            else
            {
                TxtSubtitleFound.Text = Strings.TemporaryFilesFound;
                TxtFreeUpDesc.Text = Strings.FreeUpSpaceDesc;
            }

            if (BtnCleanNow != null)
            {
                BtnCleanNow.IsEnabled = (selectedBytes > 0);
                BtnCleanNow.Opacity = (selectedBytes > 0) ? 1.0 : 0.5;
            }

            UpdateCompactUI();
        }

        private async void BtnCleanNow_Click(object sender, RoutedEventArgs e)
        {
            if (_isCleaningNow || _isScanningClean || _isProcessingCompactClean) return;
            if (_categories == null || _categories.Count == 0) return;
            long selectedBytes = _categories.Where(c => c.IsSelected).Sum(c => c.Bytes);
            if (selectedBytes == 0) return;

            _isCleaningNow = true;
            BtnCleanNow.IsEnabled = false;
            BtnReview.IsEnabled = false;
            BtnRescanClean.IsEnabled = false;

            ResetProgressArc();
            SetViewState(WidgetViewState.Scanning);
            TxtScanTitle.Text = Strings.CleaningTitle;
            TxtScanDescription.Text = Strings.CleaningDesc;
            SetProgressArc(10);

            try
            {
                long freedBytes = await CleanerEngine.CleanCategoriesAsync(_categories, (pct, msg) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        SetProgressArc(pct);
                    }));
                });

                // Limpiar estados: poner categorías seleccionadas en 0 MB inmediatamente
                foreach (var cat in _categories.Where(c => c.IsSelected))
                {
                    cat.Bytes = 0;
                    cat.FileCount = 0;
                }

                // Refrescar el ItemsControl para que los ítems en detalles muestren 0 MB
                ItemsCategories.ItemsSource = null;
                ItemsCategories.ItemsSource = _categories;

                // Actualizar contadores del widget a 0 MB
                UpdateStats();

                // Mostrar pantalla de completado con estadísticas de lo liberado
                string freedFormatted = CleanerEngine.FormatBytes(freedBytes);
                TxtCompleteStats.Text = Strings.CleanedStats(freedFormatted);

                SetViewState(WidgetViewState.Complete);

                // Esperar 2.8 segundos mostrando el checkmark verde con animación pop
                await Task.Delay(2800);

                // Volver suavemente a FullIdle mostrando el estado optimizado (0 MB)
                SetViewState(WidgetViewState.FullIdle);
            }
            catch
            {
                SetViewState(WidgetViewState.FullIdle);
            }
            finally
            {
                _isCleaningNow = false;
                BtnReview.IsEnabled = true;
                BtnRescanClean.IsEnabled = true;
                UpdateStats();
            }
        }

        private void BtnReview_Click(object sender, RoutedEventArgs e)
        {
            var workArea = SystemParameters.WorkArea;

            // Detener cualquier animación previa para evitar colisiones por clics rápidos
            PanelReviewDrawer.BeginAnimation(UIElement.OpacityProperty, null);
            TransDrawer.BeginAnimation(TranslateTransform.YProperty, null);

            // Si la posición base no se ha guardado o el cajón estaba cerrado, registrar el Top actual
            if (double.IsNaN(_baseTop) || !_isReviewOpen)
            {
                _baseTop = this.Top;
            }

            _isReviewOpen = !_isReviewOpen;
            TxtReviewBtn.Text = _isReviewOpen ? Strings.CloseDetails : Strings.Details;

            if (_isReviewOpen)
            {
                // --- DESPLEGAR DETALLES ---
                PanelReviewDrawer.Visibility = Visibility.Visible;
                PanelReviewDrawer.Opacity = 0;
                TransDrawer.Y = -10;

                // Forzar cálculo de diseño para conocer la altura total expandida
                this.UpdateLayout();
                double expandedHeight = this.ActualHeight;

                // Solo si la expansión chocaría con la barra de tareas, mover hacia arriba lo necesario
                double maxAllowedBottom = workArea.Bottom - 16;
                if (_baseTop + expandedHeight > maxAllowedBottom)
                {
                    this.Top = Math.Max(workArea.Top + 16, maxAllowedBottom - expandedHeight);
                }
                else
                {
                    this.Top = _baseTop;
                }

                var animFade = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var animSlide = new DoubleAnimation(-10, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                PanelReviewDrawer.BeginAnimation(UIElement.OpacityProperty, animFade);
                TransDrawer.BeginAnimation(TranslateTransform.YProperty, animSlide);
            }
            else
            {
                // --- CONTRAER DETALLES ---
                var animFade = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(140))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var animSlide = new DoubleAnimation(0, -6, TimeSpan.FromMilliseconds(140))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };

                animFade.Completed += (s, ev) =>
                {
                    if (!_isReviewOpen)
                    {
                        PanelReviewDrawer.Visibility = Visibility.Collapsed;
                        this.UpdateLayout();

                        // Restaurar con total fidelidad la posición base sin que salte ni se desplace
                        if (!double.IsNaN(_baseTop))
                        {
                            double maxAllowedTop = Math.Max(workArea.Top + 16, workArea.Bottom - this.ActualHeight - 16);
                            this.Top = Math.Min(_baseTop, maxAllowedTop);
                        }
                    }
                };

                PanelReviewDrawer.BeginAnimation(UIElement.OpacityProperty, animFade);
                TransDrawer.BeginAnimation(TranslateTransform.YProperty, animSlide);
            }
        }

        private async void BtnExpandCompact_Click(object sender, RoutedEventArgs e)
        {
            if (_currentState != WidgetViewState.Compact) return;

            SetViewState((_categories != null && _categories.Count > 0) ? WidgetViewState.FullIdle : WidgetViewState.ReadyToScan);

            WidgetTab targetTab = WidgetTab.Clean;
            if (_compactFeatureIndex == 1) targetTab = WidgetTab.Ports;
            else if (_compactFeatureIndex == 2) targetTab = WidgetTab.Tasks;

            TabBtnClean.IsChecked = (targetTab == WidgetTab.Clean);
            TabBtnPorts.IsChecked = (targetTab == WidgetTab.Ports);
            TabBtnTasks.IsChecked = (targetTab == WidgetTab.Tasks);

            await SwitchTabAsync(targetTab);
        }

        #region Compact Feature Carousel & Actions

        private void SwitchCompactFeature(int targetIndex, bool animate = true)
        {
            if (_isSwitchingCompactFeature) return;
            if (targetIndex < 0) targetIndex = 2;
            if (targetIndex > 2) targetIndex = 0;

            int oldIndex = _compactFeatureIndex;
            if (oldIndex == targetIndex && IsLoaded && _currentState == WidgetViewState.Compact) return;

            _compactFeatureIndex = targetIndex;
            if (animate && IsLoaded && oldIndex != targetIndex)
            {
                _isSwitchingCompactFeature = true;
            }

            FrameworkElement[] cards = new FrameworkElement[] { CompactCardClean, CompactCardPorts, CompactCardTasks };
            TranslateTransform[] trans = new TranslateTransform[] { TransCompactClean, TransCompactPorts, TransCompactTasks };
            Border[] dots = new Border[] { DotCompactClean, DotCompactPorts, DotCompactTasks };

            // Detener cualquier animación previa para evitar colisiones
            for (int i = 0; i < cards.Length; i++)
            {
                cards[i].BeginAnimation(UIElement.OpacityProperty, null);
                if (trans[i] != null) trans[i].BeginAnimation(TranslateTransform.XProperty, null);
            }

            var accentBrush = (Brush)this.FindResource("BrushAccent");
            var mutedBrush = (Brush)this.FindResource("BrushTextMuted");

            // Update Dot/Pill Indicators (active is a 14px pill, inactive is 4px)
            for (int i = 0; i < dots.Length; i++)
            {
                bool isActive = (i == _compactFeatureIndex);
                if (animate && IsLoaded)
                {
                    var animWidth = new DoubleAnimation(isActive ? 14 : 4, TimeSpan.FromMilliseconds(180))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    dots[i].BeginAnimation(Border.WidthProperty, animWidth);
                }
                else
                {
                    dots[i].Width = isActive ? 14 : 4;
                }
                dots[i].Background = isActive ? accentBrush : mutedBrush;
                dots[i].Opacity = isActive ? 1.0 : 0.45;
            }

            // Cross-fade & horizontal slide transition between feature cards
            if (oldIndex != targetIndex && animate && IsLoaded)
            {
                int dir = (targetIndex > oldIndex || (oldIndex == 2 && targetIndex == 0)) && !(oldIndex == 0 && targetIndex == 2) ? 1 : -1;

                var oldCard = cards[oldIndex];
                var oldTrans = trans[oldIndex];
                var newCard = cards[targetIndex];
                var newTrans = trans[targetIndex];

                // Asegurar que tarjetas no involucradas permanezcan ocultas
                for (int i = 0; i < cards.Length; i++)
                {
                    if (i != oldIndex && i != targetIndex)
                    {
                        cards[i].Visibility = Visibility.Collapsed;
                        cards[i].Opacity = 0;
                    }
                }

                var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(130))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var transOut = new DoubleAnimation(0, -10 * dir, TimeSpan.FromMilliseconds(130))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                };
                var capturedOld = oldCard;
                fadeOut.Completed += (s, ev) =>
                {
                    capturedOld.Visibility = Visibility.Collapsed;
                };
                oldCard.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                oldTrans.BeginAnimation(TranslateTransform.XProperty, transOut);

                newCard.Visibility = Visibility.Visible;
                newTrans.X = 10 * dir;
                var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                var transIn = new DoubleAnimation(10 * dir, 0, TimeSpan.FromMilliseconds(220))
                {
                    EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                };
                fadeIn.Completed += (s, ev) =>
                {
                    _isSwitchingCompactFeature = false;
                };
                newCard.BeginAnimation(UIElement.OpacityProperty, fadeIn);
                newTrans.BeginAnimation(TranslateTransform.XProperty, transIn);
            }
            else
            {
                _isSwitchingCompactFeature = false;
                for (int i = 0; i < cards.Length; i++)
                {
                    bool isCurrent = (i == _compactFeatureIndex);
                    cards[i].Visibility = isCurrent ? Visibility.Visible : Visibility.Collapsed;
                    cards[i].Opacity = isCurrent ? 1 : 0;
                    if (trans[i] != null) trans[i].X = 0;
                }
            }

            UpdateCompactUI();

            // Lazy scan if feature opened without data
            if (_compactFeatureIndex == 1 && _portsList.Count == 0 && !_isScanningPorts && !_isKillingPorts)
            {
                var ignored = RefreshPortsAsync();
            }
            else if (_compactFeatureIndex == 2 && _processGroups.Count == 0 && !_isScanningTasks && !_isKillingTasks)
            {
                var ignored = RefreshTasksAsync();
            }
        }

        private void UpdateCompactUI()
        {
            if (TxtCompactCleanSize == null) return;

            // 1. Clean Feature
            long selectedBytes = (_categories != null) ? _categories.Where(c => c.IsSelected).Sum(c => c.Bytes) : 0;
            string formattedSize = CleanerEngine.FormatBytes(selectedBytes);
            TxtCompactCleanSize.Text = formattedSize;
            TxtCompactCleanTag.Text = Strings.IsSpanish ? "LIMPIEZA" : "CLEANER";

            if (_isProcessingCompactClean)
            {
                // En progreso de escaneo o limpieza compacta: conservar texto actual
            }
            else if (_categories == null || _categories.Count == 0)
            {
                TxtCompactCleanDesc.Text = Strings.IsSpanish ? "Sin escanear" : "Not scanned";
                TxtCompactCleanAction.Text = Strings.IsSpanish ? "Scan" : "Scan";
                IcoCompactCleanAction.Text = "\uE721";
                BtnCompactCleanAction.Background = (Brush)this.FindResource("BrushAccent");
                BtnCompactCleanAction.Visibility = Visibility.Visible;
            }
            else if (selectedBytes > 0)
            {
                TxtCompactCleanDesc.Text = Strings.IsSpanish ? "Liberable ahora" : "Ready to clean";
                TxtCompactCleanAction.Text = Strings.IsSpanish ? "Limpiar" : "Clean";
                IcoCompactCleanAction.Text = "\uE74C";
                BtnCompactCleanAction.Background = (Brush)this.FindResource("BrushAccent");
                BtnCompactCleanAction.Visibility = Visibility.Visible;
            }
            else
            {
                TxtCompactCleanDesc.Text = Strings.IsSpanish ? "Sistema optimizado" : "System clean";
                TxtCompactCleanAction.Text = Strings.IsSpanish ? "Scan" : "Scan";
                IcoCompactCleanAction.Text = "\uE721";
                BtnCompactCleanAction.Background = (Brush)this.FindResource("BrushReviewBg");
                BtnCompactCleanAction.Visibility = Visibility.Visible;
            }

            // 2. Ports Feature
            TxtCompactPortsTag.Text = Strings.IsSpanish ? "PUERTOS DEV" : "DEV PORTS";
            if (_isScanningPorts)
            {
                TxtCompactPortsTitle.Text = Strings.IsSpanish ? "Escaneando..." : "Scanning...";
                TxtCompactPortsDesc.Text = Strings.IsSpanish ? "Buscando procesos..." : "Checking ports...";
                BtnCompactPortsAction.Visibility = Visibility.Collapsed;
            }
            else
            {
                int busyCount = _portsList.Count(p => p.IsActive);
                if (busyCount == 0)
                {
                    TxtCompactPortsTitle.Text = Strings.IsSpanish ? "Todos libres" : "All free";
                    TxtCompactPortsDesc.Text = Strings.IsSpanish ? "Sin puertos ocupados" : "No busy dev ports";
                    BtnCompactPortsAction.Visibility = Visibility.Collapsed;
                }
                else
                {
                    TxtCompactPortsTitle.Text = Strings.IsSpanish 
                        ? string.Format("{0} ocupado{1}", busyCount, busyCount > 1 ? "s" : "") 
                        : string.Format("{0} busy", busyCount);

                    var activePortNumbers = _portsList.Where(p => p.IsActive).Take(3).Select(p => ":" + p.PortNumber);
                    TxtCompactPortsDesc.Text = string.Join(", ", activePortNumbers);
                    TxtCompactPortsAction.Text = Strings.IsSpanish ? "Kill" : "Kill";
                    BtnCompactPortsAction.Visibility = Visibility.Visible;
                }
            }

            // 3. Tasks & RAM Feature
            TxtCompactTasksTag.Text = Strings.IsSpanish ? "PROCESOS & RAM" : "TASKS & RAM";
            if (_isScanningTasks)
            {
                TxtCompactTasksTitle.Text = Strings.IsSpanish ? "Escaneando..." : "Scanning...";
                TxtCompactTasksDesc.Text = Strings.IsSpanish ? "Analizando memoria..." : "Diagnosing RAM...";
                BtnCompactTasksAction.Visibility = Visibility.Collapsed;
            }
            else
            {
                bool hasRunaways = _processGroups.Any(g => g.IsRunaway);
                long totalMemoryMb = _processGroups.Where(g => g.IsActive).Sum(g => g.TotalMemoryBytes) / (1024 * 1024);
                int totalProcs = _processGroups.Where(g => g.IsActive).Sum(g => g.Count);

                string memStr = (totalMemoryMb >= 1024) 
                    ? string.Format("{0:0.#} GB RAM", totalMemoryMb / 1024.0) 
                    : string.Format("{0} MB RAM", totalMemoryMb);

                TxtCompactTasksTitle.Text = memStr;

                if (hasRunaways)
                {
                    var runawayNames = _processGroups.Where(g => g.IsRunaway).Select(g => g.DisplayName);
                    TxtCompactTasksDesc.Text = Strings.IsSpanish 
                        ? ("Bucle: " + string.Join(", ", runawayNames)) 
                        : ("Runaway: " + string.Join(", ", runawayNames));
                    TxtCompactTasksAction.Text = Strings.IsSpanish ? "Kill" : "Kill";
                    BtnCompactTasksAction.Visibility = Visibility.Visible;
                }
                else
                {
                    TxtCompactTasksDesc.Text = Strings.IsSpanish 
                        ? string.Format("{0} procesos dev", totalProcs) 
                        : string.Format("{0} dev processes", totalProcs);
                    BtnCompactTasksAction.Visibility = Visibility.Collapsed;
                }
            }
        }

        private void BtnCompactPrev_Click(object sender, RoutedEventArgs e)
        {
            SwitchCompactFeature(_compactFeatureIndex - 1);
        }

        private void BtnCompactNext_Click(object sender, RoutedEventArgs e)
        {
            SwitchCompactFeature(_compactFeatureIndex + 1);
        }

        private void DotCompactClean_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwitchCompactFeature(0);
        }

        private void DotCompactPorts_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwitchCompactFeature(1);
        }

        private void DotCompactTasks_MouseDown(object sender, MouseButtonEventArgs e)
        {
            SwitchCompactFeature(2);
        }

        private void CompactWidgetContainer_MouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta < 0)
            {
                SwitchCompactFeature(_compactFeatureIndex + 1);
            }
            else if (e.Delta > 0)
            {
                SwitchCompactFeature(_compactFeatureIndex - 1);
            }
            e.Handled = true;
        }

        private async void BtnCompactCleanAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isProcessingCompactClean || _isScanningClean || _isCleaningNow) return;

            long selectedBytes = (_categories != null) ? _categories.Where(c => c.IsSelected).Sum(c => c.Bytes) : 0;
            if (_categories == null || _categories.Count == 0 || selectedBytes == 0)
            {
                // Escaneo silencioso en modo compacto (permanece 100% en compacto sin expandir)
                _isProcessingCompactClean = true;
                BtnCompactCleanAction.IsEnabled = false;
                TxtCompactCleanDesc.Text = Strings.IsSpanish ? "Escaneando..." : "Scanning...";
                TxtCompactCleanSize.Text = "...";
                IcoCompactCleanAction.Text = "\uE72C";
                StartRotateAnimation(IcoCompactCleanAction);

                try
                {
                    var scanResult = await CleanerEngine.ScanAsync((pct, msg) => { });
                    _categories = scanResult.Categories;
                    foreach (var cat in _categories)
                    {
                        cat.Name = Strings.GetCategoryName(cat.Id);
                        cat.Description = Strings.GetCategoryDesc(cat.Id);
                    }
                    ItemsCategories.ItemsSource = null;
                    ItemsCategories.ItemsSource = _categories;

                    long newSelected = _categories.Where(c => c.IsSelected).Sum(c => c.Bytes);
                    long newFiles = _categories.Where(c => c.IsSelected).Sum(c => c.FileCount);
                    TxtTotalSize.Text = CleanerEngine.FormatBytes(newSelected);
                    TxtFileCount.Text = Strings.InFiles(newFiles);
                    if (newSelected == 0)
                    {
                        TxtSubtitleFound.Text = Strings.SystemClean;
                        TxtFreeUpDesc.Text = Strings.SystemCleanDesc;
                    }
                    else
                    {
                        TxtSubtitleFound.Text = Strings.TemporaryFilesFound;
                        TxtFreeUpDesc.Text = Strings.FreeUpSpaceDesc;
                    }
                }
                catch
                {
                    TxtCompactCleanSize.Text = "0 MB";
                }
                finally
                {
                    StopRotateAnimation(IcoCompactCleanAction);
                    _isProcessingCompactClean = false;
                    BtnCompactCleanAction.IsEnabled = true;
                    UpdateCompactUI();
                }
            }
            else
            {
                // Limpieza silenciosa en modo compacto (permanece 100% en compacto sin expandir)
                _isProcessingCompactClean = true;
                BtnCompactCleanAction.IsEnabled = false;
                TxtCompactCleanDesc.Text = Strings.IsSpanish ? "Limpiando..." : "Cleaning...";
                IcoCompactCleanAction.Text = "\uE72C";
                StartRotateAnimation(IcoCompactCleanAction);

                try
                {
                    long freedBytes = await CleanerEngine.CleanCategoriesAsync(_categories, (pct, msg) => { });

                    foreach (var cat in _categories.Where(c => c.IsSelected))
                    {
                        cat.Bytes = 0;
                        cat.FileCount = 0;
                    }

                    ItemsCategories.ItemsSource = null;
                    ItemsCategories.ItemsSource = _categories;

                    TxtTotalSize.Text = "0 MB";
                    TxtFileCount.Text = Strings.InFiles(0);
                    TxtSubtitleFound.Text = Strings.SystemClean;
                    TxtFreeUpDesc.Text = Strings.SystemCleanDesc;
                    TxtCompleteStats.Text = Strings.CleanedStats(CleanerEngine.FormatBytes(freedBytes));
                }
                catch
                {
                    // Fallback
                }
                finally
                {
                    StopRotateAnimation(IcoCompactCleanAction);
                    _isProcessingCompactClean = false;
                    BtnCompactCleanAction.IsEnabled = true;
                    UpdateCompactUI();
                }
            }
        }

        private async void BtnCompactPortsAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingPorts || _isScanningPorts) return;
            var busy = _portsList.Where(p => p.IsActive).ToList();
            if (busy.Count == 0) return;

            _isKillingPorts = true;
            BtnCompactPortsAction.IsEnabled = false;
            TxtCompactPortsDesc.Text = Strings.IsSpanish ? "Cerrando puertos..." : "Freeing ports...";

            try
            {
                await PortManager.KillSelectedPortsAsync(busy);
                await RefreshPortsAsync();
            }
            finally
            {
                _isKillingPorts = false;
                BtnCompactPortsAction.IsEnabled = true;
                UpdateCompactUI();
            }
        }

        private async void BtnCompactTasksAction_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingTasks || _isScanningTasks) return;
            var runaways = _processGroups.Where(g => g.IsRunaway).ToList();
            if (runaways.Count == 0) return;

            _isKillingTasks = true;
            BtnCompactTasksAction.IsEnabled = false;
            TxtCompactTasksDesc.Text = Strings.IsSpanish ? "Terminando bucles..." : "Killing runaways...";

            try
            {
                await ProcessGuardian.KillAllRunawaysAsync(runaways);
                await RefreshTasksAsync();
            }
            finally
            {
                _isKillingTasks = false;
                BtnCompactTasksAction.IsEnabled = true;
                UpdateCompactUI();
            }
        }

        #endregion

        private async void BtnScanPC_Click(object sender, RoutedEventArgs e)
        {
            await TriggerScanAsync();
        }

        private async void BtnRescanClean_Click(object sender, RoutedEventArgs e)
        {
            await TriggerScanAsync();
        }

        public void OnCategoryCheckChanged(object sender, RoutedEventArgs e)
        {
            UpdateStats();
        }

        #endregion

        #region Tab Navigation Engine

        private async void TabBtn_Click(object sender, RoutedEventArgs e)
        {
            var rb = sender as RadioButton;
            if (rb == null || rb.Tag == null) return;
            string tag = rb.Tag.ToString();

            WidgetTab targetTab;
            if (tag == "Ports") targetTab = WidgetTab.Ports;
            else if (tag == "Tasks") targetTab = WidgetTab.Tasks;
            else targetTab = WidgetTab.Clean;

            if (_currentTab == targetTab && IsLoaded) return;
            await SwitchTabAsync(targetTab);
        }

        private async Task SwitchTabAsync(WidgetTab newTab)
        {
            if (_isSwitchingTab) return;
            _isSwitchingTab = true;

            try
            {
                var oldTab = _currentTab;
                _currentTab = newTab;

                if (newTab == WidgetTab.Clean) _compactFeatureIndex = 0;
                else if (newTab == WidgetTab.Ports) _compactFeatureIndex = 1;
                else if (newTab == WidgetTab.Tasks) _compactFeatureIndex = 2;

                FrameworkElement oldElem = GetTabElement(oldTab);
                FrameworkElement newElem = GetTabElement(newTab);
                TranslateTransform newTrans = GetTabTransform(newTab);

                // Cerrar el cajón de detalles si salimos de la pestaña Clean
                if (oldTab == WidgetTab.Clean && _isReviewOpen)
                {
                    PanelReviewDrawer.Visibility = Visibility.Collapsed;
                    _isReviewOpen = false;
                    TxtReviewBtn.Text = Strings.Details;
                }

                if (oldElem != null && oldElem != newElem)
                {
                    var fadeOut = new DoubleAnimation(1, 0, TimeSpan.FromMilliseconds(120))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseIn }
                    };
                    var captured = oldElem;
                    fadeOut.Completed += (s, e) => { captured.Visibility = Visibility.Collapsed; };
                    captured.BeginAnimation(UIElement.OpacityProperty, fadeOut);
                }

                if (newElem != null)
                {
                    newElem.Visibility = Visibility.Visible;
                    var fadeIn = new DoubleAnimation(0, 1, TimeSpan.FromMilliseconds(220))
                    {
                        EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                    };
                    newElem.BeginAnimation(UIElement.OpacityProperty, fadeIn);

                    if (newTrans != null)
                    {
                        newTrans.Y = 6;
                        var slideIn = new DoubleAnimation(6, 0, TimeSpan.FromMilliseconds(220))
                        {
                            EasingFunction = new CubicEase { EasingMode = EasingMode.EaseOut }
                        };
                        newTrans.BeginAnimation(TranslateTransform.YProperty, slideIn);
                    }
                }

                // Anclaje en pantalla para evitar saltos o colisiones con la barra de tareas
                this.UpdateLayout();
                var workArea = SystemParameters.WorkArea;
                if (this.Top + this.ActualHeight > workArea.Bottom - 16)
                {
                    this.Top = Math.Max(workArea.Top + 16, workArea.Bottom - this.ActualHeight - 16);
                }

                if (newTab == WidgetTab.Ports)
                {
                    await RefreshPortsAsync();
                }
                else if (newTab == WidgetTab.Tasks)
                {
                    await RefreshTasksAsync();
                }
            }
            finally
            {
                _isSwitchingTab = false;
            }
        }

        private FrameworkElement GetTabElement(WidgetTab tab)
        {
            switch (tab)
            {
                case WidgetTab.Clean: return TabCleanContainer;
                case WidgetTab.Ports: return TabPortsContainer;
                case WidgetTab.Tasks: return TabTasksContainer;
                default: return TabCleanContainer;
            }
        }

        private TranslateTransform GetTabTransform(WidgetTab tab)
        {
            switch (tab)
            {
                case WidgetTab.Clean: return TransCleanTab;
                case WidgetTab.Ports: return TransPortsTab;
                case WidgetTab.Tasks: return TransTasksTab;
                default: return TransCleanTab;
            }
        }

        #endregion

        #region Ports Engine (KillerPort)

        private async Task RefreshPortsAsync()
        {
            if (_isScanningPorts || _isKillingPorts) return;
            _isScanningPorts = true;
            BtnRefreshPorts.IsEnabled = false;
            BtnAddCustomPort.IsEnabled = false;
            BtnKillAllBusy.IsEnabled = false;
            StartRotateAnimation(IcoRefreshPorts);
            UpdateCompactUI();

            if (ItemsPorts.ItemsSource == null)
            {
                ItemsPorts.ItemsSource = _portsList;
            }

            TxtPortsSummary.Text = Strings.IsSpanish ? "Escaneando puertos dev..." : "Scanning dev ports...";

            try
            {
                var result = await PortManager.ScanPortsAsync(_customPorts);
                _portsList.Clear();
                foreach (var item in result)
                {
                    _portsList.Add(item);
                }

                int busyCount = _portsList.Count(p => p.IsActive);
                TxtPortsSummary.Text = (busyCount == 0) ? Strings.AllPortsFree : Strings.BusyPortsCount(busyCount);

                BtnKillAllBusy.IsEnabled = (busyCount > 0);
                BtnKillAllBusy.Opacity = (busyCount > 0) ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                TxtPortsSummary.Text = "Error scanning ports: " + ex.Message;
            }
            finally
            {
                _isScanningPorts = false;
                BtnRefreshPorts.IsEnabled = true;
                BtnAddCustomPort.IsEnabled = true;
                BtnKillAllBusy.IsEnabled = _portsList.Any(p => p.IsActive);
                BtnKillAllBusy.Opacity = _portsList.Any(p => p.IsActive) ? 1.0 : 0.5;
                StopRotateAnimation(IcoRefreshPorts);
                UpdateCompactUI();
            }
        }

        private async void BtnRefreshPorts_Click(object sender, RoutedEventArgs e)
        {
            await RefreshPortsAsync();
        }

        private async void BtnKillAllBusy_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingPorts || _isScanningPorts) return;
            var busy = _portsList.Where(p => p.IsActive).ToList();
            if (busy.Count == 0) return;

            _isKillingPorts = true;
            BtnKillAllBusy.IsEnabled = false;
            BtnRefreshPorts.IsEnabled = false;
            BtnAddCustomPort.IsEnabled = false;
            TxtPortsSummary.Text = Strings.IsSpanish ? "Matando procesos en puertos..." : "Killing processes on ports...";

            try
            {
                await PortManager.KillSelectedPortsAsync(busy);
                await RefreshPortsAsync();
            }
            finally
            {
                _isKillingPorts = false;
                BtnKillAllBusy.IsEnabled = _portsList.Any(p => p.IsActive);
                BtnKillAllBusy.Opacity = _portsList.Any(p => p.IsActive) ? 1.0 : 0.5;
                BtnRefreshPorts.IsEnabled = true;
                BtnAddCustomPort.IsEnabled = true;
                UpdateCompactUI();
            }
        }

        private async void BtnKillSinglePort_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingPorts || _isScanningPorts) return;
            var btn = sender as Button;
            var item = (btn != null) ? btn.Tag as PortItem : null;
            if (item == null || !item.IsActive) return;

            _isKillingPorts = true;
            BtnKillAllBusy.IsEnabled = false;
            BtnRefreshPorts.IsEnabled = false;
            BtnAddCustomPort.IsEnabled = false;
            if (btn != null)
            {
                btn.IsEnabled = false;
                btn.Opacity = 0.5;
            }

            TxtPortsSummary.Text = Strings.IsSpanish 
                ? string.Format("Matando :{0} (PID {1})...", item.PortNumber, item.ProcessId) 
                : string.Format("Killing :{0} (PID {1})...", item.PortNumber, item.ProcessId);

            try
            {
                await PortManager.KillPortAsync(item.PortNumber, item.ProcessId);
                await RefreshPortsAsync();
            }
            finally
            {
                _isKillingPorts = false;
                BtnKillAllBusy.IsEnabled = _portsList.Any(p => p.IsActive);
                BtnKillAllBusy.Opacity = _portsList.Any(p => p.IsActive) ? 1.0 : 0.5;
                BtnRefreshPorts.IsEnabled = true;
                BtnAddCustomPort.IsEnabled = true;
                if (btn != null)
                {
                    btn.IsEnabled = true;
                    btn.Opacity = 1.0;
                }
                UpdateCompactUI();
            }
        }

        private void TxtCustomPort_TextChanged(object sender, TextChangedEventArgs e)
        {
            TxtCustomPortPlaceholder.Visibility = string.IsNullOrEmpty(TxtCustomPort.Text) 
                ? Visibility.Visible 
                : Visibility.Collapsed;
        }

        private async void TxtCustomPort_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                await AddCustomPortAsync();
            }
        }

        private async void BtnAddCustomPort_Click(object sender, RoutedEventArgs e)
        {
            await AddCustomPortAsync();
        }

        private async Task AddCustomPortAsync()
        {
            if (_isScanningPorts || _isKillingPorts) return;
            int port;
            if (int.TryParse(TxtCustomPort.Text.Trim(), out port) && port > 0 && port <= 65535)
            {
                _customPorts.Add(port);
                TxtCustomPort.Text = "";
                await RefreshPortsAsync();
            }
        }

        #endregion

        #region Process & RAM Guardian Engine (Matar_Node Generalized)

        private async Task RefreshTasksAsync()
        {
            if (_isScanningTasks || _isKillingTasks) return;
            _isScanningTasks = true;
            BtnRefreshTasks.IsEnabled = false;
            BtnKillAllRunaways.IsEnabled = false;
            StartRotateAnimation(IcoRefreshTasks);
            UpdateCompactUI();

            if (ItemsProcessGroups.ItemsSource == null)
            {
                ItemsProcessGroups.ItemsSource = _processGroups;
            }

            TxtTotalDevRam.Text = Strings.IsSpanish ? "Diagnosticando procesos y memoria..." : "Diagnosing processes & RAM...";

            try
            {
                var report = await ProcessGuardian.ScanProcessesAsync();
                _processGroups.Clear();
                foreach (var g in report.Groups)
                {
                    _processGroups.Add(g);
                }

                TxtTotalDevRam.Text = Strings.TotalDevRam(report.FormattedTotalMemory, report.TotalInstances);
                BannerRunawayAlert.Visibility = report.HasRunawayAlert ? Visibility.Visible : Visibility.Collapsed;

                bool hasRunaway = report.Groups.Any(g => g.IsRunaway);
                BtnKillAllRunaways.IsEnabled = hasRunaway;
                BtnKillAllRunaways.Opacity = hasRunaway ? 1.0 : 0.5;
            }
            catch (Exception ex)
            {
                TxtTotalDevRam.Text = "Error scanning tasks: " + ex.Message;
            }
            finally
            {
                _isScanningTasks = false;
                BtnRefreshTasks.IsEnabled = true;
                bool hasRunaway = _processGroups.Any(g => g.IsRunaway);
                BtnKillAllRunaways.IsEnabled = hasRunaway;
                BtnKillAllRunaways.Opacity = hasRunaway ? 1.0 : 0.5;
                StopRotateAnimation(IcoRefreshTasks);
                UpdateCompactUI();
            }
        }

        private async void BtnRefreshTasks_Click(object sender, RoutedEventArgs e)
        {
            await RefreshTasksAsync();
        }

        private async void BtnKillAllRunaways_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingTasks || _isScanningTasks) return;
            var runaways = _processGroups.Where(g => g.IsRunaway).ToList();
            if (runaways.Count == 0) return;

            _isKillingTasks = true;
            BtnKillAllRunaways.IsEnabled = false;
            BtnRefreshTasks.IsEnabled = false;
            TxtTotalDevRam.Text = Strings.IsSpanish ? "Terminando procesos en bucle..." : "Terminating runaway processes...";

            try
            {
                await ProcessGuardian.KillAllRunawaysAsync(runaways);
                await RefreshTasksAsync();
            }
            finally
            {
                _isKillingTasks = false;
                bool hasRunaway = _processGroups.Any(g => g.IsRunaway);
                BtnKillAllRunaways.IsEnabled = hasRunaway;
                BtnKillAllRunaways.Opacity = hasRunaway ? 1.0 : 0.5;
                BtnRefreshTasks.IsEnabled = true;
                UpdateCompactUI();
            }
        }

        private async void BtnKillGroup_Click(object sender, RoutedEventArgs e)
        {
            if (_isKillingTasks || _isScanningTasks) return;
            var btn = sender as Button;
            var group = (btn != null) ? btn.Tag as ProcessGroupItem : null;
            if (group == null || !group.IsActive) return;

            _isKillingTasks = true;
            BtnKillAllRunaways.IsEnabled = false;
            BtnRefreshTasks.IsEnabled = false;
            if (btn != null)
            {
                btn.IsEnabled = false;
                btn.Opacity = 0.5;
            }

            TxtTotalDevRam.Text = Strings.IsSpanish 
                ? string.Format("Terminando árbol de {0}...", group.DisplayName) 
                : string.Format("Terminating {0} process tree...", group.DisplayName);

            try
            {
                await ProcessGuardian.KillGroupAsync(group.ExecutableName);
                await RefreshTasksAsync();
            }
            finally
            {
                _isKillingTasks = false;
                bool hasRunaway = _processGroups.Any(g => g.IsRunaway);
                BtnKillAllRunaways.IsEnabled = hasRunaway;
                BtnKillAllRunaways.Opacity = hasRunaway ? 1.0 : 0.5;
                BtnRefreshTasks.IsEnabled = true;
                if (btn != null)
                {
                    btn.IsEnabled = true;
                    btn.Opacity = 1.0;
                }
                UpdateCompactUI();
            }
        }

        #endregion

        #region Fluent 2 Options ContextMenu

        private void BtnOptions_Click(object sender, RoutedEventArgs e)
        {
            var menu = new ContextMenu
            {
                Style = (Style)this.FindResource("FluentContextMenu")
            };

            // 1. Alternar Tema
            var miTheme = CreateMenuItem(
                _isLightMode ? Strings.MenuThemeDark : Strings.MenuThemeLight,
                _isLightMode ? "\uEC46" : "\uE706",
                (s, ev) => ApplyTheme(!_isLightMode)
            );
            menu.Items.Add(miTheme);

            // 2. Alternar Vista (Completa / Compacta)
            var miMode = CreateMenuItem(
                (_currentState == WidgetViewState.Compact) ? Strings.MenuFull : Strings.MenuCompact,
                (_currentState == WidgetViewState.Compact) ? "\uE740" : "\uE799",
                (s, ev) =>
                {
                    if (_currentState == WidgetViewState.Compact)
                        BtnExpandCompact_Click(null, null);
                    else
                        SetViewState(WidgetViewState.Compact);
                }
            );
            menu.Items.Add(miMode);

            // 3. Idioma
            var miLang = CreateMenuItem(
                Strings.MenuLang,
                "\uE774", // Globe
                (s, ev) =>
                {
                    Strings.ToggleLanguage();
                    ApplyLocalization();
                    UpdateStats();
                }
            );
            menu.Items.Add(miLang);

            // Separador
            var sep = new Separator { Style = (Style)this.FindResource("FluentMenuSeparator") };
            menu.Items.Add(sep);

            // 4. Ocultar en la Bandeja
            var miHide = CreateMenuItem(
                Strings.MenuHideToTray,
                "\uE711", // Dismiss / Hide
                (s, ev) => HideWidget()
            );
            menu.Items.Add(miHide);

            // 5. Salir de CleanDesk
            var miClose = CreateMenuItem(
                Strings.MenuExitApp,
                "\uE8BB", // Close
                (s, ev) => ExitApplication()
            );
            menu.Items.Add(miClose);

            menu.PlacementTarget = BtnOptions;
            menu.Placement = System.Windows.Controls.Primitives.PlacementMode.Bottom;
            menu.IsOpen = true;
        }

        private MenuItem CreateMenuItem(string title, string glyph, RoutedEventHandler onClick)
        {
            var item = new MenuItem
            {
                Header = title,
                Style = (Style)this.FindResource("FluentMenuItem")
            };

            var iconBlock = new TextBlock
            {
                Text = glyph,
                FontFamily = (FontFamily)this.FindResource("FontIcons"),
                FontSize = 12,
                Foreground = (Brush)this.FindResource("BrushTextSecondary"),
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center
            };
            item.Icon = iconBlock;

            if (onClick != null) item.Click += onClick;
            return item;
        }

        #endregion

        #region System Tray & Widget Visibility (Hidden Icons)

        private void InitNotifyIcon()
        {
            if (_notifyIcon != null) return;

            try
            {
                _notifyIcon = new System.Windows.Forms.NotifyIcon();
                _notifyIcon.Icon = AppIconHelper.GetAppIcon();
                _notifyIcon.Text = "CleanDesk";
                _notifyIcon.Visible = true;

                _notifyIcon.MouseClick += (s, e) =>
                {
                    if (e.Button == System.Windows.Forms.MouseButtons.Left)
                    {
                        Dispatcher.Invoke(new Action(() =>
                        {
                            ToggleWidgetVisibility();
                        }));
                    }
                };

                _notifyIcon.DoubleClick += (s, e) =>
                {
                    Dispatcher.Invoke(new Action(() =>
                    {
                        ShowWidget();
                    }));
                };

                _trayMenu = new System.Windows.Forms.ContextMenuStrip();
                _trayMenu.ShowImageMargin = false;
                _trayMenu.ShowCheckMargin = false;
                _trayMenu.Renderer = new CleanDeskTrayRenderer();
                _trayMenu.Padding = new System.Windows.Forms.Padding(4, 6, 4, 6);
                _trayMenu.Opening += (s, e) =>
                {
                    PopulateTrayMenuItems();
                };

                _notifyIcon.ContextMenuStrip = _trayMenu;
                PopulateTrayMenuItems();
            }
            catch
            {
                // Fallback silencioso si el subsistema WinForms no está disponible
            }
        }

        private void DisposeNotifyIcon()
        {
            try
            {
                if (_notifyIcon != null)
                {
                    _notifyIcon.Visible = false;
                    _notifyIcon.ContextMenuStrip = null;
                    _notifyIcon.Dispose();
                    _notifyIcon = null;
                }
                if (_trayMenu != null)
                {
                    _trayMenu.Dispose();
                    _trayMenu = null;
                }
            }
            catch { }
        }

        public void ToggleWidgetVisibility()
        {
            if (this.Visibility == Visibility.Visible && this.IsActive)
            {
                HideWidget();
            }
            else
            {
                ShowWidget();
            }
        }

        public void ShowWidget()
        {
            if (this.Visibility != Visibility.Visible)
            {
                this.Visibility = Visibility.Visible;
            }
            this.Show();
            this.Activate();
            this.Topmost = true;
            this.Topmost = false;
        }

        public void HideWidget()
        {
            this.Visibility = Visibility.Collapsed;
        }

        public void ExitApplication()
        {
            _isExplicitExit = true;
            DisposeNotifyIcon();
            this.Close();
            Application.Current.Shutdown();
        }

        private void PopulateTrayMenuItems()
        {
            if (_trayMenu == null) return;

            _trayMenu.Items.Clear();

            // 0. Header / Brand Title
            var miHeader = new System.Windows.Forms.ToolStripMenuItem("CLEANDESK • WIDGET");
            miHeader.Enabled = false;
            miHeader.Font = new System.Drawing.Font("Segoe UI", 8.25f, System.Drawing.FontStyle.Bold);
            miHeader.Padding = new System.Windows.Forms.Padding(12, 4, 12, 4);
            miHeader.Tag = "Header";
            _trayMenu.Items.Add(miHeader);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // 1. Mostrar / Ocultar
            bool isVisible = (this.Visibility == Visibility.Visible);
            string toggleText = isVisible 
                ? (Strings.IsSpanish ? "◨   Ocultar Widget" : "◨   Hide Widget")
                : (Strings.IsSpanish ? "◧   Mostrar Widget" : "◧   Show Widget");
            var miToggle = new System.Windows.Forms.ToolStripMenuItem(
                toggleText,
                null,
                (s, e) => Dispatcher.Invoke(new Action(() => ToggleWidgetVisibility()))
            );
            miToggle.Font = new System.Drawing.Font("Segoe UI", 9.5f, System.Drawing.FontStyle.Bold);
            miToggle.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miToggle);

            // 2. Alternar Modo (Compacto / Completo)
            bool isCompact = (_currentState == WidgetViewState.Compact);
            string viewText = isCompact
                ? (Strings.IsSpanish ? "⤢   Modo Completo" : "⤢   Full Mode")
                : (Strings.IsSpanish ? "⤡   Modo Compacto" : "⤡   Compact Mode");
            var miView = new System.Windows.Forms.ToolStripMenuItem(
                viewText,
                null,
                (s, e) => Dispatcher.Invoke(new Action(() =>
                {
                    ShowWidget();
                    if (_currentState == WidgetViewState.Compact)
                    {
                        BtnExpandCompact_Click(null, null);
                    }
                    else
                    {
                        SetViewState(WidgetViewState.Compact);
                    }
                }))
            );
            miView.Font = new System.Drawing.Font("Segoe UI", 9.25f, System.Drawing.FontStyle.Regular);
            miView.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miView);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // 3. Escanear PC
            string scanText = (Strings.IsSpanish ? "🧹  Escanear PC ahora" : "🧹  Scan PC Now");
            var miScan = new System.Windows.Forms.ToolStripMenuItem(
                scanText,
                null,
                (s, e) => Dispatcher.BeginInvoke(new Action(() =>
                {
                    ShowWidget();
                    var ignored = TriggerScanAsync();
                }))
            );
            miScan.Font = new System.Drawing.Font("Segoe UI", 9.25f, System.Drawing.FontStyle.Regular);
            miScan.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miScan);

            // 4. Liberar Puertos Ocupados
            int busyPorts = _portsList.Count(p => p.IsActive);
            string portsLabel = (busyPorts > 0)
                ? (Strings.IsSpanish ? string.Format("⚡  Liberar Puertos ({0})", busyPorts) : string.Format("⚡  Kill Busy Ports ({0})", busyPorts))
                : (Strings.IsSpanish ? "⚡  Liberar Puertos (0)" : "⚡  Kill Busy Ports (0)");
            var miPorts = new System.Windows.Forms.ToolStripMenuItem(
                portsLabel,
                null,
                (s, e) => Dispatcher.Invoke(new Action(() =>
                {
                    ShowWidget();
                    BtnKillAllBusy_Click(null, null);
                }))
            );
            miPorts.Enabled = (busyPorts > 0);
            miPorts.Font = new System.Drawing.Font("Segoe UI", 9.25f, System.Drawing.FontStyle.Regular);
            miPorts.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miPorts);

            // 5. Terminar Bucles Dev
            bool hasRunaways = _processGroups.Any(g => g.IsRunaway);
            string tasksLabel = Strings.IsSpanish 
                ? (hasRunaways ? "🛡️  Terminar Bucles Dev (!)" : "🛡️  Terminar Bucles Dev")
                : (hasRunaways ? "🛡️  Kill Dev Loops (!)" : "🛡️  Kill Dev Loops");
            var miTasks = new System.Windows.Forms.ToolStripMenuItem(
                tasksLabel,
                null,
                (s, e) => Dispatcher.Invoke(new Action(() =>
                {
                    ShowWidget();
                    BtnKillAllRunaways_Click(null, null);
                }))
            );
            miTasks.Enabled = hasRunaways;
            miTasks.Font = new System.Drawing.Font("Segoe UI", 9.25f, System.Drawing.FontStyle.Regular);
            miTasks.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miTasks);

            _trayMenu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            // 6. Salir de CleanDesk
            string exitText = (Strings.IsSpanish ? "✕   Salir de CleanDesk" : "✕   Exit CleanDesk");
            var miExit = new System.Windows.Forms.ToolStripMenuItem(
                exitText,
                null,
                (s, e) => Dispatcher.Invoke(new Action(() => ExitApplication()))
            );
            miExit.Tag = "Danger";
            miExit.Font = new System.Drawing.Font("Segoe UI", 9.25f, System.Drawing.FontStyle.Regular);
            miExit.Padding = new System.Windows.Forms.Padding(12, 6, 12, 6);
            _trayMenu.Items.Add(miExit);
        }

        #endregion
    }
}
