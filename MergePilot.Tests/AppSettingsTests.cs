using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace MergePilot.Tests
{
    public class AppSettingsTests : IDisposable
    {
        private string _testSettingsPath;

        public AppSettingsTests()
        {
            // Create a temporary directory for test settings
            _testSettingsPath = Path.Combine(Path.GetTempPath(), "MergePilot.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSettingsPath);
        }

        public void Dispose()
        {
            // Clean up the temporary directory
            if (Directory.Exists(_testSettingsPath))
                Directory.Delete(_testSettingsPath, recursive: true);
        }

        [Fact]
        public void Load_WithNoExistingFile_ReturnsValidSettings()
        {
            // Arrange
            var settings = new AppSettings();

            // Act - Load should return an instance with valid properties
            var loaded = AppSettings.Load();

            // Assert - Just verify it returns a non-null AppSettings object
            Assert.NotNull(loaded);
            Assert.NotNull(loaded.Repositories);
            Assert.NotNull(loaded.CustomBranches);
            Assert.NotNull(loaded.RecentBranches);
        }

        [Fact]
        public void Save_CreatesSettingsDirectory()
        {
            // Arrange
            var settings = new AppSettings
            {
                AutoOpenLogs = false,
                StreamLogs = true,
                LogFilePath = "/path/to/log.txt"
            };

            // Act
            settings.Save();

            // Assert - Directory should be created
            var settingsDir = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MergePilot"
            );
            Assert.True(Directory.Exists(settingsDir));
        }

        [Fact]
        public void Save_And_Load_RoundTrip_PreservesData()
        {
            // Arrange
            var originalSettings = new AppSettings
            {
                AutoOpenLogs = false,
                StreamLogs = true,
                LogFilePath = "/path/to/log.txt",
                FlushIntervalMs = 500,
                LastSourceBranch = "feature/test",
                LastTargetBranch = "develop",
                LogFontSize = 14.0,
                LogMaxChars = 500000
            };

            // Add some repositories
            originalSettings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "TestRepo",
                Path = "/path/to/repo",
                RemoteUrl = "https://github.com/test/repo"
            });

            // Add some branches
            originalSettings.RecentBranches.Add("main");
            originalSettings.RecentBranches.Add("develop");
            originalSettings.RecentBranches.Add("feature/new");

            // Add branch state
            originalSettings.BranchCheckedState["main"] = true;
            originalSettings.BranchCheckedState["develop"] = false;
            originalSettings.BranchExpandedState["feature"] = true;

            // Act
            originalSettings.Save();
            var loadedSettings = AppSettings.Load();

            // Assert
            Assert.Equal(originalSettings.AutoOpenLogs, loadedSettings.AutoOpenLogs);
            Assert.Equal(originalSettings.StreamLogs, loadedSettings.StreamLogs);
            Assert.Equal(originalSettings.LogFilePath, loadedSettings.LogFilePath);
            Assert.Equal(originalSettings.FlushIntervalMs, loadedSettings.FlushIntervalMs);
            Assert.Equal(originalSettings.LastSourceBranch, loadedSettings.LastSourceBranch);
            Assert.Equal(originalSettings.LastTargetBranch, loadedSettings.LastTargetBranch);
            Assert.Equal(originalSettings.LogFontSize, loadedSettings.LogFontSize);
            Assert.Equal(originalSettings.LogMaxChars, loadedSettings.LogMaxChars);

            // Verify repositories
            Assert.Single(loadedSettings.Repositories);
            Assert.Equal("TestRepo", loadedSettings.Repositories[0].Name);
            Assert.Equal("/path/to/repo", loadedSettings.Repositories[0].Path);
            Assert.Equal("https://github.com/test/repo", loadedSettings.Repositories[0].RemoteUrl);

            // Verify branches
            Assert.Equal(3, loadedSettings.RecentBranches.Count);
            Assert.Contains("main", loadedSettings.RecentBranches);
            Assert.Contains("develop", loadedSettings.RecentBranches);
            Assert.Contains("feature/new", loadedSettings.RecentBranches);

            // Verify branch state
            Assert.True(loadedSettings.BranchCheckedState["main"] == true);
            Assert.True(loadedSettings.BranchCheckedState["develop"] == false);
            Assert.True(loadedSettings.BranchExpandedState["feature"]);
        }

        [Fact]
        public void RepositoryEntry_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var entry = new AppSettings.RepositoryEntry
            {
                Name = "MyRepo",
                Path = "/path/to/repo",
                RemoteUrl = "https://github.com/user/repo"
            };

            // Act
            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<AppSettings.RepositoryEntry>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(entry.Name, deserialized.Name);
            Assert.Equal(entry.Path, deserialized.Path);
            Assert.Equal(entry.RemoteUrl, deserialized.RemoteUrl);
        }

        [Fact]
        public void BranchEntry_CanBeSerializedAndDeserialized()
        {
            // Arrange
            var entry = new AppSettings.BranchEntry
            {
                BranchName = "feature/test",
                Repository = "MyRepo"
            };

            // Act
            var json = JsonSerializer.Serialize(entry, new JsonSerializerOptions { WriteIndented = true });
            var deserialized = JsonSerializer.Deserialize<AppSettings.BranchEntry>(json);

            // Assert
            Assert.NotNull(deserialized);
            Assert.Equal(entry.BranchName, deserialized.BranchName);
            Assert.Equal(entry.Repository, deserialized.Repository);
        }
    }
}
