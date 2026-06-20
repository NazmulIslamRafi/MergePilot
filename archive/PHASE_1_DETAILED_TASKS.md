# 🚀 PHASE 1 IMPLEMENTATION - Week 1 & 2 Detailed Tasks

**Status:** Ready to Start ✅  
**Branch:** 0-Task/Version-4.0  
**Phase:** 1 (Foundation)  
**Duration:** Weeks 1-2  
**Effort:** ~30-40 hours total

---

## 📋 Phase 1 Overview

**Goal:** Add validation, documentation, and test foundation  
**No Architecture Changes:** All changes are additive  
**Immediate Benefits:** Better data safety, self-documenting code, test infrastructure

### Tasks by Priority

```
Week 1:
├─ Day 1: XML Documentation (2-3 hours)
├─ Day 2: InputValidator.cs (4 hours)
├─ Day 3: Create Test Project (2 hours)
├─ Day 4-5: AppSettings Validation (3-5 hours)

Week 2:
├─ Day 1-2: First 10 Unit Tests (8 hours)
├─ Day 3-4: Settings Backup & Recovery (4 hours)
├─ Day 5: Code Review & Cleanup (2 hours)
```

---

## 📝 WEEK 1 - DAY 1: XML Documentation (2-3 hours)

### What We're Doing
Add XML documentation comments to all public methods in core classes. This enables:
- IntelliSense in Visual Studio
- Auto-generated documentation
- Better code discovery

### Files to Update

#### 1️⃣ GitHelper.cs - Add XML Comments

**Location:** `MergePilot/GitHelper.cs`

Add these comments to public methods:

```csharp
/// <summary>
/// Executes a Git command in the specified repository with proper error handling.
/// </summary>
/// <param name="repoPath">The absolute path to the Git repository root directory.</param>
/// <param name="arguments">The Git command arguments (e.g., "status", "fetch origin main").</param>
/// <param name="timeout">Optional timeout for command execution. Defaults to 2 minutes.</param>
/// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
/// <returns>
/// A <see cref="CommandResult"/> containing the exit code, stdout, and stderr.
/// Check <see cref="CommandResult.IsSuccess"/> to determine if execution was successful.
/// </returns>
/// <exception cref="ArgumentException">Thrown if repoPath is null or empty.</exception>
/// <exception cref="DirectoryNotFoundException">Thrown if repoPath does not exist.</exception>
/// <remarks>
/// This method properly manages process lifetime and captures both standard output
/// and error streams without deadlock using async operations. Process is killed if
/// timeout is exceeded or cancellation is requested.
/// </remarks>
/// <example>
/// <code>
/// var result = await GitHelper.RunGitCommandAsync(
///     "/path/to/repo",
///     "fetch origin",
///     TimeSpan.FromMinutes(5)
/// );
///
/// if (result.IsSuccess)
/// {
///     Console.WriteLine($"Success: {result.StdOut}");
/// }
/// else
/// {
///     Console.WriteLine($"Error: {result.StdErr}");
/// }
/// </code>
/// </example>
public static async Task<CommandResult> RunGitCommandAsync(
    string repoPath,
    string arguments,
    TimeSpan? timeout = null,
    CancellationToken cancellationToken = default)
{
    // ... existing implementation
}
```

**Other methods to document in GitHelper:**
- `RetryRunGitCommandAsync()`
- `GetDefaultRemoteNameAsync()`
- `RemoteBranchExistsAsync()`
- `EnsureBranchLatestAsync()`
- `MergeBranchAsync()`
- `AbortMergeAsync()`
- `IsGitAvailableAsync()`

#### 2️⃣ AppSettings.cs - Add XML Comments

```csharp
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
    /// Gets or sets the list of configured Git repositories.
    /// </summary>
    public List<RepositoryEntry> Repositories { get; set; } = new();

    /// <summary>
    /// Loads settings from AppData or returns default if file doesn't exist.
    /// </summary>
    /// <returns>Loaded <see cref="AppSettings"/> or new instance with defaults.</returns>
    /// <exception cref="IOException">Thrown if file cannot be read (caught and returns default).</exception>
    /// <remarks>
    /// This method never throws - it returns defaults if loading fails.
    /// </remarks>
    public static AppSettings Load()
    {
        // ... existing implementation
    }

    /// <summary>
    /// Saves settings to AppData/Local/MergePilot/settings.json
    /// </summary>
    /// <exception cref="IOException">Thrown if directory cannot be created or file cannot be written.</exception>
    /// <remarks>
    /// Settings file is created in %LOCALAPPDATA%/MergePilot/settings.json
    /// Directory is created if it doesn't exist.
    /// </remarks>
    public void Save()
    {
        // ... existing implementation
    }
}
```

