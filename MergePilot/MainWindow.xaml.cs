using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using System.Windows.Input;
using MergePilot.ViewModels;

namespace MergePilot
{
    // VIEW BOUNDARY — this file owns only WPF event handlers and timer callbacks.
    // Soft ceiling: ~800 lines. If it grows beyond that, the new logic belongs in a service.
    // Current count: ~760 lines (June 2026). Business logic lives in the ViewModels/ and *Service.cs files.
    public partial class MainWindow : MetroWindow
    {
        // Separators
        private const string SectionSeparator = "===============================================";

        private readonly DispatcherTimer _logFlushTimer;
        private AppSettings _settings;
        private readonly RepositoryConfigurationService _repositoryConfigurationService = new();
        private readonly BranchCatalogService _branchCatalogService = new();
        private readonly BranchCatalogCoordinator _branchCatalogCoordinator;
        private readonly BranchDropdownProjectionService _branchDropdownProjectionService = new();
        private readonly BatchOperationWorkflowRunner _batchOperationWorkflowRunner = new();
        private readonly LogBufferService _logBufferService = new();
        private readonly LogExportService _logExportService = new();
        private readonly LogClipboardService _logClipboardService = new();
        private readonly IClipboardService _clipboard;
        private readonly WorkflowAvailabilityService _workflowAvailabilityService = new();
        private readonly MainWindowViewModel? _viewModel;
        private readonly IUserDialogService _dialogs;
        private readonly IFilePickerService _filePickers;
        private readonly RefreshWorkflowService _refreshWorkflowService = new();
        private readonly RefreshWorkflowVisualState _refreshWorkflowVisualState = new();
        private readonly RepositoryListVisualState _repositoryListVisualState = new();
        private readonly RepositoryManagerWorkflowService _repositoryManagerWorkflowService = new();
        private readonly MainWindowShortcutService _shortcutService = new();
        private readonly MainWindowClosingService _closingService = new();
        private readonly MainWindowEventHookService _eventHookService = new();
        private readonly MainWindowTimerService _timerService = new();
        private readonly BatchOperationCallbackFactory _batchCallbackFactory;
        private readonly InlineRepositoryEditorWorkflowService _inlineRepositoryEditorWorkflowService;
        private RichTextBoxLogRenderer? _logRenderer;

        private readonly BranchSelectionState _branchSelectionState = new();
        private readonly RepositorySelectionState _repositorySelectionState = new();
        private readonly BranchDropdownSelectionCoordinator _branchDropdownSelectionCoordinator;
        private readonly BranchDropdownVisualState _branchDropdownVisualState = new();
        private readonly RepositorySelectionCoordinator _repositorySelectionCoordinator;

        // Prevent re-entrant checkbox events
        private bool _isUpdatingCheckBoxes = false;
        private bool _isUpdatingRepoCBs = false;

        private DispatcherTimer? _smoothScrollTimer;

