using System;
using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Coordinates branch dropdown selection changes without depending on WPF controls.
    /// </summary>
    public class BranchDropdownSelectionCoordinator
    {
        private readonly BranchSelectionState _selectionState;

        public BranchDropdownSelectionCoordinator(BranchSelectionState selectionState)
        {
            _selectionState = selectionState ?? throw new ArgumentNullException(nameof(selectionState));
        }

        public void ApplyToggle(
            BranchSelectionRole role,
            IEnumerable<BranchItem>? rootItems,
            BranchItem? toggledItem,
            bool isChecked)
        {
            if (toggledItem == null)
                return;

            toggledItem.IsChecked = isChecked;

            if (toggledItem.IsGroup)
            {
                ToggleGroup(role, rootItems, toggledItem, isChecked);
                return;
            }

            ToggleLeaf(role, toggledItem, isChecked);
        }

        public void RestoreTrackedSelection(
            BranchSelectionRole role,
            IEnumerable<BranchItem>? rootItems)
        {
            BranchTreeSelectionService.SetLeafStatesFromTracked(
                rootItems ?? Enumerable.Empty<BranchItem>(),
                _selectionState.GetSelectedSet(role));
        }

        private void ToggleLeaf(
            BranchSelectionRole role,
            BranchItem leafItem,
            bool isChecked)
        {
            _selectionState.SetBranchSelected(role, leafItem.FullName, isChecked);

            if (leafItem.Parent != null)
            {
                BranchTreeSelectionService.UpdateAncestorStates(
                    leafItem.Parent,
                    _selectionState.GetSelectedSet(role));
            }
        }

        private void ToggleGroup(
            BranchSelectionRole role,
            IEnumerable<BranchItem>? rootItems,
            BranchItem groupItem,
            bool shouldCheck)
        {
            var leafChildren = BranchOrganizer.GetChildBranches(groupItem);
            _selectionState.SetBranchesSelected(role, leafChildren, shouldCheck);

            BranchTreeSelectionService.SetLeafStates(
                rootItems ?? Enumerable.Empty<BranchItem>(),
                leafChildren,
                shouldCheck);

            if (groupItem.Parent != null)
            {
                BranchTreeSelectionService.UpdateAncestorStates(
                    groupItem.Parent,
                    _selectionState.GetSelectedSet(role));
            }
        }
    }
}
