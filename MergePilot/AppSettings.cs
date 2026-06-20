using System;
using System.IO;
using System.Text.Json;
using System.Collections.Generic;

namespace MergePilot
{
    /// <summary>
    /// Application settings including repositories, branches, and UI preferences.
    /// Settings are persisted to JSON in AppData/Local/MergePilot/settings.json
    /// </summary>
    public class AppSettings
    {
        /// <summary>
        /// Gets or sets whether logs should open automatically.
        /// </summary>
        public bool AutoOpenLogs { get; set; } = true;

        /// <summary>
        /// Gets or sets whether logs should be streamed to file.
        /// </summary>
        public bool StreamLogs { get; set; } = false;

        /// <summary>
        /// Gets or sets the optional file path for log streaming.
        /// </summary>
        public string? LogFilePath { get; set; }

        /// <summary>
        /// Gets or sets the interval in milliseconds for flushing log output (default 200ms).
        /// </summary>
        public int FlushIntervalMs { get; set; } = 200;

        /// <summary>
        /// Gets or sets the list of configured Git repositories.
        /// </summary>
        public List<RepositoryEntry> Repositories { get; set; } = new();

        /// <summary>
        /// Gets or sets the list of custom branches (manually added or discovered).
        /// </summary>
        public List<BranchEntry> CustomBranches { get; set; } = new();

        /// <summary>
        /// Gets or sets recently discovered or used branches (persisted across sessions).
        /// </summary>
        public List<string> RecentBranches { get; set; } = new();

        /// <summary>
        /// Gets or sets the last used source branch name.
        /// </summary>
        public string? LastSourceBranch { get; set; }

        /// <summary>
        /// Gets or sets the last used target branch name.
        /// </summary>
        public string? LastTargetBranch { get; set; }

        /// <summary>
        /// Gets or sets per-branch checked state (nullable for tri-state checkbox support).
        /// </summary>
        public Dictionary<string, bool?> BranchCheckedState { get; set; } = new();

        /// <summary>
        /// Gets or sets per-branch expansion state in the hierarchical tree.
        /// </summary>
        public Dictionary<string, bool> BranchExpandedState { get; set; } = new();

        /// <summary>
        /// Gets or sets whether inline logs are visible.
        /// </summary>
        public bool InlineLogsVisible { get; set; } = false;

        /// <summary>
        /// Gets or sets whether output pane shows errors-only filter.
        /// </summary>
        public bool OutputErrorsOnly { get; set; } = false;

        /// <summary>
        /// Gets or sets the last search term for output logs.
        /// </summary>
        public string? LastSearchOutput { get; set; }

        /// <summary>
        /// Gets or sets the last search term for error logs.
        /// </summary>
        public string? LastSearchError { get; set; }

        /// <summary>
        /// Gets or sets the log display font size (default 13.0, range 8-36).
        /// </summary>
        public double LogFontSize { get; set; } = 13.0;

        /// <summary>
        /// Gets or sets the maximum number of characters to keep in memory for logs (default 200,000).
        /// </summary>
        public int LogMaxChars { get; set; } = 200000;

        /// <summary>
        /// Represents a configured Git repository.
        /// </summary>
        public class RepositoryEntry
        {
            /// <summary>
            /// Gets or sets the display name of the repository.
            /// </summary>
            public string? Name { get; set; }

            /// <summary>
            /// Gets or sets the absolute file path to the repository root directory.
            /// </summary>
            public string? Path { get; set; }

            /// <summary>
            /// Gets or sets the optional remote URL or other repository metadata.
            /// </summary>
            public string? RemoteUrl { get; set; }
        }

        /// <summary>
        /// Represents a custom or discovered branch.
        /// </summary>
        public class BranchEntry
        {
            /// <summary>
            /// Gets or sets the branch name (e.g., "main", "develop").
            /// </summary>
            public string? BranchName { get; set; }

            /// <summary>
            /// Gets or sets the repository name this branch belongs to.
            /// </summary>
            public string? Repository { get; set; }

            /// <summary>
            /// Gets or sets whether this branch was added manually (typed) rather than selected from remote.
            /// </summary>
            public bool IsManual { get; set; } = false;
        }

        internal const string SettingsPathEnvironmentVariable = "MERGEPILOT_SETTINGS_PATH";

        /// <summary>
        /// Gets the path to the settings file (AppData/Local/MergePilot/settings.json by default).
        /// </summary>
        private static string SettingsPath => ResolveSettingsPath(
            SettingsPathOverride,
            Environment.GetEnvironmentVariable(SettingsPathEnvironmentVariable));

        /// <summary>
        /// Overrides the settings file path for tests.
        /// </summary>
        internal static string? SettingsPathOverride { get; set; }

