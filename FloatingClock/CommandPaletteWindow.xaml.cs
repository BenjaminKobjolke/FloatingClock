using IniParser.Model;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace FloatingClock
{
    /// <summary>
    /// Interaction logic for CommandPaletteWindow.xaml
    /// </summary>
    public partial class CommandPaletteWindow : Window
    {
        private Window parentWindow;
        private List<CommandItem> commands;

        public CommandPaletteWindow(Window parent, IniData iniData, List<CommandItem> commandList)
        {
            InitializeComponent();
            parentWindow = parent;
            commands = commandList;

            // Load settings from INI
            LoadSettings(iniData);

            // Populate command list
            CommandListBox.ItemsSource = commands;

            // Select first item by default
            if (commands.Count > 0)
            {
                CommandListBox.SelectedIndex = 0;
            }
        }

        private void LoadSettings(IniData iniData)
        {
            try
            {
                // Check if command_palette section exists
                if (iniData.Sections.ContainsSection("command_palette"))
                {
                    var section = iniData["command_palette"];

                    // Background color
                    if (!string.IsNullOrEmpty(section["background_color"]))
                    {
                        try
                        {
                            Color bgColor = (Color)ColorConverter.ConvertFromString(section["background_color"]);
                            PaletteBorder.Background = new SolidColorBrush(bgColor);
                        }
                        catch { /* Use default */ }
                    }

                    // Text color
                    if (!string.IsNullOrEmpty(section["text_color"]))
                    {
                        try
                        {
                            Color textColor = (Color)ColorConverter.ConvertFromString(section["text_color"]);
                            TitleText.Foreground = new SolidColorBrush(textColor);
                            CommandListBox.Foreground = new SolidColorBrush(textColor);
                        }
                        catch { /* Use default */ }
                    }

                    // Selected background color
                    if (!string.IsNullOrEmpty(section["selected_background"]))
                    {
                        try
                        {
                            Color selColor = (Color)ColorConverter.ConvertFromString(section["selected_background"]);
                            // Update the style trigger - this is more complex, so we'll apply it programmatically
                            UpdateSelectionBackground(selColor);
                        }
                        catch { /* Use default */ }
                    }

                    // Font family
                    if (!string.IsNullOrEmpty(section["font_family"]))
                    {
                        try
                        {
                            FontFamily fontFamily = new FontFamily(section["font_family"]);
                            CommandListBox.FontFamily = fontFamily;
                        }
                        catch { /* Use default */ }
                    }

                    // Font size
                    if (!string.IsNullOrEmpty(section["font_size"]))
                    {
                        if (double.TryParse(section["font_size"], NumberStyles.Any, CultureInfo.InvariantCulture, out double fontSize))
                        {
                            CommandListBox.FontSize = fontSize;
                        }
                    }

                    // Width
                    if (!string.IsNullOrEmpty(section["width"]))
                    {
                        if (double.TryParse(section["width"], NumberStyles.Any, CultureInfo.InvariantCulture, out double width))
                        {
                            this.Width = width;
                        }
                    }

                    // Padding
                    if (!string.IsNullOrEmpty(section["padding"]))
                    {
                        if (double.TryParse(section["padding"], NumberStyles.Any, CultureInfo.InvariantCulture, out double padding))
                        {
                            PaletteBorder.Padding = new Thickness(padding);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Error loading command palette settings: {ex.Message}");
                // Use default values
            }
        }

        private void UpdateSelectionBackground(Color color)
        {
            // Create a new style with the custom selection color
            Style itemStyle = new Style(typeof(System.Windows.Controls.ListBoxItem));
            itemStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.PaddingProperty, new Thickness(5)));
            itemStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BackgroundProperty, Brushes.Transparent));
            itemStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.ForegroundProperty, CommandListBox.Foreground));

            var template = new ControlTemplate(typeof(System.Windows.Controls.ListBoxItem));
            var border = new FrameworkElementFactory(typeof(System.Windows.Controls.Border));
            border.Name = "ItemBorder";
            border.SetBinding(System.Windows.Controls.Border.BackgroundProperty, new System.Windows.Data.Binding("Background") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetBinding(System.Windows.Controls.Border.PaddingProperty, new System.Windows.Data.Binding("Padding") { RelativeSource = new System.Windows.Data.RelativeSource(System.Windows.Data.RelativeSourceMode.TemplatedParent) });
            border.SetValue(System.Windows.Controls.Border.CornerRadiusProperty, new CornerRadius(3));

            var presenter = new FrameworkElementFactory(typeof(System.Windows.Controls.ContentPresenter));
            border.AppendChild(presenter);
            template.VisualTree = border;

            var selectedTrigger = new Trigger { Property = System.Windows.Controls.ListBoxItem.IsSelectedProperty, Value = true };
            selectedTrigger.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BackgroundProperty, new SolidColorBrush(color), "ItemBorder"));
            template.Triggers.Add(selectedTrigger);

            var hoverTrigger = new Trigger { Property = System.Windows.Controls.ListBoxItem.IsMouseOverProperty, Value = true };
            Color hoverColor = Color.FromArgb((byte)(color.A / 2), color.R, color.G, color.B);
            hoverTrigger.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.BackgroundProperty, new SolidColorBrush(hoverColor), "ItemBorder"));
            template.Triggers.Add(hoverTrigger);

            itemStyle.Setters.Add(new Setter(System.Windows.Controls.ListBoxItem.TemplateProperty, template));
            CommandListBox.ItemContainerStyle = itemStyle;
        }

        private void CommandPaletteWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // Position the window centered over parent, with screen edge validation
            PositionWindow();

            // Focus the listbox so keyboard input works immediately
            CommandListBox.Focus();
        }

        private void PositionWindow()
        {
            // Force measure to get actual size
            this.UpdateLayout();
            this.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

            // Calculate centered position over parent window
            double desiredLeft = parentWindow.Left + (parentWindow.Width - this.ActualWidth) / 2;
            double desiredTop = parentWindow.Top + (parentWindow.Height - this.ActualHeight) / 2;

            // Get current monitor
            var screen = System.Windows.Forms.Screen.FromHandle(new System.Windows.Interop.WindowInteropHelper(parentWindow).Handle);

            // Validate and adjust position to ensure palette stays within screen bounds
            ValidatePosition(ref desiredLeft, ref desiredTop, screen);

            this.Left = desiredLeft;
            this.Top = desiredTop;
        }

        private void ValidatePosition(ref double left, ref double top, System.Windows.Forms.Screen screen)
        {
            double workAreaLeft = screen.WorkingArea.X;
            double workAreaTop = screen.WorkingArea.Y;
            double workAreaWidth = screen.WorkingArea.Width;
            double workAreaHeight = screen.WorkingArea.Height;

            double windowWidth = this.ActualWidth;
            double windowHeight = this.ActualHeight;

            // Ensure window stays within work area
            // Left edge
            if (left < workAreaLeft)
                left = workAreaLeft;

            // Right edge
            if (left + windowWidth > workAreaLeft + workAreaWidth)
                left = workAreaLeft + workAreaWidth - windowWidth;

            // Top edge
            if (top < workAreaTop)
                top = workAreaTop;

            // Bottom edge
            if (top + windowHeight > workAreaTop + workAreaHeight)
                top = workAreaTop + workAreaHeight - windowHeight;
        }

        private void CommandPaletteWindow_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Escape)
            {
                // Close the palette
                e.Handled = true;
                this.Close();
            }
            else if (e.Key == Key.Enter)
            {
                // Execute selected command
                e.Handled = true;
                ExecuteSelectedCommand();
            }
            else if (e.Key == Key.Up)
            {
                // Move selection up
                e.Handled = true;
                if (CommandListBox.SelectedIndex > 0)
                {
                    CommandListBox.SelectedIndex--;
                    CommandListBox.ScrollIntoView(CommandListBox.SelectedItem);
                }
            }
            else if (e.Key == Key.Down)
            {
                // Move selection down
                e.Handled = true;
                if (CommandListBox.SelectedIndex < commands.Count - 1)
                {
                    CommandListBox.SelectedIndex++;
                    CommandListBox.ScrollIntoView(CommandListBox.SelectedItem);
                }
            }
        }

        private void ExecuteSelectedCommand()
        {
            if (CommandListBox.SelectedItem is CommandItem selectedCommand)
            {
                // Close the palette first
                this.Close();

                // Execute the command action
                selectedCommand.Action?.Invoke();
            }
        }

        private void CommandListBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            // Ensure selected item is visible
            if (CommandListBox.SelectedItem != null)
            {
                CommandListBox.ScrollIntoView(CommandListBox.SelectedItem);
            }
        }
    }
}
