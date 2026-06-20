using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Input;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// ViewModel for the MainWindow, coordinating repository operations and UI state.
    /// </summary>
    /// <remarks>
    /// This ViewModel handles:
    /// - Repository management and selection
    /// - Branch list loading and caching
    /// - Merge and pull operations with progress tracking
    /// - UI state management (progress, status messages)
    /// - Command implementation for all user actions
    /// 
    /// Separation of concerns:
    /// - Repository logic: RepositoryService
    /// - Settings persistence: AppSettings
    /// - Git operations: GitHelper via RepositoryService
    /// </remarks>
    public class MainWindowViewModel : ViewModelBase
    {
        private readonly RepositoryService _repositoryService;
        private AppSettings _settings;
        private CancellationTokenSource? _operationCancellation;
        private MainWindowWorkflowActions _workflowActions = MainWindowWorkflowActions.Empty;

        // Observable collections for UI binding
        private ObservableCollection<AppSettings.RepositoryEntry> _repositories;
        private ObservableCollection<RepositorySelectionItem> _selectableRepositories;
        private ObservableCollection<BranchItem> _sourceBranches;
        private ObservableCollection<BranchItem> _targetBranches;

        // UI state properties
        private AppSettings.RepositoryEntry? _selectedRepository;
        private BranchItem? _selectedSourceBranch;
        private BranchItem? _selectedTargetBranch;
        private int _progressPercentage;
        private string _statusMessage = "Ready";
        private bool _isOperationInProgress;
        private string _inlineRepositoryName = string.Empty;
        private string _inlineRepositoryPath = string.Empty;
        private bool _isInlineRepositoryEditing;
        private bool _canRunMergeWorkflow;
        private bool _canRunPullWorkflow;

        // Log display properties
        private string _outputLog = "";
        private string _errorLog = "";
        private bool _autoOpenLogs;
        private bool _inlineLogsVisible;

        #region Properties

        /// <summary>
        /// Gets the collection of configured repositories.
        /// </summary>
        public ObservableCollection<AppSettings.RepositoryEntry> Repositories
        {
            get => _repositories;
            private set => SetProperty(ref _repositories, value);
        }

        /// <summary>
        /// Gets the bindable repository checkbox items.
        /// </summary>
        public ObservableCollection<RepositorySelectionItem> SelectableRepositories
        {
            get => _selectableRepositories;
            private set => SetProperty(ref _selectableRepositories, value);
        }

        /// <summary>
        /// Gets the collection of source branches available for merging.
        /// </summary>
        public ObservableCollection<BranchItem> SourceBranches
        {
            get => _sourceBranches;
            private set => SetProperty(ref _sourceBranches, value);
        }

        /// <summary>
        /// Gets the collection of target branches to merge into.
        /// </summary>
        public ObservableCollection<BranchItem> TargetBranches
        {
            get => _targetBranches;
            private set => SetProperty(ref _targetBranches, value);
        }

        /// <summary>
        /// Gets or sets the currently selected repository.
        /// </summary>
        public AppSettings.RepositoryEntry? SelectedRepository
        {
            get => _selectedRepository;
            set
            {
                if (SetProperty(ref _selectedRepository, value))
                {
                    OnPropertyChanged(nameof(CanMerge));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected source branch for merge operations.
        /// </summary>
        public BranchItem? SelectedSourceBranch
        {
            get => _selectedSourceBranch;
            set
            {
                if (SetProperty(ref _selectedSourceBranch, value))
                {
                    OnPropertyChanged(nameof(CanMerge));
                }
            }
        }

        /// <summary>
        /// Gets or sets the selected target branch for merge operations.
        /// </summary>
        public BranchItem? SelectedTargetBranch
        {
            get => _selectedTargetBranch;
            set
            {
                if (SetProperty(ref _selectedTargetBranch, value))
                {
                    OnPropertyChanged(nameof(CanMerge));
                }
            }
        }

        /// <summary>
        /// Gets the current operation progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage
        {
            get => _progressPercentage;
            private set => SetProperty(ref _progressPercentage, value);
        }

        /// <summary>
        /// Gets the current status message displayed to the user.
        /// </summary>
        public string StatusMessage
        {
            get => _statusMessage;
            private set => SetProperty(ref _statusMessage, value);
        }

        /// <summary>
        /// Gets a value indicating whether an operation is currently in progress.
        /// </summary>
        public bool IsOperationInProgress
        {
            get => _isOperationInProgress;
            private set
            {
                if (SetProperty(ref _isOperationInProgress, value))
                {
                    OnPropertyChanged(nameof(CanMerge));
                }
            }
        }

        /// <summary>
        /// Gets or sets the inline repository editor name.
        /// </summary>
        public string InlineRepositoryName
        {
            get => _inlineRepositoryName;
            set => SetProperty(ref _inlineRepositoryName, value ?? string.Empty);
        }

        /// <summary>
        /// Gets or sets the inline repository editor path.
        /// </summary>
        public string InlineRepositoryPath
        {
            get => _inlineRepositoryPath;
            set => SetProperty(ref _inlineRepositoryPath, value ?? string.Empty);
        }

        /// <summary>
        /// Gets whether the inline repository editor is editing an existing repository.
        /// </summary>
        public bool IsInlineRepositoryEditing
        {
            get => _isInlineRepositoryEditing;
            private set
            {
                if (SetProperty(ref _isInlineRepositoryEditing, value))
                {
                    OnPropertyChanged(nameof(CanAddInlineRepository));
                    OnPropertyChanged(nameof(CanUpdateInlineRepository));
                }
            }
        }

        /// <summary>
        /// Gets whether the inline editor can add a new repository.
        /// </summary>
        public bool CanAddInlineRepository => !IsInlineRepositoryEditing;

        /// <summary>
        /// Gets whether the inline editor can update the selected repository.
        /// </summary>
        public bool CanUpdateInlineRepository => IsInlineRepositoryEditing;

        /// <summary>
        /// Gets whether the current multi-repository merge workflow can run.
        /// </summary>
        public bool CanRunMergeWorkflow
        {
            get => _canRunMergeWorkflow;
            private set => SetProperty(ref _canRunMergeWorkflow, value);
        }

        /// <summary>
        /// Gets whether the current multi-repository pull workflow can run.
        /// </summary>
        public bool CanRunPullWorkflow
        {
            get => _canRunPullWorkflow;
            private set => SetProperty(ref _canRunPullWorkflow, value);
        }

        /// <summary>
        /// Gets or sets the output log content.
        /// </summary>
        public string OutputLog
        {
            get => _outputLog;
            set => SetProperty(ref _outputLog, value);
        }

        /// <summary>
        /// Gets or sets the error log content.
        /// </summary>
        public string ErrorLog
        {
            get => _errorLog;
            set => SetProperty(ref _errorLog, value);
        }

        /// <summary>
        /// Gets or sets whether logs should auto-open when operations complete.
        /// </summary>
        public bool AutoOpenLogs
        {
            get => _autoOpenLogs;
            set => SetProperty(ref _autoOpenLogs, value);
        }

        /// <summary>
        /// Gets or sets whether inline logs are visible in the UI.
        /// </summary>
        public bool InlineLogsVisible
        {
            get => _inlineLogsVisible;
            set => SetProperty(ref _inlineLogsVisible, value);
        }

        /// <summary>
        /// Gets a value indicating whether a merge operation can be performed.
        /// </summary>
        public bool CanMerge
        {
            get => SelectedRepository != null &&
                   SelectedSourceBranch != null &&
                   SelectedTargetBranch != null &&
                   SelectedSourceBranch.FullName != SelectedTargetBranch.FullName &&
                   !IsOperationInProgress;
        }

        #endregion

        #region Commands

        /// <summary>
        /// Gets the command to refresh branch lists from the selected repository.
        /// </summary>
        public ICommand RefreshBranchesCommand { get; }

        /// <summary>
        /// Gets the command to perform a merge operation.
        /// </summary>
        public ICommand MergeCommand { get; }

        /// <summary>
        /// Gets the command to pull the selected branch.
        /// </summary>
        public ICommand PullBranchCommand { get; }

        /// <summary>
        /// Gets the command to cancel the current operation.
        /// </summary>
        public ICommand CancelOperationCommand { get; }

        /// <summary>
        /// Gets the command to open the repository management dialog.
        /// </summary>
        public ICommand ManageRepositoriesCommand { get; }

        /// <summary>
        /// Gets the command to toggle log visibility.
        /// </summary>
        public ICommand ToggleLogsCommand { get; }

        /// <summary>
        /// Gets the production workflow command for the current multi-branch merge path.
        /// </summary>
        public ICommand MergeWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for the current multi-repository pull path.
        /// </summary>
        public ICommand PullWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for refreshing the current UI selections.
        /// </summary>
        public ICommand RefreshWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for opening repository management.
        /// </summary>
        public ICommand ManageRepositoriesWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for toggling logs.
        /// </summary>
        public ICommand ToggleLogsWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for browsing repository paths.
        /// </summary>
        public ICommand BrowseRepositoryPathWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for adding an inline repository.
        /// </summary>
        public ICommand AddRepositoryWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for editing an inline repository.
        /// </summary>
        public ICommand EditRepositoryWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for updating an inline repository.
        /// </summary>
        public ICommand UpdateRepositoryWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for removing an inline repository.
        /// </summary>
        public ICommand RemoveRepositoryWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for saving logs.
        /// </summary>
        public ICommand SaveLogsWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for copying logs.
        /// </summary>
        public ICommand CopyLogsWorkflowCommand { get; }

        /// <summary>
        /// Gets the production workflow command for clearing logs.
        /// </summary>
        public ICommand ClearLogsWorkflowCommand { get; }

        #endregion

        /// <summary>
        /// Initializes a new instance of MainWindowViewModel.
        /// </summary>
        public MainWindowViewModel()
        {
            _repositoryService = new RepositoryService();
            _settings = AppSettings.LoadWithFallback();

            // Initialize collections
            _repositories = new ObservableCollection<AppSettings.RepositoryEntry>(_settings.Repositories);
            _selectableRepositories = new ObservableCollection<RepositorySelectionItem>(
                _settings.Repositories.Select(repository => new RepositorySelectionItem(repository)));
            _sourceBranches = new ObservableCollection<BranchItem>();
            _targetBranches = new ObservableCollection<BranchItem>();

            // Initialize properties
            _autoOpenLogs = _settings.AutoOpenLogs;
            _inlineLogsVisible = _settings.InlineLogsVisible;
            _statusMessage = "Ready";

            // Wire up progress events
            _repositoryService.ProgressChanged += (s, e) =>
            {
                ProgressPercentage = e.ProgressPercentage;
                StatusMessage = e.StatusMessage;
            };

            // Initialize commands
            RefreshBranchesCommand = new AsyncRelayCommand(
                execute: _ => RefreshBranchesAsync(),
                canExecute: _ => SelectedRepository != null && !IsOperationInProgress
            );

            MergeCommand = new AsyncRelayCommand(
                execute: _ => MergeBranchAsync(),
                canExecute: _ => CanMerge
            );

            PullBranchCommand = new AsyncRelayCommand(
                execute: _ => PullBranchAsync(),
                canExecute: _ => SelectedRepository != null && SelectedSourceBranch != null && !IsOperationInProgress
            );

            CancelOperationCommand = new RelayCommand(
                execute: _ => CancelOperation(),
                canExecute: _ => IsOperationInProgress
            );

            ManageRepositoriesCommand = new RelayCommand(
                execute: _ => _workflowActions.ManageRepositories()
            );

            ToggleLogsCommand = new RelayCommand(
                execute: _ => InlineLogsVisible = !InlineLogsVisible
            );

            MergeWorkflowCommand = new RelayCommand(
                _ => _workflowActions.Merge(),
                _ => CanRunMergeWorkflow);
            PullWorkflowCommand = new RelayCommand(
                _ => _workflowActions.Pull(),
                _ => CanRunPullWorkflow);
            RefreshWorkflowCommand = new RelayCommand(_ => _workflowActions.Refresh());
            ManageRepositoriesWorkflowCommand = new RelayCommand(_ => _workflowActions.ManageRepositories());
            ToggleLogsWorkflowCommand = new RelayCommand(_ => _workflowActions.ToggleLogs());
            BrowseRepositoryPathWorkflowCommand = new RelayCommand(_ => _workflowActions.BrowseRepositoryPath());
            AddRepositoryWorkflowCommand = new RelayCommand(_ => _workflowActions.AddRepository());
            EditRepositoryWorkflowCommand = new RelayCommand(_ => _workflowActions.EditRepository());
            UpdateRepositoryWorkflowCommand = new RelayCommand(_ => _workflowActions.UpdateRepository());
            RemoveRepositoryWorkflowCommand = new RelayCommand(_ => _workflowActions.RemoveRepository());
            SaveLogsWorkflowCommand = new RelayCommand(_ => _workflowActions.SaveLogs());
            CopyLogsWorkflowCommand = new RelayCommand(_ => _workflowActions.CopyLogs());
            ClearLogsWorkflowCommand = new RelayCommand(_ => _workflowActions.ClearLogs());
        }

        /// <summary>
        /// Configures the production workflows invoked by command bindings.
        /// </summary>
        public void ConfigureWorkflowActions(MainWindowWorkflowActions workflowActions)
        {
            _workflowActions = workflowActions ?? MainWindowWorkflowActions.Empty;
        }

        /// <summary>
        /// Loads and applies settings from AppSettings.
        /// Called from MainWindow code-behind during initialization.
        /// </summary>
        public void LoadSettings()
        {
            _settings = AppSettings.LoadWithFallback();

            // Update UI state from settings
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                ReplaceRepositories(_settings.Repositories);

                AutoOpenLogs = _settings.AutoOpenLogs;
                InlineLogsVisible = _settings.InlineLogsVisible;

                // Select first repository if available
                if (Repositories.Count > 0)
                {
                    SelectedRepository = Repositories[0];
                }
            });
        }

        /// <summary>
        /// Replaces the bound branch dropdown collections.
        /// </summary>
        public void ReplaceBranchDropdownItems(
            IEnumerable<BranchItem>? sourceItems,
            IEnumerable<BranchItem>? targetItems)
        {
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                ReplaceItems(SourceBranches, sourceItems);
                ReplaceItems(TargetBranches, targetItems);
            });
        }

        /// <summary>
        /// Replaces the bound repository list collection.
        /// </summary>
        public void ReplaceRepositories(IEnumerable<AppSettings.RepositoryEntry>? repositories)
        {
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                Repositories.Clear();
                SelectableRepositories.Clear();
                foreach (var repository in repositories ?? Enumerable.Empty<AppSettings.RepositoryEntry>())
                {
                    Repositories.Add(repository);
                    SelectableRepositories.Add(new RepositorySelectionItem(repository));
                }
            });
        }

        /// <summary>
        /// Updates the checked state for a repository checkbox item by path.
        /// </summary>
        public void SetRepositorySelected(string? repositoryPath, bool isSelected)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
                return;

            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                var item = SelectableRepositories.FirstOrDefault(repository =>
                    string.Equals(repository.Path, repositoryPath, StringComparison.OrdinalIgnoreCase));
                if (item != null)
                    item.IsSelected = isSelected;
            });
        }

        /// <summary>
        /// Clears all repository checkbox selections.
        /// </summary>
        public void ClearRepositorySelections()
        {
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                foreach (var repository in SelectableRepositories)
                {
                    repository.IsSelected = false;
                }
            });
        }

        /// <summary>
        /// Updates the inline repository path from the folder picker workflow.
        /// </summary>
        public void SetInlineRepositoryPath(string? repositoryPath)
        {
            InlineRepositoryPath = repositoryPath ?? string.Empty;
        }

        /// <summary>
        /// Populates the inline editor with an existing repository.
        /// </summary>
        public void BeginInlineRepositoryEdit(AppSettings.RepositoryEntry? repository)
        {
            InlineRepositoryName = repository?.Name ?? string.Empty;
            InlineRepositoryPath = repository?.Path ?? string.Empty;
            IsInlineRepositoryEditing = true;
        }

        /// <summary>
        /// Clears the inline repository editor and returns it to add mode.
        /// </summary>
        public void ClearInlineRepositoryEditor()
        {
            InlineRepositoryName = string.Empty;
            InlineRepositoryPath = string.Empty;
            IsInlineRepositoryEditing = false;
        }

        /// <summary>
        /// Updates command availability for the current production merge/pull workflows.
        /// </summary>
        public void UpdateWorkflowAvailability(bool canRunMergeWorkflow, bool canRunPullWorkflow)
        {
            var changed = false;
            changed |= SetWorkflowAvailability(ref _canRunMergeWorkflow, canRunMergeWorkflow, nameof(CanRunMergeWorkflow));
            changed |= SetWorkflowAvailability(ref _canRunPullWorkflow, canRunPullWorkflow, nameof(CanRunPullWorkflow));

            if (changed)
                CommandManager.InvalidateRequerySuggested();
        }

        /// <summary>
        /// Adds a target dropdown item when a UI marker is needed for a missing remote branch.
        /// </summary>
        public void AddTargetBranchDropdownItemIfMissing(BranchItem item)
        {
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                if (!TargetBranches.Any(existing =>
                        string.Equals(existing.FullName, item.FullName, StringComparison.OrdinalIgnoreCase)))
                {
                    TargetBranches.Add(item);
                }
            });
        }

        private static void ReplaceItems(
            ObservableCollection<BranchItem> collection,
            IEnumerable<BranchItem>? items)
        {
            collection.Clear();
            foreach (var item in items ?? Enumerable.Empty<BranchItem>())
            {
                collection.Add(item);
            }
        }

        private bool SetWorkflowAvailability(ref bool field, bool value, string propertyName)
        {
            if (field == value)
                return false;

            field = value;
            OnPropertyChanged(propertyName);
            return true;
        }

        /// <summary>
        /// Refreshes the branch lists from the selected repository.
        /// </summary>
        private async Task RefreshBranchesAsync()
        {
            if (SelectedRepository == null)
            {
                StatusMessage = "Please select a repository first";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedRepository.Path))
            {
                StatusMessage = "Selected repository path is missing";
                return;
            }

            IsOperationInProgress = true;
            ProgressPercentage = 0;

            try
            {
                var cancellationToken = GetCancellationToken();

                // Fetch branches from service (uses cache if valid)
                var branches = await _repositoryService.GetBranchesAsync(
                    SelectedRepository.Path,
                    forceRefresh: false,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                // Update UI collections on dispatcher
                DispatcherHelper.InvokeOnDispatcher(() =>
                {
                    SourceBranches.Clear();
                    TargetBranches.Clear();

                    foreach (var branch in branches)
                    {
                        SourceBranches.Add(branch);
                        TargetBranches.Add(branch);
                    }

                    StatusMessage = $"Loaded {branches.Count()} branches";
                    ProgressPercentage = 100;
                });
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Operation canceled";
                ProgressPercentage = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Failed to load branches: {ex.Message}";
                ProgressPercentage = 0;
            }
            finally
            {
                IsOperationInProgress = false;
            }
        }

        /// <summary>
        /// Performs a merge operation from source to target branch.
        /// </summary>
        private async Task MergeBranchAsync()
        {
            if (!CanMerge)
            {
                StatusMessage = "Invalid merge configuration";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedRepository?.Path))
            {
                StatusMessage = "Selected repository path is missing";
                return;
            }

            IsOperationInProgress = true;
            ProgressPercentage = 0;

            try
            {
                var cancellationToken = GetCancellationToken();

                var result = await _repositoryService.MergeBranchAsync(
                    SelectedRepository.Path,
                    SelectedSourceBranch!.FullName,
                    SelectedTargetBranch!.FullName,
                    cancellationToken
                ).ConfigureAwait(false);

                StatusMessage = result.Message;

                if (result.Status == MergeStatus.Success)
                {
                    ProgressPercentage = 100;
                    // Log the success
                    AppendOutputLog($"Merge completed: {result.Message}");

                    // Refresh branches to show updated state
                    await RefreshBranchesAsync().ConfigureAwait(false);
                }
                else if (result.Status == MergeStatus.Conflict)
                {
                    AppendErrorLog($"Merge conflict: {result.Message}");
                }
                else
                {
                    AppendErrorLog($"Merge failed: {result.Message}");
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Merge canceled";
                ProgressPercentage = 0;
            }
            catch (Exception ex)
            {
                StatusMessage = $"Merge failed: {ex.Message}";
                AppendErrorLog($"Error: {ex.Message}");
                ProgressPercentage = 0;
            }
            finally
            {
                IsOperationInProgress = false;
            }
        }

        /// <summary>
        /// Pulls the latest changes for the selected branch.
        /// </summary>
        private async Task PullBranchAsync()
        {
            if (SelectedRepository == null || SelectedSourceBranch == null)
            {
                StatusMessage = "Please select a repository and branch";
                return;
            }

            if (string.IsNullOrWhiteSpace(SelectedRepository.Path))
            {
                StatusMessage = "Selected repository path is missing";
                return;
            }

            IsOperationInProgress = true;
            ProgressPercentage = 0;

            try
            {
                var cancellationToken = GetCancellationToken();

                var result = await _repositoryService.PullBranchAsync(
                    SelectedRepository.Path,
                    SelectedSourceBranch.FullName,
                    cancellationToken
                ).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    StatusMessage = $"Successfully pulled {SelectedSourceBranch.Name}";
                    ProgressPercentage = 100;
                    AppendOutputLog(StatusMessage);
                    await RefreshBranchesAsync().ConfigureAwait(false);
                }
                else
                {
                    StatusMessage = $"Pull failed: {result.StdErr}";
                    AppendErrorLog(result.StdErr);
                }
            }
            catch (OperationCanceledException)
            {
                StatusMessage = "Pull canceled";
            }
            catch (Exception ex)
            {
                StatusMessage = $"Pull failed: {ex.Message}";
                AppendErrorLog($"Error: {ex.Message}");
            }
            finally
            {
                IsOperationInProgress = false;
            }
        }

        /// <summary>
        /// Cancels the current operation.
        /// </summary>
        private void CancelOperation()
        {
            _operationCancellation?.Cancel();
            StatusMessage = "Operation cancellation requested...";
        }

        /// <summary>
        /// Gets or creates a cancellation token source for the current operation.
        /// </summary>
        private CancellationToken GetCancellationToken()
        {
            _operationCancellation = new CancellationTokenSource();
            return _operationCancellation.Token;
        }

        /// <summary>
        /// Appends text to the output log.
        /// </summary>
        private void AppendOutputLog(string message)
        {
            OutputLog += DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine;
        }

        /// <summary>
        /// Appends text to the error log.
        /// </summary>
        private void AppendErrorLog(string message)
        {
            ErrorLog += DateTime.Now.ToString("HH:mm:ss") + " " + message + Environment.NewLine;
        }

        /// <summary>
        /// Saves current settings to persistent storage.
        /// </summary>
        public void SaveSettings()
        {
            _settings.AutoOpenLogs = AutoOpenLogs;
            _settings.InlineLogsVisible = InlineLogsVisible;
            _settings.Repositories.Clear();
            _settings.Repositories.AddRange(Repositories);
            _settings.Save();
        }
    }

    /// <summary>
    /// Helper for dispatcher operations (running on UI thread).
    /// </summary>
    internal static class DispatcherHelper
    {
        /// <summary>
        /// Invokes an action on the UI dispatcher if needed.
        /// </summary>
        public static void InvokeOnDispatcher(Action action)
        {
            var dispatcher = System.Windows.Application.Current?.Dispatcher;
            if (dispatcher == null || dispatcher.CheckAccess())
                action();
            else
                dispatcher.Invoke(action);
        }
    }
}
