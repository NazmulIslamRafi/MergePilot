# Quick Reference: Local Repository & Branch Management

## ⚡ Quick Start (5 Minutes)

### 1️⃣ Add Your First Repository
```
Step 1: Press Ctrl+B
         ↓
Step 2: Click "➕ Add Repository"
         ↓
Step 3: Name: MyProject
         ↓
Step 4: Click "🗂️ Browse" → Select C:\path\to\myproject
         (Must contain .git folder)
         ↓
Step 5: Click "Save"
         ↓
✅ Repository added!
```

### 2️⃣ Load Branches from Repository
```
Step 1: Click "🔄 Refresh Branches"
         ↓
Step 2: Wait for "Branches refreshed successfully!"
         ↓
✅ All branches from repo now available!
```

### 3️⃣ Use in Merge Operation
```
Step 1: Close RepositoryManager (auto-reloads)
         ↓
Step 2: In Main Window:
         • Select repository (checkbox)
         • Select source branch (dropdown)
         • Select target branch (dropdown)
         ↓
Step 3: Click "Merge" (Ctrl+M)
         ↓
✅ Merge complete!
```

---

## 🎮 Keyboard Shortcuts

| Shortcut | Action | Window |
|----------|--------|--------|
| `Ctrl+B` | Open Repository Manager | Main Window |
| `Ctrl+M` | Merge selected branches | Main Window |
| `Ctrl+P` | Pull branches | Main Window |
| `Ctrl+L` | Toggle logs visibility | Main Window |
| `Shift+E` | Toggle error pane | Main Window |

---

## 📱 UI Controls Reference

### Repository Manager Window

#### 📁 Repositories Tab
| Control | Action | Result |
|---------|--------|--------|
| `➕ Add Repository` | Click | Opens AddRepositoryDialog |
| `✏️` (per repo) | Click | Edit repository name/path |
| `🗑️` (per repo) | Click | Delete repository |
| `🔄 Refresh Branches` | Click | Auto-discovers branches from all repos |

#### 🌿 Custom Branches Tab
| Control | Action | Result |
|---------|--------|--------|
| `➕ Add Branch` | Click | Opens AddBranchDialog |
| `✏️` (per branch) | Click | Edit branch name/repo |
| `🗑️` (per branch) | Click | Delete branch |

### Main Window Integration
| Control | Action | Result |
|---------|--------|--------|
| Repository Checkboxes | Check | Select repo for operations |
| Source Branch Dropdown | Select | Choose merge source |
| Target Branch Dropdown | Select | Choose merge target |
| Merge Button | Click | Execute merge operation |

---

## 💾 Data Persistence

### Where Settings Are Saved
```
Windows:  C:\Users\[YourName]\AppData\Local\MergePilot\settings.json
Shortcut: %LOCALAPPDATA%\MergePilot\settings.json
```

### What Gets Saved
- ✅ Repository list (name, path, URL)
- ✅ Custom branches (name, associated repo)
- ✅ Recent branches (last used)
- ✅ UI state (log visibility, font size)
- ✅ Window state (size, position)

### When It Gets Saved
- ⏱️ After adding repository
- ⏱️ After adding/editing/deleting branch
- ⏱️ After refreshing branches
- ⏱️ After merge operation (recent branches)
- ⏱️ On app shutdown

---

## 🔍 How It Works

### Adding a Repository
```
AddRepositoryDialog
    ↓ (validates)
RepositoryEntry created
    ↓
Added to AppSettings.Repositories[]
    ↓
_settings.Save() → JSON file
    ↓
UI list refreshed
```

### Auto-Discovering Branches
```
Click "🔄 Refresh"
    ↓
For each repository:
    ↓
GitHelper.GetRemoteBranchesAsync()
    ↓
Executes: git ls-remote --heads origin
    ↓
Parses: refs/heads/branch-name
    ↓
Creates BranchEntry for each
    ↓
Adds to AppSettings.CustomBranches[]
    ↓
_settings.Save()
    ↓
UI lists refreshed
```

### Using Branch in Merge
```
Select branch from dropdown
    ↓
Added to RecentBranches[]
    ↓
Merge operation executes
    ↓
_settings.Save() (includes recent branches)
```

---

## ❌ Troubleshooting

### Problem: "Directory does not contain .git folder"
**Cause:** Selected path is not a Git repository  
**Solution:**  
1. Select a folder with `.git` subfolder  
2. For GitHub repos: Clone the repo first  
3. Or run `git init` in the folder

### Problem: Branches not appearing in dropdown
**Cause:** CustomBranches list is empty  
**Solution:**  
1. Add repository first
2. Click "🔄 Refresh Branches"
3. Wait for completion
4. Check "Custom Branches" tab

