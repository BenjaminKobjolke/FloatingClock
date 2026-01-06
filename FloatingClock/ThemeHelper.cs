using Microsoft.Win32;
using System.Drawing;
using FloatingClock.Config;

namespace FloatingClock
{
    /// <summary>
    /// Helper class for Windows theme detection
    /// </summary>
    public static class ThemeHelper
    {
        /// <summary>
        /// Determines if Windows is currently using a light theme
        /// </summary>
        /// <returns>True if light theme is active, false for dark theme</returns>
        public static bool IsWindowsUsingLightTheme()
        {
            try
            {
                using (var key = Registry.CurrentUser.OpenSubKey(Constants.ThemeRegistryPath, false))
                {
                    var value = key?.GetValue(Constants.ThemeValueName);
                    return value != null && (int)value == 1;
                }
            }
            catch
            {
                return true; // Default to light theme if detection fails
            }
        }

        /// <summary>
        /// Gets the appropriate icon for the current Windows theme
        /// Light theme = light taskbar background -> use dark icon
        /// Dark theme = dark taskbar background -> use light icon
        /// </summary>
        /// <returns>The theme-appropriate icon</returns>
        public static Icon GetThemeAppropriateIcon()
        {
            return IsWindowsUsingLightTheme()
                ? Properties.Resources.icon_dark
                : Properties.Resources.icon_light;
        }

        /// <summary>
        /// Determines the effective theme based on user preference
        /// </summary>
        /// <param name="userPreference">User preference: "auto", "light", or "dark"</param>
        /// <returns>The effective theme: "light" or "dark"</returns>
        public static string GetEffectiveTheme(string userPreference)
        {
            if (userPreference == "light")
                return "light";
            if (userPreference == "dark")
                return "dark";

            // Auto mode: follow system theme
            return IsWindowsUsingLightTheme() ? "light" : "dark";
        }

        /// <summary>
        /// Determines if dark theme should be used based on user preference
        /// </summary>
        /// <param name="userPreference">User preference: "auto", "light", or "dark"</param>
        /// <returns>True if dark theme should be used</returns>
        public static bool IsDarkTheme(string userPreference)
        {
            return GetEffectiveTheme(userPreference) == "dark";
        }
    }
}
