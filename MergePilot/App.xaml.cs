using System;
using System.Threading.Tasks;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using Application = System.Windows.Application;

namespace MergePilot
{
    /// <summary>
    /// Interaction logic for App.xaml.
    /// </summary>
    public partial class App : Application
    {
        private static IServiceProvider? _services;

        /// <summary>
        /// Application-wide DI container. Available after OnStartup completes.
        /// </summary>
        public static IServiceProvider Services => _services
            ?? throw new InvalidOperationException("Services accessed before App.OnStartup completed.");

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            _services = new ServiceCollection()
                .AddMergePilotServices()
                .BuildServiceProvider();

            Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var splash = new SplashScreen();
            splash.Show();

            try
            {
                await Task.Run(AppSettings.LoadWithFallback);

                var mainWindow = new MainWindow(
                    _services.GetRequiredService<IUserDialogService>(),
                    _services.GetRequiredService<IFilePickerService>(),
                    _services.GetRequiredService<IClipboardService>());
                Application.Current.MainWindow = mainWindow;
                Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
                mainWindow.Show();
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show(
                    $"MergePilot failed to start: {ex.Message}",
                    "Startup Error",
                    MessageBoxButton.OK,
                    MessageBoxImage.Error);
                Shutdown(-1);
            }
            finally
            {
                splash.Close();
            }
        }
    }
}
