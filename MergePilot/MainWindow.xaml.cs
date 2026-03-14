using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MahApps.Metro.Controls;
using CheckBox = System.Windows.Controls.CheckBox;
using MaterialDesignThemes.Wpf;
using System.Text.Json;
using System.Windows.Forms; // used for FolderBrowserDialog
using Microsoft.VisualBasic; // used for simple input dialogs
using System.Windows.Input;

namespace MergePilot
{
    public partial class MainWindow : MetroWindow
    {
        // Separators
        private const string SectionSeparator = "===============================================";
        private const string SubSectionSeparator = "-----------------------------------------------";

        private readonly ConcurrentQueue<string> _outputQueue = new();
        private readonly ConcurrentQueue<string> _errorQueue = new();
        private readonly DispatcherTimer _logFlushTimer;
        private const int MaxLogChars = 200_000; // cap to keep UI responsive
        // Inline log boxes in main window (OutputBox and ErrorBox defined in XAML)
        private bool _autoOpenLogs = true;
        private bool _useTextLogSymbols = true;

        private bool _streamLogsToFile = false;
        private StreamWriter? _logFileWriter;
        private AppSettings _settings;
        // master buffers for non-destructive filtering/search
        private StringBuilder _outputMaster = new();
        private StringBuilder _errorMaster = new();
        private List<int> _outputMatchIndexes = new();
        private int _outputCurrentMatch = -1;
        private List<int> _errorMatchIndexes = new();
        private int _errorCurrentMatch = -1;

        // Track checked items in each ComboBox (since items are virtualized)
        private HashSet<string> _checkedSourceBranches = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _checkedTargetBranches = new(StringComparer.OrdinalIgnoreCase);

        // Track all branches for hierarchical organization
        private HashSet<string> _allSourceBranches = new(StringComparer.OrdinalIgnoreCase);
        private HashSet<string> _allTargetBranches = new(StringComparer.OrdinalIgnoreCase);

        // Prevent re-entrant checkbox events
        private bool _isUpdatingCheckBoxes = false;

        // Smooth scrolling optimization
        private DispatcherTimer? _smoothScrollTimer;
        private double _targetScrollOffset = 0;
        private int _displayedLineCount = 0;
        private const int MaxDisplayedLines = 2000; // Limit lines in view for performance

        public MainWindow()
        {
            InitializeComponent();

            // initial UI state and timers
            ValidateSelections();
            _logFlushTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(100)
            };
            _logFlushTimer.Tick += (s, e) => FlushLogQueues();
            _logFlushTimer.Start();

            // Initialize smooth scroll animation timer
            _smoothScrollTimer = new DispatcherTimer(DispatcherPriority.Background)
            {
                Interval = TimeSpan.FromMilliseconds(16) // ~60 FPS
            };
            _smoothScrollTimer.Tick += (s, e) => UpdateSmoothScroll();
            _smoothScrollTimer.Start();

            // Optimize RichTextBox rendering
            if (OutputBox != null)
            {
                OutputBox.IsEnabled = true;
            }

