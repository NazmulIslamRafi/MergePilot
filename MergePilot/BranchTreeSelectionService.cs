using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Applies selected branch state to BranchItem trees without depending on WPF controls.
    /// </summary>
    public static class BranchTreeSelectionService
    {
        public static void SetLeafStates(IEnumerable<BranchItem> items, IEnumerable<string> leafFullNames, bool isChecked)
        {
            var leafSet = new HashSet<string>(leafFullNames ?? Enumerable.Empty<string>(), System.StringComparer.OrdinalIgnoreCase);
            foreach (var item in items ?? Enumerable.Empty<BranchItem>())
            {
                SetLeafStatesRecursive(item, leafSet, isChecked);
            }
        }

        public static void SetLeafStatesFromTracked(IEnumerable<BranchItem> items, IReadOnlySet<string> trackedBranches)
        {
            var tracked = trackedBranches ?? new HashSet<string>();
            foreach (var item in items ?? Enumerable.Empty<BranchItem>())
            {
                ApplyTrackedStateRecursive(item, tracked);
            }
        }

        public static void UpdateAncestorStates(BranchItem? parentItem, IReadOnlySet<string> trackedBranches)
        {
            var current = parentItem;
            while (current != null)
            {
                current.IsChecked = GetGroupCheckedState(current, trackedBranches);
                current = current.Parent;
            }
        }

        public static bool? GetGroupCheckedState(BranchItem groupItem, IReadOnlySet<string> trackedBranches)
        {
            if (groupItem == null)
                return false;

            var tracked = trackedBranches ?? new HashSet<string>();
            var leafChildren = BranchOrganizer.GetChildBranches(groupItem);
            if (leafChildren.Count == 0)
                return false;

            var checkedCount = leafChildren.Count(tracked.Contains);
            if (checkedCount == leafChildren.Count)
                return true;

            if (checkedCount == 0)
                return false;

            return null;
        }

        private static void SetLeafStatesRecursive(BranchItem item, HashSet<string> leafFullNames, bool isChecked)
        {
            if (!item.IsGroup)
            {
                if (leafFullNames.Contains(item.FullName))
                    item.IsChecked = isChecked;
                return;
            }

            foreach (var child in item.Children)
            {
                SetLeafStatesRecursive(child, leafFullNames, isChecked);
            }
        }

        private static bool? ApplyTrackedStateRecursive(BranchItem item, IReadOnlySet<string> trackedBranches)
        {
            if (!item.IsGroup)
            {
                item.IsChecked = trackedBranches.Contains(item.FullName);
                return item.IsChecked;
            }

            foreach (var child in item.Children)
            {
                ApplyTrackedStateRecursive(child, trackedBranches);
            }

            item.IsChecked = GetGroupCheckedState(item, trackedBranches);
            return item.IsChecked;
        }
    }
}
