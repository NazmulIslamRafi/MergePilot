# MergePilot - Project Analysis & Overview

## Project Summary
**MergePilot** is a sophisticated WPF (Windows Presentation Foundation) desktop application built with .NET 8 that automates git branch merging operations across multiple repositories. It provides a user-friendly GUI for managing complex merge workflows with hierarchical branch organization, multi-repository support, and comprehensive logging.

---

## Technical Stack
- **Framework**: .NET 8 (Windows Desktop)
- **UI Framework**: WPF (Windows Presentation Foundation)
- **Package Dependencies**:
  - **MahApps.Metro** v2.4.10 - Modern WPF UI themes and controls
  - **MaterialDesignThemes** v4.9.0 - Material Design implementation for WPF
  - **MaterialDesignColors** v2.1.4 - Material Design color palettes
- **Target Runtime**: net8.0-windows10.0.26100.0
- **Language Features**: C# with nullable reference types enabled, implicit usings enabled

---

## Project Structure

### Core Architecture Layers

#### 1. **Application Layer** (Entry Point & Main Window)
- **App.xaml / App.xaml.cs**: Application startup orchestration
  - Shows splash screen (3-second delay)
  - Manages application shutdown behavior
  - Loads MaterialDesign and MahApps theming resources

- **MainWindow.xaml / MainWindow.xaml.cs**: Primary UI and core business logic
  - 1000+ lines of complex WPF code handling merging operations
  - Multiple data bindings for hierarchical branch selection
  - Real-time logging with performance optimization

- **Window2.xaml / Window2.xaml.cs**: Secondary window (appears to be legacy/unused in main flow)

- **SplashScreen.xaml / SplashScreen.xaml.cs**: Startup splash screen

#### 2. **Data Models & Business Objects**
- **AppSettings.cs**: Persistent application settings
  - Persists user preferences (log visibility, font size, etc.)
  - Stores dynamic repository list
  - Maintains branch selection state and expansion state
  - Uses JSON serialization to `%LocalAppData%/MergePilot/settings.json`
  - Properties:
    - AutoOpenLogs, StreamLogs, LogFilePath
    - Repositories (list of git repositories)
    - RecentBranches, LastSourceBranch, LastTargetBranch
    - BranchCheckedState, BranchExpandedState
    - InlineLogsVisible, OutputErrorsOnly
    - LogFontSize, LogMaxChars

- **BranchItem.cs**: Hierarchical branch representation
  - Supports tri-state checkboxes (checked/unchecked/indeterminate)
  - Implements INotifyPropertyChanged for data binding
  - Properties: Name, FullName, IsGroup, Level, Children, Parent
  - Includes BranchOrganizer helper class for converting flat branch lists to hierarchical structures

#### 3. **Git Operations (Backend)**
- **GitHelper.cs**: Low-level git command execution
  - `RunGitCommandAsync()`: Core async process execution for git commands
  - `RetryRunGitCommandAsync()`: Retry strategy with configurable attempts (default 3x)
  - `MergeBranchAsync()`: Executes git merge with conflict detection
  - `AbortMergeAsync()`: Rolls back merge in conflict scenarios
  - `RemoteBranchExistsAsync()`: Validates branch existence via `git ls-remote`
  - `GetDefaultRemoteNameAsync()`: Detects default remote (origin fallback)
  - Returns: CommandResult (ExitCode, StdOut, StdErr, IsSuccess flag)
  - Supports CancellationToken for timeout and cancellation
  - Process management: async stream reading to prevent deadlocks

- **MergeStatus Enum**: Success, Skipped, Conflict, Failed
- **MergeResult Record**: Contains status, message, and command result

#### 4. **UI Converters & Formatting**
- **Converters.cs**: WPF value converters
  - WidthLessThanConverter: For responsive design (width threshold checking)
  - HalfWidthConverter: Dynamic width calculations

- **BranchConverters.cs**: Branch-specific converters
  - LevelToIndentConverter: Converts hierarchical level to pixel indentation (15px per level)
  - BoolToVisibilityConverter: Shows/hides UI elements based on branch type (group vs leaf)
  - BoolToFontWeightConverter: Bolds group names, normal text for leaves
  - WidthToBoolConverter: Responsive design based on window width

