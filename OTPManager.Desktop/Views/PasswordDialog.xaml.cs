using System.Windows;

namespace OTPManager.Desktop.Views
{
    public partial class PasswordDialog : Window
    {
        public string Password => PasswordInput.Password;

        public PasswordDialog(string prompt = "Introduce la contraseña:")
        {
            InitializeComponent();
            PromptText.Text = prompt;
            Loaded += (s, e) => PasswordInput.Focus();
        }

        private void Accept_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
