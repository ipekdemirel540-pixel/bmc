using System;

using System.ComponentModel;

using System.IO;

using System.Windows;

using System.Windows.Controls;

using System.Windows.Input;

using System.Windows.Media;

using System.Windows.Navigation;

using ManufacturingToolsSuite.Commands;

using ManufacturingToolsSuite.Services;



namespace ManufacturingToolsSuite

{

    public partial class MainWindow : Window, INotifyPropertyChanged

    {

        private bool _isDashboardVisible = true;

        private bool _isMaximized;

        private Settings _settings = new();

        private string _activeModule = "Dashboard";



        public bool IsDashboardVisible { get => _isDashboardVisible; set { _isDashboardVisible = value; OnPropertyChanged(nameof(IsDashboardVisible)); } }

        public bool IsMaximized { get => _isMaximized; set { _isMaximized = value; OnPropertyChanged(nameof(IsMaximized)); } }



        private const string DefaultTitle = "Üretim Teknolojileri Araçları";



        public MainWindow()

        {

            InitializeComponent();

            DataContext = this;

            StateChanged += (_, __) =>
            {
                IsMaximized = WindowState == WindowState.Maximized;
                UpdateMaximizeGlyph();
            };



            CommandBindings.Add(new CommandBinding(AppCommands.Home, (s, e) => SetDashboard()));

            CommandBindings.Add(new CommandBinding(AppCommands.Back, (s, e) => SetDashboard()));

            CommandBindings.Add(new CommandBinding(AppCommands.BmcAi, (s, e) => OpenBmcAiPage(this, new RoutedEventArgs())));

            CommandBindings.Add(new CommandBinding(AppCommands.SapExcel, (s, e) => OpenExcelProcessorPage(this, new RoutedEventArgs())));

            CommandBindings.Add(new CommandBinding(AppCommands.Nesting, (s, e) => OpenNestingPage(this, new RoutedEventArgs())));

            CommandBindings.Add(new CommandBinding(AppCommands.StepViewer, (s, e) => OpenStepDataViewerPage(this, new RoutedEventArgs())));

            CommandBindings.Add(new CommandBinding(AppCommands.MakeBuy, (s, e) => OpenMakeBuyPage(this, new RoutedEventArgs())));



            if (FindName("MainFrame") is Frame frame)

                frame.Navigated += MainFrame_Navigated;



            Closing += MainWindow_Closing;

            SetDashboard();

        }



        public event PropertyChangedEventHandler? PropertyChanged;

        void OnPropertyChanged(string name) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(name));



        private void Window_Loaded(object sender, RoutedEventArgs e)

        {

            try

            {

                _settings = UserSettingsService.Load();

                if (_settings.WindowWidth > 0) Width = _settings.WindowWidth;

                if (_settings.WindowHeight > 0) Height = _settings.WindowHeight;

                WindowState = _settings.WindowState;



                ApplyThemeFromSettings();



                if (!string.IsNullOrWhiteSpace(_settings.LastModule) &&

                    !string.Equals(_settings.LastModule, "Dashboard", StringComparison.OrdinalIgnoreCase) &&

                    !string.Equals(_settings.LastModule, "IoAnalyser", StringComparison.OrdinalIgnoreCase))

                {

                    NavigateToLastModule(_settings.LastModule);

                }

                else

                {

                    SetDashboard();

                }

            }

            catch { /* ignore */ }

        }



        private void MainWindow_Closing(object? sender, CancelEventArgs e)

        {

            try

            {

                var bounds = WindowState == WindowState.Normal ? new Rect(Left, Top, Width, Height) : RestoreBounds;

                _settings.WindowWidth = bounds.Width;

                _settings.WindowHeight = bounds.Height;

                _settings.WindowState = WindowState;

                _settings.IsLightTheme = ThemeService.IsLightTheme;

                UserSettingsService.Save(_settings);

            }

            catch { /* ignore */ }

        }



