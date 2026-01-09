using System.IO;
using System.Reflection;

namespace FloatingClock.Config
{
    /// <summary>
    /// Centralized string constants and magic numbers for the application
    /// </summary>
    public static class Constants
    {
        // Application
        public const string AppName = "Floating Clock";
        public const string AppMutexName = "FloatingClockSingleInstance";

        /// <summary>
        /// Gets the settings file path based on the executable name.
        /// If the exe is FloatingClock.exe, returns FloatingClock.ini in the same directory.
        /// </summary>
        public static string GetSettingsFilePath()
        {
            string exePath = Assembly.GetExecutingAssembly().Location;
            string exeName = Path.GetFileNameWithoutExtension(exePath);
            string exeDir = Path.GetDirectoryName(exePath);
            return Path.Combine(exeDir, exeName + ".ini");
        }

        // Log paths
        public const string DebugLogSubPath = "FloatingClock\\debug.log";

        // Registry - Startup
        public const string StartupRegistryKey = @"Software\Microsoft\Windows\CurrentVersion\Run";
        public const string StartupValueName = "FloatingClock";

        // Registry - Theme detection
        public const string ThemeRegistryPath = @"SOFTWARE\Microsoft\Windows\CurrentVersion\Themes\Personalize";
        public const string ThemeValueName = "AppsUseLightTheme";

        // Windows messages
        public const int WM_SETTINGCHANGE = 0x001A;

        // Default formats
        public const string DefaultDateFormat = "dd/MM/yyyy";
        public const string DefaultTimeFormat = "HH:mm";
        public const string DefaultSecondsFormat = "ss";

        // Timer intervals (milliseconds)
        public const int ClockTickInterval = 100;
        public const int BackgroundTickInterval = 100;

        // Position offsets (DIPs)
        public const int CornerMargin = 10;
        public const int BottomExtraMargin = 20;
        public const int KeyboardMoveSpeed = 100;
        public const int KeyboardMoveSpeedSlow = 10;

        // AppBar message constants
        public const int ABM_GETSTATE = 0x4;
        public const int ABM_SETSTATE = 0xA;

        // AppBar state constants
        public const int ABS_AUTOHIDE = 0x1;
        public const int ABS_ALWAYSONTOP = 0x2;

        // Window restore constant
        public const int SW_RESTORE = 9;

        // External URLs
        public const string MoreToolsUrl = "https://www.workflow-tools.com/floating-clock/app-link";
    }
}
