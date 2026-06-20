using System.Collections.Generic;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchTreeSelectionServiceTests
    {
        [Fact]
        public void SetLeafStates_SetsOnlyMatchingLeaves()
        {
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b", "main" });

            BranchTreeSelectionService.SetLeafStates(roots, new[] { "feature/a" }, true);

            var featureA = Find(roots, "feature/a");
            var featureB = Find(roots, "feature/b");
            var main = Find(roots, "main");

            Assert.True(featureA.IsChecked);
            Assert.False(featureB.IsChecked);
            Assert.False(main.IsChecked);
        }

        [Fact]
        public void SetLeafStatesFromTracked_SetsGroupToTrueWhenAllChildrenSelected()
        {
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b" });
            var selected = new HashSet<string> { "feature/a", "feature/b" };

            BranchTreeSelectionService.SetLeafStatesFromTracked(roots, selected);

            Assert.True(Find(roots, "feature").IsChecked);
        }

        [Fact]
        public void SetLeafStatesFromTracked_SetsGroupToIndeterminateWhenSomeChildrenSelected()
        {
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/a", "feature/b" });
            var selected = new HashSet<string> { "feature/a" };

            BranchTreeSelectionService.SetLeafStatesFromTracked(roots, selected);

            Assert.Null(Find(roots, "feature").IsChecked);
        }

        [Fact]
        public void UpdateAncestorStates_UpdatesParentAndGrandparent()
        {
            var roots = BranchOrganizer.OrganizeBranches(new[] { "feature/auth/a", "feature/auth/b" });
            var childGroup = Find(roots, "feature/auth");
            var selected = new HashSet<string> { "feature/auth/a" };

            BranchTreeSelectionService.UpdateAncestorStates(childGroup, selected);

            Assert.Null(childGroup.IsChecked);
            Assert.Null(Find(roots, "feature").IsChecked);
        }

        private static BranchItem Find(IEnumerable<BranchItem> items, string fullName)
        {
            foreach (var item in items)
            {
                if (item.FullName == fullName)
                    return item;

                var found = item.Children.Count > 0 ? FindOrNull(item.Children, fullName) : null;
                if (found != null)
                    return found;
            }

            throw new KeyNotFoundException(fullName);
        }

        private static BranchItem? FindOrNull(IEnumerable<BranchItem> items, string fullName)
        {
            foreach (var item in items)
            {
                if (item.FullName == fullName)
                    return item;

                var found = item.Children.Count > 0 ? FindOrNull(item.Children, fullName) : null;
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
