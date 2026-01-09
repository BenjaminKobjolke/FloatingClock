using System;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Media;
using IniParser;
using IniParser.Model;
using FloatingClock.Config;

namespace FloatingClock.Managers
{
    /// <summary>
    /// Manages application settings through INI file operations
    /// </summary>
    public class SettingsManager
    {
        private IniData iniData;
        private readonly FileIniDataParser parser;

        public SettingsManager()
        {
            parser = new FileIniDataParser();
        }

        /// <summary>
        /// Gets the loaded INI data
        /// </summary>
        public IniData Data => iniData;

        /// <summary>
        /// Loads settings from the INI file, creating defaults if it doesn't exist
        /// </summary>
        /// <returns>True if settings were loaded successfully</returns>
        public bool LoadSettings()
        {
            if (!File.Exists(Constants.GetSettingsFilePath()))
            {
                try
                {
                    iniData = CreateDefaultSettings();
                    parser.WriteFile(Constants.GetSettingsFilePath(), iniData);
                    Debug.WriteLine("Created default settings.ini");
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error creating default settings.ini: {ex.Message}");
                    iniData = CreateDefaultSettings(); // Use defaults in memory
                    return false;
                }
            }
            else
            {
                try
                {
                    iniData = parser.ReadFile(Constants.GetSettingsFilePath());
                    return true;
                }
                catch (Exception ex)
                {
                    Debug.WriteLine($"Error reading settings.ini: {ex.Message}. Using defaults.");
                    MessageBox.Show($"Error reading settings.ini: {ex.Message}\nUsing default settings.",
                        "Settings Error", MessageBoxButton.OK, MessageBoxImage.Warning);
                    iniData = CreateDefaultSettings();
                    return false;
                }
            }
        }

        /// <summary>
        /// Saves the current corner setting to the INI file
        /// </summary>
        /// <param name="corner">The corner value to save</param>
        /// <param name="monitorDeviceName">The monitor device name to save</param>
        public void SaveCornerSetting(int corner, string monitorDeviceName)
        {
            try
            {
                if (iniData == null)
                    return;

                iniData["window"]["fixed_corner"] = corner.ToString();
                iniData["window"]["monitor"] = monitorDeviceName ?? "";

                parser.WriteFile(Constants.GetSettingsFilePath(), iniData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save corner settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Saves the current window state to the INI file
        /// </summary>
        /// <param name="left">Window left position</param>
        /// <param name="top">Window top position</param>
        /// <param name="width">Window width</param>
        /// <param name="height">Window height</param>
        /// <param name="isFixed">Whether window is in fixed position mode</param>
        public void SaveWindowState(double left, double top, double width, double height, bool isFixed)
        {
            try
            {
                if (iniData == null)
                    return;

                iniData["window"]["x"] = ((int)left).ToString();
                iniData["window"]["y"] = ((int)top).ToString();
                iniData["window"]["width"] = ((int)width).ToString();
                iniData["window"]["height"] = ((int)height).ToString();
                iniData["window"]["fixed"] = isFixed ? "1" : "0";

                parser.WriteFile(Constants.GetSettingsFilePath(), iniData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save window state: {ex.Message}");
            }
        }

        /// <summary>
        /// Loads the theme mode setting from the INI file
        /// </summary>
        /// <returns>Theme mode: "auto", "light", or "dark"</returns>
        public string LoadThemeMode()
        {
            try
            {
                if (iniData != null && iniData["theme"] != null)
                {
                    var mode = iniData["theme"]["mode"];
                    if (!string.IsNullOrEmpty(mode) && (mode == "auto" || mode == "light" || mode == "dark"))
                    {
                        return mode;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Error loading theme mode: {ex.Message}");
            }
            return "auto"; // Default to auto
        }

        /// <summary>
        /// Saves the theme mode setting to the INI file
        /// </summary>
        /// <param name="mode">Theme mode: "auto", "light", or "dark"</param>
        public void SaveThemeMode(string mode)
        {
            try
            {
                if (iniData == null)
                    return;

                // Ensure theme section exists
                if (iniData["theme"] == null)
                {
                    iniData.Sections.AddSection("theme");
                }

                iniData["theme"]["mode"] = mode;
                parser.WriteFile(Constants.GetSettingsFilePath(), iniData);
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Failed to save theme mode: {ex.Message}");
            }
        }

        /// <summary>
        /// Helper method to convert a string value to double with culture fallback
        /// </summary>
        public double ConvertToDoubleWithCultureFallback(string value)
        {
            try
            {
                return Convert.ToDouble(value, System.Globalization.CultureInfo.InvariantCulture);
            }
            catch
            {
                return Convert.ToDouble(value);
            }
        }

        /// <summary>
        /// Creates default settings with all required sections and values
        /// </summary>
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

            // Theme section
            defaults["theme"]["mode"] = "auto";

            return defaults;
        }
    }
}