        private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)

        {

            if (e.ClickCount == 2) BtnMaximize_Click(sender, e);

            else DragMove();

        }



        private void BtnMinimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

        private void BtnMaximize_Click(object sender, RoutedEventArgs e)
        {
            WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;
            UpdateMaximizeGlyph();
        }

        private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();

        private void UpdateMaximizeGlyph()
        {
            if (FindName("MaximizeGlyph") is System.Windows.Controls.TextBlock glyph)
                glyph.Text = WindowState == WindowState.Maximized ? "\uE923" : "\uE922";
        }



        private void SetDashboard()

        {

            IsDashboardVisible = true;

            if (FindName("MainFrame") is Frame frame) frame.Content = null;

            UpdateNavSelection("Dashboard");

            _settings.LastModule = "Dashboard";

            RefreshDashboardContent();

        }



        private void SetModule(string title)

        {

            IsDashboardVisible = false;

            var moduleKey = GetModuleKeyByTitle(title);

            UpdateNavSelection(moduleKey);

            _settings.LastModule = moduleKey;

            _settings.RecentModule = moduleKey;

        }



        private void UpdateNavSelection(string moduleKey)

        {

            _activeModule = moduleKey;

            SetTopNavState(navHome, moduleKey == "Dashboard", (Brush)FindResource("AccentBrush"));

            SetTopNavState(navSap, moduleKey == "SapExcel", (Brush)FindResource("SAPAccentBrush"));

            SetTopNavState(navNesting, moduleKey == "Nesting", (Brush)FindResource("NestingAccentBrush"));

            SetTopNavState(navMakeBuy, moduleKey == "MakeBuy", (Brush)FindResource("MakeBuyAccentBrush"));

            SetTopNavState(navBmcAi, moduleKey == "BmcAi", (Brush)FindResource("BMCAIAccentBrush"));

        }



        private static void SetTopNavState(Button? btn, bool active, Brush accent)

        {

            if (btn == null) return;

            btn.Tag = active ? "Active" : null;

            btn.BorderBrush = active ? accent : Brushes.Transparent;

        }



        private static string GetModuleKeyByTitle(string title)

        {

            if (string.Equals(title, "BMC AI", StringComparison.OrdinalIgnoreCase)) return "BmcAi";

            if (string.Equals(title, "SAP Analizi", StringComparison.OrdinalIgnoreCase)) return "SapExcel";

            if (string.Equals(title, "Nesting", StringComparison.OrdinalIgnoreCase)) return "Nesting";

            if (string.Equals(title, "STEP/STP Görüntüleyici", StringComparison.OrdinalIgnoreCase)) return "StepViewer";

            if (string.Equals(title, "Make/Buy", StringComparison.OrdinalIgnoreCase)) return "MakeBuy";

            return "Dashboard";

        }



        private void NavigateToLastModule(string key)

        {

            switch (key)

            {

                case "BmcAi": OpenBmcAiPage(this, new RoutedEventArgs()); break;

                case "SapExcel": OpenExcelProcessorPage(this, new RoutedEventArgs()); break;

                case "Nesting": OpenNestingPage(this, new RoutedEventArgs()); break;

                case "StepViewer": OpenStepDataViewerPage(this, new RoutedEventArgs()); break;

                case "MakeBuy": OpenMakeBuyPage(this, new RoutedEventArgs()); break;

                default: SetDashboard(); break;

            }

        }



        private void MainFrame_Navigated(object? sender, NavigationEventArgs e)

        {

            if (FindName("MainFrame") is Frame frame)

            {

                if (frame.Content == null)

                    SetDashboard();

                else

                    IsDashboardVisible = false;

            }

        }



        private void OpenContactPage(object sender, RoutedEventArgs e) =>
            CorporateMessageBox.ShowInfo(
                "Sorularınız ve geri bildirimleriniz için Üretim Teknolojileri Ekibi ile iletişime geçebilirsiniz.",
                "İletişim");



        private void OpenBmcAiPage(object sender, RoutedEventArgs e)

        {

            SetModule("BMC AI");

            if (FindName("MainFrame") is Frame frame) frame.Navigate(new Pages.BmcAiPage());

        }



        private void OpenExcelProcessorPage(object sender, RoutedEventArgs e)

        {

            SetModule("SAP Analizi");

            if (FindName("MainFrame") is Frame frame) frame.Navigate(new Pages.ExcelProcessorPage());

        }



        private void OpenNestingPage(object sender, RoutedEventArgs e)

        {

            SetModule("Nesting");

            if (FindName("MainFrame") is Frame frame) frame.Navigate(new Pages.NestingPage());

        }



        private void OpenStepDataViewerPage(object sender, RoutedEventArgs e)

        {

            SetModule("STEP/STP Görüntüleyici");

            if (FindName("MainFrame") is Frame frame) frame.Navigate(new Pages.StepViewerPage());

        }



        private void OpenMakeBuyPage(object sender, RoutedEventArgs e)

        {

            SetModule("Make/Buy");

            if (FindName("MainFrame") is Frame frame) frame.Navigate(new Pages.MakeBuyPage());

        }



        private void ThemeToggleButton_Checked(object sender, RoutedEventArgs e) => SetTheme(isLight: true);

        private void ThemeToggleButton_Unchecked(object sender, RoutedEventArgs e) => SetTheme(isLight: false);



        private void ApplyThemeFromSettings()

        {

            var isLight = _settings.IsLightTheme;

            ThemeService.ApplyTheme(isLight);

            if (FindName("ThemeToggleButton") is System.Windows.Controls.Primitives.ToggleButton toggle)

                toggle.IsChecked = isLight;

        }



        private void SetTheme(bool isLight)

        {

            ThemeService.ApplyTheme(isLight);

            _settings.IsLightTheme = isLight;

            UpdateNavSelection(_activeModule);

        }



        private void RefreshDashboardContent()

        {

            try { _settings = UserSettingsService.Load(); } catch { /* keep in-memory */ }

            var recentKey = _settings.RecentModule;

            if (string.Equals(recentKey, "IoAnalyser", StringComparison.OrdinalIgnoreCase))

                recentKey = null;

            var hasRecent = !string.IsNullOrWhiteSpace(recentKey) &&

                            !string.Equals(recentKey, "Dashboard", StringComparison.OrdinalIgnoreCase);



            var nestingPath = _settings.LastNestingExcelPath;

            var hasNestingFile = !string.IsNullOrWhiteSpace(nestingPath) && File.Exists(nestingPath);

            var nestingFileName = hasNestingFile ? Path.GetFileName(nestingPath!) : null;



            if (FindName("btnResumeModule") is Button resumeBtn)

                resumeBtn.Visibility = hasRecent ? Visibility.Visible : Visibility.Collapsed;



            if (hasRecent && FindName("txtResumeModule") is TextBlock resumeText)

                resumeText.Text = $"Son modül: {GetModuleDisplayName(recentKey!)}";



            if (FindName("btnOpenLastNesting") is Button nestingBtn)

                nestingBtn.Visibility = hasNestingFile ? Visibility.Visible : Visibility.Collapsed;



            if (hasNestingFile && FindName("txtOpenLastNesting") is TextBlock openNestingText)

                openNestingText.Text = $"Son dosya: {nestingFileName}";



            if (FindName("txtNoRecentActivity") is TextBlock noActivityText)

                noActivityText.Visibility = hasRecent || hasNestingFile ? Visibility.Collapsed : Visibility.Visible;



            if (FindName("panelNestingSummary") is Border nestingPanel)

                nestingPanel.Visibility = hasNestingFile ? Visibility.Visible : Visibility.Collapsed;



            // When nesting summary is hidden, recent card fills the full sidebar height
            // so its bottom aligns with the module UniformGrid.
            if (FindName("panelRecentActivity") is Border recentPanel)

                Grid.SetRowSpan(recentPanel, hasNestingFile ? 1 : 2);



            if (hasNestingFile)

            {

                if (FindName("txtNestingSummaryFile") is TextBlock summaryFileText)

                    summaryFileText.Text = nestingFileName!;



                if (FindName("txtNestingSummaryEfficiency") is TextBlock efficiencyText)

                    efficiencyText.Text = "—";

            }

        }



        private void BtnOpenLastNesting_Click(object sender, RoutedEventArgs e) => OpenNestingPage(this, new RoutedEventArgs());



        private void BtnResumeModule_Click(object sender, RoutedEventArgs e)

        {

            if (!string.IsNullOrWhiteSpace(_settings.RecentModule))

                NavigateToLastModule(_settings.RecentModule);

        }



        private static string GetModuleDisplayName(string key) => key switch

        {

            "BmcAi" => "BMC AI",

            "SapExcel" => "SAP Analizi",

            "Nesting" => "Nesting",

            "StepViewer" => "STEP/STP Görüntüleyici",

            "MakeBuy" => "Make/Buy",

            _ => "Modül"

        };


    }

}

