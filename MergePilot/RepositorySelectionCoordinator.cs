using System;

namespace MergePilot
{
    /// <summary>
    /// Coordinates repository checkbox selection changes without depending on WPF controls.
    /// </summary>
    public class RepositorySelectionCoordinator
    {
        private readonly RepositorySelectionState _selectionState;

        public RepositorySelectionCoordinator(RepositorySelectionState selectionState)
        {
            _selectionState = selectionState ?? throw new ArgumentNullException(nameof(selectionState));
        }

        public RepositorySelectionChange ApplySelection(
            RepositorySelectionItem? repositoryItem,
            string? fallbackPath,
            bool isSelected)
        {
            var repositoryPath = repositoryItem?.Path;
            if (string.IsNullOrWhiteSpace(repositoryPath))
                repositoryPath = fallbackPath;

            if (string.IsNullOrWhiteSpace(repositoryPath))
                return RepositorySelectionChange.None;

            if (repositoryItem != null)
                repositoryItem.IsSelected = isSelected;

            _selectionState.SetSelected(repositoryPath, isSelected);

            return new RepositorySelectionChange(
                repositoryPath.Trim(),
                isSelected,
                isSelected,
                !isSelected);
        }
    }

    public readonly record struct RepositorySelectionChange(
        string RepositoryPath,
        bool IsSelected,
        bool ShouldLoadBranches,
        bool ShouldClearBranchCatalogs)
    {
        public static RepositorySelectionChange None { get; } = new(
            string.Empty,
            false,
            false,
            false);

        public bool HasRepository => !string.IsNullOrWhiteSpace(RepositoryPath);
    }
}
