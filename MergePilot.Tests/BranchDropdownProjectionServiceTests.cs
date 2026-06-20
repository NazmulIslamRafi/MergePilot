using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchDropdownProjectionServiceTests
    {
        [Fact]
        public void Build_WithHierarchicalBranches_ReturnsGroupsAndLeavesInBothLists()
        {
            var service = new BranchDropdownProjectionService();

            var projection = service.Build(
                new[] { "feature/auth/login", "main" },
                new[] { "release/v1", "develop" });

            Assert.Contains(projection.SourceItems, item => item.IsGroup && item.FullName == "feature");
            Assert.Contains(projection.SourceItems, item => item.IsGroup && item.FullName == "feature/auth");
            Assert.Contains(projection.SourceItems, item => !item.IsGroup && item.FullName == "feature/auth/login");
            Assert.Contains(projection.SourceItems, item => !item.IsGroup && item.FullName == "main");

            Assert.Contains(projection.TargetItems, item => item.IsGroup && item.FullName == "release");
            Assert.Contains(projection.TargetItems, item => !item.IsGroup && item.FullName == "release/v1");
            Assert.Contains(projection.TargetItems, item => !item.IsGroup && item.FullName == "develop");
        }

        [Fact]
        public void Build_CreatesIndependentSourceAndTargetBranchItems()
        {
            var service = new BranchDropdownProjectionService();

            var projection = service.Build(new[] { "main" }, new[] { "main" });

            var sourceMain = projection.SourceItems.Single(item => item.FullName == "main");
            var targetMain = projection.TargetItems.Single(item => item.FullName == "main");

            Assert.NotSame(sourceMain, targetMain);
        }

        [Fact]
        public void Build_CreatesIndependentSourceAndTargetGroupsAndLeaves()
        {
            var service = new BranchDropdownProjectionService();

            var projection = service.Build(new[] { "feature/a" }, new[] { "feature/a" });

            var sourceGroup = projection.SourceItems.Single(item => item.IsGroup && item.FullName == "feature");
            var targetGroup = projection.TargetItems.Single(item => item.IsGroup && item.FullName == "feature");
            var sourceLeaf = projection.SourceItems.Single(item => !item.IsGroup && item.FullName == "feature/a");
            var targetLeaf = projection.TargetItems.Single(item => !item.IsGroup && item.FullName == "feature/a");

            Assert.NotSame(sourceGroup, targetGroup);
            Assert.NotSame(sourceLeaf, targetLeaf);
            Assert.Same(sourceGroup, sourceLeaf.Parent);
            Assert.Same(targetGroup, targetLeaf.Parent);
        }

        [Fact]
        public void Build_WithLargeCatalog_PreservesAllLeaves()
        {
            var service = new BranchDropdownProjectionService();
            var branches = Enumerable.Range(0, 1000)
                .Select(i => $"feature/team-{i / 100}/branch-{i}")
                .ToList();

            var projection = service.Build(branches, branches);

            Assert.Equal(1000, CountLeaves(projection.SourceItems));
            Assert.Equal(1000, CountLeaves(projection.TargetItems));
            Assert.Contains(projection.SourceItems, item => item.IsGroup && item.FullName == "feature");
            Assert.Contains(projection.SourceItems, item => item.IsGroup && item.FullName == "feature/team-9");
        }

        private static int CountLeaves(IEnumerable<BranchItem> items)
        {
            return items.Count(item => !item.IsGroup);
        }
    }
}