### Problem: "Failed to find settings"
**Cause:** Settings file not created yet  
**Solution:**  
1. First time? Just add a repository
2. Settings.json will be created
3. Settings saved to %LOCALAPPDATA%\MergePilot\

### Problem: Changes not persisting
**Cause:** Permissions or file access issue  
**Solution:**  
1. Ensure write access to AppData folder
2. Check Windows permissions
3. Restart app to force reload

### Problem: Git command fails
**Cause:** Git not installed or not in PATH  
**Solution:**  
1. Install Git for Windows
2. Add Git to PATH
3. Restart MergePilot

---

## 📊 Settings.json Structure

### Minimal Example
```json
{
  "Repositories": [
    {
      "Name": "Backend",
      "Path": "C:\\projects\\backend",
      "RemoteUrl": "https://github.com/user/backend"
    }
  ],
  "CustomBranches": [
    {
      "BranchName": "main",
      "Repository": "Backend"
    },
    {
      "BranchName": "develop",
      "Repository": "Backend"
    },
    {
      "BranchName": "feature/auth",
      "Repository": "Backend"
    }
  ],
  "RecentBranches": ["main", "develop"],
  "AutoOpenLogs": true,
  "StreamLogs": false,
  "LogFontSize": 13.0,
  "LogMaxChars": 200000
}
```

### Manual Editing
You can edit settings.json directly:
1. Open: `%LOCALAPPDATA%\MergePilot\settings.json`
2. Edit JSON (add/remove entries)
3. Save file
4. Restart MergePilot

---

## 🎓 Common Workflows

### Workflow 1: Team Project Setup
```
Scenario: New developer joining team
─────────────────────────────────────

1. Get settings.json from team
2. Place in %LOCALAPPDATA%\MergePilot\
3. Launch MergePilot
4. All repos/branches pre-configured ✅
5. Ready to merge immediately ✅
```

### Workflow 2: Multiple Local Repositories
```
Scenario: Working with 3 repositories
──────────────────────────────────────

1. Ctrl+B → Repository Manager
2. ➕ Add Repository #1
3. ➕ Add Repository #2
4. ➕ Add Repository #3
5. 🔄 Refresh Branches (discovers all)
6. Close dialog
7. Select any repo + branches from dropdowns
8. Merge across repositories ✅
```

### Workflow 3: Feature Branch Release
```
Scenario: Releasing feature to production
─────────────────────────────────────────

1. Custom branches already exist:
   - feature/new-feature
   - release/v1.0
   - main

2. Select repository
3. Source: feature/new-feature
4. Target: release/v1.0
5. Merge ✅
6. Source: release/v1.0
7. Target: main
8. Merge ✅
```

### Workflow 4: Backing Up Configuration
```
Scenario: Backup settings before migration
──────────────────────────────────────────

1. Locate: %LOCALAPPDATA%\MergePilot\settings.json
2. Copy to: D:\backup\settings.json
3. On new machine:
4. Place backup in %LOCALAPPDATA%\MergePilot\
5. Launch MergePilot
6. All settings restored ✅
```

---

## 🔗 Related Topics

- **Full Guide**: `LOCAL_REPOSITORY_MANAGEMENT_GUIDE.md`
- **Architecture**: `ARCHITECTURE_AND_VISUAL_GUIDE.md`
- **Implementation**: `IMPLEMENTATION_SUMMARY.md`
- **Project Analysis**: `PROJECT_ANALYSIS.md`

---

## 📞 Feature Summary

| Feature | Status | Shortcut |
|---------|--------|----------|
| Add Repository | ✅ Working | Ctrl+B → ➕ Add |
| Edit Repository | ✅ Working | Ctrl+B → ✏️ |
| Delete Repository | ✅ Working | Ctrl+B → 🗑️ |
| Auto-Discover Branches | ✅ Working | Ctrl+B → 🔄 |
| Add Branch Manually | ✅ Working | Ctrl+B → ➕ Add |
| Edit Branch | ✅ Working | Ctrl+B → ✏️ |
| Delete Branch | ✅ Working | Ctrl+B → 🗑️ |
| Load in Dropdowns | ✅ Working | Auto-loaded |
| Merge Operation | ✅ Working | Ctrl+M |
| Persist Settings | ✅ Working | Auto-save |

---

## ✨ You're All Set!

Everything is ready to use:
- ✅ Build successful
- ✅ All features implemented
- ✅ Settings persistence working
- ✅ UI fully integrated
- ✅ Documentation complete

**Get started:** Press `Ctrl+B` and add your first repository! 🚀

---

**Version:** 1.0  
**Last Updated:** 2024  
**Status:** Production Ready ✅
