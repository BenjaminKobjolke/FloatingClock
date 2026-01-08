using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Threading;
using FloatingClock.Config;
using FloatingClock.Managers;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        /// <summary>
        /// Screen corner positions for docking
        /// </summary>
        public enum Corner
        {
            TopLeft = 1,
            TopRight = 2,
            BottomLeft = 3,
            BottomRight = 4
        }

        /// <summary>
        /// Mouse position relative to window, set on LeftMouseDown
        /// </summary>
        private Point relativeMousePosition;

        /// <summary>
        /// For setting the time/date text
        /// </summary>
        private DispatcherTimer timer;

        private DispatcherTimer timerBackground;

        private bool fixedPosition = true;

        private Corner currentCorner = Corner.BottomRight;

        private IniData iniData;

        private string dateFormat = "dd/MM/yyyy";
        private string timeFormat = "HH:mm";

        private bool debugMode = false;

        private Rect previousWorkArea;
        private TaskbarInfo.TaskbarPosition previousTaskbarPosition;

        private System.Windows.Forms.Screen currentMonitor;

        private CommandPaletteWindow commandPaletteWindow;

        private bool isTaskbarHidden = false;

        private bool isSystemClockHidden = false;

        private bool skipSaveOnClose = false;

        // Managers for separation of concerns
        private MonitorManager monitorManager;
        private SettingsManager settingsManager;
        private WindowPositionManager positionManager;
        private TrayManager trayManager;

        // WndProc hook for theme change detection
        private HwndSource _hwndSource;

        public MainWindow()
        {
            InitializeComponent();

            // Initialize managers
            monitorManager = new MonitorManager();
            settingsManager = new SettingsManager();
            positionManager = new WindowPositionManager(monitorManager);

            // set up basic event handlers
            FloatingClockWindow.MouseMove += MainWindow_MouseMove;
            FloatingClockWindow.KeyDown += FloatingClockWindow_KeyDown;
            FloatingClockWindow.MouseLeftButtonDown += FloatingClockWindow_MouseLeftButtonDown;
            FloatingClockWindow.Loaded += FloatingClockWindow_Loaded;

            // instantiate and initialize the clock timer
            timer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(Constants.ClockTickInterval)
            };
            timer.Tick += new System.EventHandler(Clock_Tick);
            timer.Start();

            FloatingClockWindow.Unloaded += FloatingClockWindow_Unloaded;
            FloatingClockWindow.Closing += FloatingClockWindow_Closing;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }


        private void LoadSettings()
        {
            settingsManager.LoadSettings();
            iniData = settingsManager.Data;

            string fontFamilyName = iniData["font"]["family"];
            if (!string.IsNullOrEmpty(fontFamilyName))
            {
                System.Windows.Media.FontFamily customFont = new System.Windows.Media.FontFamily(fontFamilyName);
                DateBlock.FontFamily = customFont;
                int sizeDate = Convert.ToInt32(iniData["date"]["size"]);
                DateBlock.FontSize = sizeDate;

                ClockBlock.FontFamily = customFont;
                int sizeTime = Convert.ToInt32(iniData["time"]["size"]);
                ClockBlock.FontSize = sizeTime;

                ClockBlockSeconds.FontFamily = customFont;
                int sizeSeconds = Convert.ToInt32(iniData["seconds"]["size"]);
                ClockBlockSeconds.FontSize = sizeSeconds;


            }

            fixedPosition = Convert.ToBoolean(Convert.ToInt32(iniData["window"]["fixed"]));
            debugMode = Convert.ToBoolean(Convert.ToInt32(iniData["window"]["debug"]));

            // Load saved corner preference (default to BottomRight if not specified)
            string cornerValue = iniData["window"]["fixed_corner"];
            if (!string.IsNullOrEmpty(cornerValue) && int.TryParse(cornerValue, out int cornerInt))
            {
                if (cornerInt >= 1 && cornerInt <= 4)
                {
                    currentCorner = (Corner)cornerInt;
                }
            }

            // Set window size first (needed for position validation)
            string widthStr = iniData["window"]["width"];
            string heightStr = iniData["window"]["height"];
            int parsedWidth = 0;
            int parsedHeight = 0;
            int.TryParse(widthStr?.Trim(), out parsedWidth);
            int.TryParse(heightStr?.Trim(), out parsedHeight);

            if (parsedWidth > 0 && parsedHeight > 0)
            {
                // Use Dispatcher with low priority to set size after all layout is complete
                Dispatcher.BeginInvoke(new Action(() =>
                {
                    this.Width = parsedWidth;
                    this.Height = parsedHeight;
                    // Re-adjust position after size change to fix corner docking
                    AdjustWindowPosition();
                }), System.Windows.Threading.DispatcherPriority.ApplicationIdle);
            }

            // Load and validate monitor setting
            string monitorDeviceName = iniData["window"]["monitor"];
            if (!string.IsNullOrEmpty(monitorDeviceName))
            {
                currentMonitor = monitorManager.GetMonitorByDeviceName(monitorDeviceName);
            }

            // If monitor not found or not specified, fall back to first available monitor
            if (currentMonitor == null)
            {
                currentMonitor = System.Windows.Forms.Screen.AllScreens.FirstOrDefault() ?? System.Windows.Forms.Screen.PrimaryScreen;
            }

            // Handle non-fixed position with validation
            if(!fixedPosition)
            {
                int xWindow = Convert.ToInt32(iniData["window"]["x"]);
                int yWindow = Convert.ToInt32(iniData["window"]["y"]);

                double desiredLeft = xWindow;
                double desiredTop = SystemParameters.FullPrimaryScreenHeight - yWindow;

                // Validate and adjust position to ensure it's within screen bounds
                positionManager.ValidateAndAdjustPosition(ref desiredLeft, ref desiredTop, this.Width, this.Height, currentMonitor, this);

                this.Left = desiredLeft;
                this.Top = desiredTop;
            }

            bool showSeconds = Convert.ToBoolean(Convert.ToInt32(iniData["seconds"]["show"]));
            if (showSeconds)
            {
                ClockBlockSeconds.Visibility = Visibility.Visible;
            }
            else
            {
                ClockBlockSeconds.Visibility = Visibility.Collapsed;
            }

            int xSeconds = Convert.ToInt32(iniData["seconds"]["x"]);
            int ySeconds = Convert.ToInt32(iniData["seconds"]["y"]);
            var transformSeconds = ClockBlockSeconds.RenderTransform as TranslateTransform;
            if (transformSeconds != null)
            {
                transformSeconds.X = xSeconds;
                transformSeconds.Y = ySeconds;
            }

            string secondsColorValue = iniData["seconds"]["color"];
            Color secondsColor = (Color)ColorConverter.ConvertFromString(secondsColorValue);
            ClockBlockSeconds.Foreground = new SolidColorBrush(secondsColor);

            int xTime = Convert.ToInt32(iniData["time"]["x"]);
            int yTime = Convert.ToInt32(iniData["time"]["y"]);
            var transformTime = ClockBlock.RenderTransform as TranslateTransform;
            if (transformTime != null)
            {
                transformTime.X = xTime;
                transformTime.Y = yTime;
            }

            timeFormat = iniData["time"]["format"];

            string timeColorValue = iniData["time"]["color"];
            Color timeColor = (Color)ColorConverter.ConvertFromString(timeColorValue);
            ClockBlock.Foreground = new SolidColorBrush(timeColor);

            string timeVerticalAlignmentString = iniData["time"]["vertical_alignment"];
            string timeHorizontalAlignmentString = iniData["time"]["horizontal_alignment"];
            if (Enum.TryParse(timeVerticalAlignmentString, out VerticalAlignment timeVerticalAlignmentValue))
            {
                ClockBlock.VerticalAlignment = timeVerticalAlignmentValue;
            }
            if (Enum.TryParse(timeHorizontalAlignmentString, out HorizontalAlignment timeHorizontalAlignmentValue))
            {
                ClockBlock.HorizontalAlignment = timeHorizontalAlignmentValue;
            }


            int xDate = Convert.ToInt32(iniData["date"]["x"]);
            int yDate = Convert.ToInt32(iniData["date"]["y"]);
            var transformDate = DateBlock.RenderTransform as TranslateTransform;
            if (transformDate != null)
            {
                transformDate.X = xDate;
                transformDate.Y = yDate;
            }

            bool showDate = Convert.ToBoolean(Convert.ToInt32(iniData["date"]["show"]));
            if (showDate)
            {
                DateBlock.Visibility = Visibility.Visible;
            }
            else
            {
                DateBlock.Visibility = Visibility.Collapsed;
            }

            dateFormat = iniData["date"]["format"];

            string dateColorValue = iniData["date"]["color"];
            Color dateColor = (Color)ColorConverter.ConvertFromString(dateColorValue);
            DateBlock.Foreground = new SolidColorBrush(dateColor);

            string dateVerticalAlignmentString = iniData["date"]["vertical_alignment"];
            string  dateHorizontalAlignmentString = iniData["date"]["horizontal_alignment"];
            if (Enum.TryParse(dateVerticalAlignmentString, out VerticalAlignment dateVerticalAlignmentValue))
            {
                DateBlock.VerticalAlignment = dateVerticalAlignmentValue;
            }
            if (Enum.TryParse(dateHorizontalAlignmentString, out HorizontalAlignment dateHorizontalAlignmentValue))
            {
                DateBlock.HorizontalAlignment = dateHorizontalAlignmentValue;
            }

            string backgroundColorValue = iniData["background"]["color"];
            Color backgroundColor = (Color)ColorConverter.ConvertFromString(backgroundColorValue);
            FloatingClockWindow.Background = new SolidColorBrush(backgroundColor);

            string panelVerticalAlignmentString = iniData["stackpanel"]["vertical_alignment"];
            string panelHorizontalAlignmentString = iniData["stackpanel"]["horizontal_alignment"];
            if (Enum.TryParse(panelVerticalAlignmentString, out VerticalAlignment panelVerticalAlignmentValue))
            {
                StackPanelBlock.VerticalAlignment = panelVerticalAlignmentValue;
            }
            if (Enum.TryParse(panelHorizontalAlignmentString, out HorizontalAlignment panelHorizontalAlignmentValue))
            {
                StackPanelBlock.HorizontalAlignment = panelHorizontalAlignmentValue;
            }

            bool updateBackgroundAlpha = Convert.ToBoolean(Convert.ToInt32(iniData["background"]["auto_brightness_adjustment"]));
            if (updateBackgroundAlpha)
            {
                double changeThreshold = ConvertToDoubleWithCultureFallback(iniData["background"]["threshold_change"]);
                ScreenCaptureHelper.BrightnessChangeThreshold = changeThreshold;

                double minThreshold = ConvertToDoubleWithCultureFallback(iniData["background"]["threshold_min"]);                
                ScreenCaptureHelper.MinBrightnessThreshold = minThreshold;

                double maxThreshold = ConvertToDoubleWithCultureFallback(iniData["background"]["threshold_max"]);
                ScreenCaptureHelper.MaxBrightnessThreshold = maxThreshold;

                double damping = ConvertToDoubleWithCultureFallback(iniData["background"]["damping"]);
                ScreenCaptureHelper.DampingFactor = damping;

                double maxAlpha = ConvertToDoubleWithCultureFallback(iniData["background"]["alpha_max"]);
                ScreenCaptureHelper.AlphaMax = maxAlpha;

                double minAlpha = ConvertToDoubleWithCultureFallback(iniData["background"]["alpha_min"]);
                ScreenCaptureHelper.AlphaMin = minAlpha;

                timerBackground = new DispatcherTimer
                {
                    Interval = System.TimeSpan.FromMilliseconds(Constants.BackgroundTickInterval)
                };
                timerBackground.Tick += new System.EventHandler(Background_Tick);
                timerBackground.Start();
            }
        }

        private static double ConvertToDoubleWithCultureFallback(string stringValue)
        {
            // Try parsing with invariant culture (dot as decimal separator)
            if (double.TryParse(stringValue, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                return result;
            }

            // Try parsing with a culture that uses comma as decimal separator, e.g., French
            if (double.TryParse(stringValue, NumberStyles.Any, new CultureInfo("fr-FR"), out result))
            {
                return result;
            }

            // Handle the case where neither parsing is successful
            throw new FormatException("String is not a valid double format.");
        }

        /// <summary>
        /// Finds a monitor by its device name
        /// </summary>
        /// <param name="deviceName">Device name (e.g., "\\\\.\\DISPLAY1")</param>
        /// <returns>The Screen object if found, otherwise null</returns>

        /// <summary>
        /// Cycles to the next available monitor while preserving the current corner position
        /// </summary>
        private void CycleToNextMonitor()
        {
            currentMonitor = monitorManager.CycleToNextMonitor(currentMonitor);

            // Re-dock to the same corner on the new monitor
            DockToCorner(currentCorner);

            // Save the new monitor to settings
            SaveCornerToSettings();
        }

        /// <summary>
        /// Shows the command palette window
        /// </summary>
        private void ShowCommandPalette()
        {
            // Don't open multiple instances
            if (commandPaletteWindow != null && commandPaletteWindow.IsVisible)
                return;

            // Get current command list with states
            var commands = GetCommands();

            // Create and show palette window
            commandPaletteWindow = new CommandPaletteWindow(this, iniData, commands);
            commandPaletteWindow.Owner = this;
            commandPaletteWindow.ShowDialog();
        }

        /// <summary>
        /// Handles right-click on the window to open the command palette
        /// </summary>
        private void Window_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            ShowCommandPalette();
            e.Handled = true;
        }

        /// <summary>
        /// Gets the list of available commands with their current states
        /// </summary>
        private List<CommandItem> GetCommands()
        {
            var commands = new List<CommandItem>();

            // Show/Hide Seconds
            bool secondsVisible = ClockBlockSeconds.Visibility == Visibility.Visible;
            commands.Add(new CommandItem(
                secondsVisible ? "Hide Seconds" : "Show Seconds",
                "S",
                () => { ClockBlockSeconds.Visibility = secondsVisible ? Visibility.Collapsed : Visibility.Visible; },
                secondsVisible
            ));

            // Toggle Fixed/Free Mode
            commands.Add(new CommandItem(
                "Free Position Mode",
                "F",
                () => {
                    fixedPosition = !fixedPosition;
                    if (fixedPosition)
                    {
                        DockToCorner(currentCorner);
                        SaveCornerToSettings();
                    }
                },
                !fixedPosition
            ));

            // Move with arrow keys (informational only)
            commands.Add(new CommandItem(
                "Move with Arrow Keys",
                "↑↓←→",
                () => { /* No action - informational */ },
                !fixedPosition
            ));

            // Dock to corners
            commands.Add(new CommandItem(
                "Dock to Top-Left",
                "1",
                () => { currentCorner = Corner.TopLeft; fixedPosition = true; DockToCorner(currentCorner); SaveCornerToSettings(); },
                currentCorner == Corner.TopLeft && fixedPosition
            ));

            commands.Add(new CommandItem(
                "Dock to Top-Right",
                "2",
                () => { currentCorner = Corner.TopRight; fixedPosition = true; DockToCorner(currentCorner); SaveCornerToSettings(); },
                currentCorner == Corner.TopRight && fixedPosition
            ));

            commands.Add(new CommandItem(
                "Dock to Bottom-Left",
                "3",
                () => { currentCorner = Corner.BottomLeft; fixedPosition = true; DockToCorner(currentCorner); SaveCornerToSettings(); },
                currentCorner == Corner.BottomLeft && fixedPosition
            ));

            commands.Add(new CommandItem(
                "Dock to Bottom-Right",
                "4",
                () => { currentCorner = Corner.BottomRight; fixedPosition = true; DockToCorner(currentCorner); SaveCornerToSettings(); },
                currentCorner == Corner.BottomRight && fixedPosition
            ));

            // Cycle to next monitor
            int monitorCount = System.Windows.Forms.Screen.AllScreens.Length;
            commands.Add(new CommandItem(
                monitorCount > 1 ? "Cycle to Next Monitor" : "Cycle to Next Monitor (only 1 available)",
                "5",
                () => { fixedPosition = true; CycleToNextMonitor(); },
                false
            ));

            // Cycle to next corner
            commands.Add(new CommandItem(
                "Cycle to Next Corner",
                "N",
                () => { currentCorner = (Corner)(((int)currentCorner % 4) + 1); fixedPosition = true; DockToCorner(currentCorner); SaveCornerToSettings(); },
                false
            ));

            // Open settings
            commands.Add(new CommandItem(
                "Open Settings",
                "Settings",
                () => { ShowSettingsWindow(); },
                false
            ));

            // Toggle taskbar visibility
            commands.Add(new CommandItem(
                "Hide Windows Taskbar",
                "T",
                () => { ToggleTaskbarVisibility(); },
                isTaskbarHidden
            ));

            // Toggle system clock visibility (Windows 11+ only, Windows 10 users can use System Settings)
            if (IsWindows11OrNewer())
            {
                commands.Add(new CommandItem(
                    "Hide System Clock",
                    "C",
                    () => { ToggleSystemClockVisibility(); },
                    isSystemClockHidden
                ));
            }

            // Exit application
            commands.Add(new CommandItem(
                "Exit Application",
                "Esc",
                () => { this.Close(); },
                false
            ));

            return commands;
        }

        /// <summary>
        /// Toggles the Windows taskbar autohide state
        /// </summary>
        private void ToggleTaskbarVisibility()
        {
            // Get current taskbar state
            APPBARDATA abd = new APPBARDATA();
            abd.cbSize = Marshal.SizeOf(typeof(APPBARDATA));

            IntPtr currentState = SHAppBarMessage(Constants.ABM_GETSTATE, ref abd);
            int state = currentState.ToInt32();

            // Toggle autohide state
            bool isCurrentlyAutoHidden = (state & Constants.ABS_AUTOHIDE) != 0;

            // Set new state (toggle autohide, preserve always-on-top)
            abd.lParam = isCurrentlyAutoHidden ? new IntPtr(state & ~Constants.ABS_AUTOHIDE) : new IntPtr(state | Constants.ABS_AUTOHIDE);
            SHAppBarMessage(Constants.ABM_SETSTATE, ref abd);

            // Update tracking flag
            isTaskbarHidden = !isCurrentlyAutoHidden;
        }

        /// <summary>
        /// Checks if running on Windows 11 or newer (build 22000+)
        /// </summary>
        private bool IsWindows11OrNewer()
        {
            try
            {
                // Use registry to get actual build number (Environment.OSVersion can be unreliable without manifest)
                using (var key = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SOFTWARE\Microsoft\Windows NT\CurrentVersion", false))
                {
                    if (key != null)
                    {
                        var buildStr = key.GetValue("CurrentBuild") as string ?? key.GetValue("CurrentBuildNumber") as string;
                        if (int.TryParse(buildStr, out int build))
                        {
                            return build >= 22000;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to detect Windows version: {ex.Message}");
            }

            // Fallback to Environment.OSVersion
            return Environment.OSVersion.Version.Build >= 22000;
        }

        /// <summary>
        /// Toggles the Windows system clock visibility via registry (Windows 11+ only)
        /// </summary>
        private void ToggleSystemClockVisibility()
        {
            // Show confirmation dialog before restarting Explorer
            var result = MessageBox.Show(
                "Hiding/showing the system clock requires restarting Windows Explorer. This will briefly interrupt your desktop. Continue?",
                "Restart Explorer",
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
                return;

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", true))
                {
                    if (key != null)
                    {
                        // ShowSystrayDateTimeValueName: 0 = clock hidden, 1 = clock visible
                        int currentValue = (int)(key.GetValue("ShowSystrayDateTimeValueName", 0) ?? 0);
                        int newValue = currentValue == 0 ? 1 : 0;
                        key.SetValue("ShowSystrayDateTimeValueName", newValue, Microsoft.Win32.RegistryValueKind.DWord);
                        isSystemClockHidden = (newValue == 0);
                    }
                }

                // Restart Explorer to apply the change
                RestartExplorer();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to toggle system clock: {ex.Message}");
            }
        }

        /// <summary>
        /// Restarts Windows Explorer to apply system tray changes
        /// </summary>
        private void RestartExplorer()
        {
            try
            {
                foreach (var process in System.Diagnostics.Process.GetProcessesByName("explorer"))
                {
                    process.Kill();
                }
                System.Diagnostics.Process.Start("explorer.exe");
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to restart Explorer: {ex.Message}");
            }
        }

        /// <summary>
        /// Reads the current system clock visibility state from registry (Windows 11+ only)
        /// </summary>
        private void InitializeSystemClockState()
        {
            // Feature only available on Windows 11+
            if (!IsWindows11OrNewer())
            {
                isSystemClockHidden = false;
                return;
            }

            try
            {
                using (var key = Microsoft.Win32.Registry.CurrentUser.OpenSubKey(
                    @"Software\Microsoft\Windows\CurrentVersion\Explorer\Advanced", false))
                {
                    if (key != null)
                    {
                        // ShowSystrayDateTimeValueName: 0 = clock hidden, 1 = clock visible
                        int currentValue = (int)(key.GetValue("ShowSystrayDateTimeValueName", 0) ?? 0);
                        isSystemClockHidden = (currentValue == 0);
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to read system clock state: {ex.Message}");
            }
        }

        /// <summary>
        /// Shows the settings editor window
        /// </summary>
        private void ShowSettingsWindow()
        {
            try
            {
                var settingsWindow = new SettingsWindow(iniData);
                settingsWindow.Owner = this;
                settingsWindow.ShowDialog();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error opening settings window: {ex.Message}");
                MessageBox.Show($"Error opening settings: {ex.Message}", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void SaveCornerToSettings()
        {
            settingsManager.SaveCornerSetting((int)currentCorner, currentMonitor?.DeviceName ?? "");
            iniData = settingsManager.Data; // Keep local copy in sync
        }

        /// <summary>
        /// Sets flag to skip saving window state on close (used when restarting from settings)
        /// </summary>
        public void SetSkipSaveOnClose()
        {
            skipSaveOnClose = true;
        }

        private void SaveWindowState()
        {
            // Update current monitor before saving
            currentMonitor = monitorManager.GetCurrentMonitor(this);

            settingsManager.SaveWindowState(this.Left, this.Top, this.Width, this.Height, fixedPosition);
            iniData = settingsManager.Data; // Keep local copy in sync
        }


        private void Window_Activated(object sender, EventArgs e)
        {
            FocusBorder.BorderThickness = new Thickness(1); // Adjust the thickness to your preference
        }

        private void Window_Deactivated(object sender, EventArgs e)
        {
            FocusBorder.BorderThickness = new Thickness(0);
        }

        private void Window_KeyDown(object sender, KeyEventArgs e)
        {
            int speed = Constants.KeyboardMoveSpeed;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                speed = Constants.KeyboardMoveSpeedSlow;
            }
            if (e.Key == Key.S)
            {
                ClockBlockSeconds.Visibility = (ClockBlockSeconds.Visibility == Visibility.Collapsed) ? Visibility.Visible : Visibility.Collapsed;
            }
            else if (e.Key == Key.F)
            {
                if (fixedPosition)
                {
                    fixedPosition = false;
                }
                else
                {
                    fixedPosition = true;
                    DockToCorner(currentCorner);
                    SaveCornerToSettings();
                }
            }
            else if (e.Key == Key.E)
            {
                // Show command palette
                ShowCommandPalette();
            }
            else if (e.Key == Key.T)
            {
                // Toggle taskbar visibility
                ToggleTaskbarVisibility();
            }
            else if (e.Key == Key.C && IsWindows11OrNewer())
            {
                // Toggle system clock visibility (Windows 11+ only)
                ToggleSystemClockVisibility();
            }
            else if (e.Key == Key.D1 || e.Key == Key.NumPad1)
            {
                currentCorner = Corner.TopLeft;
                fixedPosition = true;
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.D2 || e.Key == Key.NumPad2)
            {
                currentCorner = Corner.TopRight;
                fixedPosition = true;
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.D3 || e.Key == Key.NumPad3)
            {
                currentCorner = Corner.BottomLeft;
                fixedPosition = true;
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.D4 || e.Key == Key.NumPad4)
            {
                currentCorner = Corner.BottomRight;
                fixedPosition = true;
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.D5 || e.Key == Key.NumPad5)
            {
                // Cycle to next monitor (if available)
                fixedPosition = true;
                CycleToNextMonitor();
            }
            else if (e.Key == Key.N)
            {
                // Cycle to next corner: 1->2->3->4->1
                currentCorner = (Corner)(((int)currentCorner % 4) + 1);
                fixedPosition = true;
                currentMonitor = monitorManager.GetCurrentMonitor(this);
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.I)
            {
                // Show window and monitor information
                currentMonitor = monitorManager.GetCurrentMonitor(this);

                double dpiScaleX, dpiScaleY;
                monitorManager.GetDpiScale(this, out dpiScaleX, out dpiScaleY);
                TaskbarInfo.TaskbarPosition taskbarPosition = TaskbarInfo.GetTaskbarPosition();
                bool isAutoHide = TaskbarInfo.IsTaskbarAutoHide();

                string positionMode = fixedPosition ? $"Fixed (Corner: {currentCorner})" : "Free Position";

                MessageBox.Show(
                    $"Window Information\n\n" +
                    $"DPI Scaling: {dpiScaleX * 100:F0}% x {dpiScaleY * 100:F0}%\n\n" +
                    $"Position Mode: {positionMode}\n\n" +
                    $"Current Position (DIPs):\n" +
                    $"  Left: {this.Left:F1}\n" +
                    $"  Top: {this.Top:F1}\n\n" +
                    $"Window Size:\n" +
                    $"  Desired: {this.Width:F1} x {this.Height:F1}\n" +
                    $"  Actual: {this.ActualWidth:F1} x {this.ActualHeight:F1}\n\n" +
                    $"Monitor: {currentMonitor.DeviceName}\n" +
                    $"  Full Bounds (Physical): {currentMonitor.Bounds.X}, {currentMonitor.Bounds.Y}, {currentMonitor.Bounds.Width}x{currentMonitor.Bounds.Height}\n" +
                    $"  Full Bounds (DIPs): {currentMonitor.Bounds.X / dpiScaleX:F1}, {currentMonitor.Bounds.Y / dpiScaleY:F1}, {currentMonitor.Bounds.Width / dpiScaleX:F1}x{currentMonitor.Bounds.Height / dpiScaleY:F1}\n" +
                    $"  Working Area (Physical): {currentMonitor.WorkingArea.X}, {currentMonitor.WorkingArea.Y}, {currentMonitor.WorkingArea.Width}x{currentMonitor.WorkingArea.Height}\n" +
                    $"  Working Area (DIPs): {currentMonitor.WorkingArea.X / dpiScaleX:F1}, {currentMonitor.WorkingArea.Y / dpiScaleY:F1}, {currentMonitor.WorkingArea.Width / dpiScaleX:F1}x{currentMonitor.WorkingArea.Height / dpiScaleY:F1}\n\n" +
                    $"Taskbar: {taskbarPosition} (Auto-Hide: {isAutoHide})",
                    "Window Info",
                    MessageBoxButton.OK);
            }
            else if (e.Key == Key.Left)
            {
                fixedPosition = false;
                double newLeft = this.Left - speed;
                double newTop = this.Top;

                // Update current monitor and validate position stays within bounds
                currentMonitor = monitorManager.GetCurrentMonitor(this);
                positionManager.ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height, currentMonitor, this);

                this.Left = newLeft;
            }
            else if (e.Key == Key.Right)
            {
                fixedPosition = false;
                double newLeft = this.Left + speed;
                double newTop = this.Top;

                // Update current monitor and validate position stays within bounds
                currentMonitor = monitorManager.GetCurrentMonitor(this);
                positionManager.ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height, currentMonitor, this);

                this.Left = newLeft;
            }
            else if (e.Key == Key.Up)
            {
                fixedPosition = false;
                double newLeft = this.Left;
                double newTop = this.Top - speed;

                // Update current monitor and validate position stays within bounds
                currentMonitor = monitorManager.GetCurrentMonitor(this);
                positionManager.ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height, currentMonitor, this);

                this.Top = newTop;
            }
            else if (e.Key == Key.Down)
            {
                fixedPosition = false;
                double newLeft = this.Left;
                double newTop = this.Top + speed;

                // Update current monitor and validate position stays within bounds
                currentMonitor = monitorManager.GetCurrentMonitor(this);
                positionManager.ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height, currentMonitor, this);

                this.Top = newTop;
            }
        }      


        private void FloatingClockWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
        }

        private void FloatingClockWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // Save current window state (position and fixed flag) when closing
            // Skip if restarting from settings (settings already saved new values)
            if (!skipSaveOnClose)
            {
                SaveWindowState();
            }

            // Stop timers
            timer?.Stop();
            timerBackground?.Stop();

            // Remove WndProc hook
            _hwndSource?.RemoveHook(WndProc);

            // Dispose tray manager
            trayManager?.Dispose();
            trayManager = null;
        }

        /// <summary>
        /// Use some Windows API to make the window disappear from Alt-Tab
        /// </summary>
        /// Courtesy of https://sfckoverflow.com/a/551847/10148350
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_Loaded(object sender, RoutedEventArgs e)
        {
            WindowInteropHelper wndHelper = new WindowInteropHelper(this);

            //int exStyle = (int)GetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE);
            //exStyle |= (int)ExtendedWindowStyles.WS_EX_TOOLWINDOW;
            //SetWindowLong(wndHelper.Handle, (int)GetWindowLongFields.GWL_EXSTYLE, (IntPtr)exStyle);
            LoadSettings();

            // Initialize system clock state from registry
            InitializeSystemClockState();

            // Set up WndProc hook for theme change detection
            _hwndSource = HwndSource.FromHwnd(wndHelper.Handle);
            _hwndSource?.AddHook(WndProc);

            // Initialize system tray icon
            InitializeTrayIcon();

            // Initialize WorkArea and taskbar tracking for change detection
            previousWorkArea = SystemParameters.WorkArea;
            previousTaskbarPosition = TaskbarInfo.GetTaskbarPosition();
            AdjustWindowPosition();
        }

        /// <summary>
        /// Initializes the system tray icon with theme-aware icon
        /// </summary>
        private void InitializeTrayIcon()
        {
            var icon = ThemeHelper.GetThemeAppropriateIcon();
            trayManager = new TrayManager(icon);
            trayManager.ShowRequested += (s, args) =>
            {
                this.Show();
                this.WindowState = WindowState.Normal;
                this.Activate();
            };
            trayManager.SettingsRequested += (s, args) => ShowSettingsWindow();
            trayManager.ExitRequested += (s, args) => this.Close();
        }

        /// <summary>
        /// Windows message handler for detecting theme changes
        /// </summary>
        private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
        {
            if (msg == Constants.WM_SETTINGCHANGE)
            {
                // Update tray icon when Windows theme changes
                trayManager?.UpdateIcon(ThemeHelper.GetThemeAppropriateIcon());
            }
            return IntPtr.Zero;
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Re-validate current monitor (it may have been disconnected)
            if (currentMonitor != null)
            {
                // Check if current monitor still exists
                string currentDeviceName = currentMonitor.DeviceName;
                currentMonitor = monitorManager.GetMonitorByDeviceName(currentDeviceName);

                // If monitor no longer exists, fall back to first available
                if (currentMonitor == null)
                {
                    currentMonitor = System.Windows.Forms.Screen.AllScreens.FirstOrDefault() ?? System.Windows.Forms.Screen.PrimaryScreen;
                }
            }

            AdjustWindowPosition();
        }

        private void AdjustWindowPosition()
        {
            if(!fixedPosition)
            {
                return;
            }
            DockToCorner(currentCorner);
        }

        /// <summary>
        /// Docks the window to a specific screen corner
        /// </summary>
        /// <param name="corner">The corner to dock to (1=TopLeft, 2=TopRight, 3=BottomLeft, 4=BottomRight)</param>
        private void DockToCorner(Corner corner)
        {
            // Ensure we have a valid monitor
            if (currentMonitor == null)
            {
                currentMonitor = System.Windows.Forms.Screen.AllScreens.FirstOrDefault() ?? System.Windows.Forms.Screen.PrimaryScreen;
            }

            // Get DPI scaling to convert from physical pixels to DIPs
            double dpiScaleX, dpiScaleY;
            monitorManager.GetDpiScale(this, out dpiScaleX, out dpiScaleY);

            // Use WorkingArea to respect taskbar position on the current monitor
            // Convert from physical pixels (Screen API) to device-independent pixels (WPF)
            double workAreaLeft = currentMonitor.WorkingArea.X / dpiScaleX;
            double workAreaTop = currentMonitor.WorkingArea.Y / dpiScaleY;
            double workAreaWidth = currentMonitor.WorkingArea.Width / dpiScaleX;
            double workAreaHeight = currentMonitor.WorkingArea.Height / dpiScaleY;
            double fullScreenWidth = currentMonitor.Bounds.Width / dpiScaleX;
            double fullScreenHeight = currentMonitor.Bounds.Height / dpiScaleY;

            // Get taskbar position using Windows API
            TaskbarInfo.TaskbarPosition taskbarPosition = TaskbarInfo.GetTaskbarPosition();
            bool isAutoHide = TaskbarInfo.IsTaskbarAutoHide();

            // When auto-hide is enabled, treat as if no taskbar for positioning purposes
            // (auto-hide taskbar doesn't reduce WorkArea, so we position at screen edge)
            bool taskbarAtTop = (taskbarPosition == TaskbarInfo.TaskbarPosition.Top) && !isAutoHide;
            bool taskbarAtBottom = (taskbarPosition == TaskbarInfo.TaskbarPosition.Bottom) && !isAutoHide;
            bool taskbarAtLeft = (taskbarPosition == TaskbarInfo.TaskbarPosition.Left) && !isAutoHide;
            bool taskbarAtRight = (taskbarPosition == TaskbarInfo.TaskbarPosition.Right) && !isAutoHide;

            // Use ActualWidth/ActualHeight for accurate positioning (falls back to Width/Height if not yet rendered)
            double windowWidth = this.ActualWidth > 0 ? this.ActualWidth : this.Width;
            double windowHeight = this.ActualHeight > 0 ? this.ActualHeight : this.Height;

            // DEBUG: Show debug information when debug mode is enabled
            if (debugMode)
            {
                // Calculate target position before moving
                double targetLeft = 0;
                double targetTop = 0;

                switch (corner)
                {
                    case Corner.TopLeft:
                        targetLeft = workAreaLeft + Constants.CornerMargin;
                        targetTop = workAreaTop + Constants.CornerMargin;
                        break;
                    case Corner.TopRight:
                        targetLeft = workAreaLeft + workAreaWidth - windowWidth - Constants.CornerMargin;
                        targetTop = workAreaTop + Constants.CornerMargin;
                        break;
                    case Corner.BottomLeft:
                        targetLeft = workAreaLeft + Constants.CornerMargin;
                        targetTop = workAreaTop + workAreaHeight - windowHeight - Constants.BottomExtraMargin;
                        break;
                    case Corner.BottomRight:
                        targetLeft = workAreaLeft + workAreaWidth - windowWidth - Constants.CornerMargin;
                        targetTop = workAreaTop + workAreaHeight - windowHeight - Constants.BottomExtraMargin;
                        break;
                }

                double targetRight = targetLeft + windowWidth;
                double targetBottom = targetTop + windowHeight;

                MessageBox.Show(
                    $"DEBUG: Corner Positioning\n\n" +
                    $"DPI Scaling: {dpiScaleX * 100:F0}% x {dpiScaleY * 100:F0}%\n\n" +
                    $"Monitor: {currentMonitor.DeviceName}\n\n" +
                    $"Screen Bounds (Physical Pixels):\n" +
                    $"  Full: {currentMonitor.Bounds.X}, {currentMonitor.Bounds.Y}, {currentMonitor.Bounds.Width}x{currentMonitor.Bounds.Height}\n" +
                    $"  Working Area: {currentMonitor.WorkingArea.X}, {currentMonitor.WorkingArea.Y}, {currentMonitor.WorkingArea.Width}x{currentMonitor.WorkingArea.Height}\n\n" +
                    $"Screen Bounds (DIPs - after scaling):\n" +
                    $"  Full: {currentMonitor.Bounds.X / dpiScaleX:F1}, {currentMonitor.Bounds.Y / dpiScaleY:F1}, {fullScreenWidth:F1}x{fullScreenHeight:F1}\n" +
                    $"  Working Area: {workAreaLeft:F1}, {workAreaTop:F1}, {workAreaWidth:F1}x{workAreaHeight:F1}\n\n" +
                    $"Taskbar: {taskbarPosition} (Auto-Hide: {isAutoHide})\n\n" +
                    $"Target Corner: {corner}\n\n" +
                    $"Window Size (Actual): {this.ActualWidth:F1} x {this.ActualHeight:F1}\n" +
                    $"Window Size (Desired): {this.Width:F1} x {this.Height:F1}\n" +
                    $"Window Size (Used): {windowWidth:F1} x {windowHeight:F1}\n\n" +
                    $"Target Position (DIPs):\n" +
                    $"  Top-Left: ({targetLeft:F1}, {targetTop:F1})\n" +
                    $"  Bottom-Right: ({targetRight:F1}, {targetBottom:F1})",
                    "Debug: DockToCorner",
                    MessageBoxButton.OK);
            }

            switch (corner)
            {
                case Corner.TopLeft:
                    this.Left = workAreaLeft + Constants.CornerMargin;
                    // WorkArea.Top is already 0 when no top taskbar, offset when taskbar at top
                    this.Top = workAreaTop + Constants.CornerMargin;
                    break;
                case Corner.TopRight:
                    this.Left = workAreaLeft + workAreaWidth - windowWidth - Constants.CornerMargin;
                    // WorkArea.Top is already 0 when no top taskbar, offset when taskbar at top
                    this.Top = workAreaTop + Constants.CornerMargin;
                    break;
                case Corner.BottomLeft:
                    this.Left = workAreaLeft + Constants.CornerMargin;
                    // Position above bottom edge/taskbar with extra margin (up from bottom)
                    this.Top = workAreaTop + workAreaHeight - windowHeight - Constants.BottomExtraMargin;
                    break;
                case Corner.BottomRight:
                    this.Left = workAreaLeft + workAreaWidth - windowWidth - Constants.CornerMargin;
                    // Position above bottom edge/taskbar with extra margin (up from bottom)
                    this.Top = workAreaTop + workAreaHeight - windowHeight - Constants.BottomExtraMargin;
                    break;
            }
        }

        /// <summary>
        /// Called every interval, updates the clock and date displays
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clock_Tick(object sender, EventArgs e)
        {
            // Detect taskbar changes by comparing both WorkArea and taskbar position
            Rect currentWorkArea = SystemParameters.WorkArea;
            TaskbarInfo.TaskbarPosition currentTaskbarPosition = TaskbarInfo.GetTaskbarPosition();

            if (currentWorkArea != previousWorkArea || currentTaskbarPosition != previousTaskbarPosition)
            {
                previousWorkArea = currentWorkArea;
                previousTaskbarPosition = currentTaskbarPosition;
                AdjustWindowPosition();
            }

            DateTime now = DateTime.Now;
            DateBlock.Text = now.ToString(dateFormat);

            ClockBlock.Text = now.ToString(timeFormat);

            ClockBlockSeconds.Text = now.ToString("ss");
        }

        private void Background_Tick(object sender, EventArgs e)
        {            
            string backgroundColorValue = iniData["background"]["color"];
            Color backgroundColor = (Color)ColorConverter.ConvertFromString(backgroundColorValue);

            bool success = ScreenCaptureHelper.AdjustBackgroundTransparency(this, backgroundColor);
        }

        /// <summary>
        /// Capture the mouse cursor position relative to window when the user clicks
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            relativeMousePosition = e.GetPosition(this);
        }

        /// <summary>
        /// Exit the program when the user hits Escape
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void FloatingClockWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // If command palette is open, close it first
                if (commandPaletteWindow != null && commandPaletteWindow.IsVisible)
                {
                    commandPaletteWindow.Close();
                }
                else
                {
                    // Otherwise close the application
                    Close();
                }
            }
        }

        /// <summary>
        /// Allows the user to drag the window around the screen
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void MainWindow_MouseMove(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                Point absoluteMousePosition = GetCursorPosition();

                // Convert physical pixels to DIPs (GetCursorPos returns physical pixels, WPF uses DIPs)
                monitorManager.GetDpiScale(this, out double dpiScaleX, out double dpiScaleY);
                double mouseX = absoluteMousePosition.X / dpiScaleX;
                double mouseY = absoluteMousePosition.Y / dpiScaleY;

                double left = mouseX - relativeMousePosition.X;
                double top = mouseY - relativeMousePosition.Y;
                Left = left; Top = top;
            }
        }

        #region Windows API

        // for simplicity and extensibility put these into enums
        [Flags]
        public enum ExtendedWindowStyles
        {
            WS_EX_TOOLWINDOW = 0x00000080
        }

        public enum GetWindowLongFields
        {
            GWL_EXSTYLE = (-20)
        }

        /// <summary>
        /// for grabbing the absolute cursor position from Windows
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        public struct POINT
        {
            public int X;
            public int Y;

            public static implicit operator Point(POINT point)
            {
                return new Point(point.X, point.Y);
            }
        }

        [DllImport("user32.dll")]
        public static extern bool GetCursorPos(out POINT lpPoint);

        /// <summary>
        /// Grabs the absolute cursor position from Windows and converts it to a Point object
        /// </summary>
        /// <returns></returns>
        public static Point GetCursorPosition()
        {
            GetCursorPos(out POINT lpPoint);

            return lpPoint;
        }

        [DllImport("user32.dll")]
        public static extern IntPtr GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr", SetLastError = true)]
        public static extern IntPtr IntSetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong", SetLastError = true)]
        public static extern Int32 IntSetWindowLong(IntPtr hWnd, int nIndex, Int32 dwNewLong);

        [DllImport("kernel32.dll", EntryPoint = "SetLastError")]
        public static extern void SetLastError(int dwErrorCode);

        [DllImport("shell32.dll")]
        public static extern IntPtr SHAppBarMessage(int dwMessage, ref APPBARDATA pData);

        // APPBARDATA structure for taskbar autohide
        [StructLayout(LayoutKind.Sequential)]
        public struct APPBARDATA
        {
            public int cbSize;
            public IntPtr hWnd;
            public int uCallbackMessage;
            public int uEdge;
            public RECT rc;
            public IntPtr lParam;
        }

        [StructLayout(LayoutKind.Sequential)]
        public struct RECT
        {
            public int left;
            public int top;
            public int right;
            public int bottom;
        }

        
        /// <summary>
        /// Simple conversion from IntPtr to a int
        /// </summary>
        /// <param name="intPtr"></param>
        /// <returns></returns>
        private static int IntPtrToInt32(IntPtr intPtr)
        {
            return unchecked((int)intPtr.ToInt64());
        }

        /// <summary>
        /// Wrapper for Windows API interface
        /// </summary>
        /// Courtesy of https://stackoverflow.com/a/551847/10148350
        /// <param name="hWnd"></param>
        /// <param name="nIndex"></param>
        /// <param name="dwNewLong"></param>
        /// <returns></returns>
        public static IntPtr SetWindowLong(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            int error = 0;
            IntPtr result = IntPtr.Zero;

            SetLastError(0);

            if (IntPtr.Size == 4)
            {
                int tempResult = IntSetWindowLong(hWnd, nIndex, IntPtrToInt32(dwNewLong));
                error = Marshal.GetLastWin32Error();
                result = new IntPtr(tempResult);
            } else
            {
                result = IntSetWindowLongPtr(hWnd, nIndex, dwNewLong);
                error = Marshal.GetLastWin32Error();
            }

            if ((result == IntPtr.Zero) && (error != 0))
            {
                throw new System.ComponentModel.Win32Exception(error);
            }

            return result;
        }
    }
    #endregion
}
