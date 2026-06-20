using System;
using System.IO;
using System.Linq;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchCatalogServiceTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public BranchCatalogServiceTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.BranchCatalog", Guid.NewGuid().ToString());
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
        public void GetInitialBranchCatalog_CombinesCustomThenRecentWithCaseInsensitiveDedupe()
        {
            var settings = new AppSettings();
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "feature/a", Repository = "Repo" });
            settings.RecentBranches.Add("MAIN");
            settings.RecentBranches.Add("develop");
            var service = new BranchCatalogService();

            var catalog = service.GetInitialBranchCatalog(settings);

            Assert.Equal(new[] { "main", "feature/a", "develop" }, catalog);
        }

        [Fact]
        public void GetCustomBranchesForRepository_ReturnsOnlyMatchingRepositoryBranches()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "RepoA", Path = "C:\\repo-a" });
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "RepoB", Path = "C:\\repo-b" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "RepoA" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "develop", Repository = "RepoB" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "MAIN", Repository = "RepoA" });
            var service = new BranchCatalogService();

            var branches = service.GetCustomBranchesForRepository(settings, "c:\\REPO-a");

            Assert.Single(branches);
            Assert.Equal("main", branches[0]);
        }

        [Fact]
        public void AddRecentBranch_WithNewBranch_AddsSavesAndReturnsTrue()
        {
            var settings = new AppSettings();
            var service = new BranchCatalogService();

            var added = service.AddRecentBranch(settings, " feature/new ");

            Assert.True(added);
            Assert.Single(settings.RecentBranches);
            Assert.Equal("feature/new", settings.RecentBranches[0]);
            Assert.True(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void AddRecentBranch_WithDuplicateBranch_ReturnsFalseWithoutSaving()
        {
            var settings = new AppSettings();
            settings.RecentBranches.Add("main");
            var service = new BranchCatalogService();

            var added = service.AddRecentBranch(settings, "MAIN");

            Assert.False(added);
            Assert.Single(settings.RecentBranches);
            Assert.False(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void AddRecentBranch_TrimsOldestBranchesToLimit()
        {
            var settings = new AppSettings();
            for (var i = 0; i < 100; i++)
                settings.RecentBranches.Add($"branch-{i}");

            var service = new BranchCatalogService();

            var added = service.AddRecentBranch(settings, "branch-100", maxRecentBranches: 100);

            Assert.True(added);
            Assert.Equal(100, settings.RecentBranches.Count);
            Assert.DoesNotContain("branch-0", settings.RecentBranches);
            Assert.Equal("branch-100", settings.RecentBranches.Last());
        }
    }
}
