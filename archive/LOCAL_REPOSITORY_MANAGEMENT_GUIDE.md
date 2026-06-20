# Local Repository & Branch Management Guide

## 📋 Overview

I've implemented a complete **Local Repository and Branch Management System** for MergePilot. This allows you to:

1. ✅ **Add/Edit/Remove repositories** from your local machine
2. ✅ **Store repositories and branches** locally in settings file
3. ✅ **Load branches** from local settings into dropdown lists
4. ✅ **Discover branches** automatically from repositories
5. ✅ **Manage custom branches** manually

---

## 🎯 How It Works

### Flow Diagram
```
User Opens MergePilot
    ↓
Main Window loads AppSettings from LocalAppData
    ↓
PopulateRepositoriesFromSettings() called
    ├─ Creates checkboxes for each repository
    └─ Loads all custom branches into Source/Target dropdowns
    ↓
User clicks "Ctrl+B" (Manage Repos)
    ↓
RepositoryManager Window opens
    ├─ Tab 1: Repository Management
    │   ├─ ➕ Add Repository (via dialog)
    │   ├─ ✏️ Edit Repository
    │   ├─ 🗑️ Delete Repository
    │   └─ 🔄 Refresh Branches (auto-discover from repos)
    │
    └─ Tab 2: Custom Branches
        ├─ ➕ Add Branch (manually)
        ├─ ✏️ Edit Branch
        └─ 🗑️ Delete Branch
    ↓
Changes saved to LocalAppData/MergePilot/settings.json
    ↓
Main window reloads and shows updated branches
```

---

## 📁 New Files Created

### 1. **RepositoryManager.xaml / RepositoryManager.xaml.cs**
   - Main window for managing repositories and branches
   - Two tabs: Repositories & Custom Branches
   - Features:
     - List all repositories with edit/delete buttons
     - List all custom branches with edit/delete buttons
     - Auto-refresh branches from repositories
     - Add new repositories and branches

### 2. **AddRepositoryDialog.xaml / AddRepositoryDialog.xaml.cs**
   - Dialog for adding/editing repositories
   - Fields:
     - Repository Name (e.g., "Backend API")
     - Repository Path (e.g., "D:\projects\backend")
     - Remote URL (optional, e.g., "https://github.com/user/repo")
   - Validates:
     - Path exists
     - Contains .git folder

### 3. **AddBranchDialog.xaml / AddBranchDialog.xaml.cs**
   - Dialog for adding/editing branches
   - Fields:
     - Repository (dropdown selection)
     - Branch Name (e.g., "feature/new-feature")
   - Can be used for manual branch entry or auto-discovered branches

---

## 🗄️ Data Structure (AppSettings)

### New Properties Added:
```csharp
// AppSettings.cs
public class RepositoryEntry
{
    public string? Name { get; set; }        // "Backend API"
    public string? Path { get; set; }        // "D:\projects\backend"
    public string? RemoteUrl { get; set; }   // "https://github.com/user/repo"
}

public class BranchEntry
{
    public string? BranchName { get; set; }  // "feature/new-feature"
    public string? Repository { get; set; }  // "Backend API"
}

// In AppSettings class:
public List<RepositoryEntry> Repositories { get; set; } = new();
public List<BranchEntry> CustomBranches { get; set; } = new();
```

### Storage Location:
```
%LocalAppData%\MergePilot\settings.json
```

### Example settings.json:
```json
{
  "Repositories": [
    {
      "Name": "Backend API",
      "Path": "D:\\projects\\backend",
      "RemoteUrl": "https://github.com/user/backend"
    },
    {
      "Name": "Frontend",
      "Path": "D:\\projects\\frontend",
      "RemoteUrl": "https://github.com/user/frontend"
    }
  ],
  "CustomBranches": [
    {
      "BranchName": "main",
      "Repository": "Backend API"
    },
    {
      "BranchName": "develop",
      "Repository": "Backend API"
    },
    {
      "BranchName": "feature/new-feature",
      "Repository": "Backend API"
    },
    {
      "BranchName": "main",
      "Repository": "Frontend"
    },
    {
      "BranchName": "release/v1.0",
      "Repository": "Frontend"
    }
  ],
  "RecentBranches": [],
  "LastSourceBranch": null,
  "LastTargetBranch": null,
  "BranchCheckedState": {},
  "BranchExpandedState": {},
  "AutoOpenLogs": true,
  "StreamLogs": false,
  "LogFontSize": 13.0,
  "LogMaxChars": 200000
}
```

---

## 🚀 Using the Repository Manager

### Opening Repository Manager
```
Press: Ctrl + B
Or: Click: "Branch/Repository Manager" Button
```

### Adding a Repository
1. Click **➕ Add Repository** button
2. Enter repository name (e.g., "Backend API")
3. Click **🗂️ Browse** to select folder (must contain .git)
4. Optionally enter remote URL
5. Click **Save**

✅ Repository is saved to settings.json

### Refreshing Branches
1. Go to **Repositories Tab**
2. Click **🔄 Refresh Branches** button
3. MergePilot will:
   - Query each repository using `git ls-remote --heads origin`
   - Auto-discover all branches
   - Add them to **CustomBranches** list

✅ Branches are automatically saved

### Adding Custom Branch Manually
1. Go to **Custom Branches Tab**
2. Click **➕ Add Branch**
3. Select repository from dropdown
4. Enter branch name
5. Click **Save**

✅ Branch added to settings and appears in Source/Target dropdowns