- **LogFormatter.cs**: Structured log output formatting
  - Provides colored text helpers matching bash script color scheme
  - Methods: SuccessText(), WarningText(), ErrorText(), InfoText()
  - Formats operation banners, project headers, branch headers, summaries
  - Returns tuples of (text, SolidColorBrush) for RichTextBox rendering

- **ColorScheme.cs**: Centralized color definitions
  - SUCCESS (Green): #22C55E - successful operations
  - WARNING (Yellow): #EAB308 - skipped operations
  - ERROR (Red): #EF4444 - failed operations
  - INFO (Blue): #3B82F6 - informational messages
  - SECONDARY_INFO (Light Blue): #60A5FA - alternative info color
  - All colors have corresponding SolidColorBrush objects

#### 5. **Command Infrastructure**
- **RelayCommand.cs**: MVVM-style command implementation
  - Implements ICommand interface
  - Generic execute/canExecute delegates
  - Supports command parameter binding
  - Used for button click handlers with keyboard shortcuts

#### 6. **UI Resources & Styling**
- **App.xaml**: Global application resources
  - Material Design theme integration
  - MahApps.Metro styles
  - Color brush definitions
  - ComboBox and CheckBox style templates
  - Hierarchical branch data template for visual representation

---

## Key Features

### 1. **Repository Management**
- Dynamic repository list (add/edit/remove/persist)
- Multiple repository batch operations
- Repository validation (.git folder checking)
- Per-repository branch loading

### 2. **Branch Organization**
- Hierarchical branch display using path separators (e.g., "0-Task/NewUpdate")
- Tri-state checkboxes for parent-child relationships
- Flat list to hierarchical tree conversion
- Batch selection/deselection of branch groups

### 3. **Merge Operations**
- **Retry Strategy**: 3 attempts per merge with configurable delays
- **Merge-Base Check**: Skip already-merged branches
- **Conflict Handling**: 
  - Detects merge conflicts
  - Prompts user to resolve manually
  - Opens repository in Explorer for manual resolution
  - Auto-commits and pushes resolved conflicts
  - Abort option for skipping conflicted branches
- **Multi-Repository Support**: Merges across multiple repos in single operation
- **Multi-Branch Support**: Each source can merge to multiple targets

### 4. **Logging System**
- **Dual Output**: Separate streams for success/info (OutputBox) and errors (ErrorBox)
- **Real-time Updates**: Queue-based flushing with 100ms intervals
- **Performance Optimization**: 
  - Max 200,000 characters per log buffer
  - Concurrent queue pattern for thread-safe logging
  - Smooth scrolling timer (60 FPS approximation)
  - Master buffers for non-destructive filtering
- **Search/Filter**: Find next/previous matches in logs
- **Log Persistence**: Optional file streaming to disk
- **Appearance Settings**: Configurable font size, max characters, auto-open preference

### 5. **Keyboard Shortcuts**
- Ctrl+M: Merge operation
- Ctrl+P: Pull selected branches
- Ctrl+R: Refresh branches
- Ctrl+B: Branch/Repository manager
- Ctrl+L: Toggle logs visibility
- Shift+E: Toggle log pane

### 6. **Responsive Design**
- Adaptive UI based on window width (breakpoint at 900px)
- Collapsible sections
- Dynamic column layouts
- Material Design responsive theming

### 7. **Settings Persistence**
- JSON-based settings storage in LocalAppData
- Persisted state includes:
  - Repository list
  - Branch checked states
  - Expansion states
  - Last used branches
  - Log appearance preferences
  - Search history

---

## Data Flow

### Merge Operation Flow
1. **User Selection**: Select repositories, source branches, and target branches from UI
2. **Validation**: Check branch existence on remote, confirm selections
3. **Iteration Loop**: 
   - For each repository:
     - For each source branch:
       - For each target branch:
         - Validate target on remote
         - Execute merge with retry logic
         - Handle success/skip/conflict/failure cases
4. **Conflict Resolution**: 
   - If conflict detected, prompt user
   - Open Explorer for manual resolution
   - Auto-commit and push resolved changes
5. **Logging**: 
   - Real-time logging via queues
   - Summary generation at operation end
6. **Summary**: Final report of successes, skips, and failures