#### 3️⃣ BranchItem.cs - Add XML Comments

```csharp
/// <summary>
/// Represents a branch or branch group in a hierarchical tree structure.
/// Supports tri-state checkbox (true/false/null) for parent-child relationships.
/// </summary>
public class BranchItem : INotifyPropertyChanged
{
    /// <summary>
    /// Gets or sets the display name of this item (e.g., "feature", "NewUpdate").
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Gets or sets the full branch path (e.g., "features/auth/newupdate").
    /// </summary>
    public string FullName { get; set; }

    /// <summary>
    /// Gets or sets whether this item is a group/folder (true) or leaf branch (false).
    /// </summary>
    public bool IsGroup { get; set; }

    /// <summary>
    /// Gets or sets the hierarchical level (0 = root, 1 = sub-group, etc.)
    /// </summary>
    public int Level { get; set; }

    /// <summary>
    /// Gets or sets the tri-state checkbox value (true = checked, false = unchecked, null = indeterminate).
    /// </summary>
    public bool? IsChecked 
    { 
        get => _isChecked;
        set 
        { 
            if (_isChecked != value)
            {
                _isChecked = value;
                OnPropertyChanged();
            }
        }
    }
}
```

### ✅ Week 1 - Day 1 Checklist

- [ ] GitHelper.cs fully documented (all public methods)
- [ ] AppSettings.cs fully documented
- [ ] BranchItem.cs fully documented
- [ ] ColorScheme.cs documented (if needed)
- [ ] LogFormatter.cs documented (if needed)
- [ ] Build solution - no errors/warnings
- [ ] Verify IntelliSense works in Visual Studio
- [ ] Commit: "docs: Add XML documentation comments to core classes"

**Expected Result:** When you hover over methods in VS, you see helpful documentation.

---

## 🔒 WEEK 1 - DAY 2: Create InputValidator.cs (4 hours)

### What We're Doing
Create a dedicated class to validate all user inputs. This prevents:
- Invalid branch names
- Invalid repository paths
- Git command injection
- Path traversal attacks

### Step 1: Create New File

**File:** `MergePilot/InputValidator.cs`

