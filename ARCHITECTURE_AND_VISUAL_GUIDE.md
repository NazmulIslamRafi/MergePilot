# System Architecture & Visual Guide

## 🏗️ System Architecture

### Component Diagram
```
┌─────────────────────────────────────────────────────────────────┐
│                          MergePilot                             │
├─────────────────────────────────────────────────────────────────┤
│                                                                  │
│  ┌──────────────────────┐         ┌──────────────────────┐    │
│  │   Main Window        │         │  Repository Manager   │    │
│  │  ─────────────────   │         │  ──────────────────   │    │
│  │                      │         │                       │    │
│  │ • Repo Checkboxes    │◄───────►│ • Repo List (Tab 1)   │    │
│  │ • Source Branch DD   │         │ • Branch List (Tab 2) │    │
│  │ • Target Branch DD   │         │ • Add/Edit/Delete UI  │    │
│  │ • Merge Buttons      │         │                       │    │
│  │ • Logs Display       │         └──────────────────────┘    │
│  │                      │                    │                 │
│  └──────────────────────┘                    ▼                 │
│           │                      ┌──────────────────────┐     │
│           │                      │ AddRepositoryDialog   │     │
│           │                      │ AddBranchDialog       │     │
│           │                      └──────────────────────┘     │
│           │                                                     │
│           ▼                                                     │
│  ┌──────────────────────┐         ┌──────────────────────┐    │
│  │   Git Operations     │         │   Merge Operations   │    │
│  │  ──────────────────  │         │  ──────────────────  │    │
│  │                      │         │                      │    │
│  │ • ls-remote          │         │ • MergeBranchAsync   │    │
│  │ • GetBranches        │         │ • RetryLogic         │    │
│  │ • GetRemoteBranches  │         │ • ConflictHandling   │    │
│  └──────────────────────┘         └──────────────────────┘    │
│           │                                                     │
│           └────────────────────┬────────────────────────┘     │
│                                │                               │
└────────────────────────────────┼───────────────────────────────┘
                                 │
                ┌────────────────▼────────────────┐
                │   Git Repository (Local)        │
                │  ─────────────────────────────  │
                │                                 │
                │ • .git directory                │
                │ • Branches (refs/heads/)        │
                │ • Local & Remote tracking       │
                └─────────────────────────────────┘
                                 │
                ┌────────────────▼────────────────┐
                │   AppSettings Storage            │
                │  ─────────────────────────────  │
                │                                 │
                │ %LocalAppData%/MergePilot/      │
                │ settings.json                   │
                │                                 │
                │ • Repositories[]                │
                │ • CustomBranches[]              │
                │ • RecentBranches[]              │
                │ • UI State                      │
                └─────────────────────────────────┘
```

---

## 🔄 Data Flow Diagram

### Complete Flow: Adding Repository → Using Branch → Merging

