using System.Windows;
using Application = System.Windows.Application;

namespace MergePilot
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        protected override void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // Prevent auto-shutdown
            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashScreen();
            splash.Show();

            // Simulate loading (use await Task.Delay for async)
            System.Threading.Thread.Sleep(3000);

            var mainWindow = new MainWindow();
            //mainWindow.Show();

            // Close splash after main window shows
            splash.Close();

            // Now revert shutdown mode
            Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
            Application.Current.MainWindow = mainWindow;
        }


    }

}
