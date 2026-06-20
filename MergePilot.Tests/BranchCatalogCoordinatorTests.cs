using System;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchCatalogCoordinatorTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public BranchCatalogCoordinatorTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.BranchCatalogCoordinator", Guid.NewGuid().ToString());
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
        public void LoadInitialCatalog_ReplacesExistingCatalogWithInitialBranches()
        {
            var settings = new AppSettings();
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" });
            settings.RecentBranches.Add("develop");
            var state = new BranchSelectionState();
            state.AddBranchToCatalogs("old");
            var coordinator = CreateCoordinator();

            coordinator.LoadInitialCatalog(settings, state);

            Assert.Equal(new[] { "main", "develop" }, state.SourceBranchCatalog);
            Assert.Equal(new[] { "main", "develop" }, state.TargetBranchCatalog);
        }

        [Fact]
        public void LoadRepositoryCatalog_ReplacesExistingCatalogWithRepositoryBranches()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "RepoA", Path = "C:\\repo-a" });
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "RepoB", Path = "C:\\repo-b" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "RepoA" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "develop", Repository = "RepoB" });
            var state = new BranchSelectionState();
            state.AddBranchToCatalogs("old");
            var coordinator = CreateCoordinator();

            coordinator.LoadRepositoryCatalog(settings, "c:\\REPO-a", state);

            Assert.Single(state.SourceBranchCatalog);
            Assert.Contains("main", state.SourceBranchCatalog);
            Assert.DoesNotContain("old", state.SourceBranchCatalog);
        }

        [Fact]
        public void AddRecentBranch_AddsToRecentAndInMemoryCatalog()
        {
            var settings = new AppSettings();
            var state = new BranchSelectionState();
            var coordinator = CreateCoordinator();

            var added = coordinator.AddRecentBranch(settings, " feature/new ", state);

            Assert.True(added);
            Assert.Contains("feature/new", settings.RecentBranches);
            Assert.Contains("feature/new", state.SourceBranchCatalog);
            Assert.Contains("feature/new", state.TargetBranchCatalog);
        }

        [Fact]
        public void AddRecentBranch_WhenDuplicateStillEnsuresCatalogContainsBranch()
        {
            var settings = new AppSettings();
            settings.RecentBranches.Add("main");
            var state = new BranchSelectionState();
            var coordinator = CreateCoordinator();

            var added = coordinator.AddRecentBranch(settings, "MAIN", state);

            Assert.False(added);
            Assert.Single(settings.RecentBranches);
            Assert.Contains("MAIN", state.SourceBranchCatalog);
        }

        [Fact]
        public void ClearCatalogs_RemovesSourceAndTargetCatalogs()
        {
            var state = new BranchSelectionState();
            state.AddBranchToCatalogs("main");
            var coordinator = CreateCoordinator();

            coordinator.ClearCatalogs(state);

            Assert.Empty(state.SourceBranchCatalog);
            Assert.Empty(state.TargetBranchCatalog);
        }

        [Fact]
        public void ClearCatalogsAndSelections_RemovesCatalogsAndSelectedBranches()
        {
            var state = new BranchSelectionState();
            state.AddBranchToCatalogs("main");
            state.SetBranchSelected(BranchSelectionRole.Source, "main", true);
            state.SetBranchSelected(BranchSelectionRole.Target, "develop", true);
            var coordinator = CreateCoordinator();

            coordinator.ClearCatalogsAndSelections(state);

            Assert.Empty(state.SourceBranchCatalog);
            Assert.Empty(state.TargetBranchCatalog);
            Assert.Empty(state.SelectedSourceBranches);
            Assert.Empty(state.SelectedTargetBranches);
        }

        private static BranchCatalogCoordinator CreateCoordinator()
        {
            return new BranchCatalogCoordinator(new BranchCatalogService());
        }
    }
}
