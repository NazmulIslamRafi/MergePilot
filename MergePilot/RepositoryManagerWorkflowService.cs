using System;
using System.Windows;

namespace MergePilot
{
    /// <summary>
    /// Owns launching the repository manager dialog and reloading settings afterward.
    /// </summary>
    public class RepositoryManagerWorkflowService
    {
        private readonly IRepositoryManagerDialogService _dialogService;
        private readonly Func<AppSettings> _settingsLoader;

        public RepositoryManagerWorkflowService(
            IRepositoryManagerDialogService? dialogService = null,
            Func<AppSettings>? settingsLoader = null)
        {
            _dialogService = dialogService ?? new WpfRepositoryManagerDialogService();
            _settingsLoader = settingsLoader ?? AppSettings.LoadWithFallback;
        }

        public AppSettings ShowAndReload(AppSettings settings, Window? owner = null)
        {
            ArgumentNullException.ThrowIfNull(settings);

            _dialogService.ShowRepositoryManager(settings, owner);
            return _settingsLoader();
        }
    }
}
