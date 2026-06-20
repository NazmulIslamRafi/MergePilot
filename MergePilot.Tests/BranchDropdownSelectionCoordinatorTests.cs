using System.Linq;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchDropdownSelectionCoordinatorTests
    {
        [Fact]
        public void ApplyToggle_WhenLeafSelected_TracksRoleAndUpdatesAncestors()
        {
            var state = new BranchSelectionState();
            var coordinator = new BranchDropdownSelectionCoordinator(state);
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b" });
            var flat = BranchOrganizer.FlattenBranches(roots);
            var leaf = Find(flat, "feature/a");

            coordinator.ApplyToggle(BranchSelectionRole.Source, flat, leaf, true);

            Assert.Contains("feature/a", state.SelectedSourceBranches);
            Assert.DoesNotContain("feature/a", state.SelectedTargetBranches);
            Assert.True(leaf.IsChecked);
            Assert.Null(Find(flat, "feature").IsChecked);
        }

        [Fact]
        public void ApplyToggle_WhenGroupSelected_TracksAndChecksAllLeafChildren()
        {
            var state = new BranchSelectionState();
            var coordinator = new BranchDropdownSelectionCoordinator(state);
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b", "main" });
            var flat = BranchOrganizer.FlattenBranches(roots);
            var group = Find(flat, "feature");

            coordinator.ApplyToggle(BranchSelectionRole.Target, flat, group, true);

            Assert.Contains("feature/a", state.SelectedTargetBranches);
            Assert.Contains("feature/b", state.SelectedTargetBranches);
            Assert.DoesNotContain("main", state.SelectedTargetBranches);
            Assert.True(Find(flat, "feature/a").IsChecked);
            Assert.True(Find(flat, "feature/b").IsChecked);
            Assert.True(group.IsChecked);
        }

        [Fact]
        public void ApplyToggle_WhenGroupUnchecked_RemovesLeafChildren()
        {
            var state = new BranchSelectionState();
            var coordinator = new BranchDropdownSelectionCoordinator(state);
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b" });
            var flat = BranchOrganizer.FlattenBranches(roots);
            var group = Find(flat, "feature");

            coordinator.ApplyToggle(BranchSelectionRole.Source, flat, group, true);
            coordinator.ApplyToggle(BranchSelectionRole.Source, flat, group, false);

            Assert.Empty(state.SelectedSourceBranches);
            Assert.False(Find(flat, "feature/a").IsChecked);
            Assert.False(Find(flat, "feature/b").IsChecked);
            Assert.False(group.IsChecked);
        }

        [Fact]
        public void RestoreTrackedSelection_AppliesTrackedBranchesForRole()
        {
            var state = new BranchSelectionState();
            var coordinator = new BranchDropdownSelectionCoordinator(state);
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b" });
            var flat = BranchOrganizer.FlattenBranches(roots);
            state.SetBranchSelected(BranchSelectionRole.Target, "feature/a", true);

            coordinator.RestoreTrackedSelection(BranchSelectionRole.Target, flat);

            Assert.True(Find(flat, "feature/a").IsChecked);
            Assert.False(Find(flat, "feature/b").IsChecked);
            Assert.Null(Find(flat, "feature").IsChecked);
        }

        private static BranchItem Find(System.Collections.Generic.IEnumerable<BranchItem> items, string fullName)
        {
            return items.Single(item => item.FullName == fullName);
        }
    }
}