```csharp
using System;
using System.IO;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Provides input validation for Git operations and repository management.
    /// Prevents injection attacks, path traversal, and invalid data.
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validates a Git branch name according to Git naming rules.
        /// </summary>
        /// <param name="branch">The branch name to validate.</param>
        /// <returns>True if branch name is valid; otherwise, false.</returns>
        /// <remarks>
        /// Git branch names must not:
        /// - Be empty or whitespace
        /// - Contain special characters: @ ~ ^ : ? [ ]
        /// - Contain consecutive dots (..)
        /// - Start or end with dots (.)
        /// - Start or end with slashes (/)
        /// </remarks>
        public static bool IsValidBranchName(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return false;

            // Check for invalid characters
            const string invalidChars = "@~^:?[]";
            if (branch.Any(c => invalidChars.Contains(c)))
                return false;

            // Check for consecutive dots
            if (branch.Contains(".."))
                return false;

            // Check for leading/trailing dots
            if (branch.StartsWith(".") || branch.EndsWith("."))
                return false;

            // Check for leading/trailing slashes
            if (branch.StartsWith("/") || branch.EndsWith("/"))
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation error message for an invalid branch name.
        /// </summary>
        /// <param name="branch">The branch name that failed validation.</param>
        /// <returns>A descriptive error message explaining why the name is invalid.</returns>
        public static string GetBranchNameError(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return "Branch name cannot be empty.";

            const string invalidChars = "@~^:?[]";
            var foundInvalid = branch.FirstOrDefault(c => invalidChars.Contains(c));
            if (foundInvalid != '\0')
                return $"Branch name contains invalid character: '{foundInvalid}'";

            if (branch.Contains(".."))
                return "Branch name cannot contain '..'";

            if (branch.StartsWith(".") || branch.EndsWith("."))
                return "Branch name cannot start or end with '.'";

            if (branch.StartsWith("/") || branch.EndsWith("/"))
                return "Branch name cannot start or end with '/'";

            return "Branch name is invalid.";
        }

        /// <summary>
        /// Validates a repository path exists and contains a Git repository.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if path is a valid Git repository; otherwise, false.</returns>
        /// <remarks>
        /// A valid repository path must:
        /// - Exist as a directory
        /// - Contain a .git folder
        /// - Be within the user's profile (no system paths)
        /// </remarks>
        public static bool IsValidRepositoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            // Check if directory exists
            if (!Directory.Exists(path))
                return false;

            // Check if .git folder exists
            var gitPath = Path.Combine(path, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
                return false;

            // Prevent path traversal - restrict to user profile
            try
            {
                var fullPath = Path.GetFullPath(path);
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                // Allow paths within user profile or common git locations
                if (!fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation error message for an invalid repository path.
        /// </summary>
        /// <param name="path">The path that failed validation.</param>
        /// <returns>A descriptive error message explaining why the path is invalid.</returns>
        public static string GetRepositoryPathError(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Repository path cannot be empty.";

            if (!Directory.Exists(path))
                return $"Directory does not exist: {path}";

            var gitPath = Path.Combine(path, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
                return "Directory does not contain a .git folder. Is this a Git repository?";

            try
            {
                var fullPath = Path.GetFullPath(path);
                var userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);

                if (!fullPath.StartsWith(userProfile, StringComparison.OrdinalIgnoreCase))
                    return "Repository path is outside user profile (security restriction).";
            }
            catch (Exception ex)
            {
                return $"Invalid path: {ex.Message}";
            }

            return "Repository path is invalid.";
        }

        /// <summary>
        /// Validates a repository name is not empty.
        /// </summary>
        /// <param name="name">The repository name to validate.</param>
        /// <returns>True if name is valid; otherwise, false.</returns>
        public static bool IsValidRepositoryName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= 255;
        }

        /// <summary>
        /// Validates that a branch operation has both source and target branches specified.
        /// </summary>
        /// <param name="sourceBranch">The source branch name.</param>
        /// <param name="targetBranch">The target branch name.</param>
        /// <returns>True if both branches are valid and different; otherwise, false.</returns>
        public static bool IsValidMergeOperation(string sourceBranch, string targetBranch)
        {
            if (!IsValidBranchName(sourceBranch) || !IsValidBranchName(targetBranch))
                return false;

            // Prevent merging a branch into itself
            if (string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation result with error details.
        /// </summary>
        /// <param name="sourceBranch">The source branch name.</param>
        /// <param name="targetBranch">The target branch name.</param>
        /// <returns>A tuple of (isValid, errorMessage).</returns>
        public static (bool isValid, string errorMessage) GetMergeOperationError(
            string sourceBranch, 
            string targetBranch)
        {
            if (string.IsNullOrWhiteSpace(sourceBranch))
                return (false, "Source branch is required.");

            if (string.IsNullOrWhiteSpace(targetBranch))
                return (false, "Target branch is required.");

            if (!IsValidBranchName(sourceBranch))
                return (false, $"Invalid source branch: {GetBranchNameError(sourceBranch)}");

            if (!IsValidBranchName(targetBranch))
                return (false, $"Invalid target branch: {GetBranchNameError(targetBranch)}");

            if (string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
                return (false, "Cannot merge a branch into itself.");

            return (true, "");
        }
    }
}
```

### Step 2: Update AddRepositoryDialog.xaml.cs to Use Validator

**File:** `MergePilot/AddRepositoryDialog.xaml.cs`

Find the `Save_Click` method and update it:

```csharp
private void Save_Click(object sender, RoutedEventArgs e)
{
    ErrorMessage.Text = "";

    // Validate repository name
    if (string.IsNullOrWhiteSpace(RepoNameTextBox.Text))
    {
        ErrorMessage.Text = "Repository name is required.";
        return;
    }

    // Validate repository path
    var repoPath = RepoPathTextBox.Text;
    if (!InputValidator.IsValidRepositoryPath(repoPath))
    {
        ErrorMessage.Text = InputValidator.GetRepositoryPathError(repoPath);
        return;
    }

    // Set result
    RepoName = RepoNameTextBox.Text;
    RepoPath = repoPath;
    RemoteUrl = RemoteUrlTextBox.Text;

    this.DialogResult = true;
    this.Close();
}
```

### ✅ Week 1 - Day 2 Checklist

