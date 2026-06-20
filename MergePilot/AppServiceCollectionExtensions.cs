using Microsoft.Extensions.DependencyInjection;

namespace MergePilot
{
    /// <summary>
    /// Registers all MergePilot services into the DI container.
    /// Add new services here as they are extracted from MainWindow.
    /// </summary>
    internal static class AppServiceCollectionExtensions
    {
        public static IServiceCollection AddMergePilotServices(this IServiceCollection services)
        {
            // Adapter services — registered via their interfaces so tests can substitute mocks
            services.AddSingleton<IUserDialogService, WpfUserDialogService>();
            services.AddSingleton<IFilePickerService, WpfFilePickerService>();
            services.AddSingleton<IClipboardService, WpfClipboardService>();

            return services;
        }
    }
}