        internal static string ResolveSettingsPath(string? explicitOverride, string? environmentOverride)
        {
            if (!string.IsNullOrWhiteSpace(explicitOverride))
                return explicitOverride;

            if (!string.IsNullOrWhiteSpace(environmentOverride))
                return environmentOverride;

            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MergePilot",
                "settings.json");
        }

        /// <summary>
        /// Loads application settings from the settings file. Returns a new instance if the file does not exist or deserialization fails.
        /// </summary>
        /// <returns>The loaded AppSettings or a new instance with default values.</returns>
        public static AppSettings Load()
        {
            try
            {
                var path = SettingsPath;
                if (File.Exists(path))
                {
                    return LoadFromFile(path);
                }
            }
            catch { }
            return new AppSettings();
        }

        /// <summary>
        /// Loads application settings and returns defaults if the settings file is corrupted or invalid.
        /// Falls back to a backup file when the primary settings file cannot be read.
        /// </summary>
        /// <returns>The loaded and validated settings, backup settings, or default settings.</returns>
        public static AppSettings LoadWithFallback()
        {
            try
            {
                if (File.Exists(SettingsPath))
                {
                    var settings = LoadFromFile(SettingsPath);
                    if (settings.Validate().IsValid)
                        return settings;
                }
                else
                {
                    return new AppSettings();
                }
            }
            catch { }

            var backupPath = SettingsPath + ".bak";
            if (File.Exists(backupPath))
            {
                try
                {
                    var json = File.ReadAllText(backupPath);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    var backupSettings = JsonSerializer.Deserialize<AppSettings>(json, opts);

                    if (backupSettings?.Validate().IsValid == true)
                        return backupSettings;
                }
                catch { }
            }

            return new AppSettings();
        }

        private static AppSettings LoadFromFile(string path)
        {
            var json = File.ReadAllText(path);
            var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
            return JsonSerializer.Deserialize<AppSettings>(json, opts) ?? new AppSettings();
        }

        /// <summary>
        /// Validates the current settings values and configured repositories.
        /// </summary>
        /// <returns>A validation result with any discovered errors.</returns>
        public AppSettingsValidationResult Validate()
        {
            var errors = new List<string>();

            if (FlushIntervalMs < 50 || FlushIntervalMs > 5000)
                errors.Add("FlushIntervalMs must be between 50 and 5000ms.");

            if (LogFontSize < 8 || LogFontSize > 36)
                errors.Add("LogFontSize must be between 8 and 36.");

            if (LogMaxChars < 1000 || LogMaxChars > 1000000)
                errors.Add("LogMaxChars must be between 1,000 and 1,000,000.");

            if (Repositories == null)
            {
                errors.Add("Repositories cannot be null.");
            }
            else
            {
                for (var i = 0; i < Repositories.Count; i++)
                {
                    var repo = Repositories[i];
                    if (repo == null)
                    {
                        errors.Add($"Repository {i}: Entry cannot be null.");
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(repo.Name))
                        errors.Add($"Repository {i}: Name is required.");

                    if (string.IsNullOrWhiteSpace(repo.Path))
                        errors.Add($"Repository {i}: Path is required.");
                }
            }

            CustomBranches ??= new List<BranchEntry>();
            RecentBranches ??= new List<string>();
            BranchCheckedState ??= new Dictionary<string, bool?>();
            BranchExpandedState ??= new Dictionary<string, bool>();

            return new AppSettingsValidationResult(errors.Count == 0, errors);
        }

        /// <summary>
        /// Saves the current application settings to the settings file. Creates the directory if it does not exist.
        /// If the save fails, the exception is silently caught.
        /// </summary>
        public void Save()
        {
            try
            {
                SaveAtomically();
            }
            catch { }
        }

        /// <summary>
        /// Saves settings with a backup and temporary file so interrupted writes do not corrupt settings.
        /// </summary>
        public void SaveAtomically()
        {
            var dir = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);

            if (File.Exists(SettingsPath))
                File.Copy(SettingsPath, SettingsPath + ".bak", overwrite: true);

            var tempPath = SettingsPath + ".tmp";
            var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
            File.WriteAllText(tempPath, json);
            File.Move(tempPath, SettingsPath, overwrite: true);
        }
    }

    /// <summary>
    /// Result of AppSettings validation.
    /// </summary>
    public class AppSettingsValidationResult
    {
        /// <summary>
        /// Gets whether the settings are valid.
        /// </summary>
        public bool IsValid { get; }

        /// <summary>
        /// Gets validation errors. Empty when settings are valid.
        /// </summary>
        public List<string> Errors { get; }

        /// <summary>
        /// Initializes a new instance of the AppSettingsValidationResult class.
        /// </summary>
        public AppSettingsValidationResult(bool isValid, List<string> errors)
        {
            IsValid = isValid;
            Errors = errors ?? new List<string>();
        }
    }
}
