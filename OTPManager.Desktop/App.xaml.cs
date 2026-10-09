using OTPManager.Desktop.Services;
using System.Windows;

namespace OTPManager.Desktop
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            ThemeService.Initialize();
        }
    }
}