            // register keyboard shortcuts
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => Merge_Click(null, null)), Key.M, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => PullSelectedBranches_Click(null, null)), Key.P, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => RefreshBranches_Click(null, null)), Key.R, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ManageRepos_Click(null, null)), Key.B, ModifierKeys.Control));
            this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ToggleLogs()), Key.L, ModifierKeys.Control));
            this.PreviewKeyDown += MainWindow_PreviewKeyDown;

            // register log shortcuts (save/copy/clear)
            RegisterLogShortcuts();

            // load settings and apply
            _settings = AppSettings.Load();
            _autoOpenLogs = _settings.AutoOpenLogs;

            // populate dynamic repositories from settings
            PopulateRepositoriesFromSettings();

            // hook branch combobox dropdown events
            SourceBranchBox.DropDownOpened += BranchComboBox_DropDownOpened;
            TargetBranchBox.DropDownOpened += BranchComboBox_DropDownOpened;

            // restore inline log visibility and appearance
            try
            {
                if (_settings.LogFontSize > 0)
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
                var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
                if (key == System.Windows.Input.Key.E && (System.Windows.Input.Keyboard.Modifiers & System.Windows.Input.ModifierKeys.Shift) == System.Windows.Input.ModifierKeys.Shift)
                {
                    ToggleLogs();
                    e.Handled = true;
                }
            }
            catch { }
        }

        private void LstRepos_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
        {
            try
            {
                if (lstRepos == null) return;

                // ListView holds AppSettings.RepositoryEntry objects as items
                if (lstRepos.SelectedItem is AppSettings.RepositoryEntry repo && !string.IsNullOrWhiteSpace(repo.Path))
                {
                    try
                    {
                        foreach (var child in DynamicRepoPanel.Children)
                        {
                            if (child is CheckBox cb && cb.Tag is string cbPath && string.Equals(cbPath, repo.Path, StringComparison.OrdinalIgnoreCase))
                            {
                                cb.IsChecked = true;
                                cb.BringIntoView();
                                break;
                            }
                        }
                    }
                    catch { }

                    try { lstRepos.ScrollIntoView(repo); /* focus not needed */ } catch { }
                }
            }
            catch { }
        }

        private void BrowseRepoPath_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                using var dlg = new FolderBrowserDialog();
                dlg.Description = "Select repository folder (should contain .git)";
                dlg.UseDescriptionForTitle = true;
                var res = dlg.ShowDialog();
                if (res == System.Windows.Forms.DialogResult.OK || res == System.Windows.Forms.DialogResult.Yes)
                {
                    txtRepoPath.Text = dlg.SelectedPath;
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
                DynamicRepoPanel.Children.Clear();
                lstRepos?.Items.Clear();
                foreach (var repo in _settings.Repositories ?? new System.Collections.Generic.List<AppSettings.RepositoryEntry>())
                {
                    var cb = new CheckBox
                    {
                        Content = repo.Name ?? repo.Path,
                        Tag = repo.Path,
                        Style = (System.Windows.Style)this.Resources["DynamicRepoCheckBoxStyle"],
                        Margin = new System.Windows.Thickness(0, 0, 15, 0)
                    };
                    cb.Checked += CheckBox_Changed;
                    cb.Unchecked += CheckBox_Changed;
                    DynamicRepoPanel.Children.Add(cb);
                    // Add ListBoxItem with Tag set to path so selection can map back to dynamic checkbox
                    try
                    {
                        if (lstRepos != null)
                        {
                            lstRepos.Items.Add(repo);
                        }
                    }
                    catch { }
                }

                // Also load persisted recent branches into comboboxes
                SourceBranchBox.Items.Clear();
                TargetBranchBox.Items.Clear();
                if (_settings.RecentBranches != null)
                {
                    foreach (var b in _settings.RecentBranches.Distinct(StringComparer.OrdinalIgnoreCase))
                    {
                        if (!string.IsNullOrWhiteSpace(b))
                        {
                            SourceBranchBox.Items.Add(b);
                            TargetBranchBox.Items.Add(b);
                        }
                    }
                }
                
                // Ensure ListBox items are selectable visually (use default selection brush)
                try
                {
                    if (lstRepos != null)
                    {
                        lstRepos.SelectionMode = System.Windows.Controls.SelectionMode.Single;
                        lstRepos.SelectionChanged += LstRepos_SelectionChanged;
                    }
                }
                catch { }
            }
            catch { }
        }
        // Helper to convert hex color to brush
        private static System.Windows.Media.Brush BrushFromHex(string hex)
        {
            try
            {
                var conv = new System.Windows.Media.BrushConverter();
                return (System.Windows.Media.Brush?)conv.ConvertFromString(hex) ?? System.Windows.Media.Brushes.White;
            }
            catch
            {
                return System.Windows.Media.Brushes.White;
            }
        }

        private void ManageRepos_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                // Toggle visibility of management panel
                if (RepoManagePanel.Visibility == Visibility.Visible)
                {
                    RepoManagePanel.Visibility = Visibility.Collapsed;
                }
                else
                {
                    RepoManagePanel.Visibility = Visibility.Visible;
                    // populate list
                    lstRepos.Items.Clear();
                    foreach (var r in _settings.Repositories ?? new System.Collections.Generic.List<AppSettings.RepositoryEntry>())
                    {
                        try
                        {
                            lstRepos.Items.Add(r);
                        }
                        catch { }
                    }
                }
            }
            catch (Exception ex)
            {
                AppendError($"Failed to open repo manager: {ex.Message}");
            }
        }

        private void EditRepo_Click(object sender, RoutedEventArgs e)
        {
            if (lstRepos.SelectedIndex < 0)
            {
                System.Windows.MessageBox.Show("Select a repository to edit.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            // Map selected ListViewItem back to settings entry
            var idx = lstRepos.SelectedIndex;
            var entry = _settings.Repositories?[idx];
            try
            {
                if (lstRepos.SelectedItem is AppSettings.RepositoryEntry r)
                {
                    var found = _settings.Repositories?.Find(rr => string.Equals(rr.Path, r.Path, StringComparison.OrdinalIgnoreCase));
                    if (found != null) entry = found;
                }
            }
            catch { }
            // populate fields for editing
            txtRepoName.Text = entry?.Name ?? string.Empty;
            txtRepoPath.Text = entry?.Path ?? string.Empty;
            btnUpdateRepo.IsEnabled = true;
            btnAddRepo.IsEnabled = false;
        }

        private void UpdateRepo_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (lstRepos.SelectedIndex < 0)
                {
                    System.Windows.MessageBox.Show("Select a repository to update.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }

                var idx = lstRepos.SelectedIndex;
                var name = txtRepoName?.Text?.Trim();
                var path = txtRepoPath?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                {
                    System.Windows.MessageBox.Show("Name and path are required.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // validation: ensure .git exists
                if (!Directory.Exists(System.IO.Path.Combine(path, ".git")))
                {
                    var res = System.Windows.MessageBox.Show("The selected path does not contain a .git folder. Add anyway?", "Git folder not found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                        return;
                }

                if (_settings.Repositories != null && idx >= 0 && idx < _settings.Repositories.Count)
                {
                    _settings.Repositories[idx].Name = name;
                    _settings.Repositories[idx].Path = path;
                }
                _settings.Save();
                PopulateRepositoriesFromSettings();
                lstRepos.SelectedIndex = idx;
                txtRepoName.Text = string.Empty;
                txtRepoPath.Text = string.Empty;
                btnUpdateRepo.IsEnabled = false;
                btnAddRepo.IsEnabled = true;
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
                var name = txtRepoName?.Text?.Trim();
                var path = txtRepoPath?.Text?.Trim();
                if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(path))
                {
                    System.Windows.MessageBox.Show("Repo name and path are required.", "Invalid input", MessageBoxButton.OK, MessageBoxImage.Warning);
                    return;
                }

                // validation: ensure .git exists
                if (!Directory.Exists(System.IO.Path.Combine(path, ".git")))
                {
                    var res = System.Windows.MessageBox.Show("The selected path does not contain a .git folder. Add anyway?", "Git folder not found", MessageBoxButton.YesNo, MessageBoxImage.Question);
                    if (res != MessageBoxResult.Yes)
                        return;
                }

                _settings.Repositories ??= new System.Collections.Generic.List<AppSettings.RepositoryEntry>();
                var newEntry = new AppSettings.RepositoryEntry { Name = name, Path = path };
                _settings.Repositories.Add(newEntry);
                _settings.Save();
                PopulateRepositoriesFromSettings();
                lstRepos.Items.Add(newEntry);
                txtRepoName.Text = string.Empty;
                txtRepoPath.Text = string.Empty;
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
                if (lstRepos.SelectedIndex < 0)
                {
                    System.Windows.MessageBox.Show("Select a repository to remove.", "No selection", MessageBoxButton.OK, MessageBoxImage.Information);
                    return;
                }
                var idx = lstRepos.SelectedIndex;
                var confirm = System.Windows.MessageBox.Show($"Remove '{_settings.Repositories[idx].Name}'?", "Confirm remove", MessageBoxButton.YesNo, MessageBoxImage.Question);
                if (confirm != MessageBoxResult.Yes) return;

                // remove from settings
                if (_settings.Repositories != null && idx < _settings.Repositories.Count)
                {
                    _settings.Repositories.RemoveAt(idx);
                    _settings.Save();
                    PopulateRepositoriesFromSettings();
                    // ensure selection reflects change
                    if (lstRepos.Items.Count > idx) lstRepos.Items.RemoveAt(idx);
                }
            }
            catch (Exception ex)
            {
                AppendError($"Failed to remove repository: {ex.Message}");
            }
        }

        private async void Merge_Click(object sender, RoutedEventArgs e)
        {
            var cts = new CancellationTokenSource();
            btnMerge.IsEnabled = false;
            ButtonProgressAssist.SetIsIndeterminate(btnMerge, true);

            var successList = new List<string>();
            var skipList = new List<string>();
            var failList = new List<string>();

            // Get all items from dropdowns (multiple selection)
            var sources = GetSelectedBranchesFromComboBox(SourceBranchBox);
            var targets = GetSelectedBranchesFromComboBox(TargetBranchBox);
            var repoPaths = GetSelectedRepositories();

            AppendOutput(LogFormatter.FormatOperationStart("MERGE OPERATION"),true);

            try
            {
                if (sources.Count == 0)
                {
                    AppendError("❌ No source branches selected. Please select at least one source branch.");
                    return;
                }

                if (targets.Count == 0)
                {
                    AppendError("❌ No target branches selected. Please select at least one target branch.");
                    return;
                }

                // persist last used branches
                _settings.LastSourceBranch = string.Join("|", sources);
                _settings.LastTargetBranch = string.Join("|", targets);
                _settings.Save();

                foreach (var repo in repoPaths)
                {
                    AppendOutput(LogFormatter.FormatProjectHeader(System.IO.Path.GetFileName(repo), repo), true);
                    if (!System.IO.Directory.Exists(repo) || !System.IO.Directory.Exists(System.IO.Path.Combine(repo, ".git")))
                    {
                        AppendError($"❌ Repository path not found or not a git repo: {repo}");
                        failList.Add($"{repo} | all branches | repo not found");
                        continue;
                    }

                    var repoSuccessList = new List<string>();
                    var repoSkipList = new List<string>();
                    var repoFailList = new List<string>();

                    // Merge each source into each target
                    foreach (var source in sources)
                    {
                        foreach (var targetBranch in targets)
                        {
                            AppendOutput(LogFormatter.FormatBranchOperationHeader(source, targetBranch), true);

                            try
                            {
                                // Validate target branch exists on remote (using detected remote)
                                var remoteName = await GitHelper.GetDefaultRemoteNameAsync(repo, cts.Token).ConfigureAwait(false);
                                var existsOnRemote = await GitHelper.RemoteBranchExistsAsync(repo, remoteName, targetBranch, cts.Token).ConfigureAwait(false);
                                if (!existsOnRemote)
                                {
                                    var ask = Dispatcher.Invoke(() => System.Windows.MessageBox.Show($"Target branch '{targetBranch}' was not found on '{remoteName}' for repo '{repo}'.\n\nDo you want to continue anyway?", "Target branch not found", MessageBoxButton.YesNo, MessageBoxImage.Warning));
                                    if (ask != MessageBoxResult.Yes)
                                    {
                                        Dispatcher.Invoke(() => AppendOutput($"⏭ Skipped: target branch not found on remote"));
                                        failList.Add($"{repo} | {targetBranch} | target-not-found");
                                        repoFailList.Add($"{source}→{targetBranch}");
                                        continue;
                                    }
                                }

                                // Run merge operation on background thread
                                var result = await GitHelper.MergeBranchAsync(repo, source, targetBranch, cts.Token);

                                // Handle results — only UI interactions are invoked on Dispatcher
                                switch (result.Status)
                                {
                                    case MergeStatus.Success:
                                        Dispatcher.Invoke(() =>
                                        {
                                            AppendOutput($"✔ {result.Message}");
                                            successList.Add($"{repo} | {source}→{targetBranch}");
                                            repoSuccessList.Add($"{source}→{targetBranch}");
                                        });
                                        break;

                                    case MergeStatus.Skipped:
                                        Dispatcher.Invoke(() =>
                                        {
                                            AppendOutput($"⏭ {result.Message}");
                                            skipList.Add($"{repo} | {source}→{targetBranch}");
                                            repoSkipList.Add($"{source}→{targetBranch}");
                                        });
                                        break;

                                    case MergeStatus.Conflict:
                                        // Log conflict on UI
                                        Dispatcher.Invoke(() => AppendError($"❌ Conflict: {result.Message}"));

                                        // Ask user what to do (synchronously marshal to UI thread)
                                        var msg = $"Merge conflict while merging '{source}' into '{targetBranch}' for repo '{repo}'.\n\n" +
                                                  "Choose 'Yes' to open repository folder and resolve manually, then press OK to continue.\n" +
                                                  "Choose 'No' to abort merge and skip this branch.";
                                        var userChoice = (MessageBoxResult)Dispatcher.Invoke(() => System.Windows.MessageBox.Show(msg, "Merge Conflict", MessageBoxButton.YesNo, MessageBoxImage.Warning, MessageBoxResult.No));

                                        if (userChoice == MessageBoxResult.Yes)
                                        {
                                            // Open repo in Explorer for user to resolve (UI)
                                            Dispatcher.Invoke(() =>
                                            {
                                                try
                                                {
                                                    Process.Start(new ProcessStartInfo
                                                    {
                                                        FileName = "explorer",
                                                        Arguments = $"\"{repo}\"",
                                                        UseShellExecute = true
                                                    });
                                                }
                                                catch
                                                {
                                                    AppendError($"⚠ Could not open explorer for: {repo}");
                                                }
                                            });

                                            // Wait for user to resolve conflicts manually (UI)
                                            var resolvedPrompt = Dispatcher.Invoke(() =>
                                                System.Windows.MessageBox.Show("Resolve conflicts in the opened repository (stage & commit). Click OK when done, or Cancel to abort & skip.", "Resolve Conflicts", MessageBoxButton.OKCancel, MessageBoxImage.Information));

                                            if (resolvedPrompt == MessageBoxResult.Cancel)
                                            {
                                                // Abort merge (background) and record failure on UI
                                                var abortResult = await GitHelper.AbortMergeAsync(repo, cts.Token);
                                                Dispatcher.Invoke(() =>
                                                {
                                                    AppendError($"❌ Merge aborted: {abortResult.StdErr}");
                                                    failList.Add($"{repo} | {targetBranch} | aborted by user");
                                                    repoFailList.Add($"{source}→{targetBranch}");
                                                });

                                                // skip further processing of this target branch
                                                continue;
                                            }

                                            // Attempt to add/commit/push the resolution on background thread
                                            var add = await GitHelper.RetryRunGitCommandAsync(repo, "add -A", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1), cts.Token);
                                            if (!add.IsSuccess)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    AppendError($"❌ 'git add' failed: {add.StdErr}");
                                                    failList.Add($"{repo} | {targetBranch} | add failed");
                                                    repoFailList.Add($"{source}→{targetBranch}");
                                                });
                                                continue;
                                            }

                                            var commit = await GitHelper.RetryRunGitCommandAsync(repo, "commit -m \"Resolve merge conflicts by user\"", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1), cts.Token);
                                            if (!commit.IsSuccess)
                                            {
                                                // If nothing to commit, log info and continue to push attempt
                                                Dispatcher.Invoke(() =>
                                                {
                                                    AppendOutput($"ℹ 'git commit' returned: {commit.StdErr} {commit.StdOut}");
                                                });
                                            }

                                            var push = await GitHelper.RetryRunGitCommandAsync(repo, $"push origin {targetBranch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cts.Token);
                                            if (!push.IsSuccess)
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    AppendError($"❌ Push failed after manual resolution: {push.StdErr}");
                                                    failList.Add($"{repo} | {targetBranch} | push failed after resolution");
                                                    repoFailList.Add($"{source}→{targetBranch}");
                                                });
                                            }
                                            else
                                            {
                                                Dispatcher.Invoke(() =>
                                                {
                                                    AppendOutput($"✔ Manual resolution pushed: {targetBranch}");
                                                    successList.Add($"{repo} | {source}→{targetBranch}");
                                                    repoSuccessList.Add($"{source}→{targetBranch}");
                                                });
                                            }
                                        }
                                        else
                                        {
                                            // Abort merge and skip (background) then update UI lists
                                            var abort = await GitHelper.AbortMergeAsync(repo, cts.Token);
                                            Dispatcher.Invoke(() =>
                                            {
                                                AppendError($"❌ Merge aborted for {targetBranch}: {abort.StdErr}");
                                                failList.Add($"{repo} | {targetBranch} | merge conflict (aborted)");
                                                repoFailList.Add($"{source}→{targetBranch}");
                                            });
                                        }
                                        break;

                                    case MergeStatus.Failed:
                                        Dispatcher.Invoke(() =>
                                        {
                                            AppendError($"❌ {result.Message}");
                                            failList.Add($"{repo} | {source}→{targetBranch} | failed");
                                            repoFailList.Add($"{source}→{targetBranch}");
                                        });
                                        break;
                                }
                            }
                            catch (Exception ex)
                            {
                                // Ensure UI updates are done on UI thread
                                Dispatcher.Invoke(() =>
                                {
                                    AppendError($"❌ Exception for {targetBranch}: {ex.Message}");
                                    failList.Add($"{repo} | {targetBranch} | exception");
                                    repoFailList.Add($"{source}→{targetBranch}");
                                });
                            }
                        } // target branches
                    } // source branches

                    // Project-level summary
                    AppendOutput(LogFormatter.FormatProjectSummary(System.IO.Path.GetFileName(repo), repoSuccessList, repoSkipList, repoFailList), true);
                } // repo
            }
            finally
            {
                Dispatcher.Invoke(() =>
                {
                    ButtonProgressAssist.SetIsIndeterminate(btnMerge, false);
                    btnMerge.IsEnabled = true;
                });
                // Final summary (log to single log window)
                AppendOutput(LogFormatter.FormatFinalSummary("MERGE OPERATION", successList, skipList, failList), true);
                AppendOutput("🎉 Merging finished.");
            }
        }

        private void CheckBox_Changed(object sender, RoutedEventArgs e)
        {
            ValidateSelections();

            // If a repository checkbox was checked, attempt to load remote branches for quick selection
            try
            {
                if (sender is CheckBox cb && cb.IsChecked == true && cb.Tag is string repoPath && !string.IsNullOrWhiteSpace(repoPath))
                {
                    // fire-and-forget async load (updates UI via Dispatcher)
                    _ = LoadBranchesForRepoAsync(repoPath);
                    // also attempt to validate current TargetBranchBox for this repo's remote
                    _ = ValidateTargetForRepoAsync(repoPath, TargetBranchBox.Text.Trim());
                }
            }
            catch { }
        }

        private void BranchCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            // Called when a branch checkbox in the ComboBox template is checked/unchecked
            try
            {
                // Ignore events while we're programmatically updating checkboxes
                if (_isUpdatingCheckBoxes)
                    return;

                if (sender is CheckBox checkBox && checkBox.DataContext is BranchItem branchItem)
                {
                    // Find which ComboBox contains this BranchItem
                    System.Windows.Controls.ComboBox sourceCombo = null;
                    
                    // Try finding by visual tree first (more efficient)
                    DependencyObject current = checkBox;
                    while (current != null)
                    {
                        if (current is System.Windows.Controls.ComboBox combo)
                        {
                            sourceCombo = combo;
                            break;
                        }
                        current = VisualTreeHelper.GetParent(current);
                    }

                    // If not found by visual tree, check which ComboBox contains this item
                    if (sourceCombo == null)
                    {
                        if (IsItemInComboBoxByItem(SourceBranchBox, branchItem))
                            sourceCombo = SourceBranchBox;
                        else if (IsItemInComboBoxByItem(TargetBranchBox, branchItem))
                            sourceCombo = TargetBranchBox;
                    }

                    // Only proceed if we found the source combo
                    if (sourceCombo == null)
                        return;

                    bool isChecked = checkBox.IsChecked == true;

                    if (branchItem.IsGroup)
                    {
                        // Parent group was toggled - toggle all children in THIS combobox only
                        ToggleGroupChildren(branchItem, isChecked, sourceCombo);
                    }
                    else
                    {
                        // Leaf branch was toggled - update tracking and parent state in THIS combobox only
                        UpdateLeafBranchState(branchItem, isChecked, sourceCombo);
                    }

                    ValidateSelections();
                }
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
            bool hasExactlyOneSourceBranch = _checkedSourceBranches.Count == 1;
            bool hasTargetBranches = _checkedTargetBranches.Count > 0;
            bool hasSourceBranches = _checkedSourceBranches.Count > 0;

            // Merge button: needs EXACTLY 1 source branch and at least 1 target branch
            btnMerge.IsEnabled = hasExactlyOneSourceBranch && hasTargetBranches;

            // Pull button: needs at least 1 source branch selected
            btnPullBranches.IsEnabled = hasSourceBranches;
        }

        /// <summary>
        /// Updates the state of a leaf branch and propagates to parent
        /// </summary>
        private void UpdateLeafBranchState(BranchItem leafItem, bool isChecked, System.Windows.Controls.ComboBox sourceCombo)
        {
            bool isSourceCombo = ReferenceEquals(sourceCombo, SourceBranchBox);
            bool isTargetCombo = ReferenceEquals(sourceCombo, TargetBranchBox);

            if (isSourceCombo)
            {
                if (isChecked)
                    _checkedSourceBranches.Add(leafItem.FullName);
                else
                    _checkedSourceBranches.Remove(leafItem.FullName);
            }

            if (isTargetCombo)
            {
                if (isChecked)
                    _checkedTargetBranches.Add(leafItem.FullName);
                else
                    _checkedTargetBranches.Remove(leafItem.FullName);
            }

            // Update parent state in the same combobox (with re-entrancy protection)
            if (leafItem.Parent != null)
            {
                try
                {
                    _isUpdatingCheckBoxes = true;
                    UpdateParentCheckBoxState(leafItem.Parent, sourceCombo);
                }
                finally
                {
                    _isUpdatingCheckBoxes = false;
                }
            }
        }

        /// <summary>
        /// Finds a BranchItem by its full name in the combobox items
        /// </summary>
        private BranchItem FindBranchItemByFullName(System.Windows.Controls.ComboBox comboBox, string fullName)
        {
            if (comboBox?.Items == null || string.IsNullOrWhiteSpace(fullName))
                return null;

            foreach (var item in comboBox.Items)
            {
                if (item is BranchItem branchItem && branchItem.FullName == fullName)
                    return branchItem;
            }

            return null;
        }

        /// <summary>
        /// Checks if a BranchItem is in the given ComboBox (by object reference)
        /// </summary>
        private bool IsItemInComboBoxByItem(System.Windows.Controls.ComboBox comboBox, BranchItem item)
        {
            if (comboBox?.Items == null || item == null) 
                return false;

            foreach (var comboItem in comboBox.Items)
            {
                // Compare by object reference, not by FullName
                // This is critical for distinguishing between Source and Target dropdowns
                if (ReferenceEquals(comboItem, item))
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Checks if an item is in the given ComboBox
        /// </summary>
        private bool IsItemInComboBox(System.Windows.Controls.ComboBox comboBox, string branchName)
        {
            if (comboBox?.Items == null) return false;

            foreach (var item in comboBox.Items)
            {
                if (item is BranchItem branchItem && branchItem.FullName == branchName && !branchItem.IsGroup)
                    return true;
                else if (item is string stringItem && stringItem == branchName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Toggles all children of a group
        /// </summary>
        private void ToggleGroupChildren(BranchItem groupItem, bool shouldCheck, System.Windows.Controls.ComboBox sourceCombo)
        {
            bool isSourceCombo = ReferenceEquals(sourceCombo, SourceBranchBox);
            bool isTargetCombo = ReferenceEquals(sourceCombo, TargetBranchBox);

            var leafChildren = BranchOrganizer.GetChildBranches(groupItem);

            try
            {
                _isUpdatingCheckBoxes = true;

                // Update tracking set and UI for all child leaves
                foreach (var childBranchName in leafChildren)
                {
                    if (isSourceCombo)
                    {
                        if (shouldCheck)
                            _checkedSourceBranches.Add(childBranchName);
                        else
                            _checkedSourceBranches.Remove(childBranchName);
                    }
                    
                    if (isTargetCombo)
                    {
                        if (shouldCheck)
                            _checkedTargetBranches.Add(childBranchName);
                        else
                            _checkedTargetBranches.Remove(childBranchName);
                    }
                }

                // Update all child checkboxes in the UI
                UpdateChildCheckBoxesInUI(sourceCombo, groupItem, shouldCheck);

                // Update parent checkbox state recursively in the same combobox
                if (groupItem.Parent != null)
                {
                    UpdateParentCheckBoxState(groupItem.Parent, sourceCombo);
                }
            }
            finally
            {
                _isUpdatingCheckBoxes = false;
            }
        }

        /// <summary>
        /// Updates all child checkboxes in the UI to match the parent state
        /// </summary>
        private void UpdateChildCheckBoxesInUI(System.Windows.Controls.ComboBox comboBox, BranchItem parentItem, bool shouldCheck)
        {
            if (comboBox?.Items == null) return;

            var leafChildren = BranchOrganizer.GetChildBranches(parentItem);
            
            // Update the BranchItem data models directly
            UpdateBranchItemIsChecked(comboBox, leafChildren, shouldCheck);

            // Also update the visual checkboxes for immediate feedback
            var checkBoxes = FindAllVisualChildren<CheckBox>(comboBox);
            foreach (var checkBox in checkBoxes)
            {
                if (checkBox.DataContext is BranchItem branchItem && leafChildren.Contains(branchItem.FullName))
                {
                    checkBox.IsChecked = shouldCheck;
                }
            }
        }

        /// <summary>
        /// Recursively updates IsChecked property on BranchItem objects
        /// </summary>
        private void UpdateBranchItemIsChecked(System.Windows.Controls.ComboBox comboBox, List<string> leafFullNames, bool shouldCheck)
        {
            if (comboBox?.Items == null) return;

            foreach (var item in comboBox.Items)
            {
                if (item is BranchItem branchItem)
                {
                    // Check if this is a leaf child
                    if (leafFullNames.Contains(branchItem.FullName))
                    {
                        branchItem.IsChecked = shouldCheck;
                    }
                    // Recursively check children
                    else if (branchItem.Children.Count > 0)
                    {
                        UpdateBranchItemIsCheckedRecursive(branchItem.Children, leafFullNames, shouldCheck);
                    }
                }
            }
        }

        /// <summary>
        /// Helper to recursively update BranchItem checkboxes
        /// </summary>
        private void UpdateBranchItemIsCheckedRecursive(List<BranchItem> items, List<string> leafFullNames, bool shouldCheck)
        {
            foreach (var item in items)
            {
                if (leafFullNames.Contains(item.FullName))
                {
                    item.IsChecked = shouldCheck;
                }
                else if (item.Children.Count > 0)
                {
                    UpdateBranchItemIsCheckedRecursive(item.Children, leafFullNames, shouldCheck);
                }
            }
        }

        /// <summary>
        /// Updates the parent checkbox state based on children (tri-state logic)
        /// </summary>
        private void UpdateParentCheckBoxState(BranchItem parentItem, System.Windows.Controls.ComboBox sourceCombo)
        {
            if (parentItem == null) return;

            bool isSourceCombo = ReferenceEquals(sourceCombo, SourceBranchBox);
            HashSet<string> trackedBranches = isSourceCombo ? _checkedSourceBranches : _checkedTargetBranches;

            var leafChildren = BranchOrganizer.GetChildBranches(parentItem);
            if (leafChildren.Count == 0) return;

            int checkedCount = leafChildren.Count(child => trackedBranches.Contains(child));
            int totalCount = leafChildren.Count;

            // Determine new state
            bool? newState = null;
            if (checkedCount == totalCount && totalCount > 0)
                newState = true;
            else if (checkedCount == 0)
                newState = false;
            else
                newState = null; // Indeterminate

            // Update the parent item's IsChecked property (this triggers binding update)
            parentItem.IsChecked = newState;

            // Also update the visual checkbox immediately
            var checkBoxes = FindAllVisualChildren<CheckBox>(sourceCombo);
            foreach (var checkBox in checkBoxes)
            {
                if (checkBox.DataContext is BranchItem branchItem && branchItem.FullName == parentItem.FullName)
                {
                    checkBox.IsChecked = newState;
                    break;
                }
            }

            // Recursively update grandparent
            if (parentItem.Parent != null)
            {
                UpdateParentCheckBoxState(parentItem.Parent, sourceCombo);
            }
        }

        private void BranchComboBox_DropDownOpened(object sender, EventArgs e)
        {
            // When a ComboBox dropdown opens, restore the checked state of all checkboxes from tracking collections
            try
            {
                _isUpdatingCheckBoxes = true;

                if (sender is System.Windows.Controls.ComboBox comboBox)
                {
                    HashSet<string> trackedBranches = ReferenceEquals(comboBox, SourceBranchBox) 
                        ? _checkedSourceBranches 
                        : _checkedTargetBranches;

                    // First pass: Update leaf branch items from tracked state
                    UpdateBranchItemStatesFromTracked(comboBox.Items.Cast<object>().ToList(), trackedBranches);

                    // Second pass: Update parent branch items based on their children's state
                    foreach (var item in comboBox.Items)
                    {
                        if (item is BranchItem branchItem && branchItem.IsGroup)
                        {
                            var leafChildren = BranchOrganizer.GetChildBranches(branchItem);
                            int checkedCount = leafChildren.Count(child => trackedBranches.Contains(child));

                            if (checkedCount == leafChildren.Count && leafChildren.Count > 0)
                                branchItem.IsChecked = true;
                            else if (checkedCount == 0)
                                branchItem.IsChecked = false;
                            else
                                branchItem.IsChecked = null; // Indeterminate
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"BranchComboBox_DropDownOpened error: {ex.Message}");
            }
            finally
            {
                _isUpdatingCheckBoxes = false;
            }
        }

        /// <summary>
        /// Recursively updates BranchItem IsChecked state from tracked branches
        /// </summary>
        private void UpdateBranchItemStatesFromTracked(List<object> items, HashSet<string> trackedBranches)
        {
            foreach (var item in items)
            {
                if (item is BranchItem branchItem)
                {
                    if (!branchItem.IsGroup)
                    {
                        // For leaves, update from tracked set
                        branchItem.IsChecked = trackedBranches.Contains(branchItem.FullName);
                    }
                    else if (branchItem.Children.Count > 0)
                    {
                        // Recursively update children
                        UpdateBranchItemStatesFromTracked(branchItem.Children.Cast<object>().ToList(), trackedBranches);
                    }
                }
            }
        }

        private async Task ValidateTargetForRepoAsync(string repoPath, string targetBranch)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(targetBranch))
                    return;

                var remote = await GitHelper.GetDefaultRemoteNameAsync(repoPath).ConfigureAwait(false);
                var exists = await GitHelper.RemoteBranchExistsAsync(repoPath, remote, targetBranch).ConfigureAwait(false);
                // If not exists, show subtle UI cue by adding an item to TargetBranchBox with suffix (missing)
                if (!exists)
                {
                    Dispatcher.Invoke(() =>
                    {
                        // Ensure item exists but marked
                        var display = $"{targetBranch} (missing on {remote})";
                        if (!TargetBranchBox.Items.Contains(display))
                            TargetBranchBox.Items.Add(display);
                    });
                }
            }
            catch { }
        }

        private async Task LoadBranchesForRepoAsync(string repoPath)
        {
            try
            {
                // Ensure repo path exists and contains git
                if (!Directory.Exists(repoPath) || !Directory.Exists(Path.Combine(repoPath, ".git")))
                    return;

                // Use ls-remote to list remote heads
                var res = await GitHelper.RetryRunGitCommandAsync(repoPath, "ls-remote --heads origin", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(30));
                if (!res.IsSuccess)
                {
                    // fallback: try listing local branches
                    var localRes = await GitHelper.RetryRunGitCommandAsync(repoPath, "for-each-ref --format='%(refname:short)' refs/heads/", 3, TimeSpan.FromSeconds(2), TimeSpan.FromSeconds(10));
                    if (!localRes.IsSuccess) return;
                    var locals = localRes.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).Where(s => !string.IsNullOrWhiteSpace(s)).ToList();
                    foreach (var b in locals)
                    {
                        AddBranchToRecentAndUi(b);
                    }
                    return;
                }

                var lines = res.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var line in lines)
                {
                    // format: <sha>\trefs/heads/<branch>
                    var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        var refPart = parts[1];
                        var prefix = "refs/heads/";
                        if (refPart.StartsWith(prefix))
                        {
                            var branch = refPart.Substring(prefix.Length).Trim();
                            if (!string.IsNullOrWhiteSpace(branch))
                                AddBranchToRecentAndUi(branch);
                        }
                    }
                }
            }
            catch { }
        }

        private void AddBranchToRecentAndUi(string branch)
        {
            try
            {
                // update settings recent branches with dedupe (case-insensitive)
                _settings.RecentBranches ??= new List<string>();
                if (!_settings.RecentBranches.Contains(branch, StringComparer.OrdinalIgnoreCase))
                {
                    _settings.RecentBranches.Add(branch);
                    if (_settings.RecentBranches.Count > 100)
                        _settings.RecentBranches.RemoveRange(0, _settings.RecentBranches.Count - 100);
                    _settings.Save();
                }

                // Track the new branch
                _allSourceBranches.Add(branch);
                _allTargetBranches.Add(branch);

                Dispatcher.Invoke(() =>
                {
                    // Rebuild the hierarchical branch lists
                    RefreshBranchLists();
                });
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
                // Create hierarchical organization of all branches
                var sourceItems = BranchOrganizer.OrganizeBranches(_allSourceBranches);
                var targetItems = BranchOrganizer.OrganizeBranches(_allTargetBranches);

                // Flatten and set items
                var sourceFlatItems = BranchOrganizer.FlattenBranches(sourceItems);
                var targetFlatItems = BranchOrganizer.FlattenBranches(targetItems);

                // Clear and repopulate comboboxes
                SourceBranchBox.Items.Clear();
                foreach (var item in sourceFlatItems)
                {
                    SourceBranchBox.Items.Add(item);
                }

                TargetBranchBox.Items.Clear();
                foreach (var item in targetFlatItems)
                {
                    TargetBranchBox.Items.Add(item);
                }
            }
            catch (Exception ex)
            {
                AppendError($"Error refreshing branch lists: {ex.Message}");
            }
        }

        // Button handler to pull selected branches from origin (mirrors provided bash script behavior)
        private async void PullSelectedBranches_Click(object sender, RoutedEventArgs e)
        {
            var cts = new CancellationTokenSource();
            // Disable the pull button and show indeterminate progress on it
            btnPullBranches.IsEnabled = false;
            ButtonProgressAssist.SetIsIndeterminate(btnPullBranches, true);

            try
            {
                var successList = new List<string>();
                var skipList = new List<string>();
                var failList = new List<string>();
                var repoPaths = GetSelectedRepositories();
                var sourceBranches = GetSelectedBranchesFromComboBox(SourceBranchBox);

                AppendOutput(LogFormatter.FormatOperationStart("PULL / FETCH OPERATION"), true);

                if (sourceBranches.Count == 0)
                {
                    AppendError("❌ No source branches selected. Please select at least one source branch.");
                    return;
                }

                foreach (var repo in repoPaths)
                {
                    AppendOutput(LogFormatter.FormatProjectHeader(System.IO.Path.GetFileName(repo), repo), true);
                    if (!System.IO.Directory.Exists(repo) || !System.IO.Directory.Exists(System.IO.Path.Combine(repo, ".git")))
                    {
                        AppendError($"❌ Repository path not found or not a git repo: {repo}");
                        failList.Add($"{repo} | pull failed | repo not found");
                        continue;
                    }

                    var repoSuccessList = new List<string>();
                    var repoSkipList = new List<string>();
                    var repoFailList = new List<string>();

                    foreach (var branch in sourceBranches)
                    {
                        try
                        {
                            var result = await GitHelper.EnsureBranchLatestAsync(repo, branch, cts.Token);
                            if (result.IsSuccess)
                            {
                                // detect skip vs update/create from message
                                if (!string.IsNullOrEmpty(result.Message) && result.Message.Contains("already up-to-date", StringComparison.OrdinalIgnoreCase))
                                {
                                    AppendOutput($"⏭ {result.Message}");
                                    skipList.Add($"{repo} | {branch}");
                                    repoSkipList.Add(branch);
                                }
                                else
                                {
                                    AppendOutput($"✔ {result.Message}");
                                    successList.Add($"{repo} | {branch}");
                                    repoSuccessList.Add(branch);
                                    AddBranchToRecentAndUi(branch);
                                }
                            }
                            else
                            {
                                AppendError($"❌ {result.Message}");
                                failList.Add($"{repo} | {branch} | {result.Message}");
                                repoFailList.Add(branch);
                            }
                        }
                        catch (Exception ex)
                        {
                            AppendError($"❌ Exception while updating '{branch}': {ex.Message}");
                            failList.Add($"{repo} | {branch} | exception");
                            repoFailList.Add(branch);
                        }
                    }

                    // Project-level summary
                    AppendOutput(LogFormatter.FormatProjectSummary(System.IO.Path.GetFileName(repo), repoSuccessList, repoSkipList, repoFailList),true);
                }

                // Final summary for pull operation
                AppendOutput(LogFormatter.FormatFinalSummary("PULL / FETCH OPERATION", successList, skipList, failList),true);
                AppendOutput("🎉 Pull operation finished.\n");
            }
            finally
            {
                ButtonProgressAssist.SetIsIndeterminate(btnPullBranches, false);
                btnPullBranches.IsEnabled = true;
            }
        }

        private void RefreshBranches_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                btnRefreshBranches.IsEnabled = false;
                ButtonProgressAssist.SetIsIndeterminate(btnRefreshBranches, true);

                // Clear the tracked checked branches
                _checkedSourceBranches.Clear();
                _checkedTargetBranches.Clear();

                // Clear all branch collections to rebuild them
                _allSourceBranches.Clear();
                _allTargetBranches.Clear();

                // Preserve current branch text values so user's explicit selections aren't lost
                var prevSource = SourceBranchBox?.Text;
                var prevTarget = TargetBranchBox?.Text;

                // Unselect any checked repositories in the dynamic panel
                try
                {
                    foreach (var child in DynamicRepoPanel.Children)
                    {
                        if (child is CheckBox cb)
                            cb.IsChecked = false;
                    }
                }
                catch { }

                // Clear selection in the persisted list display
                try { if (lstRepos != null) lstRepos.SelectedIndex = -1; } catch { }

                // Clear branch items to refresh
                SourceBranchBox.Items.Clear();
                TargetBranchBox.Items.Clear();

                // Clear selection index but restore typed/selected text so the dropdown value remains
                try
                {
                    if (SourceBranchBox != null)
                    {
                        SourceBranchBox.SelectedIndex = -1;
                        if (!string.IsNullOrWhiteSpace(prevSource))
                        {
                            SourceBranchBox.Text = prevSource;
                        }
                    }
                    if (TargetBranchBox != null)
                    {
                        TargetBranchBox.SelectedIndex = -1;
                        if (!string.IsNullOrWhiteSpace(prevTarget))
                        {
                            TargetBranchBox.Text = prevTarget;
                        }
                    }
                }
                catch { }

                AppendOutput("UI refreshed (selections preserved where possible).");
            }
            catch (Exception ex)
            {
                AppendError($"Failed to refresh UI: {ex.Message}");
            }
            finally
            {
                ButtonProgressAssist.SetIsIndeterminate(btnRefreshBranches, false);
                btnRefreshBranches.IsEnabled = true;
            }
        }

        #region Private methods
        private List<string> GetClientBranch()
        {
            // Client branch selection removed; callers should use explicit TargetBranchBox instead.
            return new List<string>();
        }
        private List<string> GetSelectedEnvironments()
        {
            // Environment selection removed; return empty list. Use TargetBranchBox for explicit target.
            return new List<string>();
        }
        private List<string> GetSelectedBranchesFromComboBox(System.Windows.Controls.ComboBox comboBox)
        {
            var selected = new List<string>();
            try
            {
                if (comboBox == null) return selected;

                // Use the tracked collections instead of searching the visual tree
                // (visual tree search fails because WPF virtualizes ComboBox items)
                if (ReferenceEquals(comboBox, SourceBranchBox))
                {
                    selected.AddRange(_checkedSourceBranches);
                }
                else if (ReferenceEquals(comboBox, TargetBranchBox))
                {
                    selected.AddRange(_checkedTargetBranches);
                }
            }
            catch { }
            return selected;
        }

        // Helper to find all visual children of a specific type
        private List<T> FindAllVisualChildren<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            var children = new List<T>();
            if (parent == null) return children;

            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    children.Add(typedChild);

                children.AddRange(FindAllVisualChildren<T>(child));
            }
            return children;
        }

        // Helper to find visual child of a specific type
        private T? FindVisualChild<T>(System.Windows.DependencyObject parent) where T : System.Windows.DependencyObject
        {
            if (parent == null) return null;
            
            int childCount = System.Windows.Media.VisualTreeHelper.GetChildrenCount(parent);
            for (int i = 0; i < childCount; i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(parent, i);
                if (child is T typedChild)
                    return typedChild;
                
                var result = FindVisualChild<T>(child);
                if (result != null)
                    return result;
            }
            return null;
        }

        private List<string> GetSelectedRepositories()
        {
            var selectedRepos = new List<string>();

            try
            {
                foreach (var child in DynamicRepoPanel.Children)
                {
                    if (child is CheckBox cb && cb.IsChecked == true && cb.Tag is string path && !string.IsNullOrWhiteSpace(path))
                        selectedRepos.Add(path);
                }
            }
            catch { }

            return selectedRepos;
        }

        private bool PanelHasCheckedCheckBox(System.Windows.Controls.Panel panel)
        {
            foreach (var child in panel.Children)
            {
                if (child is CheckBox cb && cb.IsChecked == true)
                    return true;

                if (child is System.Windows.Controls.Panel innerPanel && PanelHasCheckedCheckBox(innerPanel))
                    return true;

                // cover ContentControls that may host a CheckBox (e.g. WrapPanel items)
                if (child is System.Windows.Controls.ContentControl contentControl && contentControl.Content is CheckBox contentCb && contentCb.IsChecked == true)
                    return true;
            }
            return false;
        }

        #endregion

        private void MetroWindow_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            // save quick settings
            try
            {
                _settings.LogFontSize = OutputBox.FontSize;
                _settings.LogMaxChars = _settings.LogMaxChars; // already in settings
                _settings.Save();
            }
            catch { }
            App.Current.Shutdown();
        }

        private void ClearErrorBox_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                if (OutputBox != null) OutputBox.Document.Blocks.Clear();
                _outputMaster.Clear();
                _errorMaster.Clear();
            }
            catch { }
        }

        // Navigation + highlighting helpers
        private void HighlightOutputMatch(int matchIndex)
        {
            // Output matching removed - single log output only
        }

        private void HighlightErrorMatch(int matchIndex)
        {
            // Error matching removed - single log output only
        }

        private System.Windows.Documents.TextPointer? GetTextPointerAt(System.Windows.Controls.RichTextBox box, int charIndex)
        {
            try
            {
                var navigator = box.Document.ContentStart;
                int cnt = 0;
                while (navigator != null && navigator.CompareTo(box.Document.ContentEnd) < 0)
                {
                    if (navigator.GetPointerContext(System.Windows.Documents.LogicalDirection.Forward) == System.Windows.Documents.TextPointerContext.Text)
                    {
                        string? run = navigator.GetTextInRun(System.Windows.Documents.LogicalDirection.Forward);
                        if (!string.IsNullOrEmpty(run))
                        {
                            if (cnt + run.Length >= charIndex)
                            {
                                return navigator.GetPositionAtOffset(charIndex - cnt);
                            }
                            cnt += run.Length;
                        }
                    }
                    navigator = navigator.GetNextContextPosition(System.Windows.Documents.LogicalDirection.Forward);
                }
            }
            catch { }
            return null;
        }

        private void PrevOutputMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_outputMatchIndexes.Count == 0) return;
            _outputCurrentMatch = (_outputCurrentMatch - 1 + _outputMatchIndexes.Count) % _outputMatchIndexes.Count;
            HighlightOutputMatch(_outputCurrentMatch);
        }

        private void NextOutputMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_outputMatchIndexes.Count == 0) return;
            _outputCurrentMatch = (_outputCurrentMatch + 1) % _outputMatchIndexes.Count;
            HighlightOutputMatch(_outputCurrentMatch);
        }

        private void PrevErrorMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_errorMatchIndexes.Count == 0) return;
            _errorCurrentMatch = (_errorCurrentMatch - 1 + _errorMatchIndexes.Count) % _errorMatchIndexes.Count;
            HighlightErrorMatch(_errorCurrentMatch);
        }

        private void NextErrorMatch_Click(object sender, RoutedEventArgs e)
        {
            if (_errorMatchIndexes.Count == 0) return;
            _errorCurrentMatch = (_errorCurrentMatch + 1) % _errorMatchIndexes.Count;
            HighlightErrorMatch(_errorCurrentMatch);
        }

        private void ChkFilterChanged(object sender, RoutedEventArgs e)
        {
            // Filter functionality removed - single log output
        }

        private void AppendOutput(string text, bool status = false)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = "";
            if (status)
                line = $"{text}";
            else
                line = $"[{timestamp}] {text}";
            _outputQueue.Enqueue(line);
            _outputMaster.Append(line);
        }

        private void AppendError(string text)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var line = $"[{timestamp}] {text}";
            _errorQueue.Enqueue(line);
            _errorMaster.Append(line);
        }

        private void FlushLogQueues()
        {
            try
            {
                // Batch both queues together for more efficient rendering
                var hasOutput = _outputQueue.Count > 0;
                var hasError = _errorQueue.Count > 0;
                
                if (!hasOutput && !hasError) return;

                var combinedSb = new StringBuilder();

                // Flush output queue
                if (hasOutput)
                {
                    while (_outputQueue.TryDequeue(out var item))
                        combinedSb.Append(item);
                }

                // Flush error queue
                if (hasError)
                {
                    while (_errorQueue.TryDequeue(out var item))
                        combinedSb.Append(item);
                }

                if (combinedSb.Length > 0)
                {
                    var text = combinedSb.ToString();
                    try
                    {
                        if (OutputBox != null)
                        {
                            // Batch update: single call to append both queues
                            AppendToRichTextBox(OutputBox, text, OutputBox.Foreground);
                            
                            // Trim master buffers
                            var max = _settings?.LogMaxChars > 0 ? _settings.LogMaxChars : MaxLogChars;
                            if (_outputMaster.Length > max)
                                _outputMaster.Remove(0, _outputMaster.Length - max);
                            if (_errorMaster.Length > max)
                                _errorMaster.Remove(0, _errorMaster.Length - max);
                        }

                        if (_streamLogsToFile && _logFileWriter != null)
                        {
                            _logFileWriter.Write(text);
                            _logFileWriter.Flush();
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

        private void OpenLogWindow_Click(object sender, RoutedEventArgs e)
        {
            ToggleLogs();
        }

        private void ToggleLogs_Click(object sender, RoutedEventArgs e)
        {
            ToggleLogs();
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

        private void chkAutoOpen_Checked(object sender, RoutedEventArgs e)
        {
            _autoOpenLogs = true;
            _settings.AutoOpenLogs = true;
            _settings.Save();
        }

        private void chkAutoOpen_Unchecked(object sender, RoutedEventArgs e)
        {
            _autoOpenLogs = false;
            _settings.AutoOpenLogs = false;
            _settings.Save();
        }

        private void chkStreamLogs_Checked(object sender, RoutedEventArgs e)
        {
            _settings.StreamLogs = true;
            _settings.Save();
        }

        private void chkStreamLogs_Unchecked(object sender, RoutedEventArgs e)
        {
            StopStreamLogs();
            _settings.StreamLogs = false;
            _settings.Save();
        }

        private void SelectLogPath_Click(object sender, RoutedEventArgs e) => SelectLogPath();

        private void SelectLogPath()
        {
            var dlg = new Microsoft.Win32.SaveFileDialog()
            {
                Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                DefaultExt = "txt",
                FileName = "mergepilot.log"
            };

            if (dlg.ShowDialog(this) == true)
            {
                _settings.LogFilePath = dlg.FileName;
                _settings.Save();
            }
        }

        private void SaveLogs_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var dlg = new Microsoft.Win32.SaveFileDialog()
                {
                    Filter = "Text Files (*.txt)|*.txt|All Files (*.*)|*.*",
                    DefaultExt = "txt",
                    FileName = "mergepilot_logs.txt"
                };

                if (dlg.ShowDialog(this) == true)
                {
                    var combined = new StringBuilder();
                    combined.Append(SectionSeparator + "\n");
                    combined.Append("--- OUTPUT ---\n");
                    combined.Append(SectionSeparator + "\n");
                    combined.Append(_outputMaster.ToString());
                    combined.Append(SectionSeparator + "\n");
                    combined.Append("--- ERRORS ---\n");
                    combined.Append(SectionSeparator + "\n");
                    combined.Append(_errorMaster.ToString());
                    System.IO.File.WriteAllText(dlg.FileName, combined.ToString());
                }
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
                var combined = new StringBuilder();
                combined.Append(SectionSeparator + "\n");
                combined.Append("--- OUTPUT ---\n");
                combined.Append(SectionSeparator + "\n");
                combined.Append(_outputMaster.ToString());
                combined.Append(SectionSeparator + "\n");
                combined.Append("--- ERRORS ---\n");
                combined.Append(SectionSeparator + "\n");
                combined.Append(_errorMaster.ToString());
                System.Windows.Clipboard.SetText(combined.ToString());
            }
            catch { }
        }

        // Keyboard shortcuts: Save (Ctrl+S), Copy (Ctrl+C), Clear (Ctrl+K)
        private void RegisterLogShortcuts()
        {
            try
            {
                this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => SaveLogs_Click(null, null)), Key.S, ModifierKeys.Control));
                this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => CopyLogs_Click(null, null)), Key.D, ModifierKeys.Control | ModifierKeys.Shift));
                this.InputBindings.Add(new KeyBinding(new RelayCommand(_ => ClearErrorBox_Click(null, null)), Key.K, ModifierKeys.Control));
            }
            catch { }
        }


        // Old detached LogWindow removed; inline logs used instead

        private void GenerateAndLogSummary(string title, List<string> success, List<string> skipped, List<string> failed)
        {
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"========== {title} ==========\n");
            sb.Append(SectionSeparator + "\n");
            sb.Append($"Timestamp: {DateTime.Now:yyyy-MM-dd HH:mm:ss}\n");
            sb.Append($"✅ SUCCESSFUL ({success.Count}):\n");
            if (success.Count > 0)
            {
                foreach (var s in success) sb.Append($" ✔ {s}\n");
            }

            sb.Append($"⏭ SKIPPED ({skipped.Count}):\n");
            if (skipped.Count > 0)
            {
                foreach (var s in skipped) sb.Append($" ⏭ {s}\n");
            }

            sb.Append($"❌ FAILED ({failed.Count}):\n");
            if (failed.Count > 0)
            {
                foreach (var f in failed) sb.Append($" ❌ {f}\n");
            }

            var summary = sb.ToString();
            AppendOutput(summary);
            AppendError(summary);
        }

        // Toggle streaming to files
        private void StartStreamLogs(string path)
        {
            try
            {
                _logFileWriter?.Dispose();
                _logFileWriter = new StreamWriter(new FileStream(path, FileMode.Append, FileAccess.Write, FileShare.Read)) { AutoFlush = true };
                _streamLogsToFile = true;
            }
            catch { }
        }

        private void StopStreamLogs()
        {
            try
            {
                _logFileWriter?.Flush();
                _logFileWriter?.Dispose();
                _logFileWriter = null;
                _streamLogsToFile = false;
            }
            catch { }
        }

        // Add this private helper method to MainWindow class to fix CS0103 for AppendToRichTextBox

        private void AppendToRichTextBox(System.Windows.Controls.RichTextBox box, string text, System.Windows.Media.Brush? foreground)
        {
            try
            {
                if (box?.Document == null) return;

                // Use batch mode for better rendering performance
                box.BeginChange();
                
                // Split text into lines and apply appropriate colors based on content
                var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
                
                int linesAdded = 0;
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line)) continue;

                    // Determine color based on line content
                    System.Windows.Media.Brush lineColor = foreground ?? System.Windows.Media.Brushes.White;

                    // Success patterns (GREEN)
                    if (line.Contains("✅") || line.Contains("✔") || line.Contains("SUCCESSFUL") || 
                        line.Contains("Updated") || line.Contains("created") || line.Contains("Successfully"))
                    {
                        lineColor = ColorScheme.SuccessBrush;
                    }
                    // Warning/Skipped patterns (YELLOW)
                    else if (line.Contains("⏭") || line.Contains("SKIPPED") || line.Contains("skipped") || 
                             line.Contains("already up-to-date") || line.Contains("⚠") || line.Contains("Warning"))
                    {
                        lineColor = ColorScheme.WarningBrush;
                    }
                    // Error/Failed patterns (RED)
                    else if (line.Contains("❌") || line.Contains("FAILED") || line.Contains("failed") || 
                             line.Contains("Error") || line.Contains("error") || line.Contains("Exception"))
                    {
                        lineColor = ColorScheme.ErrorBrush;
                    }
                    // Info patterns (BLUE)
                    else if (line.Contains("🚀") || line.Contains("📁") || line.Contains("🔀") || 
                             line.Contains("📥") || line.Contains("Processing") || line.Contains("Attempt") ||
                             line.Contains("Timestamp") || line.Contains("Path:") || line.Contains("🔄"))
                    {
                        lineColor = ColorScheme.InfoBrush;
                    }

                    // Create paragraph once and append
                    var para = new System.Windows.Documents.Paragraph();
                    para.Margin = new System.Windows.Thickness(0);
                    para.Padding = new System.Windows.Thickness(0);
                    para.LineHeight = 1.0;

                    var run = new System.Windows.Documents.Run(line);
                    run.Foreground = lineColor;
                    para.Inlines.Add(run);
                    
                    box.Document.Blocks.Add(para);
                    linesAdded++;
                    _displayedLineCount++;
                }

                box.EndChange();

                // Trigger smooth scroll to end
                _targetScrollOffset = double.MaxValue;
                
                // Periodically cull old lines to maintain performance (keep recent logs)
                if (_displayedLineCount > MaxDisplayedLines)
                {
                    CullOldLogLines(box);
                }
            }
            catch { }
        }

        private void CullOldLogLines(System.Windows.Controls.RichTextBox box)
        {
            try
            {
                var linesToRemove = _displayedLineCount - (MaxDisplayedLines - 100);
                if (linesToRemove <= 0) return;

                box.BeginChange();
                for (int i = 0; i < linesToRemove && box.Document.Blocks.Count > 0; i++)
                {
                    var firstBlock = box.Document.Blocks.FirstBlock;
                    if (firstBlock != null)
                        box.Document.Blocks.Remove(firstBlock);
                }
                box.EndChange();

                _displayedLineCount = box.Document.Blocks.Count;
            }
            catch { }
        }

        private void UpdateSmoothScroll()
        {
            try
            {
                if (OutputBox?.Document == null) return;
                var scrollViewer = FindScrollViewer(OutputBox);
                if (scrollViewer == null) return;

                var scrollableHeight = scrollViewer.ScrollableHeight;
                if (scrollableHeight <= 0) return;

                var currentOffset = scrollViewer.VerticalOffset;
                
                if (_targetScrollOffset == double.MaxValue)
                {
                    _targetScrollOffset = scrollableHeight;
                }

                // Smooth & slow scroll with easing animation
                var diff = _targetScrollOffset - currentOffset;
                
                if (Math.Abs(diff) > 0.5)
                {
                    // Very smooth easing: only move 6% of remaining distance per frame
                    // This creates a slow, buttery smooth scroll effect
                    var easeAmount = diff * 0.06; // Slow easing multiplier
                    scrollViewer.ScrollToVerticalOffset(currentOffset + easeAmount);
                }
                else if (Math.Abs(diff) > 0)
                {
                    // Final snap to target when very close
                    scrollViewer.ScrollToEnd();
                }
            }
            catch { }
        }

        private System.Windows.Controls.ScrollViewer? FindScrollViewer(System.Windows.DependencyObject obj)
        {
            for (int i = 0; i < System.Windows.Media.VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = System.Windows.Media.VisualTreeHelper.GetChild(obj, i);
                if (child is System.Windows.Controls.ScrollViewer sv) return sv;
                var found = FindScrollViewer(child);
                if (found != null) return found;
            }
            return null;
        }
    }
}