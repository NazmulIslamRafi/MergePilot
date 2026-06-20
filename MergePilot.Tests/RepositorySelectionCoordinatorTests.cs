using Xunit;

namespace MergePilot.Tests
{
    public class RepositorySelectionCoordinatorTests
    {
        [Fact]
        public void ApplySelection_WithRepositoryItem_SelectsItemAndTracksPath()
        {
            var state = new RepositorySelectionState();
            var coordinator = new RepositorySelectionCoordinator(state);
            var item = CreateItem("C:\\repo");

            var change = coordinator.ApplySelection(item, fallbackPath: null, isSelected: true);

            Assert.True(item.IsSelected);
            Assert.Contains("C:\\repo", state.SelectedRepositoryPaths);
            Assert.Equal("C:\\repo", change.RepositoryPath);
            Assert.True(change.ShouldLoadBranches);
            Assert.False(change.ShouldClearBranchCatalogs);
        }

        [Fact]
        public void ApplySelection_WithFallbackPath_TracksPathWithoutItem()
        {
            var state = new RepositorySelectionState();
            var coordinator = new RepositorySelectionCoordinator(state);

            var change = coordinator.ApplySelection(null, " C:\\repo ", isSelected: true);

            Assert.Contains("C:\\repo", state.SelectedRepositoryPaths);
            Assert.Equal("C:\\repo", change.RepositoryPath);
            Assert.True(change.HasRepository);
        }

        [Fact]
        public void ApplySelection_WhenUnchecked_RemovesPathAndRequestsBranchCatalogClear()
        {
            var state = new RepositorySelectionState();
            var coordinator = new RepositorySelectionCoordinator(state);
            var item = CreateItem("C:\\repo");
            coordinator.ApplySelection(item, fallbackPath: null, isSelected: true);

            var change = coordinator.ApplySelection(item, fallbackPath: null, isSelected: false);

            Assert.False(item.IsSelected);
            Assert.Empty(state.SelectedRepositoryPaths);
            Assert.False(change.ShouldLoadBranches);
            Assert.True(change.ShouldClearBranchCatalogs);
        }

        [Fact]
        public void ApplySelection_WithBlankPath_ReturnsNoChange()
        {
            var state = new RepositorySelectionState();
            var coordinator = new RepositorySelectionCoordinator(state);

            var change = coordinator.ApplySelection(null, " ", isSelected: true);

            Assert.False(change.HasRepository);
            Assert.Empty(state.SelectedRepositoryPaths);
        }

        private static RepositorySelectionItem CreateItem(string path)
        {
            return new RepositorySelectionItem(new AppSettings.RepositoryEntry
            {
                Name = "Repo",
                Path = path
            });
        }
    }
}
