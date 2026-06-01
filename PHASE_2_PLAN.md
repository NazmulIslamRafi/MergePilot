# Phase 2: MVVM Refactoring & Performance Optimization - Implementation Plan

## Overview
Phase 2 will refactor the code-behind heavy MainWindow into a proper MVVM architecture while implementing performance optimizations and async/await improvements.

## Phase 2 Objectives

### 1. **MVVM Architecture Implementation**
- Create `ViewModels/MainWindowViewModel.cs`
- Create `ViewModels/RepositoryViewModel.cs`
- Create `Views/MainWindow.xaml` (rebind to ViewModel)
- Implement `INotifyPropertyChanged` and `ICommand` patterns
- Move business logic from code-behind to ViewModel

### 2. **Async/Await Improvements**
- Refactor `RepositoryManager.cs` for async operations
- Implement proper cancellation token support
- Add async task tracking and progress reporting
- Prevent UI blocking during Git operations

### 3. **Performance Optimization**
- Implement branch list caching with invalidation
- Optimize RichTextBox rendering with virtual scrolling
- Reduce log processing overhead
- Implement efficient branch filtering

### 4. **UI Enhancements**
- Add progress indicators for long operations
- Implement smooth scrolling for logs
- Add operation cancellation UI
- Improve error presentation

## Detailed Tasks

### Phase 2.1: View Model Infrastructure (Days 1-2)

#### Create Base ViewModel Class
**File**: `MergePilot/ViewModels/ViewModelBase.cs`
```csharp
public abstract class ViewModelBase : INotifyPropertyChanged
{
    public event PropertyChangedEventHandler? PropertyChanged;

    protected void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }

    protected bool SetProperty<T>(ref T backingField, T value, string propertyName)
    {
        if (EqualityComparer<T>.Default.Equals(backingField, value))
            return false;

        backingField = value;
        OnPropertyChanged(propertyName);
        return true;
    }
}
```

#### Create RelayCommand Implementation
**File**: `MergePilot/ViewModels/RelayCommand.cs`
```csharp
public class RelayCommand : ICommand
{
    private readonly Action<object?> _execute;
    private readonly Predicate<object?>? _canExecute;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => _canExecute?.Invoke(parameter) ?? true;

    public void Execute(object? parameter) => _execute(parameter);
}

public class AsyncRelayCommand : ICommand
{
    private readonly Func<object?, Task> _execute;
    private readonly Predicate<object?>? _canExecute;
    private bool _isExecuting;

    public event EventHandler? CanExecuteChanged
    {
        add { CommandManager.RequerySuggested += value; }
        remove { CommandManager.RequerySuggested -= value; }
    }

    public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
    {
        _execute = execute ?? throw new ArgumentNullException(nameof(execute));
        _canExecute = canExecute;
    }

    public bool CanExecute(object? parameter) => !_isExecuting && (_canExecute?.Invoke(parameter) ?? true);

    public async void Execute(object? parameter)
    {
        if (!CanExecute(parameter))
            return;

        _isExecuting = true;
        CommandManager.InvalidateRequerySuggested();

        try
        {
            await _execute(parameter);
        }
        finally
        {
            _isExecuting = false;
            CommandManager.InvalidateRequerySuggested();
        }
    }
}
```

### Phase 2.2: RepositoryManager Async Refactoring (Days 2-3)

**Current Issues**:
- Synchronous blocking operations
- No proper cancellation support
- Mixed sync/async patterns

**Solution**:
- Make all Git operations async
- Implement CancellationToken support
- Add progress reporting

```csharp
public class RepositoryManager
{
    public event EventHandler<OperationProgressEventArgs>? ProgressChanged;

    public async Task<IEnumerable<BranchItem>> GetBranchesAsync(
        string repoPath,
        CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (_branchCache.TryGetValue(repoPath, out var cached))
        {
            if (DateTime.UtcNow - cached.timestamp < CacheDuration)
                return cached.branches;
        }

        // Fetch from Git
        var branches = await GitHelper.GetBranchesAsync(repoPath, cancellationToken);

        // Cache result
        _branchCache[repoPath] = (branches, DateTime.UtcNow);

        ProgressChanged?.Invoke(this, new OperationProgressEventArgs(100, "Branches loaded"));

        return branches;
    }
}
```

### Phase 2.3: MainWindowViewModel Creation (Days 3-4)

**File**: `MergePilot/ViewModels/MainWindowViewModel.cs`

