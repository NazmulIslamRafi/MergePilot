using Xunit;

namespace MergePilot.Tests
{
    public class RepositorySelectionItemTests
    {
        [Fact]
        public void DisplayName_WithRepositoryName_ReturnsName()
        {
            var item = new RepositorySelectionItem(new AppSettings.RepositoryEntry
            {
                Name = "Friendly",
                Path = "C:\\repo"
            });

            Assert.Equal("Friendly", item.DisplayName);
        }

        [Fact]
        public void DisplayName_WithBlankRepositoryName_ReturnsPath()
        {
            var item = new RepositorySelectionItem(new AppSettings.RepositoryEntry
            {
                Name = " ",
                Path = "C:\\repo"
            });

            Assert.Equal("C:\\repo", item.DisplayName);
        }

        [Fact]
        public void IsSelected_WhenChanged_RaisesPropertyChanged()
        {
            var item = new RepositorySelectionItem(new AppSettings.RepositoryEntry
            {
                Name = "Repo",
                Path = "C:\\repo"
            });
            var raised = false;
            item.PropertyChanged += (_, args) =>
            {
                if (args.PropertyName == nameof(RepositorySelectionItem.IsSelected))
                    raised = true;
            };

            item.IsSelected = true;

            Assert.True(raised);
        }
    }
}
