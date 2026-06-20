using System.Collections.Generic;

namespace MergePilot
{
    /// <summary>
    /// Builds flattened source/target branch dropdown item lists from branch catalogs.
    /// </summary>
    public class BranchDropdownProjectionService
    {
        public BranchDropdownProjection Build(
            IEnumerable<string> sourceBranchCatalog,
            IEnumerable<string> targetBranchCatalog)
        {
            var sourceTree = BranchOrganizer.OrganizeBranches(sourceBranchCatalog);
            var targetTree = BranchOrganizer.OrganizeBranches(targetBranchCatalog);

            return new BranchDropdownProjection(
                BranchOrganizer.FlattenBranches(sourceTree),
                BranchOrganizer.FlattenBranches(targetTree));
        }
    }

    public record BranchDropdownProjection(
        IReadOnlyList<BranchItem> SourceItems,
        IReadOnlyList<BranchItem> TargetItems);
}