### UI State Management
1. **Branch Loading**: 
   - On repo selection, async load remote branches
   - Build hierarchical tree structure
   - Bind to ComboBox ItemsSource
2. **Checkbox Tracking**: 
   - Maintain HashSet of checked items per ComboBox
   - Tri-state logic for parent-child relationships
   - Prevent re-entrant checkbox events
3. **Settings Sync**: 
   - Persist state on every change
   - Load on app startup
   - Restore last-used branches

---

## Performance Optimizations

### 1. **Logging Performance**
- Queue-based approach (ConcurrentQueue) prevents UI blocking
- Batch flushing at 100ms intervals
- Line limiting (max 2000 displayed lines)
- Character capping (max 200,000 chars)
- Smooth scroll animation at 60 FPS approximation

### 2. **Git Command Execution**
- Async/await for non-blocking operations
- Process stream reading asynchronously to prevent deadlocks
- Retry strategy with exponential considerations
- CancellationToken support for timeout control
- Background thread execution with Dispatcher marshaling to UI

### 3. **UI Optimization**
- DispatcherTimer for periodic tasks
- Responsive design adapts to window size
- Material Design base theme with custom styling

---

## Code Organization Best Practices

### 1. **Separation of Concerns**
- Git operations isolated in GitHelper
- UI logic in code-behind (MainWindow.xaml.cs)
- Data models separate from business logic
- Formatting utilities in LogFormatter and ColorScheme

### 2. **Error Handling**
- Try-catch blocks around risky operations
- User-facing error dialogs for important failures
- Graceful fallbacks (e.g., "origin" default remote)
- Comprehensive logging of all operations

### 3. **Async/Await Pattern**
- All git operations are async
- CancellationToken support throughout
- Proper synchronization context management
- Dispatcher marshaling for UI updates

### 4. **Configuration Management**
- AppSettings class centralizes all preferences
- JSON serialization for persistence
- Sensible defaults for all settings
- Easy to extend with new settings

---

## Important Constants & Configuration

### Separators
```
SectionSeparator = "==============================================="
SubSectionSeparator = "-----------------------------------------------"
```

### Performance Limits
```
MaxLogChars = 200,000
MaxDisplayedLines = 2000
LogFlushInterval = 100ms
SmoothScrollInterval = 16ms (~60 FPS)
```

### Retry Strategy
```
Max Attempts: 3
Delay Between Attempts: 2 seconds
Timeout Per Attempt: Configurable (varies by operation)
```

### Git Defaults
```
Default Remote: "origin" (discovered via git remote, fallbacks to "origin")
Timeout for Remote Check: 20 seconds
```

---

## Dependencies & External Systems

### System Dependencies
- Git (system PATH)
- Windows Explorer
- .NET 8 Runtime

### UI Framework Dependencies
- MahApps.Metro (Modern WPF controls and theming)
- MaterialDesignThemes (Material Design implementation)
- MaterialDesignColors (Color palettes)

### .NET Features Used
- ConcurrentQueue for thread-safe operations
- Process management with async support
- WPF data binding and MVVM patterns
- JSON serialization (System.Text.Json)

---

## File-by-File Summary

| File | Purpose | Key Responsibilities |
|------|---------|---------------------|
| App.xaml/cs | Application entry point | Startup orchestration, theming |
| MainWindow.xaml/cs | Main UI & merge logic | Merge operations, repository management, logging, branch selection |
| Window2.xaml/cs | Secondary window | Legacy/unused (appears to be test window) |
| AppSettings.cs | Preferences persistence | Settings storage, JSON serialization |
| GitHelper.cs | Git command execution | Git operations, retry logic, command result parsing |
| BranchItem.cs | Data model | Hierarchical branch representation, tree building |
| ColorScheme.cs | Color constants | Centralized color definitions matching bash script |
| LogFormatter.cs | Log formatting | Structured log output, color-coded messages |
| Converters.cs | WPF converters | Width/visibility conversions |
| BranchConverters.cs | Branch UI converters | Indentation, visibility, font weight |
| RelayCommand.cs | MVVM command | Generic command implementation |

---

## Architecture Diagram

