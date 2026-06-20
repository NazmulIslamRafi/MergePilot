using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests
{
    public class RepositoryConfigurationServiceTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public RepositoryConfigurationServiceTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.RepositoryConfiguration", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_settingsDirectory);
            AppSettings.SettingsPathOverride = Path.Combine(_settingsDirectory, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

            if (Directory.Exists(_settingsDirectory))
                Directory.Delete(_settingsDirectory, recursive: true);
        }

        [Fact]
        public void AddRepository_WithGitRepository_AddsAndSavesRepository()
        {
            var settings = new AppSettings();
            var repoPath = CreateGitRepositoryDirectory();
            var service = new RepositoryConfigurationService();

            var result = service.AddRepository(settings, "Repo", repoPath, "https://example.com/repo.git");

            Assert.True(result.IsSuccess);
            Assert.Single(settings.Repositories);
            Assert.Equal("Repo", settings.Repositories[0].Name);
            Assert.Equal(repoPath, settings.Repositories[0].Path);
            Assert.Equal("https://example.com/repo.git", settings.Repositories[0].RemoteUrl);
            Assert.True(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void AddRepository_WithMissingGitAndRequireGit_ReturnsFailure()
        {
            var settings = new AppSettings();
            var repoPath = Directory.CreateDirectory(Path.Combine(_settingsDirectory, "not-git")).FullName;
            var service = new RepositoryConfigurationService();

            var result = service.AddRepository(settings, "Repo", repoPath);

            Assert.False(result.IsSuccess);
            Assert.Empty(settings.Repositories);
            Assert.Contains(".git", result.Message);
        }

        [Fact]
        public void AddRepository_WithMissingGitAllowed_PreservesInlineAddAnywayBehavior()
        {
            var settings = new AppSettings();
            var repoPath = Path.Combine(_settingsDirectory, "not-created");
            var service = new RepositoryConfigurationService();

            var result = service.AddRepository(
                settings,
                "Repo",
                repoPath,
                pathRequirement: RepositoryPathRequirement.AllowMissingGitRepository);

            Assert.True(result.IsSuccess);
            Assert.Single(settings.Repositories);
            Assert.Equal(repoPath, settings.Repositories[0].Path);
        }

        [Fact]
        public void UpdateRepository_WithValidIndex_UpdatesNamePathAndPreservesRemoteWhenNull()
        {
            var settings = new AppSettings();
            var oldPath = CreateGitRepositoryDirectory("old");
            var newPath = CreateGitRepositoryDirectory("new");
            settings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Old",
                Path = oldPath,
                RemoteUrl = "https://example.com/original.git"
            });

            var service = new RepositoryConfigurationService();

            var result = service.UpdateRepository(settings, 0, "New", newPath);

            Assert.True(result.IsSuccess);
            Assert.Equal("New", settings.Repositories[0].Name);
            Assert.Equal(newPath, settings.Repositories[0].Path);
            Assert.Equal("https://example.com/original.git", settings.Repositories[0].RemoteUrl);
        }

        [Fact]
        public void RemoveRepository_WithValidIndex_RemovesAndSavesRepository()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" });
            var service = new RepositoryConfigurationService();

            var result = service.RemoveRepository(settings, 0);

            Assert.True(result.IsSuccess);
            Assert.Empty(settings.Repositories);
            Assert.True(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void AddBranch_WithValidInput_AddsBranch()
        {
            var settings = new AppSettings();
            var service = new RepositoryConfigurationService();

            var result = service.AddBranch(settings, "feature/test", "Repo");

            Assert.True(result.IsSuccess);
            Assert.Single(settings.CustomBranches);
            Assert.Equal("feature/test", settings.CustomBranches[0].BranchName);
            Assert.Equal("Repo", settings.CustomBranches[0].Repository);
        }

        [Fact]
        public void AddBranch_WithInvalidBranchName_ReturnsFailure()
        {
            var settings = new AppSettings();
            var service = new RepositoryConfigurationService();

            var result = service.AddBranch(settings, "feature bad", "Repo");

            Assert.False(result.IsSuccess);
            Assert.Empty(settings.CustomBranches);
            Assert.Contains("Invalid branch name", result.Message);
        }

        [Fact]
        public void UpdateBranch_WithInvalidBranchName_ReturnsFailure()
        {
            var branch = new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" };
            var settings = new AppSettings();
            settings.CustomBranches.Add(branch);
            var service = new RepositoryConfigurationService();

            var result = service.UpdateBranch(settings, branch, "feature..bad", "Repo");

            Assert.False(result.IsSuccess);
            Assert.Equal("main", branch.BranchName);
            Assert.Contains("Invalid branch name", result.Message);
        }

        [Fact]
        public void RemoveBranch_WithExistingBranch_RemovesBranch()
        {
            var branch = new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" };
            var settings = new AppSettings();
            settings.CustomBranches.Add(branch);
            var service = new RepositoryConfigurationService();

            var result = service.RemoveBranch(settings, branch);

            Assert.True(result.IsSuccess);
            Assert.Empty(settings.CustomBranches);
        }

        [Fact]
        public async Task RefreshBranchesFromRepositoriesAsync_AddsNewBranchesAndSkipsDuplicates()
        {
            var settings = new AppSettings();
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" });
            var repositories = new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" }
            };

            var service = new RepositoryConfigurationService((_, _) =>
                Task.FromResult<IEnumerable<string>>(new[] { "main", "develop", "feature/new" }));

            var result = await service.RefreshBranchesFromRepositoriesAsync(settings, repositories);

            Assert.Equal(1, result.RefreshedRepositories);
            Assert.Equal(2, result.AddedBranches);
            Assert.Equal(1, result.DuplicateBranches);
            Assert.Contains(settings.CustomBranches, b => b.BranchName == "develop" && b.Repository == "Repo");
            Assert.Contains(settings.CustomBranches, b => b.BranchName == "feature/new" && b.Repository == "Repo");
        }

        [Fact]
        public async Task RefreshBranchesFromRepositoriesAsync_TracksProviderFailures()
        {
            var settings = new AppSettings();
            var repositories = new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" }
            };

            var service = new RepositoryConfigurationService((_, _) =>
                throw new InvalidOperationException("fetch failed"));

            var result = await service.RefreshBranchesFromRepositoriesAsync(settings, repositories);

            Assert.Equal(1, result.FailedRepositories);
            Assert.Single(result.Errors);
            Assert.Contains("fetch failed", result.Errors[0]);
        }

        [Fact]
        public async Task RefreshBranchesFromRepositoriesAsync_HonorsCancellation()
        {
            var settings = new AppSettings();
            var repositories = new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" }
            };
            var service = new RepositoryConfigurationService((_, token) =>
            {
                token.ThrowIfCancellationRequested();
                return Task.FromResult<IEnumerable<string>>(Array.Empty<string>());
            });

            using var cts = new CancellationTokenSource();
            cts.Cancel();

            await Assert.ThrowsAsync<OperationCanceledException>(
                () => service.RefreshBranchesFromRepositoriesAsync(settings, repositories, cts.Token));
        }

        private string CreateGitRepositoryDirectory(string name = "repo")
        {
            var repoPath = Path.Combine(_settingsDirectory, name);
            Directory.CreateDirectory(repoPath);
            Directory.CreateDirectory(Path.Combine(repoPath, ".git"));
            return repoPath;
        }
    }
}
