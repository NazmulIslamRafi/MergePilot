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
        }

        /// <summary>
        /// Gets the path to the settings file (AppData/Local/MergePilot/settings.json).
        /// </summary>
        private static string SettingsPath => Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "MergePilot", "settings.json");

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
                    var json = File.ReadAllText(path);
                    var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                    return JsonSerializer.Deserialize<AppSettings>(json, opts) ?? new AppSettings();
                }
            }
            catch { }
            return new AppSettings();
        }

        /// <summary>
        /// Saves the current application settings to the settings file. Creates the directory if it does not exist.
        /// If the save fails, the exception is silently caught.
        /// </summary>
        public void Save()
        {
            try
            {
                var dir = Path.GetDirectoryName(SettingsPath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                    Directory.CreateDirectory(dir);

                var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(SettingsPath, json);
            }
            catch { }
        }
    }
}