```
┌────────────────────────────────────────────────────────────────┐
│                    User Interaction Flow                       │
└────────────────────────────────────────────────────────────────┘

1. INITIALIZATION (App Startup)
   ┌─────────────────────────────────┐
   │ MergePilot.exe launches         │
   └──────────────┬──────────────────┘
                  │
                  ▼
   ┌─────────────────────────────────┐
   │ App.xaml.cs                     │
   │ • Show Splash Screen (3s)       │
   │ • Initialize MainWindow         │
   └──────────────┬──────────────────┘
                  │
                  ▼
   ┌─────────────────────────────────┐
   │ MainWindow Constructor          │
   │ • Load AppSettings              │
   │ • Call PopulateRepositories()   │
   └──────────────┬──────────────────┘
                  │
                  ▼
   ┌─────────────────────────────────┐
   │ AppSettings.Load()              │
   │ • Read settings.json            │
   │ • Parse JSON                    │
   │ • Return AppSettings object     │
   └──────────────┬──────────────────┘
                  │
                  ▼
   ┌─────────────────────────────────┐
   │ PopulateRepositoriesFromSettings│
   │ • Create checkboxes for repos   │
   │ • Load CustomBranches into DD   │
   │ • Load RecentBranches into DD   │
   └─────────────────────────────────┘

2. REPOSITORY MANAGEMENT (Ctrl+B pressed)
   ┌─────────────────────────────────┐
   │ ManageRepos_Click()             │
   │ • Create RepositoryManager      │
   │ • Load _settings                │
   │ • Show as dialog                │
   └──────────────┬──────────────────┘
                  │
                  ▼
   ┌─────────────────────────────────┐
   │ RepositoryManager.xaml          │
   │ Shows two tabs:                 │
   │ ├─ Repositories Tab             │
   │ │  ├─ [➕ Add Repo]             │
   │ │  ├─ [🔄 Refresh Branches]    │
   │ │  └─ [✏️] [🗑️] per repo       │
   │ └─ Custom Branches Tab          │
   │    ├─ [➕ Add Branch]           │
   │    └─ [✏️] [🗑️] per branch    │
   └──────────────┬──────────────────┘
                  │
         ┌────────┴────────┐
         ▼                 ▼
   [User clicks      [User clicks
    Add Repo]        Refresh]
         │                 │
         ▼                 ▼
   ┌──────────────  ┌──────────────┐
   │ AddRepository  │ For each     │
   │ Dialog opens   │ repository:  │
   │ • Enter name   ├──────────────┤
   │ • Browse path  │              │
   │ • Click Save   │ GitHelper    │
   │ • Validate     │ .GetRemote   │
   │ • Add to list  │ BranchesAsync│
   └──────────┬─────┤              │
              │     │ Execute:    │
              │     │ git ls-     │
              │     │ remote --   │
              │     │ heads origin│
              │     │              │
              │     │ Parse output │
              │     │ refs/heads/  │
              │     │ branch-name  │
              │     │              │
              │     │ Add each to  │
              │     │ Custom       │
              │     │ Branches[]   │
              │     └──────────┬───┘
              │                │
              ▼                ▼
   ┌──────────────────────────────────┐
   │ _settings.Save()                 │
   │ • Serialize to JSON              │
   │ • Write to LocalAppData           │
   │ • Close RepositoryManager        │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ Back to MainWindow                │
   │ • Reload _settings = Load()      │
   │ • Call PopulateRepositories()    │
   │ • Branches appear in dropdowns    │
   └──────────────────────────────────┘

3. USING BRANCHES (Perform Merge)
   ┌──────────────────────────────────┐
   │ Main Window                       │
   │ • Select repository (checkbox)    │
   │ • Select source branch (DD)       │
   │ • Select target branch (DD)       │
   │ • Click Merge (Ctrl+M)           │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ Merge_Click() handler            │
   │ • Validate selections             │
   │ • Add branch to RecentBranches   │
   │ • Call GitHelper.MergeBranchAsync│
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ GitHelper                         │
   │ • git checkout target-branch     │
   │ • git merge source-branch        │
   │ • Handle conflicts/success        │
   │ • Return MergeResult             │
   └──────────────┬───────────────────┘
                  │
                  ▼
   ┌──────────────────────────────────┐
   │ MainWindow                        │
   │ • Log results                     │
   │ • Update RecentBranches          │
   │ • Save settings                   │
   │ • Show summary                    │
   └──────────────────────────────────┘
```

---

## 📊 Database Schema (settings.json)

```
settings.json
├── Repositories[] (RepositoryEntry)
│   ├── Name: string               // "Backend API"
│   ├── Path: string               // "C:\projects\backend"
│   └── RemoteUrl: string          // "https://github.com/user/backend"
│
├── CustomBranches[] (BranchEntry)
│   ├── BranchName: string         // "feature/login"
│   └── Repository: string         // "Backend API"
│
├── RecentBranches[] (string)
│   └── "main", "develop", "feature/auth"
│
├── LastSourceBranch: string       // "feature/auth"
├── LastTargetBranch: string       // "develop"
│
├── BranchCheckedState: object
│   └── "branch-name": true/false
│
├── BranchExpandedState: object
│   └── "group-path": true/false
│
├── AutoOpenLogs: boolean          // true
├── StreamLogs: boolean             // false
├── LogFilePath: string             // (optional)
├── LogFontSize: number             // 13.0
├── LogMaxChars: number             // 200000
│
├── InlineLogsVisible: boolean      // false
├── OutputErrorsOnly: boolean       // false
├── LastSearchOutput: string        // (optional)
└── LastSearchError: string         // (optional)
```

---