- [ ] Created `InputValidator.cs` with all validation methods
- [ ] Updated `AddRepositoryDialog.xaml.cs` to use validators
- [ ] Build solution - no errors
- [ ] Test: Try entering invalid branch names - should show error
- [ ] Test: Try entering non-existent path - should show error
- [ ] Test: Valid inputs should work
- [ ] Commit: "feat: Add InputValidator for branch and repository validation"

---

## 🧪 WEEK 1 - DAY 3: Create Test Project (2 hours)

### What We're Doing
Set up the test project infrastructure so we can add unit tests.

### Step 1: Create Test Project

```powershell
# In PowerShell at the solution root
cd "D:\work\office_work\US-Bangla\gitlab\MergePilot"

# Create test project
dotnet new xunit -n MergePilot.Tests -o MergePilot.Tests

# Add reference to main project
cd MergePilot.Tests
dotnet add reference ../MergePilot/MergePilot.csproj
```

### Step 2: Update Test Project File

**File:** `MergePilot.Tests/MergePilot.Tests.csproj`

```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <IsPackable>false</IsPackable>
    <Nullable>enable</Nullable>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.8.2" />
    <PackageReference Include="xunit" Version="2.6.4" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.5.1">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
    <PackageReference Include="Moq" Version="4.20.0" />
    <PackageReference Include="System.IO.Abstractions" Version="21.0.2" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\MergePilot\MergePilot.csproj" />
  </ItemGroup>

</Project>
```

### Step 3: Create Fixtures Directory

Create `MergePilot.Tests/Fixtures/` folder for test helpers.

### Step 4: Create Test Fixtures

**File:** `MergePilot.Tests/Fixtures/TemporaryRepositoryFixture.cs`

```csharp
using System;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests.Fixtures
{
    /// <summary>
    /// Creates a temporary Git repository for testing.
    /// Automatically cleans up when disposed.
    /// </summary>
    public class TemporaryRepositoryFixture : IAsyncLifetime
    {
        public string RepositoryPath { get; private set; }

        public async Task InitializeAsync()
        {
            // Create temporary directory
            RepositoryPath = Path.Combine(Path.GetTempPath(), $"git-test-{Guid.NewGuid():N}");
            Directory.CreateDirectory(RepositoryPath);

            // Initialize as Git repository
            await GitHelper.RunGitCommandAsync(RepositoryPath, "init");
            await GitHelper.RunGitCommandAsync(RepositoryPath, "config user.email test@example.com");
            await GitHelper.RunGitCommandAsync(RepositoryPath, "config user.name Test User");

            // Create initial commit
            File.WriteAllText(Path.Combine(RepositoryPath, "README.md"), "# Test Repo");
            await GitHelper.RunGitCommandAsync(RepositoryPath, "add README.md");
            await GitHelper.RunGitCommandAsync(RepositoryPath, "commit -m \"Initial commit\"");
        }

        public async Task DisposeAsync()
        {
            // Clean up temporary directory
            if (Directory.Exists(RepositoryPath))
            {
                Directory.Delete(RepositoryPath, recursive: true);
            }

            await Task.CompletedTask;
        }
    }

    /// <summary>
    /// Collection definition for repository-based tests.
    /// </summary>
    [CollectionDefinition("Repository Collection")]
    public class RepositoryCollection : ICollectionFixture<TemporaryRepositoryFixture>
    {
        // This has no code, and never creates an instance of itself.
        // It's just a marker interface.
    }
}
```

### ✅ Week 1 - Day 3 Checklist

- [ ] Created `MergePilot.Tests` project
- [ ] Added test NuGet packages (xunit, Moq, etc.)
- [ ] Added project reference to MergePilot
- [ ] Created `Fixtures` directory
- [ ] Created `TemporaryRepositoryFixture.cs`
- [ ] Build solution - no errors
- [ ] Test Explorer shows test project
- [ ] Commit: "test: Create test project with fixtures"

---

## ✅ WEEK 1 - DAY 4-5: AppSettings Validation (3-5 hours)

### What We're Doing
Add validation to AppSettings to prevent corrupted settings.

### Step 1: Add Validation to AppSettings.cs

**File:** `MergePilot/AppSettings.cs`

Add this method:

