using System;

namespace MergePilot
{
    /// <summary>
    /// Applies branch catalog service results to branch selection state.
    /// </summary>
    public class BranchCatalogCoordinator
    {
        private readonly BranchCatalogService _branchCatalogService;

        public BranchCatalogCoordinator(BranchCatalogService branchCatalogService)
        {
            _branchCatalogService = branchCatalogService
                ?? throw new ArgumentNullException(nameof(branchCatalogService));
        }

        public void LoadInitialCatalog(AppSettings settings, BranchSelectionState branchSelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);

            branchSelectionState.ClearBranchCatalogs();
            foreach (var branch in _branchCatalogService.GetInitialBranchCatalog(settings))
            {
                branchSelectionState.AddBranchToCatalogs(branch);
            }
        }

        public void LoadRepositoryCatalog(
            AppSettings settings,
            string repoPath,
            BranchSelectionState branchSelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);

            branchSelectionState.ClearBranchCatalogs();
            foreach (var branch in _branchCatalogService.GetCustomBranchesForRepository(settings, repoPath))
            {
                branchSelectionState.AddBranchToCatalogs(branch);
            }
        }

        public bool AddRecentBranch(
            AppSettings settings,
            string? branch,
            BranchSelectionState branchSelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);

            var trimmed = branch?.Trim();
            var addedToRecent = _branchCatalogService.AddRecentBranch(settings, trimmed);
            branchSelectionState.AddBranchToCatalogs(trimmed);
            return addedToRecent;
        }

        public void ClearCatalogs(BranchSelectionState branchSelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);

            branchSelectionState.ClearBranchCatalogs();
        }

        public void ClearCatalogsAndSelections(BranchSelectionState branchSelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);

            branchSelectionState.ClearBranchCatalogs();
            branchSelectionState.ClearSelections();
        }
    }
}
