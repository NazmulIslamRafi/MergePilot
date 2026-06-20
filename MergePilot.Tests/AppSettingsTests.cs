using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Xunit;

namespace MergePilot.Tests
{
    public class AppSettingsTests : IDisposable
    {
        private readonly string _testSettingsPath;

        public AppSettingsTests()
        {
            // Create a temporary directory for test settings
            _testSettingsPath = Path.Combine(Path.GetTempPath(), "MergePilot.Tests", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_testSettingsPath);
            AppSettings.SettingsPathOverride = Path.Combine(_testSettingsPath, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

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
        public void ResolveSettingsPath_WithEnvironmentOverride_UsesEnvironmentPath()
        {
            var environmentPath = Path.Combine(_testSettingsPath, "profile-settings.json");

            var resolved = AppSettings.ResolveSettingsPath(
                explicitOverride: null,
                environmentOverride: environmentPath);

            Assert.Equal(environmentPath, resolved);
        }

        [Fact]
        public void ResolveSettingsPath_WithExplicitOverride_PrefersExplicitOverride()
        {
            var explicitPath = Path.Combine(_testSettingsPath, "explicit-settings.json");
            var environmentPath = Path.Combine(_testSettingsPath, "profile-settings.json");

            var resolved = AppSettings.ResolveSettingsPath(
                explicitOverride: explicitPath,
                environmentOverride: environmentPath);

            Assert.Equal(explicitPath, resolved);
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
                _testSettingsPath
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

        [Fact]
        public void Validate_WithDefaultSettings_ReturnsValid()
        {
            // Arrange
            var settings = new AppSettings();

            // Act
            var result = settings.Validate();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void Validate_WithInvalidLogSettings_ReturnsErrors()
        {
            // Arrange
            var settings = new AppSettings
            {
                FlushIntervalMs = 10,
                LogFontSize = 4,
                LogMaxChars = 10
            };

            // Act
            var result = settings.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("FlushIntervalMs"));
            Assert.Contains(result.Errors, e => e.Contains("LogFontSize"));
            Assert.Contains(result.Errors, e => e.Contains("LogMaxChars"));
        }

        [Fact]
        public void Validate_WithMissingRepositoryFields_ReturnsErrors()
        {
            // Arrange
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry());

            // Act
            var result = settings.Validate();

            // Assert
            Assert.False(result.IsValid);
            Assert.Contains(result.Errors, e => e.Contains("Name is required"));
            Assert.Contains(result.Errors, e => e.Contains("Path is required"));
        }

        [Fact]
        public void Validate_WithUnavailableRepositoryPath_KeepsSettingsValid()
        {
            // Arrange
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "OfflineRepo",
                Path = Path.Combine(_testSettingsPath, "repo-that-is-not-available")
            });

            // Act
            var result = settings.Validate();

            // Assert
            Assert.True(result.IsValid);
            Assert.Empty(result.Errors);
        }

        [Fact]
        public void SaveAtomically_WhenExistingSettingsExist_CreatesBackup()
        {
            // Arrange
            var original = new AppSettings { AutoOpenLogs = false };
            original.SaveAtomically();

            var updated = new AppSettings { AutoOpenLogs = true };

            // Act
            updated.SaveAtomically();

            // Assert
            var backupPath = AppSettings.SettingsPathOverride + ".bak";
            Assert.True(File.Exists(backupPath));

            var backupJson = File.ReadAllText(backupPath);
            var backup = JsonSerializer.Deserialize<AppSettings>(backupJson);
            Assert.NotNull(backup);
            Assert.False(backup.AutoOpenLogs);
        }

        [Fact]
        public void LoadWithFallback_WithCorruptedPrimary_LoadsValidBackup()
        {
            // Arrange
            var backup = new AppSettings
            {
                AutoOpenLogs = false,
                LogFontSize = 16,
                LogMaxChars = 300000
            };

            var backupJson = JsonSerializer.Serialize(backup, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(AppSettings.SettingsPathOverride!, "{ invalid json");
            File.WriteAllText(AppSettings.SettingsPathOverride + ".bak", backupJson);

            // Act
            var loaded = AppSettings.LoadWithFallback();

            // Assert
            Assert.False(loaded.AutoOpenLogs);
            Assert.Equal(16, loaded.LogFontSize);
            Assert.Equal(300000, loaded.LogMaxChars);
        }

        [Fact]
        public void LoadWithFallback_WithInvalidPrimary_ReturnsDefaultsWhenNoBackupExists()
        {
            // Arrange
            var invalid = new AppSettings
            {
                FlushIntervalMs = 1,
                LogFontSize = 1,
                LogMaxChars = 1
            };
            invalid.SaveAtomically();

            // Act
            var loaded = AppSettings.LoadWithFallback();

            // Assert
            Assert.Equal(200, loaded.FlushIntervalMs);
            Assert.Equal(13.0, loaded.LogFontSize);
            Assert.Equal(200000, loaded.LogMaxChars);
        }
    }
}
