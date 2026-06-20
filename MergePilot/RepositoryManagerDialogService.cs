using System.Windows;

namespace MergePilot
{
    public interface IRepositoryManagerDialogService
    {
        void ShowRepositoryManager(AppSettings settings, Window? owner);
    }

    public sealed class WpfRepositoryManagerDialogService : IRepositoryManagerDialogService
    {
        public void ShowRepositoryManager(AppSettings settings, Window? owner)
        {
            var dialog = new RepositoryManager();
            dialog.LoadData(settings);
            dialog.Owner = owner;
            dialog.ShowDialog();
        }
    }
}