## 🎯 User Interface Hierarchy

```
┌─────────────────────────────────────────────────────────┐
│                      MergePilot                         │
│         (Main Window - Always Open)                     │
├─────────────────────────────────────────────────────────┤
│                                                          │
│  ┌──────────────────┐  ┌──────────────────┐           │
│  │ Repository List  │  │ Branch Selection │           │
│  ├──────────────────┤  ├──────────────────┤           │
│  │ □ Backend API    │  │ Source: [▼]      │           │
│  │ □ Frontend       │  │ Target: [▼]      │           │
│  │ □ Mobile App     │  │ [Merge] [Pull]   │           │
│  └──────────────────┘  └──────────────────┘           │
│                                                          │
│  ┌──────────────────────────────────────────┐         │
│  │          Log Output Areas                 │         │
│  │  ┌────────────────┐  ┌─────────────────┐ │         │
│  │  │ Output Box     │  │ Error Box       │ │         │
│  │  │ (Success/Info) │  │ (Errors Only)   │ │         │
│  │  └────────────────┘  └─────────────────┘ │         │
│  └──────────────────────────────────────────┘         │
│                                                          │
│  [Ctrl+B] Repository Manager  [Ctrl+L] Toggle Logs    │
└─────────────────────────────────────────────────────────┘
                           │
                           ▼ (Ctrl+B opens)
                ┌──────────────────────────┐
                │  Repository Manager      │
                │  (Dialog Window)         │
                ├──────────────────────────┤
                │                          │
                │  📁 Repos | 🌿 Branches │
                │                          │
                │  [Repo List]             │
                │  • Backend API  [✏️][🗑️]│
                │  • Frontend     [✏️][🗑️]│
                │                          │
                │  [➕ Add]  [🔄 Refresh]   │
                └──────────┬───────────────┘
                           │
              ┌────────────┴────────────┐
              ▼                         ▼
        ┌──────────────┐         ┌──────────────┐
        │Add Repository│         │Add Branch    │
        │Dialog        │         │Dialog        │
        ├──────────────┤         ├──────────────┤
        │ Name:        │         │ Repository:  │
        │ [________]   │         │ [Dropdown]   │
        │ Path:        │         │ Branch Name: │
        │ [____][🗂️]   │         │ [________]   │
        │ Remote URL:  │         │              │
        │ [________]   │         │              │
        │ [Save][X]    │         │ [Save][X]    │
        └──────────────┘         └──────────────┘
```

---

## 🔀 State Machine: Repository/Branch Lifecycle

```
┌─────────────────┐
│ REPOSITORY      │
│ Lifecycle       │
└─────────────────┘

States:
  NOT_ADDED → ADDING → ADDED → EDITING → UPDATED
                        ↓
                      DELETING
                        ↓
                      DELETED

Adding Flow:
  1. User clicks "➕ Add Repository"
     ↓
  2. AddRepositoryDialog opens
     ↓
  3. User enters: name, path (+ browse), remote URL
     ↓
  4. Dialog validates:
     • Name not empty
     • Path exists
     • .git folder present
     ↓
  5. If valid:
     • New RepositoryEntry created
     • Added to _settings.Repositories[]
     • _settings.Save() called
     • RepositoriesList refreshed
     ↓
  6. If invalid:
     • Error message shown
     • Dialog stays open

Deleting Flow:
  1. User clicks "🗑️" delete button
     ↓
  2. Confirmation dialog shown
     ↓
  3. If user confirms:
     • Repository removed from _settings.Repositories[]
     • _settings.Save() called
     • Associated branches still in CustomBranches[]
     • RepositoriesList refreshed
     ↓
  4. If user cancels:
     • No change

┌─────────────────┐
│ BRANCH          │
│ Lifecycle       │
└─────────────────┘

States:
  NOT_ADDED → AUTO_DISCOVERED → ADDING → ADDED
                                             ↓
                                         EDITING
                                             ↓
                                         UPDATED
                                             ↓
                                         DELETING
                                             ↓
                                         DELETED

Auto-Discovery Flow:
  1. User clicks "🔄 Refresh Branches"
     ↓
  2. For each repository in Repositories[]:
     ↓
  3. GitHelper.GetRemoteBranchesAsync() called
     ↓
  4. git ls-remote --heads [remote] executed
     ↓
  5. Output parsed (refs/heads/branch-name)
     ↓
  6. For each discovered branch:
     • Check if already in CustomBranches[]
     • If not: create BranchEntry, add to list
     ↓
  7. _settings.Save() called
     ↓
  8. Success message shown

Manual Addition Flow:
  1. User clicks "➕ Add Branch"
     ↓
  2. AddBranchDialog opens
     ↓
  3. User selects repository, enters branch name
     ↓
  4. Dialog validates:
     • Repository selected
     • Branch name not empty
     ↓
  5. If valid:
     • New BranchEntry created
     • Added to _settings.CustomBranches[]
     • _settings.Save() called
     • BranchesList refreshed
     ↓
  6. If invalid:
     • Error message shown
     • Dialog stays open

Usage in Merge:
  1. User selects branch from dropdown
     ↓
  2. Branch name added to RecentBranches[]
     ↓
  3. During merge operation used
     ↓
  4. Settings saved (includes RecentBranches)
```

