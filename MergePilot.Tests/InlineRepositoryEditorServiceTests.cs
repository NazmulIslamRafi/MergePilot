using System;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class InlineRepositoryEditorServiceTests : IDisposable
    {
        private readonly string _testDirectory;

        public InlineRepositoryEditorServiceTests()
        {
            _testDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.InlineRepositoryEditor", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testDirectory);
            AppSettings.SettingsPathOverride = Path.Combine(_testDirectory, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

            if (Directory.Exists(_testDirectory))
                Directory.Delete(_testDirectory, recursive: true);
        }

        [Fact]
        public void ResolveSelectedRepository_PrefersSelectedItemPathOverIndex()
        {
            var settings = CreateSettingsWithRepositories();
            var service = CreateService();
            var selectedItem = new AppSettings.RepositoryEntry
            {
                Name = "Different list item",
                Path = "c:\\repo-two"
            };

            var selection = service.ResolveSelectedRepository(settings, selectedIndex: 0, selectedItem);

            Assert.True(selection.HasRepository);
            Assert.Equal(1, selection.Index);
            Assert.Equal("Repo Two", selection.Repository?.Name);
        }

        [Fact]
        public void ResolveSelectedRepository_FallsBackToSelectedIndex()
        {
            var settings = CreateSettingsWithRepositories();
            var service = CreateService();

            var selection = service.ResolveSelectedRepository(settings, selectedIndex: 0, selectedRepository: null);

            Assert.True(selection.HasRepository);
            Assert.Equal(0, selection.Index);
            Assert.Equal("Repo One", selection.Repository?.Name);
        }

        [Fact]
        public void ResolveSelectedRepository_WithInvalidSelection_ReturnsNoRepository()
        {
            var settings = new AppSettings();
            var service = CreateService();

            var selection = service.ResolveSelectedRepository(settings, selectedIndex: -1, selectedRepository: null);

            Assert.False(selection.HasRepository);
        }

        [Fact]
        public void RequiresMissingGitConfirmation_ReturnsFalseForGitDirectoryOrFileMarker()
        {
            var service = CreateService();
            var gitDirectoryRepo = CreateRepositoryWithGitDirectory("repo-dir");
            var gitFileRepo = CreateRepositoryWithGitFile("repo-file");

            Assert.False(service.RequiresMissingGitConfirmation(gitDirectoryRepo));
            Assert.False(service.RequiresMissingGitConfirmation(gitFileRepo));
        }

        [Fact]
        public void AddUpdateRemoveRepository_DelegatesThroughConfigurationService()
        {
            var settings = new AppSettings();
            var service = CreateService();

            var addResult = service.AddRepository(settings, "Repo", "C:\\missing", allowMissingGitRepository: true);
            var updateResult = service.UpdateRepository(settings, addResult.Index, "Renamed", "C:\\renamed", allowMissingGitRepository: true);
            var removeResult = service.RemoveRepository(settings, updateResult.Index);

            Assert.True(addResult.IsSuccess);
            Assert.True(updateResult.IsSuccess);
            Assert.True(removeResult.IsSuccess);
            Assert.Empty(settings.Repositories);
        }

        private static InlineRepositoryEditorService CreateService()
        {
            return new InlineRepositoryEditorService(new RepositoryConfigurationService());
        }

        private static AppSettings CreateSettingsWithRepositories()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Repo One",
                Path = "C:\\repo-one"
            });
            settings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Repo Two",
                Path = "C:\\repo-two"
            });

            return settings;
        }

        private string CreateRepositoryWithGitDirectory(string name)
        {
            var repositoryPath = Path.Combine(_testDirectory, name);
            Directory.CreateDirectory(Path.Combine(repositoryPath, ".git"));
            return repositoryPath;
        }

        private string CreateRepositoryWithGitFile(string name)
        {
            var repositoryPath = Path.Combine(_testDirectory, name);
            Directory.CreateDirectory(repositoryPath);
            File.WriteAllText(Path.Combine(repositoryPath, ".git"), "gitdir: ../actual-git");
            return repositoryPath;
        }
    }
}