---

## 📊 Main Window Integration

### Updated PopulateRepositoriesFromSettings()
```csharp
// Loads repositories as checkboxes
// Loads custom branches into Source/Target ComboBoxes
// Also loads recent branches (existing functionality)

SourceBranchBox.Items → Contains all custom branches
TargetBranchBox.Items → Contains all custom branches
```

### Updated ManageRepos_Click()
```csharp
private void ManageRepos_Click(object sender, RoutedEventArgs e)
{
    var dialog = new RepositoryManager();
    dialog.LoadData(_settings);
    dialog.Owner = this;
    if (dialog.ShowDialog() == true)
    {
        // Reload repositories and branches
        _settings = AppSettings.Load();
        PopulateRepositoriesFromSettings();
    }
}
```

---

## 🔧 Git Helper Methods Added

### GetRemoteBranchesAsync()
```csharp
public static async Task<List<string>> GetRemoteBranchesAsync(
    string repoPath, 
    string remoteName = "origin", 
    CancellationToken cancellationToken = default)
{
    // Executes: git ls-remote --heads origin
    // Returns list of branch names from remote
    // Used for auto-discovery in Repository Manager
}
```

---

## 📋 Workflow Examples

### Example 1: Adding Your First Repository
```
1. Press Ctrl+B → Repository Manager opens
2. Click "➕ Add Repository"
3. Name: "My Backend"
4. Path: C:\projects\my-backend (contains .git)
5. Remote URL: https://github.com/user/my-backend
6. Click Save → Repository added!
7. Click "🔄 Refresh Branches"
8. Wait for auto-discovery
9. ✅ All branches now appear in Source/Target dropdowns
```

### Example 2: Using Custom Branches
```
1. Repository already added
2. Click "🌿 Custom Branches" tab
3. Click "➕ Add Branch"
4. Repository: "My Backend"
5. Branch: feature/login-system
6. Click Save → Branch added!
7. Close dialog
8. In Main Window, dropdown shows: feature/login-system
9. Select it and perform merge operation
```

### Example 3: Editing an Existing Repository
```
1. Press Ctrl+B → Repository Manager
2. Click "✏️" button next to repository
3. Change name or path
4. Click Save → Updates saved!
5. Main window auto-reloads changes
```

---

## ✨ Key Features

| Feature | How It Works |
|---------|-------------|
| **Local Storage** | All repos/branches saved in settings.json |
| **Persistence** | Settings loaded on app startup |
| **Auto-Discovery** | Click refresh to find all branches from repos |
| **Manual Entry** | Add custom branches without repo access |
| **Validation** | Checks if path exists and contains .git |
| **No Network Required** | Works entirely offline after initial setup |
| **Dropdown Population** | Source/Target dropdowns auto-filled |

---

## 🛠️ Technical Details

### Data Persistence Flow
```
User Changes (Add/Edit/Delete)
    ↓
AppSettings.Save()
    ↓
JSON serialized to LocalAppData\MergePilot\settings.json
    ↓
Next app startup
    ↓
AppSettings.Load()
    ↓
Data restored from JSON
```

### Thread Safety
- Repository Manager runs on UI thread
- Git operations are async (non-blocking)
- Settings save happens synchronously (acceptable for small JSON files)

### Performance Considerations
- HashSet used for duplicate detection
- Repository count: typically < 50 (acceptable)
- Branch count per repo: typically < 500 (acceptable)
- Settings file is loaded once on startup

---

## 📝 Common Scenarios

### Scenario 1: Team Switch to MergePilot
```
1. Each dev gets settings.json from shared location
2. Place in %LocalAppData%\MergePilot\
3. MergePilot loads pre-configured repos/branches
4. No manual setup needed!
```

### Scenario 2: Moving Repository to Different Path
```
1. Click ✏️ to edit repository
2. Click 🗂️ to browse new location
3. Click Save
4. All branches still associated correctly
```

### Scenario 3: Backup & Restore Branches
```
1. settings.json in %LocalAppData%\MergePilot\
2. Backup this file
3. Restore on new machine
4. All repos/branches restored!
```

---

## 🐛 Troubleshooting

### Issue: "Directory does not contain .git folder"
- **Cause**: Selected path is not a git repository
- **Fix**: Select a folder that contains `.git` subfolder

### Issue: Branches don't appear in dropdowns
- **Cause**: CustomBranches list is empty
- **Fix**: 
  1. Add repository
  2. Click "🔄 Refresh Branches"
  3. Wait for completion

### Issue: Settings not persisting
- **Cause**: Permission issues in LocalAppData
- **Fix**: 
  1. Ensure MergePilot has write access
  2. Check folder permissions
  3. Restart application

### Issue: Can't find settings.json
- **Location**: `%AppData%\Local\MergePilot\settings.json`
- **On Windows**: `C:\Users\[YourUsername]\AppData\Local\MergePilot\`

---

## 🔐 Security Notes

- Settings stored locally in user's AppData (not shared)
- Remote URLs optional (only for reference)
- No credentials stored
- No internet access required
- All operations happen locally

---

## 📚 Summary

You now have a complete local repository and branch management system:

✅ **Add repositories** from your local machine  
✅ **Store them** in settings.json  
✅ **Auto-discover branches** using git commands  
✅ **Manually add branches** without git access  
✅ **Manage everything** in a dedicated UI window  
✅ **Persist all data** across app restarts  
✅ **Use branches** in merge/pull operations  

Happy branch merging! 🎉