        public MainWindow(
            IUserDialogService? dialogs = null,
            IFilePickerService? filePickers = null,
            IClipboardService? clipboard = null)
        {
            _dialogs = dialogs ?? new WpfUserDialogService();
            _filePickers = filePickers ?? new WpfFilePickerService();
            _clipboard = clipboard ?? new WpfClipboardService();

            InitializeComponent();

            _branchDropdownSelectionCoordinator = new BranchDropdownSelectionCoordinator(_branchSelectionState);
            _repositorySelectionCoordinator = new RepositorySelectionCoordinator(_repositorySelectionState);
            _inlineRepositoryEditorWorkflowService = new InlineRepositoryEditorWorkflowService(
                new InlineRepositoryEditorService(_repositoryConfigurationService),
                _dialogs);
            _branchCatalogCoordinator = new BranchCatalogCoordinator(_branchCatalogService);

            // The DataContext is set to MainWindowViewModel in XAML
            // Store reference to ViewModel for logging integration
            _viewModel = this.DataContext as MainWindowViewModel;
            _batchCallbackFactory = new BatchOperationCallbackFactory(
                InvokeOnUiThread,
                AppendOutput,
                AppendError,
                AddBranchToRecentAndUi,
                new MergeInteractionService(
                    _dialogs,
                    new ExplorerRepositoryFolderOpener(),
                    AppendError));
            ConfigureWorkflowCommands();

            // initial UI state and timers
            ValidateSelections();
            _logFlushTimer = _timerService.StartTimer(
                TimeSpan.FromMilliseconds(100),
                (_, _) => FlushLogQueues());

            // Initialize smooth scroll animation timer
            _smoothScrollTimer = _timerService.StartTimer(
                TimeSpan.FromMilliseconds(16),
                (_, _) => _logRenderer?.UpdateSmoothScroll());

            // Optimize RichTextBox rendering
            if (OutputBox != null)
            {
                OutputBox.IsEnabled = true;
                _logRenderer = new RichTextBoxLogRenderer(OutputBox);
            }

            RegisterActionShortcuts();
            _eventHookService.HookPreviewKeyDown(this, MainWindow_PreviewKeyDown);

            // register log shortcuts (save/copy/clear)
            RegisterLogShortcuts();

            // load settings and apply
            _settings = AppSettings.LoadWithFallback();
            _logBufferService.MaxChars = _settings.LogMaxChars > 0 ? _settings.LogMaxChars : LogBufferService.DefaultMaxChars;
            if (_settings.StreamLogs && !string.IsNullOrWhiteSpace(_settings.LogFilePath))
                StartStreamLogs(_settings.LogFilePath);
            PopulateRepositoriesFromSettings();

            // Initialize ViewModel with settings
            if (_viewModel != null)
            {
                _viewModel.LoadSettings();
            }

            // hook branch combobox dropdown events
            _eventHookService.HookBranchDropdowns(
                SourceBranchBox,
                TargetBranchBox,
                BranchComboBox_DropDownOpened);

            // restore inline log visibility and appearance
            try
            {
                if (_settings.LogFontSize > 0 && OutputBox != null)
                {
                    OutputBox.FontSize = _settings.LogFontSize;
                }
            }
            catch { }
        }

        private void MainWindow_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            try
            {
                e.Handled = _shortcutService.HandlePreviewKeyDown(
                    e.Key,
                    e.SystemKey,
                    Keyboard.Modifiers,
                    ToggleLogs);
            }
            catch { }
        }

