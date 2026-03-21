using System.Windows;
using System.Windows.Input;

namespace FloatingClock
{
    public partial class InputDialog : Window
    {
        public string InputValue { get; private set; }

        public InputDialog(string title, string defaultValue)
        {
            InitializeComponent();
            TitleText.Text = title;
            InputTextBox.Text = defaultValue;
        }

        private void InputDialog_Loaded(object sender, RoutedEventArgs e)
        {
            InputTextBox.Focus();
            InputTextBox.SelectAll();
        }

        private void InputDialog_KeyDown(object sender, KeyEventArgs e)
        {
            if (e.Key == Key.Enter)
            {
                e.Handled = true;
                InputValue = InputTextBox.Text;
                DialogResult = true;
                Close();
            }
            else if (e.Key == Key.Escape)
            {
                e.Handled = true;
                DialogResult = false;
                Close();
            }
        }
    }
}