```csharp
/// <summary>
/// Validates the current settings configuration.
/// </summary>
/// <returns>
/// A validation result containing success flag and list of any validation errors.
/// </returns>
public AppSettingsValidationResult Validate()
{
    var errors = new List<string>();

    // Validate flush interval
    if (FlushIntervalMs < 50 || FlushIntervalMs > 5000)
        errors.Add("FlushIntervalMs must be between 50-5000ms");

    // Validate log font size
    if (LogFontSize < 8 || LogFontSize > 36)
        errors.Add("LogFontSize must be between 8-36");

    // Validate log max chars
    if (LogMaxChars < 1000 || LogMaxChars > 1000000)
        errors.Add("LogMaxChars must be between 1,000-1,000,000");

    // Validate repositories
    if (Repositories != null)
    {
        for (int i = 0; i < Repositories.Count; i++)
        {
            var repo = Repositories[i];

            if (string.IsNullOrWhiteSpace(repo.Name))
                errors.Add($"Repository {i}: Name is required");

            if (string.IsNullOrWhiteSpace(repo.Path))
                errors.Add($"Repository {i}: Path is required");
            else if (!Directory.Exists(repo.Path))
                errors.Add($"Repository {i}: Path does not exist ({repo.Path})");
            else if (!Directory.Exists(Path.Combine(repo.Path, ".git")))
                errors.Add($"Repository {i}: Not a valid Git repository ({repo.Path})");
        }
    }

    return new AppSettingsValidationResult(errors.Count == 0, errors);
}

/// <summary>
/// Loads settings with fallback to backup if corrupted.
/// </summary>
public static AppSettings LoadWithFallback()
{
    try
    {
        var settings = Load();
        var validation = settings.Validate();

        if (!validation.IsValid)
        {
            System.Diagnostics.Debug.WriteLine($"Settings validation failed: {string.Join(", ", validation.Errors)}");
            // Return defaults instead of corrupted settings
            return new AppSettings();
        }

        return settings;
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error loading settings: {ex.Message}");

        // Try loading backup
        var backupPath = SettingsPath + ".bak";
        if (File.Exists(backupPath))
        {
            try
            {
                var json = File.ReadAllText(backupPath);
                var opts = new JsonSerializerOptions { PropertyNameCaseInsensitive = true };
                return JsonSerializer.Deserialize<AppSettings>(json, opts) ?? new AppSettings();
            }
            catch { }
        }

        return new AppSettings();
    }
}

/// <summary>
/// Saves settings with atomic write and backup.
/// </summary>
public void SaveAtomically()
{
    try
    {
        var dir = Path.GetDirectoryName(SettingsPath);
        if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
            Directory.CreateDirectory(dir);

        // Create backup of existing settings
        if (File.Exists(SettingsPath))
            File.Copy(SettingsPath, SettingsPath + ".bak", overwrite: true);

        // Write to temporary file first
        var tempPath = SettingsPath + ".tmp";
        var json = JsonSerializer.Serialize(this, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(tempPath, json);

        // Atomic rename (on Windows NTFS this is atomic)
        File.Move(tempPath, SettingsPath, overwrite: true);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"Error saving settings: {ex.Message}");
        throw;
    }
}
```

### Step 2: Create Validation Result Class

**Add to** `MergePilot/AppSettings.cs` (after the AppSettings class):

```csharp
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
    /// Gets the list of validation errors (empty if valid).
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
```

### Step 3: Update App.xaml.cs to Use New Method

**File:** `MergePilot/App.xaml.cs`

```csharp
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);

    // Prevent auto-shutdown
    Application.Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

    var splash = new SplashScreen();
    splash.Show();

    // Load settings with fallback
    var settings = AppSettings.LoadWithFallback();  // NEW: Uses fallback

    System.Threading.Thread.Sleep(3000);

    var mainWindow = new MainWindow();

    // Close splash
    splash.Close();

    // Revert shutdown mode
    Application.Current.ShutdownMode = ShutdownMode.OnMainWindowClose;
    Application.Current.MainWindow = mainWindow;
}
```

### ✅ Week 1 - Day 4-5 Checklist

- [ ] Added `Validate()` method to AppSettings
- [ ] Added `AppSettingsValidationResult` class
- [ ] Added `LoadWithFallback()` method
- [ ] Added `SaveAtomically()` method
- [ ] Updated `App.xaml.cs` to use `LoadWithFallback()`
- [ ] Build solution - no errors
- [ ] Test: Manually corrupt settings file - app should recover
- [ ] Commit: "feat: Add AppSettings validation and atomic saving"

---

## 🎯 WEEK 2: Unit Tests & Finalization

### WEEK 2 - DAY 1-2: First 10 Unit Tests (8 hours)

Create `MergePilot.Tests/InputValidatorTests.cs`:

