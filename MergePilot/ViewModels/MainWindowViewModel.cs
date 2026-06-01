using System;
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

        // Observable collections for UI binding
        private ObservableCollection<AppSettings.RepositoryEntry> _repositories;
        private ObservableCollection<BranchItem> _sourceBranches;
        private ObservableCollection<BranchItem> _targetBranches;

        // UI state properties
        private AppSettings.RepositoryEntry? _selectedRepository;
        private BranchItem? _selectedSourceBranch;
        private BranchItem? _selectedTargetBranch;
        private int _progressPercentage;
        private string _statusMessage = "Ready";
        private bool _isOperationInProgress;

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

        #endregion

        /// <summary>
        /// Initializes a new instance of MainWindowViewModel.
        /// </summary>
        public MainWindowViewModel()
        {
            _repositoryService = new RepositoryService();
            _settings = AppSettings.Load();

            // Initialize collections
            _repositories = new ObservableCollection<AppSettings.RepositoryEntry>(_settings.Repositories);
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
                execute: _ => OpenRepositoryManager()
            );

            ToggleLogsCommand = new RelayCommand(
                execute: _ => InlineLogsVisible = !InlineLogsVisible
            );
        }

        /// <summary>
        /// Loads and applies settings from AppSettings.
        /// Called from MainWindow code-behind during initialization.
        /// </summary>
        public void LoadSettings()
        {
            _settings = AppSettings.Load();

            // Update UI state from settings
            DispatcherHelper.InvokeOnDispatcher(() =>
            {
                Repositories.Clear();
                foreach (var repo in _settings.Repositories)
                {
                    Repositories.Add(repo);
                }

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
        /// Refreshes the branch lists from the selected repository.
        /// </summary>
        private async Task RefreshBranchesAsync()
        {
            if (SelectedRepository == null)
            {
                StatusMessage = "Please select a repository first";
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

            IsOperationInProgress = true;
            ProgressPercentage = 0;

            try
            {
                var cancellationToken = GetCancellationToken();

                var result = await _repositoryService.MergeBranchAsync(
                    SelectedRepository!.Path,
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
        /// Opens the repository management dialog.
        /// </summary>
        private void OpenRepositoryManager()
        {
            // This will be implemented when RepositoryManager UI is migrated to MVVM
            StatusMessage = "Repository manager will open here";
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
            if (dispatcher?.CheckAccess() == true)
                action();
            else
                dispatcher?.Invoke(action);
        }
    }
}