        private void LstRepos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                // ListView holds AppSettings.RepositoryEntry objects as items
                var listSelection = _repositoryListVisualState.GetSelection(lstRepos);
                if (listSelection.Repository is AppSettings.RepositoryEntry repo && !string.IsNullOrWhiteSpace(repo.Path))
                {
                    _viewModel?.SetRepositorySelected(repo.Path, true);
                    var change = _repositorySelectionCoordinator.ApplySelection(
                        repositoryItem: null,
                        fallbackPath: repo.Path,
                        isSelected: true);

                    if (change.ShouldLoadBranches)
                        _ = LoadBranchesForRepoAsync(change.RepositoryPath);

                    _repositoryListVisualState.ScrollIntoView(lstRepos, repo);
                }
            }
            catch { }
        }

        private void BrowseRepoPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var selectedPath = _filePickers.SelectFolder(
                    "Select repository folder (should contain .git)",
                    useDescriptionForTitle: true);
                if (!string.IsNullOrWhiteSpace(selectedPath))
                {
                    _viewModel?.SetInlineRepositoryPath(selectedPath);
                }
            }
            catch (Exception ex)
            {
                AppendError($"Folder picker failed: {ex.Message}");
            }
        }

        private void PopulateRepositoriesFromSettings()
        {
            try
            {
                _repositorySelectionState.Clear();
                _viewModel?.ReplaceRepositories(_settings.Repositories);

                _branchCatalogCoordinator.LoadInitialCatalog(_settings, _branchSelectionState);

                RefreshBranchLists();

                // Ensure ListBox items are selectable visually (use default selection brush)
                try
                {
                    _repositoryListVisualState.ConfigureSingleSelection(lstRepos, LstRepos_SelectionChanged);
                }
                catch { }
            }
            catch { }
        }
        private void ManageRepos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _settings = _repositoryManagerWorkflowService.ShowAndReload(_settings, this);
                PopulateRepositoriesFromSettings();
            }
            catch (Exception ex)
            {
                AppendError($"Failed to open repository manager: {ex.Message}");
            }
        }

        private void EditRepo_Click(object sender, RoutedEventArgs e)
        {
            var listSelection = _repositoryListVisualState.GetSelection(lstRepos);

            var result = _inlineRepositoryEditorWorkflowService.BeginEdit(
                _settings,
                listSelection.Index,
                listSelection.Repository);

            if (result.ShouldEdit)
                _viewModel?.BeginInlineRepositoryEdit(result.Repository);
        }

        private void UpdateRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var listSelection = _repositoryListVisualState.GetSelection(lstRepos);
                var result = _inlineRepositoryEditorWorkflowService.UpdateRepository(
                    _settings,
                    listSelection.Index,
                    listSelection.Repository,
                    _viewModel?.InlineRepositoryName,
                    _viewModel?.InlineRepositoryPath);
                if (!result.ShouldRefresh)
                    return;

                PopulateRepositoriesFromSettings();
                _repositoryListVisualState.SelectIndexIfAvailable(lstRepos, result.Index);
                _viewModel?.ClearInlineRepositoryEditor();
            }
            catch (Exception ex)
            {
                AppendError($"Failed to update repository: {ex.Message}");
            }
        }

        private void AddRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var result = _inlineRepositoryEditorWorkflowService.AddRepository(
                    _settings,
                    _viewModel?.InlineRepositoryName,
                    _viewModel?.InlineRepositoryPath);
                if (!result.ShouldRefresh)
                    return;

                PopulateRepositoriesFromSettings();
                _viewModel?.ClearInlineRepositoryEditor();
            }
            catch (Exception ex)
            {
                AppendError($"Failed to add repository: {ex.Message}");
            }
        }

        private void RemoveRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var listSelection = _repositoryListVisualState.GetSelection(lstRepos);
                var result = _inlineRepositoryEditorWorkflowService.RemoveRepository(
                    _settings,
                    listSelection.Index,
                    listSelection.Repository);
                if (!result.ShouldRefresh)
                    return;

                PopulateRepositoriesFromSettings();
            }
            catch (Exception ex)
            {
                AppendError($"Failed to remove repository: {ex.Message}");
            }
        }

        private async void Merge_Click(object sender, RoutedEventArgs e)
        {
            using var cts = new CancellationTokenSource();
            using var progress = ButtonProgressScope.Start(btnMerge);

            var sources = _branchSelectionState.GetSelectedBranches(BranchSelectionRole.Source);
            var targets = _branchSelectionState.GetSelectedBranches(BranchSelectionRole.Target);
            var repoPaths = GetSelectedRepositories();
            var callbacks = _batchCallbackFactory.CreateMergeCallbacks();

            await _batchOperationWorkflowRunner.RunMergeAsync(
                _settings,
                repoPaths,
                sources,
                targets,
                callbacks,
                cts.Token);
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            if (_isUpdatingRepoCBs) return;
            try
            {
                if (sender is not CheckBox cb)
                    return;

                var isSelected = cb.IsChecked == true;
                var currentPath = (cb.DataContext as RepositorySelectionItem)?.Path ?? cb.Tag as string ?? string.Empty;

                // Enforce single-select: when a repo is checked, uncheck all others and reset branch state
                if (isSelected && _viewModel != null)
                {
                    try
                    {
                        _isUpdatingRepoCBs = true;
                        foreach (var item in _viewModel.SelectableRepositories)
                        {
                            if (!string.Equals(item.Path, currentPath, StringComparison.OrdinalIgnoreCase) && item.IsSelected)
                            {
                                item.IsSelected = false;
                                _repositorySelectionState.SetSelected(item.Path, false);
                            }
                        }
                    }
                    finally
                    {
                        _isUpdatingRepoCBs = false;
                    }
                    // Clear branch selections from the previously selected repo
                    _branchCatalogCoordinator.ClearCatalogsAndSelections(_branchSelectionState);
                }

                var change = _repositorySelectionCoordinator.ApplySelection(
                    cb.DataContext as RepositorySelectionItem,
                    cb.Tag as string,
                    isSelected);

                if (!change.HasRepository)
                    return;

                if (change.ShouldLoadBranches)
                {
                    _ = LoadBranchesForRepoAsync(change.RepositoryPath);
                }
                else if (change.ShouldClearBranchCatalogs)
                {
                    _branchCatalogCoordinator.ClearCatalogsAndSelections(_branchSelectionState);
                    InvokeOnUiThread(RefreshBranchLists);
                }
            }
            catch { }
            finally
            {
                ValidateSelections();
            }
        }

        private void BranchCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Called when a branch checkbox in the ComboBox template is checked/unchecked
            try
            {
                // Ignore events while we're programmatically updating checkboxes
                if (_isUpdatingCheckBoxes)
                    return;

                if (sender is not CheckBox checkBox || checkBox.DataContext is not BranchItem branchItem)
                    return;

                var context = _branchDropdownVisualState.ResolveBranchItemContext(
                    branchItem,
                    SourceBranchBox,
                    TargetBranchBox);
                if (context == null)
                    return;

                try
                {
                    _isUpdatingCheckBoxes = true;
                    _branchDropdownSelectionCoordinator.ApplyToggle(
                        context.Role,
                        context.Items,
                        branchItem,
                        checkBox.IsChecked == true);
                }
                finally
                {
                    _isUpdatingCheckBoxes = false;
                }

                ValidateSelections();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BranchCheckBox_Changed error: {ex.Message}");
            }
        }

        /// <summary>
        /// Validates and updates button states based on current selections
        /// </summary>
        private void ValidateSelections()
        {
            var availability = _workflowAvailabilityService.Evaluate(
                _branchSelectionState,
                _repositorySelectionState);

            _viewModel?.UpdateWorkflowAvailability(
                availability.CanRunMergeWorkflow,
                availability.CanRunPullWorkflow);
        }

        private void BranchComboBox_DropDownOpened(object? sender, EventArgs e)
        {
            // When a ComboBox dropdown opens, restore the checked state of all checkboxes from tracking collections
            try
            {
                if (sender is not System.Windows.Controls.ComboBox comboBox)
                    return;

                var role = _branchDropdownVisualState.ResolveRole(
                    comboBox,
                    SourceBranchBox,
                    TargetBranchBox);
                if (role == null)
                    return;

                try
                {
                    _isUpdatingCheckBoxes = true;
                    _branchDropdownSelectionCoordinator.RestoreTrackedSelection(
                        role.Value,
                        _branchDropdownVisualState.GetBranchItems(comboBox));
                }
                finally
                {
                    _isUpdatingCheckBoxes = false;
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BranchComboBox_DropDownOpened error: {ex.Message}");
            }
        }

        private async Task LoadBranchesForRepoAsync(string repoPath)
        {
            try
            {
                await Task.CompletedTask;
                _branchCatalogCoordinator.LoadRepositoryCatalog(
                    _settings,
                    repoPath,
                    _branchSelectionState);

                // Refresh UI with all branches
                InvokeOnUiThread(RefreshBranchLists);
            }
            catch { }
        }

        private void AddBranchToRecentAndUi(string branch)
        {
            try
            {
                _branchCatalogCoordinator.AddRecentBranch(_settings, branch, _branchSelectionState);

                InvokeOnUiThread(RefreshBranchLists);
            }
            catch { }
        }

        /// <summary>
        /// Refreshes the branch dropdown lists with hierarchical organization
        /// </summary>
        private void RefreshBranchLists()
        {
            try
            {
                var projection = _branchDropdownProjectionService.Build(
                    _branchSelectionState.SourceBranchCatalog,
                    _branchSelectionState.TargetBranchCatalog);

                _viewModel?.ReplaceBranchDropdownItems(
                    projection.SourceItems,
                    projection.TargetItems);
            }
            catch (Exception ex)
            {
                AppendError($"Error refreshing branch lists: {ex.Message}");
            }
        }

        // Button handler to pull selected branches from origin (mirrors provided bash script behavior)
        private async void PullSelectedBranches_Click(object sender, RoutedEventArgs e)
        {
            using var cts = new CancellationTokenSource();
            using var progress = ButtonProgressScope.Start(btnPullBranches);

            var repoPaths = GetSelectedRepositories();
            var sourceBranches = _branchSelectionState.GetSelectedBranches(BranchSelectionRole.Source);
            var callbacks = _batchCallbackFactory.CreatePullCallbacks();

            await _batchOperationWorkflowRunner.RunPullAsync(
                repoPaths,
                sourceBranches,
                callbacks,
                cts.Token);
        }

        private void RefreshBranches_Click(object sender, RoutedEventArgs e)
        {
            using var progress = ButtonProgressScope.Start(btnRefreshBranches);

            try
            {
                // Preserve current branch text values so user's explicit selections aren't lost
                var prevSource = SourceBranchBox?.Text;
                var prevTarget = TargetBranchBox?.Text;

                var refresh = _refreshWorkflowService.ResetSelections(
                    _branchSelectionState,
                    _repositorySelectionState,
                    prevSource,
                    prevTarget);

                _viewModel?.ClearRepositorySelections();

                // Clear branch items to refresh
                _viewModel?.ReplaceBranchDropdownItems(
                    refresh.SourceItems,
                    refresh.TargetItems);

                _refreshWorkflowVisualState.Apply(
                    refresh,
                    lstRepos,
                    SourceBranchBox,
                    TargetBranchBox);

                AppendOutput(refresh.Message);
            }
            catch (Exception ex)
            {
                AppendError($"Failed to refresh UI: {ex.Message}");
            }
        }

        #region Private methods
        private List<string> GetSelectedRepositories()
        {
            return _repositorySelectionState.GetSelectedRepositories();
        }

        #endregion

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save quick settings
            try
            {
                _timerService.Stop(_logFlushTimer);
                _timerService.Stop(_smoothScrollTimer);
                _eventHookService.UnhookBranchDropdowns(
                    SourceBranchBox,
                    TargetBranchBox,
                    BranchComboBox_DropDownOpened);
                _eventHookService.UnhookPreviewKeyDown(this, MainWindow_PreviewKeyDown);

                _closingService.SaveAndDispose(
                    _settings,
                    OutputBox.FontSize,
                    _logBufferService);
            }
            catch { }
            App.Current.Shutdown();
        }

        private void ClearErrorBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logRenderer?.Clear();
                _logBufferService.Clear();
            }
            catch { }
        }

        private void ConfigureWorkflowCommands()
        {
            _viewModel?.ConfigureWorkflowActions(new MainWindowWorkflowActions
            {
                Merge = () => Merge_Click(this, new RoutedEventArgs()),
                Pull = () => PullSelectedBranches_Click(this, new RoutedEventArgs()),
                Refresh = () => RefreshBranches_Click(this, new RoutedEventArgs()),
                ManageRepositories = () => ManageRepos_Click(this, new RoutedEventArgs()),
                ToggleLogs = ToggleLogs,
                BrowseRepositoryPath = () => BrowseRepoPath_Click(this, new RoutedEventArgs()),
                AddRepository = () => AddRepo_Click(this, new RoutedEventArgs()),
                EditRepository = () => EditRepo_Click(this, new RoutedEventArgs()),
                UpdateRepository = () => UpdateRepo_Click(this, new RoutedEventArgs()),
                RemoveRepository = () => RemoveRepo_Click(this, new RoutedEventArgs()),
                SaveLogs = () => SaveLogs_Click(this, new RoutedEventArgs()),
                CopyLogs = () => CopyLogs_Click(this, new RoutedEventArgs()),
                ClearLogs = () => ClearErrorBox_Click(this, new RoutedEventArgs())
            });
        }

        private void AppendOutput(string text, bool status = false)
        {
            _logBufferService.AppendOutput(text, status);
        }

        private void AppendError(string text)
        {
            _logBufferService.AppendError(text);
        }

        private void InvokeOnUiThread(Action action)
        {
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
        }

        private void FlushLogQueues()
        {
            try
            {
                _logBufferService.MaxChars = _settings?.LogMaxChars > 0 ? _settings.LogMaxChars : LogBufferService.DefaultMaxChars;
                var batch = _logBufferService.Drain();
                if (!batch.HasText) return;

                if (batch.CombinedText.Length > 0)
                {
                    try
                    {
                        if (OutputBox != null)
                        {
                            _logRenderer?.Append(batch.CombinedText, OutputBox.Foreground);
                        }
                    }
                    catch { }
                }
            }
            catch
            {
                // swallow logging errors
            }
        }

        private void ToggleLogs()
        {
            try
            {
                // In the new layout, the logs are always visible, but we can toggle the section visibility
                // This could be replaced with scrolling or other interaction if needed
                // Currently logs are part of the main layout
                try
                {
                    // persist state
                    _settings.Save();
                }
                catch { }
                // ensure logs scrolled to end
                try { OutputBox?.ScrollToEnd(); } catch { }
            }
            catch { }
        }

        private void SaveLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logExportService.SaveToSelectedFile(
                    _logBufferService,
                    _filePickers,
                    SectionSeparator,
                    this);
            }
            catch (Exception ex)
            {
                AppendError($"Failed to save logs: {ex.Message}");
            }
        }

        private void CopyLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                _logClipboardService.CopyToClipboard(
                    _logBufferService,
                    _clipboard,
                    SectionSeparator);
            }
            catch { }
        }

        // Keyboard shortcuts: Save (Ctrl+S), Copy (Ctrl+C), Clear (Ctrl+K)
        private void RegisterLogShortcuts()
        {
            try
            {
                _shortcutService.RegisterLogShortcuts(
                    InputBindings,
                    _viewModel,
                    new MainWindowLogShortcutActions
                    {
                        SaveLogs = () => SaveLogs_Click(this, new RoutedEventArgs()),
                        CopyLogs = () => CopyLogs_Click(this, new RoutedEventArgs()),
                        ClearLogs = () => ClearErrorBox_Click(this, new RoutedEventArgs())
                    });
            }
            catch { }
        }

        private void RegisterActionShortcuts()
        {
            try
            {
                _shortcutService.RegisterActionShortcuts(
                    InputBindings,
                    _viewModel,
                    new MainWindowShortcutActions
                    {
                        Merge = () => Merge_Click(this, new RoutedEventArgs()),
                        Pull = () => PullSelectedBranches_Click(this, new RoutedEventArgs()),
                        Refresh = () => RefreshBranches_Click(this, new RoutedEventArgs()),
                        ManageRepositories = () => ManageRepos_Click(this, new RoutedEventArgs()),
                        ToggleLogs = ToggleLogs
                    });
            }
            catch { }
        }


        // Toggle streaming to files
        private void StartStreamLogs(string path)
        {
            try
            {
                _logBufferService.StartStreaming(path);
            }
            catch { }
        }

        private void StopStreamLogs()
        {
            try
            {
                _logBufferService.StopStreaming();
            }
            catch { }
        }

    }
}