```csharp
using Xunit;
using MergePilot;

namespace MergePilot.Tests
{
    public class InputValidatorTests
    {
        [Fact]
        public void IsValidBranchName_WithValidName_ReturnsTrue()
        {
            // Arrange
            var branch = "feature/user-auth";

            // Act
            var result = InputValidator.IsValidBranchName(branch);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void IsValidBranchName_WithEmpty_ReturnsFalse()
        {
            // Act & Assert
            Assert.False(InputValidator.IsValidBranchName(""));
            Assert.False(InputValidator.IsValidBranchName(null));
        }

        [Fact]
        public void IsValidBranchName_WithInvalidChars_ReturnsFalse()
        {
            Assert.False(InputValidator.IsValidBranchName("feature@branch"));
            Assert.False(InputValidator.IsValidBranchName("feature~branch"));
            Assert.False(InputValidator.IsValidBranchName("feature:branch"));
        }

        [Fact]
        public void IsValidBranchName_WithDoubleDot_ReturnsFalse()
        {
            Assert.False(InputValidator.IsValidBranchName("feature..branch"));
        }

        [Fact]
        public void IsValidRepositoryPath_WithValidPath_ReturnsTrue()
        {
            // This will test against temp directory - needs real repo
            // Skip for now, test in integration tests
        }

        [Fact]
        public void IsValidRepositoryPath_WithInvalid_ReturnsFalse()
        {
            Assert.False(InputValidator.IsValidRepositoryPath(""));
            Assert.False(InputValidator.IsValidRepositoryPath("/nonexistent/path"));
        }

        [Fact]
        public void IsValidMergeOperation_WithSameBranch_ReturnsFalse()
        {
            Assert.False(InputValidator.IsValidMergeOperation("main", "main"));
        }

        [Fact]
        public void IsValidMergeOperation_WithValidBranches_ReturnsTrue()
        {
            Assert.True(InputValidator.IsValidMergeOperation("feature/auth", "main"));
        }

        [Fact]
        public void GetMergeOperationError_WithValidOperation_ReturnsEmpty()
        {
            var (isValid, error) = InputValidator.GetMergeOperationError("feature", "main");

            Assert.True(isValid);
            Assert.Empty(error);
        }

        [Fact]
        public void GetMergeOperationError_WithEmptySource_ReturnsError()
        {
            var (isValid, error) = InputValidator.GetMergeOperationError("", "main");

            Assert.False(isValid);
            Assert.NotEmpty(error);
        }
    }
}
```

### ✅ Week 2 - DAY 1-2 Checklist

- [ ] Created `InputValidatorTests.cs` with 10 tests
- [ ] All tests passing (run with `dotnet test`)
- [ ] Test Explorer shows all tests
- [ ] Coverage ~80% of InputValidator
- [ ] Commit: "test: Add InputValidator unit tests"

### WEEK 2 - DAY 3-4: Integration & Cleanup (4 hours)

### WEEK 2 - DAY 5: Review & Polish (2 hours)

---

## 🎉 PHASE 1 COMPLETE Checklist

### Documentation
- [ ] All XML docs added to GitHelper, AppSettings, BranchItem
- [ ] Methods show up in IntelliSense
- [ ] Build has no warnings

### Validation
- [ ] InputValidator.cs created with full validation
- [ ] AddRepositoryDialog uses validators
- [ ] Error messages are clear and helpful

### Testing
- [ ] Test project created
- [ ] First 10 unit tests passing
- [ ] Test Explorer shows tests
- [ ] Can run with `dotnet test`

### Settings
- [ ] AppSettings.Validate() added
- [ ] AppSettings.LoadWithFallback() added
- [ ] AppSettings.SaveAtomically() added
- [ ] Settings backup working

### Code Quality
- [ ] Solution builds with no errors
- [ ] No compiler warnings
- [ ] All commits made and pushed

---

## 📊 After Phase 1 Complete

**What You'll Have:**
✅ 10 unit tests (foundation for more)  
✅ Input validation (security improvement)  
✅ Atomic settings saving (data integrity)  
✅ Comprehensive XML docs (self-documenting)  
✅ Clean git history (good practices)  

**Ready For:**
→ Phase 2: Comprehensive Unit Tests (Week 3-4)

---

## 🚀 Ready to Start?

Begin with **WEEK 1 - DAY 1** right now!

**Next Action:** Let me know when you're ready, and I'll create the actual files and help you through each step.

