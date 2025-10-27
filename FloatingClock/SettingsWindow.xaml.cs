using IniParser;
using IniParser.Model;
using System;
using System.Diagnostics;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace FloatingClock
{
    public partial class SettingsWindow : Window
    {
        private IniData iniData;
        private bool hasChanges = false;
        private bool isLoading = true;

        public SettingsWindow(IniData ini)
        {
            InitializeComponent();
            iniData = ini;

            // Populate font comboboxes
            PopulateFontComboBoxes();

            // Load all settings
            LoadSettings();

            isLoading = false;
        }

        private void PopulateFontComboBoxes()
        {
            var fonts = System.Drawing.FontFamily.Families
                .Select(f => f.Name)
                .OrderBy(name => name)
                .ToList();

            foreach (var font in fonts)
            {
                FontFamilyComboBox.Items.Add(font);
                PaletteFontFamilyComboBox.Items.Add(font);
            }
        }

        private void LoadSettings()
        {
            try
            {
                // Font section
                SetComboBoxValue(FontFamilyComboBox, iniData["font"]["family"]);

                // Window section
                SetSliderAndTextBox(WindowWidthSlider, WindowWidthTextBox, iniData["window"]["width"]);
                SetSliderAndTextBox(WindowHeightSlider, WindowHeightTextBox, iniData["window"]["height"]);
                WindowFixedCheckBox.IsChecked = iniData["window"]["fixed"] == "1";
                SetComboBoxByTag(WindowFixedCornerComboBox, iniData["window"]["fixed_corner"]);
                WindowDebugCheckBox.IsChecked = iniData["window"]["debug"] == "1";

                // Background section
                BackgroundColorTextBox.Text = iniData["background"]["color"];
                UpdateColorPreview(BackgroundColorTextBox, BackgroundColorPreview);
                BackgroundAutoBrightnessCheckBox.IsChecked = iniData["background"]["auto_brightness_adjustment"] == "1";
                SetSliderAndTextBox(BackgroundThresholdChangeSlider, BackgroundThresholdChangeTextBox, iniData["background"]["threshold_change"]);
                SetSliderAndTextBox(BackgroundThresholdMinSlider, BackgroundThresholdMinTextBox, iniData["background"]["threshold_min"]);
                SetSliderAndTextBox(BackgroundThresholdMaxSlider, BackgroundThresholdMaxTextBox, iniData["background"]["threshold_max"]);
                SetSliderAndTextBox(BackgroundAlphaMaxSlider, BackgroundAlphaMaxTextBox, iniData["background"]["alpha_max"]);
                SetSliderAndTextBox(BackgroundAlphaMinSlider, BackgroundAlphaMinTextBox, iniData["background"]["alpha_min"]);
                SetSliderAndTextBox(BackgroundDampingSlider, BackgroundDampingTextBox, iniData["background"]["damping"]);

                // Date section
                DateShowCheckBox.IsChecked = iniData["date"]["show"] == "1";
                SetSliderAndTextBox(DateSizeSlider, DateSizeTextBox, iniData["date"]["size"]);
                DateFormatTextBox.Text = iniData["date"]["format"];
                DateColorTextBox.Text = iniData["date"]["color"];
                UpdateColorPreview(DateColorTextBox, DateColorPreview);
                SetComboBoxByTag(DateVerticalAlignmentComboBox, iniData["date"]["vertical_alignment"]);
                SetComboBoxByTag(DateHorizontalAlignmentComboBox, iniData["date"]["horizontal_alignment"]);

                // Time section
                SetSliderAndTextBox(TimeSizeSlider, TimeSizeTextBox, iniData["time"]["size"]);
                TimeFormatTextBox.Text = iniData["time"]["format"];
                TimeColorTextBox.Text = iniData["time"]["color"];
                UpdateColorPreview(TimeColorTextBox, TimeColorPreview);
                SetComboBoxByTag(TimeVerticalAlignmentComboBox, iniData["time"]["vertical_alignment"]);
                SetComboBoxByTag(TimeHorizontalAlignmentComboBox, iniData["time"]["horizontal_alignment"]);

                // Seconds section
                SecondsShowCheckBox.IsChecked = iniData["seconds"]["show"] == "1";
                SetSliderAndTextBox(SecondsSizeSlider, SecondsSizeTextBox, iniData["seconds"]["size"]);
                SecondsColorTextBox.Text = iniData["seconds"]["color"];
                UpdateColorPreview(SecondsColorTextBox, SecondsColorPreview);
                SetComboBoxByTag(SecondsVerticalAlignmentComboBox, iniData["seconds"]["vertical_alignment"]);
                SetComboBoxByTag(SecondsHorizontalAlignmentComboBox, iniData["seconds"]["horizontal_alignment"]);

                // Stack panel section
                SetComboBoxByTag(StackPanelVerticalAlignmentComboBox, iniData["stackpanel"]["vertical_alignment"]);
                SetComboBoxByTag(StackPanelHorizontalAlignmentComboBox, iniData["stackpanel"]["horizontal_alignment"]);

                // Command palette section (with defaults if section doesn't exist)
                if (iniData.Sections.ContainsSection("command_palette"))
                {
                    PaletteBackgroundColorTextBox.Text = GetValueOrDefault(iniData["command_palette"]["background_color"], "#CC000000");
                    PaletteTextColorTextBox.Text = GetValueOrDefault(iniData["command_palette"]["text_color"], "#FFFFFF");
                    PaletteSelectedBackgroundTextBox.Text = GetValueOrDefault(iniData["command_palette"]["selected_background"], "#40FFFFFF");
                    SetComboBoxValue(PaletteFontFamilyComboBox, GetValueOrDefault(iniData["command_palette"]["font_family"], "Consolas"));
                    SetSliderAndTextBox(PaletteFontSizeSlider, PaletteFontSizeTextBox, GetValueOrDefault(iniData["command_palette"]["font_size"], "14"));
                    SetSliderAndTextBox(PaletteWidthSlider, PaletteWidthTextBox, GetValueOrDefault(iniData["command_palette"]["width"], "400"));
                    PaletteShowIconsCheckBox.IsChecked = GetValueOrDefault(iniData["command_palette"]["show_icons"], "1") == "1";
                }
                else
                {
                    // Use defaults
                    PaletteBackgroundColorTextBox.Text = "#CC000000";
                    PaletteTextColorTextBox.Text = "#FFFFFF";
                    PaletteSelectedBackgroundTextBox.Text = "#40FFFFFF";
                    SetComboBoxValue(PaletteFontFamilyComboBox, "Consolas");
                    SetSliderAndTextBox(PaletteFontSizeSlider, PaletteFontSizeTextBox, "14");
                    SetSliderAndTextBox(PaletteWidthSlider, PaletteWidthTextBox, "400");
                    PaletteShowIconsCheckBox.IsChecked = true;
                }

                UpdateColorPreview(PaletteBackgroundColorTextBox, PaletteBackgroundColorPreview);
                UpdateColorPreview(PaletteTextColorTextBox, PaletteTextColorPreview);
                UpdateColorPreview(PaletteSelectedBackgroundTextBox, PaletteSelectedBackgroundPreview);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error loading settings: {ex.Message}", "Load Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private string GetValueOrDefault(string value, string defaultValue)
        {
            return string.IsNullOrEmpty(value) ? defaultValue : value;
        }

        private void SetSliderAndTextBox(Slider slider, TextBox textBox, string value)
        {
            if (double.TryParse(value, NumberStyles.Any, CultureInfo.InvariantCulture, out double result))
            {
                slider.Value = result;
                textBox.Text = result.ToString(CultureInfo.InvariantCulture);
            }
        }

        private void SetComboBoxValue(ComboBox comboBox, string value)
        {
            if (string.IsNullOrEmpty(value))
                return;

            for (int i = 0; i < comboBox.Items.Count; i++)
            {
                if (comboBox.Items[i].ToString() == value)
                {
                    comboBox.SelectedIndex = i;
                    return;
                }
            }
        }

        private void SetComboBoxByTag(ComboBox comboBox, string tagValue)
        {
            if (string.IsNullOrEmpty(tagValue))
                return;

            foreach (ComboBoxItem item in comboBox.Items)
            {
                if (item.Tag?.ToString() == tagValue)
                {
                    comboBox.SelectedItem = item;
                    return;
                }
            }
        }

        private void UpdateColorPreview(TextBox textBox, System.Windows.Shapes.Rectangle preview)
        {
            try
            {
                string colorString = textBox.Text;
                if (!colorString.StartsWith("#"))
                    colorString = "#" + colorString;

                Color color = (Color)ColorConverter.ConvertFromString(colorString);
                preview.Fill = new SolidColorBrush(color);
            }
            catch
            {
                preview.Fill = Brushes.Transparent;
            }
        }

        private void Slider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            if (isLoading) return;

            var slider = sender as Slider;
            if (slider == null) return;

            // Find the corresponding textbox and determine if value should be integer or float
            TextBox textBox = null;
            bool isInteger = false;

            if (slider == WindowWidthSlider) { textBox = WindowWidthTextBox; isInteger = true; }
            else if (slider == WindowHeightSlider) { textBox = WindowHeightTextBox; isInteger = true; }
            else if (slider == BackgroundThresholdChangeSlider) textBox = BackgroundThresholdChangeTextBox;
            else if (slider == BackgroundThresholdMinSlider) textBox = BackgroundThresholdMinTextBox;
            else if (slider == BackgroundThresholdMaxSlider) textBox = BackgroundThresholdMaxTextBox;
            else if (slider == BackgroundAlphaMaxSlider) textBox = BackgroundAlphaMaxTextBox;
            else if (slider == BackgroundAlphaMinSlider) textBox = BackgroundAlphaMinTextBox;
            else if (slider == BackgroundDampingSlider) textBox = BackgroundDampingTextBox;
            else if (slider == DateSizeSlider) { textBox = DateSizeTextBox; isInteger = true; }
            else if (slider == TimeSizeSlider) { textBox = TimeSizeTextBox; isInteger = true; }
            else if (slider == SecondsSizeSlider) { textBox = SecondsSizeTextBox; isInteger = true; }
            else if (slider == PaletteFontSizeSlider) { textBox = PaletteFontSizeTextBox; isInteger = true; }
            else if (slider == PaletteWidthSlider) { textBox = PaletteWidthTextBox; isInteger = true; }

            if (textBox != null)
            {
                // Format as integer (no decimals) or float (2 decimals)
                if (isInteger)
                {
                    textBox.Text = Math.Round(slider.Value).ToString("F0", CultureInfo.InvariantCulture);
                }
                else
                {
                    textBox.Text = slider.Value.ToString("F2", CultureInfo.InvariantCulture);
                }
            }

            hasChanges = true;
        }

        private void ColorTextBox_Changed(object sender, TextChangedEventArgs e)
        {
            if (isLoading) return;

            var textBox = sender as TextBox;
            if (textBox == null) return;

            // Find the corresponding preview rectangle
            System.Windows.Shapes.Rectangle preview = null;
            if (textBox == BackgroundColorTextBox) preview = BackgroundColorPreview;
            else if (textBox == DateColorTextBox) preview = DateColorPreview;
            else if (textBox == TimeColorTextBox) preview = TimeColorPreview;
            else if (textBox == SecondsColorTextBox) preview = SecondsColorPreview;
            else if (textBox == PaletteBackgroundColorTextBox) preview = PaletteBackgroundColorPreview;
            else if (textBox == PaletteTextColorTextBox) preview = PaletteTextColorPreview;
            else if (textBox == PaletteSelectedBackgroundTextBox) preview = PaletteSelectedBackgroundPreview;

            if (preview != null)
            {
                UpdateColorPreview(textBox, preview);
            }

            hasChanges = true;
        }

        private void Setting_Changed(object sender, RoutedEventArgs e)
        {
            if (isLoading) return;
            hasChanges = true;
        }

        private void SettingsWindow_KeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            // Check for Ctrl+S
            if (e.Key == System.Windows.Input.Key.S && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Control) == System.Windows.Input.ModifierKeys.Control)
            {
                e.Handled = true;
                SaveButton_Click(sender, new RoutedEventArgs());
            }
        }

        private void SaveButton_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                SaveSettings();
                hasChanges = false;
                RestartApplication();
            }
            catch
            {
                // Error already shown in SaveSettings, don't restart
            }
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e)
        {
            hasChanges = false;
            this.Close();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            if (hasChanges)
            {
                var result = MessageBox.Show(
                    "You have unsaved changes. Do you want to save and restart the application?",
                    "Unsaved Changes",
                    MessageBoxButton.YesNoCancel,
                    MessageBoxImage.Question);

                if (result == MessageBoxResult.Yes)
                {
                    try
                    {
                        SaveSettings();
                        hasChanges = false;
                        RestartApplication();
                    }
                    catch
                    {
                        // Error already shown in SaveSettings, cancel close
                        e.Cancel = true;
                    }
                }
                else if (result == MessageBoxResult.Cancel)
                {
                    e.Cancel = true;
                }
                // If No: just close without saving (hasChanges stays true but window closes)
            }
        }

        private void SaveSettings()
        {
            try
            {
                // Font section
                iniData["font"]["family"] = FontFamilyComboBox.SelectedItem?.ToString() ?? "Consolas";

                // Window section (ensure integers, no decimals)
                iniData["window"]["width"] = Math.Round(WindowWidthSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["window"]["height"] = Math.Round(WindowHeightSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["window"]["fixed"] = WindowFixedCheckBox.IsChecked == true ? "1" : "0";
                iniData["window"]["fixed_corner"] = GetComboBoxTag(WindowFixedCornerComboBox);
                iniData["window"]["debug"] = WindowDebugCheckBox.IsChecked == true ? "1" : "0";

                // Background section
                iniData["background"]["color"] = BackgroundColorTextBox.Text;
                iniData["background"]["auto_brightness_adjustment"] = BackgroundAutoBrightnessCheckBox.IsChecked == true ? "1" : "0";
                iniData["background"]["threshold_change"] = BackgroundThresholdChangeTextBox.Text;
                iniData["background"]["threshold_min"] = BackgroundThresholdMinTextBox.Text;
                iniData["background"]["threshold_max"] = BackgroundThresholdMaxTextBox.Text;
                iniData["background"]["alpha_max"] = BackgroundAlphaMaxTextBox.Text;
                iniData["background"]["alpha_min"] = BackgroundAlphaMinTextBox.Text;
                iniData["background"]["damping"] = BackgroundDampingTextBox.Text;

                // Date section
                iniData["date"]["show"] = DateShowCheckBox.IsChecked == true ? "1" : "0";
                iniData["date"]["size"] = Math.Round(DateSizeSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["date"]["format"] = DateFormatTextBox.Text;
                iniData["date"]["color"] = DateColorTextBox.Text;
                iniData["date"]["vertical_alignment"] = GetComboBoxTag(DateVerticalAlignmentComboBox);
                iniData["date"]["horizontal_alignment"] = GetComboBoxTag(DateHorizontalAlignmentComboBox);

                // Time section
                iniData["time"]["size"] = Math.Round(TimeSizeSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["time"]["format"] = TimeFormatTextBox.Text;
                iniData["time"]["color"] = TimeColorTextBox.Text;
                iniData["time"]["vertical_alignment"] = GetComboBoxTag(TimeVerticalAlignmentComboBox);
                iniData["time"]["horizontal_alignment"] = GetComboBoxTag(TimeHorizontalAlignmentComboBox);

                // Seconds section
                iniData["seconds"]["show"] = SecondsShowCheckBox.IsChecked == true ? "1" : "0";
                iniData["seconds"]["size"] = Math.Round(SecondsSizeSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["seconds"]["color"] = SecondsColorTextBox.Text;
                iniData["seconds"]["vertical_alignment"] = GetComboBoxTag(SecondsVerticalAlignmentComboBox);
                iniData["seconds"]["horizontal_alignment"] = GetComboBoxTag(SecondsHorizontalAlignmentComboBox);

                // Stack panel section
                iniData["stackpanel"]["vertical_alignment"] = GetComboBoxTag(StackPanelVerticalAlignmentComboBox);
                iniData["stackpanel"]["horizontal_alignment"] = GetComboBoxTag(StackPanelHorizontalAlignmentComboBox);

                // Command palette section (ensure section exists)
                if (!iniData.Sections.ContainsSection("command_palette"))
                {
                    iniData.Sections.AddSection("command_palette");
                }

                iniData["command_palette"]["background_color"] = PaletteBackgroundColorTextBox.Text;
                iniData["command_palette"]["text_color"] = PaletteTextColorTextBox.Text;
                iniData["command_palette"]["selected_background"] = PaletteSelectedBackgroundTextBox.Text;
                iniData["command_palette"]["font_family"] = PaletteFontFamilyComboBox.SelectedItem?.ToString() ?? "Consolas";
                iniData["command_palette"]["font_size"] = Math.Round(PaletteFontSizeSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["command_palette"]["width"] = Math.Round(PaletteWidthSlider.Value).ToString("F0", CultureInfo.InvariantCulture);
                iniData["command_palette"]["show_icons"] = PaletteShowIconsCheckBox.IsChecked == true ? "1" : "0";

                // Write to file
                var parser = new FileIniDataParser();
                parser.WriteFile("settings.ini", iniData);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error saving settings: {ex.Message}", "Save Error", MessageBoxButton.OK, MessageBoxImage.Error);
                throw; // Re-throw so caller knows save failed
            }
        }

        private string GetComboBoxTag(ComboBox comboBox)
        {
            var selectedItem = comboBox.SelectedItem as ComboBoxItem;
            return selectedItem?.Tag?.ToString() ?? "";
        }

        private void RestartApplication()
        {
            try
            {
                // Get the current exe path
                string exePath = Process.GetCurrentProcess().MainModule.FileName;

                // Start a new instance
                Process.Start(exePath);

                // Shutdown current instance
                Application.Current.Shutdown();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error restarting application: {ex.Message}\nPlease restart manually.", "Restart Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }
    }
}