**Key Properties**:
```csharp
public class MainWindowViewModel : ViewModelBase
{
    private IEnumerable<BranchItem> _sourceBranches;
    private IEnumerable<BranchItem> _targetBranches;
    private bool _isOperationInProgress;
    private string _statusMessage;
    private int _progressPercentage;

    public ICommand MergeCommand { get; }
    public ICommand RefreshCommand { get; }
    public ICommand ManageRepositoriesCommand { get; }
    public ICommand CancelOperationCommand { get; }

    private CancellationTokenSource? _operationCancellation;
}
```

### Phase 2.4: XAML Binding Updates (Days 4-5)

**Update MainWindow.xaml**:
- Set DataContext to MainWindowViewModel
- Bind commands to buttons
- Bind properties to UI elements
- Remove code-behind logic

**Before** (Code-behind):
```csharp
private void Merge_Click(object sender, RoutedEventArgs e)
{
    // 50+ lines of logic
}
```

**After** (XAML):
```xml
<Button Command="{Binding MergeCommand}" Content="Merge" />
```

### Phase 2.5: Performance Optimization (Days 5-6)

#### Branch Cache Implementation
```csharp
private Dictionary<string, (IEnumerable<BranchItem> branches, DateTime timestamp)> _branchCache 
    = new();
private static readonly TimeSpan CacheDuration = TimeSpan.FromMinutes(5);

public void InvalidateCache(string? repoPath = null)
{
    if (repoPath == null)
        _branchCache.Clear();
    else
        _branchCache.Remove(repoPath);
}
```

#### Virtual Log Rendering
```csharp
// Only render visible lines in RichTextBox
private void OptimizeLogRendering()
{
    if (_displayedLineCount > MaxDisplayedLines)
    {
        // Remove oldest lines
        var extraLines = _displayedLineCount - MaxDisplayedLines;
        // Use RichTextBox API to remove lines
    }
}
```

## Timeline

| Phase | Task | Days | Status |
|-------|------|------|--------|
| 2.1 | MVVM Infrastructure (ViewModelBase, RelayCommand) | 2 | 🔵 Ready |
| 2.2 | RepositoryManager async refactoring | 2 | 🔵 Ready |
| 2.3 | MainWindowViewModel creation | 2 | 🔵 Ready |
| 2.4 | XAML binding updates | 2 | 🔵 Ready |
| 2.5 | Performance optimization | 2 | 🔵 Ready |
| 2.6 | Testing & validation | 2 | 🔵 Ready |
| **Total** | | **~10 days** | - |

## Testing Strategy

### Unit Tests to Add
- `ViewModelTests.cs` - Test PropertyChanged notifications
- `CommandTests.cs` - Test RelayCommand execution
- `RepositoryManagerAsyncTests.cs` - Test async operations and caching
- `MainWindowViewModelTests.cs` - Test ViewModel logic

### Integration Tests
- Test UI responsiveness during Git operations
- Test cancellation scenarios
- Test cache invalidation

## Rollout Plan

### Step 1: Infrastructure
- Create ViewModelBase and RelayCommand
- Create unit tests for these

### Step 2: RepositoryManager
- Make async with caching
- Add cancellation support
- Test thoroughly

### Step 3: ViewModel Creation
- Create MainWindowViewModel
- Migrate logic from code-behind
- Ensure backward compatibility

### Step 4: UI Migration
- Update MainWindow.xaml bindings
- Remove code-behind logic gradually
- Keep feature parity

### Step 5: Optimization
- Implement caching
- Optimize rendering
- Profile and measure

## Risks & Mitigations

| Risk | Mitigation |
|------|-----------|
| Breaking UI during refactor | Keep tests; ensure compatibility layer |
| Performance regression | Profile before/after; use virtual scrolling |
| Async deadlocks | Use ConfigureAwait(false); test cancellation |
| Cache stale data | Implement proper invalidation; short TTL |

## Success Criteria

- ✅ All UI code moved from code-behind to ViewModel
- ✅ All Git operations async with cancellation
- ✅ Branch list cached with 5-minute TTL
- ✅ Log rendering optimized (< 5% CPU for display)
- ✅ No breaking changes to end-user functionality
- ✅ Test coverage maintained at 100%
- ✅ UI remains responsive during all operations

## Estimated Effort

- **Total Time**: ~10 development days
- **Team Size**: 1 developer (optimal)
- **Complexity**: Medium (data binding, async patterns)
- **Risk Level**: Low (incremental approach with fallbacks)

---

**Next**: Start with Phase 2.1 - MVVM Infrastructure
