using System;
using WpfListView = System.Windows.Controls.ListView;
using WpfSelectionChangedEventHandler = System.Windows.Controls.SelectionChangedEventHandler;
using WpfSelectionMode = System.Windows.Controls.SelectionMode;

namespace MergePilot
{
    /// <summary>
    /// Applies and reads WPF repository list visual state.
    /// </summary>
    internal sealed class RepositoryListVisualState
    {
        public RepositoryListSelection GetSelection(WpfListView? repositoryList)
        {
            try
            {
                if (repositoryList == null)
                    return RepositoryListSelection.None;

                return new RepositoryListSelection(
                    repositoryList.SelectedIndex,
                    repositoryList.SelectedItem as AppSettings.RepositoryEntry);
            }
            catch
            {
                return RepositoryListSelection.None;
            }
        }

        public void ConfigureSingleSelection(
            WpfListView? repositoryList,
            WpfSelectionChangedEventHandler selectionChanged)
        {
            ArgumentNullException.ThrowIfNull(selectionChanged);

            TryApply(() =>
            {
                if (repositoryList == null)
                    return;

                repositoryList.SelectionMode = WpfSelectionMode.Single;
                repositoryList.SelectionChanged -= selectionChanged;
                repositoryList.SelectionChanged += selectionChanged;
            });
        }

        public void SelectIndexIfAvailable(WpfListView? repositoryList, int index)
        {
            TryApply(() =>
            {
                if (repositoryList == null)
                    return;

                if (index >= 0 && index < repositoryList.Items.Count)
                    repositoryList.SelectedIndex = index;
            });
        }

        public void ScrollIntoView(WpfListView? repositoryList, object? item)
        {
            TryApply(() =>
            {
                if (repositoryList != null && item != null)
                    repositoryList.ScrollIntoView(item);
            });
        }

        private static void TryApply(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // Preserve best-effort UI behavior from the existing code-behind.
            }
        }
    }

    internal readonly record struct RepositoryListSelection(
        int Index,
        AppSettings.RepositoryEntry? Repository)
    {
        public static RepositoryListSelection None { get; } = new(-1, null);

        public bool HasIndexSelection => Index >= 0;

        public bool HasRepository => Repository != null;
    }
}
