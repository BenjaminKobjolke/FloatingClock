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

        public MainWindow()
        {
            InitializeComponent();

            // set up basic event handlers
            FloatingClockWindow.MouseMove += MainWindow_MouseMove;
            FloatingClockWindow.KeyDown += FloatingClockWindow_KeyDown;
            FloatingClockWindow.MouseLeftButtonDown += FloatingClockWindow_MouseLeftButtonDown;
            FloatingClockWindow.Loaded += FloatingClockWindow_Loaded;

            // instantiate and initialize the clock timer
            timer = new DispatcherTimer
            {
                Interval = System.TimeSpan.FromMilliseconds(100)
            };
            timer.Tick += new System.EventHandler(Clock_Tick);
            timer.Start();

            FloatingClockWindow.Unloaded += FloatingClockWindow_Unloaded;
            FloatingClockWindow.Closing += FloatingClockWindow_Closing;
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private IniData CreateDefaultSettings()
        {
            var defaults = new IniData();

            // Font section
            defaults["font"]["family"] = "Consolas";

            // Window section
            defaults["window"]["x"] = "3560";
            defaults["window"]["y"] = "85";
            defaults["window"]["width"] = "160";
            defaults["window"]["height"] = "70";
            defaults["window"]["fixed"] = "1";
            defaults["window"]["fixed_corner"] = "4";
            defaults["window"]["debug"] = "0";
            defaults["window"]["monitor"] = "";

            // Background section
            defaults["background"]["color"] = "#99000000";
            defaults["background"]["auto_brightness_adjustment"] = "1";
            defaults["background"]["threshold_change"] = "0.05";
            defaults["background"]["threshold_min"] = "0.05";
            defaults["background"]["threshold_max"] = "0.47";
            defaults["background"]["alpha_max"] = "0.7";
            defaults["background"]["alpha_min"] = "0.1";
            defaults["background"]["damping"] = "0.1";

            // Date section
            defaults["date"]["show"] = "1";
            defaults["date"]["x"] = "0";
            defaults["date"]["y"] = "0";
            defaults["date"]["size"] = "16";
            defaults["date"]["format"] = "dd/MM/yyyy";
            defaults["date"]["color"] = "#15fc11";
            defaults["date"]["vertical_alignment"] = "Top";
            defaults["date"]["horizontal_alignment"] = "Center";

            // Time section
            defaults["time"]["x"] = "0";
            defaults["time"]["y"] = "0";
            defaults["time"]["size"] = "42";
            defaults["time"]["format"] = "HH:mm";
            defaults["time"]["color"] = "#15fc11";
            defaults["time"]["vertical_alignment"] = "Top";
            defaults["time"]["horizontal_alignment"] = "Left";

            // Seconds section
            defaults["seconds"]["show"] = "1";
            defaults["seconds"]["size"] = "15";
            defaults["seconds"]["x"] = "5";
            defaults["seconds"]["y"] = "-7";
            defaults["seconds"]["color"] = "#15fc11";
            defaults["seconds"]["vertical_alignment"] = "Top";
            defaults["seconds"]["horizontal_alignment"] = "Center";

            // StackPanel section
            defaults["stackpanel"]["vertical_alignment"] = "Bottom";
            defaults["stackpanel"]["horizontal_alignment"] = "Center";

            // Command Palette section
            defaults["command_palette"]["background_color"] = "#CC000000";
            defaults["command_palette"]["text_color"] = "#FFFFFF";
            defaults["command_palette"]["selected_background"] = "#40FFFFFF";
            defaults["command_palette"]["selected_text_color"] = "#FFFFFF";
            defaults["command_palette"]["font_family"] = "Consolas";
            defaults["command_palette"]["font_size"] = "14";
            defaults["command_palette"]["width"] = "400";
            defaults["command_palette"]["padding"] = "10";
            defaults["command_palette"]["item_padding"] = "5";
            defaults["command_palette"]["show_icons"] = "1";

            return defaults;
        }

        private void LoadSettings()
        {
            string iniPath = "settings.ini";
            var parser = new FileIniDataParser();

            // Check if settings.ini exists, if not create with defaults
            if (!System.IO.File.Exists(iniPath))
            {
                try
                {
                    iniData = CreateDefaultSettings();
                    parser.WriteFile(iniPath, iniData);
                    Debug.WriteLine("Created default settings.ini");
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating default settings.ini: {ex.Message}");
                    iniData = CreateDefaultSettings(); // Use defaults in memory
                }
            }
            else
            {
                try
                {
                    iniData = parser.ReadFile(iniPath);
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading settings.ini: {ex.Message}. Using defaults.");
                    MessageBox.Show($"Error reading settings.ini: {ex.Message}\nUsing default settings.", "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    iniData = CreateDefaultSettings();
                }
            }

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
            int widthWindow = Convert.ToInt32(iniData["window"]["width"]);
            int heightWindow = Convert.ToInt32(iniData["window"]["height"]);
            this.Width = widthWindow;
            this.Height = heightWindow;

            // Load and validate monitor setting
            string monitorDeviceName = iniData["window"]["monitor"];
            if (!string.IsNullOrEmpty(monitorDeviceName))
            {
                currentMonitor = GetMonitorByDeviceName(monitorDeviceName);
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
                ValidateAndAdjustPosition(ref desiredLeft, ref desiredTop, this.Width, this.Height);

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
                    Interval = System.TimeSpan.FromMilliseconds(100)
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
        private System.Windows.Forms.Screen GetMonitorByDeviceName(string deviceName)
        {
            if (string.IsNullOrEmpty(deviceName))
                return null;

            return System.Windows.Forms.Screen.AllScreens.FirstOrDefault(s => s.DeviceName == deviceName);
        }

        /// <summary>
        /// Gets the monitor that currently contains the window
        /// </summary>
        /// <returns>The Screen object containing the window</returns>
        private System.Windows.Forms.Screen GetCurrentMonitor()
        {
            WindowInteropHelper windowInteropHelper = new WindowInteropHelper(this);
            System.Windows.Forms.Screen screen = System.Windows.Forms.Screen.FromHandle(windowInteropHelper.Handle);
            return screen;
        }

        /// <summary>
        /// Cycles to the next available monitor while preserving the current corner position
        /// </summary>
        private void CycleToNextMonitor()
        {
            System.Windows.Forms.Screen[] allScreens = System.Windows.Forms.Screen.AllScreens;

            // If only one monitor, nothing to cycle
            if (allScreens.Length <= 1)
                return;

            // Find current monitor index
            int currentIndex = -1;
            for (int i = 0; i < allScreens.Length; i++)
            {
                if (allScreens[i].DeviceName == currentMonitor.DeviceName)
                {
                    currentIndex = i;
                    break;
                }
            }

            // Move to next monitor (wrap around to first if at end)
            int nextIndex = (currentIndex + 1) % allScreens.Length;
            currentMonitor = allScreens[nextIndex];

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

            IntPtr currentState = SHAppBarMessage(ABM_GETSTATE, ref abd);
            int state = currentState.ToInt32();

            // Toggle autohide state
            bool isCurrentlyAutoHidden = (state & ABS_AUTOHIDE) != 0;

            // Set new state (toggle autohide, preserve always-on-top)
            abd.lParam = isCurrentlyAutoHidden ? new IntPtr(state & ~ABS_AUTOHIDE) : new IntPtr(state | ABS_AUTOHIDE);
            SHAppBarMessage(ABM_SETSTATE, ref abd);

            // Update tracking flag
            isTaskbarHidden = !isCurrentlyAutoHidden;
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
            try
            {
                iniData["window"]["fixed_corner"] = ((int)currentCorner).ToString();
                iniData["window"]["fixed"] = fixedPosition ? "1" : "0";
                iniData["window"]["monitor"] = currentMonitor?.DeviceName ?? "";
                var parser = new FileIniDataParser();
                parser.WriteFile("settings.ini", iniData);
            }
            catch (Exception ex)
            {
                // Silently fail to avoid disrupting user experience
                Debug.WriteLine($"Failed to save corner setting: {ex.Message}");
            }
        }

        private void SaveWindowState()
        {
            try
            {
                // Update current monitor before saving
                currentMonitor = GetCurrentMonitor();

                // Save current window position (convert WPF coordinates to INI format)
                int xWindow = (int)this.Left;
                int yWindow = (int)(SystemParameters.FullPrimaryScreenHeight - this.Top);

                iniData["window"]["x"] = xWindow.ToString();
                iniData["window"]["y"] = yWindow.ToString();
                iniData["window"]["fixed"] = fixedPosition ? "1" : "0";
                iniData["window"]["fixed_corner"] = ((int)currentCorner).ToString();
                iniData["window"]["monitor"] = currentMonitor?.DeviceName ?? "";

                var parser = new FileIniDataParser();
                parser.WriteFile("settings.ini", iniData);
            }
            catch (Exception ex)
            {
                // Silently fail to avoid disrupting user experience
                Debug.WriteLine($"Failed to save window state: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates and adjusts window position to ensure it stays within the work area
        /// Useful when screen resolution changes between sessions
        /// </summary>
        private void ValidateAndAdjustPosition(ref double left, ref double top, double windowWidth, double windowHeight)
        {
            // Ensure we have a valid monitor
            if (currentMonitor == null)
            {
                currentMonitor = System.Windows.Forms.Screen.AllScreens.FirstOrDefault() ?? System.Windows.Forms.Screen.PrimaryScreen;
            }

            // Get current monitor's work area (respects taskbar)
            double workAreaLeft = currentMonitor.WorkingArea.X;
            double workAreaTop = currentMonitor.WorkingArea.Y;
            double workAreaWidth = currentMonitor.WorkingArea.Width;
            double workAreaHeight = currentMonitor.WorkingArea.Height;

            // Ensure window stays within work area
            // Left edge: clamp to work area left
            if (left < workAreaLeft)
                left = workAreaLeft;

            // Right edge: ensure window doesn't go off right side
            if (left + windowWidth > workAreaLeft + workAreaWidth)
                left = workAreaLeft + workAreaWidth - windowWidth;

            // Top edge: clamp to work area top
            if (top < workAreaTop)
                top = workAreaTop;

            // Bottom edge: ensure window doesn't go off bottom
            if (top + windowHeight > workAreaTop + workAreaHeight)
                top = workAreaTop + workAreaHeight - windowHeight;
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
            int speed = 100;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            {
                speed = 10;
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
                DockToCorner(currentCorner);
                SaveCornerToSettings();
            }
            else if (e.Key == Key.Left)
            {
                fixedPosition = false;
                double newLeft = this.Left - speed;
                double newTop = this.Top;

                // Update current monitor and validate position stays within bounds
                currentMonitor = GetCurrentMonitor();
                ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height);

                this.Left = newLeft;
            }
            else if (e.Key == Key.Right)
            {
                fixedPosition = false;
                double newLeft = this.Left + speed;
                double newTop = this.Top;

                // Update current monitor and validate position stays within bounds
                currentMonitor = GetCurrentMonitor();
                ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height);

                this.Left = newLeft;
            }
            else if (e.Key == Key.Up)
            {
                fixedPosition = false;
                double newLeft = this.Left;
                double newTop = this.Top - speed;

                // Update current monitor and validate position stays within bounds
                currentMonitor = GetCurrentMonitor();
                ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height);

                this.Top = newTop;
            }
            else if (e.Key == Key.Down)
            {
                fixedPosition = false;
                double newLeft = this.Left;
                double newTop = this.Top + speed;

                // Update current monitor and validate position stays within bounds
                currentMonitor = GetCurrentMonitor();
                ValidateAndAdjustPosition(ref newLeft, ref newTop, this.Width, this.Height);

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
            SaveWindowState();
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

            // position window at the bottom right of the screen
            //this.Left = SystemParameters.WorkArea.Width - this.Width;
            //this.Top = SystemParameters.WorkArea.Height - this.Height;

            //this.Left = SystemParameters.VirtualScreenWidth - this.Width;
            //this.Top = SystemParameters.VirtualScreenHeight - this.Height;

            // Initialize WorkArea and taskbar tracking for change detection
            previousWorkArea = SystemParameters.WorkArea;
            previousTaskbarPosition = TaskbarInfo.GetTaskbarPosition();
            AdjustWindowPosition();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            // Re-validate current monitor (it may have been disconnected)
            if (currentMonitor != null)
            {
                // Check if current monitor still exists
                string currentDeviceName = currentMonitor.DeviceName;
                currentMonitor = GetMonitorByDeviceName(currentDeviceName);

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

            // Use WorkingArea to respect taskbar position on the current monitor
            double workAreaLeft = currentMonitor.WorkingArea.X;
            double workAreaTop = currentMonitor.WorkingArea.Y;
            double workAreaWidth = currentMonitor.WorkingArea.Width;
            double workAreaHeight = currentMonitor.WorkingArea.Height;
            double fullScreenWidth = currentMonitor.Bounds.Width;
            double fullScreenHeight = currentMonitor.Bounds.Height;

            // Get taskbar position using Windows API
            TaskbarInfo.TaskbarPosition taskbarPosition = TaskbarInfo.GetTaskbarPosition();
            bool isAutoHide = TaskbarInfo.IsTaskbarAutoHide();

            // When auto-hide is enabled, treat as if no taskbar for positioning purposes
            // (auto-hide taskbar doesn't reduce WorkArea, so we position at screen edge)
            bool taskbarAtTop = (taskbarPosition == TaskbarInfo.TaskbarPosition.Top) && !isAutoHide;
            bool taskbarAtBottom = (taskbarPosition == TaskbarInfo.TaskbarPosition.Bottom) && !isAutoHide;
            bool taskbarAtLeft = (taskbarPosition == TaskbarInfo.TaskbarPosition.Left) && !isAutoHide;
            bool taskbarAtRight = (taskbarPosition == TaskbarInfo.TaskbarPosition.Right) && !isAutoHide;

            // DEBUG: Show taskbar detection on first call (only if debug mode enabled)
            if (this.Tag == null && debugMode)
            {
                this.Tag = "shown"; // Use Tag to track if we've shown the debug message

                double bottomGap = fullScreenHeight - (workAreaTop + workAreaHeight);
                double rightGap = fullScreenWidth - (workAreaLeft + workAreaWidth);

                MessageBox.Show(
                    $"Taskbar Position (API): {taskbarPosition}\n" +
                    $"Auto-Hide: {isAutoHide}\n\n" +
                    $"WorkArea: Left={workAreaLeft}, Top={workAreaTop}\n" +
                    $"          Width={workAreaWidth}, Height={workAreaHeight}\n\n" +
                    $"FullScreen: Width={fullScreenWidth}, Height={fullScreenHeight}\n\n" +
                    $"Gaps: Bottom={bottomGap}, Right={rightGap}",
                    "Taskbar Detection Debug",
                    MessageBoxButton.OK);
            }

            switch (corner)
            {
                case Corner.TopLeft:
                    this.Left = workAreaLeft + 10;
                    // WorkArea.Top is already 0 when no top taskbar, offset when taskbar at top
                    this.Top = workAreaTop + 10;
                    break;
                case Corner.TopRight:
                    this.Left = workAreaLeft + workAreaWidth - this.Width - 10;
                    // WorkArea.Top is already 0 when no top taskbar, offset when taskbar at top
                    this.Top = workAreaTop + 10;
                    break;
                case Corner.BottomLeft:
                    this.Left = workAreaLeft + 10;
                    // Position above bottom edge/taskbar with -20 margin (negative = up from bottom)
                    this.Top = workAreaTop + workAreaHeight - this.Height - 20;
                    break;
                case Corner.BottomRight:
                    this.Left = workAreaLeft + workAreaWidth - this.Width - 10;
                    // Position above bottom edge/taskbar with -20 margin (negative = up from bottom)
                    this.Top = workAreaTop + workAreaHeight - this.Height - 20;
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
                double left = absoluteMousePosition.X - relativeMousePosition.X;
                double top = absoluteMousePosition.Y - relativeMousePosition.Y;
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

        // AppBar message constants
        private const int ABM_GETSTATE = 0x4;
        private const int ABM_SETSTATE = 0xA;

        // AppBar state constants
        private const int ABS_AUTOHIDE = 0x1;
        private const int ABS_ALWAYSONTOP = 0x2;

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