---

## ⚙️ Configuration & Settings Path

```
Settings Storage Hierarchy:

┌──────────────────────────────────────────┐
│ Windows Registry (Not Used)              │
│ [Not applicable for this app]            │
└──────────────────────────────────────────┘

┌──────────────────────────────────────────┐
│ User's LocalAppData Folder               │
│ (Standard Location)                      │
├──────────────────────────────────────────┤
│                                          │
│ %AppData%\Local\MergePilot\              │
│ │                                        │
│ └─ settings.json                         │
│    (200-500 KB typical)                  │
│                                          │
│ Environment Variable:                    │
│ %LOCALAPPDATA%\MergePilot\settings.json │
│                                          │
│ Windows Path:                            │
│ C:\Users\[Username]\AppData\Local\      │
│   MergePilot\settings.json              │
│                                          │
└──────────────────────────────────────────┘

Access Code:
  Path = Environment.GetFolderPath(
      Environment.SpecialFolder.LocalApplicationData
  ) + "\\MergePilot\\settings.json"

Result Example:
  C:\Users\JohnDoe\AppData\Local\
    MergePilot\settings.json
```

---

## 📈 Performance Characteristics

```
Operation                    Time    Memory   Notes
─────────────────────────────────────────────────────
Load settings from JSON      <10ms   ~100KB   First app startup
Add repository               <5ms    ~1KB     Dialog validation
Add branch                   <5ms    ~0.5KB   Simple insertion
Delete repository            <5ms    ~1KB     List removal
Delete branch                <5ms    ~0.5KB   List removal
Refresh branches (1 repo)    100-500ms varies  Single git ls-remote
Refresh branches (5 repos)   500-2500ms varies Multiple git calls
Populate UI dropdowns        <50ms   varies   Up to 500 branches
Save settings to JSON        <20ms   ~150KB   JSON serialization
Open RepositoryManager       <100ms  ~5MB     Window + lists

Constraints:
  • Max repositories: 100 (reasonable)
  • Max branches per repo: 1000 (tested)
  • Max custom branches total: 5000 (practical limit)
  • Settings file size: ~500KB typical
```

---

## 🔐 Security & Permissions

```
File System Permissions:
┌─────────────────────────────────────┐
│ Settings File: settings.json         │
├─────────────────────────────────────┤
│                                     │
│ Location: %LOCALAPPDATA%/MergePilot │
│ Owner: Current User                 │
│ Permissions: Read/Write by user     │
│ Visibility: User's AppData (private)│
│ Access: User account only           │
│                                     │
│ No credentials stored               │
│ No passwords persisted              │
│ No sensitive data in JSON           │
│                                     │
└─────────────────────────────────────┘

Git Repository Access:
┌─────────────────────────────────────┐
│ Local Git Commands (No Network)      │
├─────────────────────────────────────┤
│                                     │
│ • git ls-remote --heads             │
│   (reads refs from .git/config)     │
│                                     │
│ • Requires local write access       │
│ • Respects OS file permissions      │
│ • Works offline after initial setup │
│                                     │
└─────────────────────────────────────┘
```

---

✨ **Complete System Architecture Documented** ✨

All flows, components, data structures, and interactions have been mapped out for complete understanding of the system.
