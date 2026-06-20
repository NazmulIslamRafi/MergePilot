using System;
using System.IO;
using Xunit;

namespace MergePilot.Tests
{
    public class BatchOperationRequestFactoryTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public BatchOperationRequestFactoryTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.BatchOperationRequestFactory", Guid.NewGuid().ToString());
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
        public void CreateMergeRequest_WithSourcesAndTargets_PersistsLastBranches()
        {
            var settings = new AppSettings();
            var factory = new BatchOperationRequestFactory();

            var request = factory.CreateMergeRequest(
                settings,
                new[] { " C:\\repo " },
                new[] { " feature/a ", "feature/b" },
                new[] { " develop " });

            Assert.Equal(new[] { "C:\\repo" }, request.RepositoryPaths);
            Assert.Equal(new[] { "feature/a", "feature/b" }, request.SourceBranches);
            Assert.Equal(new[] { "develop" }, request.TargetBranches);
            Assert.Equal("feature/a|feature/b", settings.LastSourceBranch);
            Assert.Equal("develop", settings.LastTargetBranch);
            Assert.True(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void CreateMergeRequest_WithMissingTargets_DoesNotPersistLastBranches()
        {
            var settings = new AppSettings
            {
                LastSourceBranch = "old-source",
                LastTargetBranch = "old-target"
            };
            var factory = new BatchOperationRequestFactory();

            var request = factory.CreateMergeRequest(
                settings,
                new[] { "C:\\repo" },
                new[] { "feature/a" },
                Array.Empty<string>());

            Assert.Empty(request.TargetBranches);
            Assert.Equal("old-source", settings.LastSourceBranch);
            Assert.Equal("old-target", settings.LastTargetBranch);
            Assert.False(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public void CreatePullRequest_TrimsAndFiltersValues()
        {
            var factory = new BatchOperationRequestFactory();

            var request = factory.CreatePullRequest(
                new[] { " C:\\repo ", "", " " },
                new[] { " main ", "" });

            Assert.Equal(new[] { "C:\\repo" }, request.RepositoryPaths);
            Assert.Equal(new[] { "main" }, request.SourceBranches);
        }
    }
}
