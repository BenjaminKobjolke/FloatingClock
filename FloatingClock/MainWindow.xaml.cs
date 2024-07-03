using IniParser;
using IniParser.Model;
using Microsoft.Win32;
using System;
using System.Diagnostics;
using System.Globalization;
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
        /// Mouse position relative to window, set on LeftMouseDown
        /// </summary>
        private Point relativeMousePosition;

        /// <summary>
        /// For setting the time/date text
        /// </summary>
        private DispatcherTimer timer;

        private DispatcherTimer timerBackground;

        private bool fixedPosition = true;

        private IniData iniData;

        private string dateFormat = "dd/MM/yyyy";
        private string timeFormat = "HH:mm";

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
            SystemEvents.DisplaySettingsChanged += SystemEvents_DisplaySettingsChanged;
        }

        private void LoadSettings()
        {
            string iniPath = "settings.ini";
            var parser = new FileIniDataParser();
            iniData = parser.ReadFile(iniPath);

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
            if(!fixedPosition)
            {
                int xWindow = Convert.ToInt32(iniData["window"]["x"]);
                int yWindow = Convert.ToInt32(iniData["window"]["y"]);
                this.Left = xWindow;
                this.Top = SystemParameters.FullPrimaryScreenHeight - yWindow;
            }

            int widthWindow = Convert.ToInt32(iniData["window"]["width"]);
            int heightWindow = Convert.ToInt32(iniData["window"]["height"]);

            this.Width = widthWindow;
            this.Height = heightWindow;

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
            bool shiftDown = false;
            int speed = 100;
            if (Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
            { 
                shiftDown = true;
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
                }
            }
            else if (e.Key == Key.Left)
            {
                fixedPosition = false;
                this.Left = this.Left - speed;
            }
            else if (e.Key == Key.Right)
            {
                fixedPosition = false;
                this.Left = this.Left + speed;
            }
            else if (e.Key == Key.Up)
            {
                fixedPosition = false;
                this.Top = this.Top - speed;
            }
            else if (e.Key == Key.Down)
            {
                fixedPosition = false;
                this.Top = this.Top + speed;
            }
        }      


        private void FloatingClockWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            SystemEvents.DisplaySettingsChanged -= SystemEvents_DisplaySettingsChanged;
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
            AdjustWindowPosition();
        }

        private void SystemEvents_DisplaySettingsChanged(object sender, EventArgs e)
        {
            AdjustWindowPosition();
        }

        private void AdjustWindowPosition()
        {
            if(!fixedPosition)
            {
                return;
            }
            this.Left = SystemParameters.FullPrimaryScreenWidth - this.Width - 10;
            this.Top = SystemParameters.FullPrimaryScreenHeight - this.Height + 20;
           
        }

        /// <summary>
        /// Called every interval, updates the clock and date displays
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void Clock_Tick(object sender, EventArgs e)
        {
            AdjustWindowPosition();
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
                Close();
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