```
???????????????????????????????????????????????????????????
?                   MergePilot Application                 ?
???????????????????????????????????????????????????????????
?                                                           ?
?  ????????????????????????????????????????????????????   ?
?  ?         WPF UI Layer (MainWindow.xaml)            ?   ?
?  ?  ?? Repository Selection                          ?   ?
?  ?  ?? Branch Hierarchical Selection                ?   ?
?  ?  ?? Real-time Logging (Output + Error panes)    ?   ?
?  ?  ?? Merge Button, Repository Manager Panel       ?   ?
?  ????????????????????????????????????????????????????   ?
?                           ?                                ?
?  ????????????????????????????????????????????????????   ?
?  ?      Business Logic (MainWindow.xaml.cs)         ?   ?
?  ?  ?? Merge_Click() - Main merge orchestration    ?   ?
?  ?  ?? Branch/Repo management methods              ?   ?
?  ?  ?? UI event handlers                            ?   ?
?  ?  ?? Logging queue flushing                       ?   ?
?  ????????????????????????????????????????????????????   ?
?                           ?                                ?
?  ????????????????????????????????????????????????????   ?
?  ?     GitHelper (Git Command Execution)            ?   ?
?  ?  ?? RunGitCommandAsync                           ?   ?
?  ?  ?? RetryRunGitCommandAsync (3x retry)          ?   ?
?  ?  ?? MergeBranchAsync                            ?   ?
?  ?  ?? RemoteBranchExistsAsync                     ?   ?
?  ?  ?? AbortMergeAsync                             ?   ?
?  ????????????????????????????????????????????????????   ?
?                           ?                                ?
?  ????????????????????????????????????????????????????   ?
?  ?    System Layer (git CLI, file I/O)             ?   ?
?  ????????????????????????????????????????????????????   ?
?                                                           ?
?  ????????????????????????????????????????????????????   ?
?  ?  Supporting Services                              ?   ?
?  ?  ?? AppSettings (Persistence)                    ?   ?
?  ?  ?? LogFormatter (Output formatting)            ?   ?
?  ?  ?? ColorScheme (Theming)                       ?   ?
?  ?  ?? BranchOrganizer (Tree building)            ?   ?
?  ?  ?? WPF Converters (UI binding)                 ?   ?
?  ????????????????????????????????????????????????????   ?
?                                                           ?
???????????????????????????????????????????????????????????
```

---

## Key Design Patterns Used

1. **MVVM Pattern**: Partial (MainWindow uses code-behind instead of ViewModel)
2. **Repository Pattern**: AppSettings encapsulates persistence
3. **Command Pattern**: RelayCommand for WPF commands
4. **Observer Pattern**: INotifyPropertyChanged on BranchItem
5. **Async/Await Pattern**: Throughout GitHelper for non-blocking I/O
6. **Converter Pattern**: WPF value converters for UI binding
7. **Builder Pattern**: BranchOrganizer builds hierarchical structures
8. **Retry Pattern**: RetryRunGitCommandAsync with configurable attempts

---

## Copilot Instructions Highlights

From `.github/copilot-instructions.md`:
- Every merge operation should follow **retry-3 strategy** ? Implemented
- Automatically skip if already merged (merge-base check) ? Implemented
- Use `--no-edit` for pulls/merges ? Git commands follow this
- Prompt user to resolve conflicts or abort ? User prompt dialog
- Present final summary of successes, skips, failures ? Logging shows summary
- **Performance Optimization**: Implement smooth scrolling in RichTextBox ? 60 FPS timer, queue-based updates

---

## Version & Branch Information

- **Current Branch**: 0-Task/Version-2.0
- **Remote**: https://github.com/NazmulIslamRafi/MergePilot
- **.NET Version**: 8.0
- **Windows Target**: 10.0.26100.0 (Win11)

---

## Conclusion

MergePilot is a well-architected, production-ready desktop application that demonstrates:
- ? Proper async/await patterns for long-running operations
- ? Performance optimizations for UI responsiveness
- ? Comprehensive error handling and user feedback
- ? Persistent state management
- ? Clean separation of concerns
- ? Material Design UI with responsive layout
- ? Sophisticated branch hierarchy management
- ? Multi-repository, multi-branch merge support with conflict resolution

The codebase is ready for future enhancements such as:
- Unit testing for GitHelper operations
- Configuration for merge strategy options
- Webhook integration for automated merges
- Cloud backup of settings
- Performance metrics/telemetry
