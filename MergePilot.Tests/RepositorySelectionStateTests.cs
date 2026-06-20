using Xunit;

namespace MergePilot.Tests
{
    public class RepositorySelectionStateTests
    {
        [Fact]
        public void SetSelected_WithCheckedPath_AddsPath()
        {
            var state = new RepositorySelectionState();

            state.SetSelected("C:\\repo", true);

            Assert.Contains("C:\\repo", state.SelectedRepositoryPaths);
        }

        [Fact]
        public void SetSelected_WithUncheckedPath_RemovesPath()
        {
            var state = new RepositorySelectionState();
            state.SetSelected("C:\\repo", true);

            state.SetSelected("C:\\repo", false);

            Assert.Empty(state.SelectedRepositoryPaths);
        }

        [Fact]
        public void SetSelected_DeduplicatesCaseInsensitively()
        {
            var state = new RepositorySelectionState();

            state.SetSelected("C:\\Repo", true);
            state.SetSelected("c:\\repo", true);

            Assert.Single(state.SelectedRepositoryPaths);
        }

        [Fact]
        public void Clear_RemovesAllSelectedPaths()
        {
            var state = new RepositorySelectionState();
            state.SetSelected("C:\\repo1", true);
            state.SetSelected("C:\\repo2", true);

            state.Clear();

            Assert.Empty(state.SelectedRepositoryPaths);
        }
    }
}
