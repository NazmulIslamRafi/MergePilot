using System;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using MahApps.Metro.Controls;
using MergePilot.ViewModels;

namespace MergePilot
{
    /// <summary>
    /// Repository and branch manager window.
    /// </summary>
    public partial class RepositoryManager : MetroWindow
    {
        private AppSettings _settings = new();
        private readonly RepositoryConfigurationService _configurationService = new();
        private readonly RepositoryManagerViewModel _viewModel;
        private readonly IUserDialogService _dialogs = new WpfUserDialogService();
        private System.Threading.CancellationTokenSource _commitLoadCts = new();

        public RepositoryManager()
        {
            InitializeComponent();

            _viewModel = new RepositoryManagerViewModel();
            DataContext = _viewModel;
            ConfigureWorkflowCommands();
        }

        public void LoadData(AppSettings settings)
        {
            _settings = settings;
            _viewModel.LoadData(settings);
            _ = LoadBranchCommitInfoAsync();
        }

        private void ConfigureWorkflowCommands()
        {
            _viewModel.ConfigureWorkflowActions(new RepositoryManagerWorkflowActions
            {
                AddRepository = AddRepository,
                EditRepository = EditRepository,
                DeleteRepository = DeleteRepository,
                RefreshBranches = RefreshBranches,
                AddBranch = AddBranch,
                EditBranch = EditBranch,
                DeleteBranch = DeleteBranch,
                Close = Close
            });
        }

        private void AddRepository()
        {
            var dialog = new AddRepositoryDialog { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var result = _configurationService.AddRepository(
                _settings,
                dialog.RepoName,
                dialog.RepoPath,
                dialog.RemoteUrl,
                RepositoryPathRequirement.RequireGitRepository);
            if (!result.IsSuccess)
            {
                _dialogs.ShowError(result.Message, "Invalid Path");
                return;
            }

            _viewModel.RefreshRepositories();
            _dialogs.ShowInformation($"Repository '{dialog.RepoName}' added successfully!", "Success");
        }

        private void EditRepository(AppSettings.RepositoryEntry? repository)
        {
            if (repository == null)
                return;

            var dialog = new AddRepositoryDialog(repository) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var result = _configurationService.UpdateRepository(
                _settings,
                _settings.Repositories.IndexOf(repository),
                dialog.RepoName,
                dialog.RepoPath,
                dialog.RemoteUrl,
                RepositoryPathRequirement.RequireGitRepository);
            if (!result.IsSuccess)
            {
                _dialogs.ShowError(result.Message, "Invalid Repository");
                return;
            }

            _viewModel.RefreshRepositories();
        }

        private void DeleteRepository(AppSettings.RepositoryEntry? repository)
        {
            if (repository == null)
                return;

            if (!_dialogs.ConfirmYesNo($"Are you sure you want to delete '{repository.Name}'?", "Confirm Delete"))
                return;

            var removeResult = _configurationService.RemoveRepository(
                _settings,
                _settings.Repositories.IndexOf(repository));
            if (!removeResult.IsSuccess)
            {
                _dialogs.ShowError(removeResult.Message, "Invalid Repository");
                return;
            }

            _viewModel.RefreshRepositories();
        }

        private async void RefreshBranches()
        {
            await RefreshBranchesAsync().ConfigureAwait(false);
        }

        private async Task RefreshBranchesAsync()
        {
            _viewModel.SetRefreshingBranches(true);

            try
            {
                var result = await _configurationService
                    .RefreshBranchesFromRepositoriesAsync(_settings, _viewModel.Repositories)
                    .ConfigureAwait(false);

                InvokeOnUiThread(_viewModel.RefreshBranches);

                if (result.FailedRepositories > 0)
                {
                    var message = $"Branches refreshed with {result.FailedRepositories} repository failure(s).";
                    if (result.Errors.Count > 0)
                        message += $"{Environment.NewLine}{result.Errors[0]}";

                    InvokeOnUiThread(() =>
                        _dialogs.ShowWarning(message, "Refresh Branches"));
                }
                else
                {
                    InvokeOnUiThread(() =>
                        _dialogs.ShowInformation(
                            $"Branches refreshed successfully! Added {result.AddedBranches} new branch(es).",
                            "Success"));
                }
            }
            catch (Exception ex)
            {
                InvokeOnUiThread(() =>
                    _dialogs.ShowError($"Error refreshing branches: {ex.Message}", "Error"));
            }
            finally
            {
                InvokeOnUiThread(() => _viewModel.SetRefreshingBranches(false));
            }
        }

        private void InvokeOnUiThread(Action action)
        {
            if (Dispatcher.CheckAccess())
                action();
            else
                Dispatcher.Invoke(action);
        }

        private async void AddBranch()
        {
            var dialog = new AddBranchDialog(_settings.Repositories.ToList()) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var result = _configurationService.AddBranch(
                _settings, dialog.BranchName, dialog.SelectedRepository, dialog.IsManual);
            if (!result.IsSuccess)
            {
                _dialogs.ShowError(result.Message, "Duplicate Branch");
                return;
            }

            _viewModel.RefreshBranches();
            _ = LoadBranchCommitInfoAsync();

            if (dialog.PushToRemote)
                await PushBranchToRemoteAsync(dialog.BranchName, dialog.SelectedRepository);
        }

        private async void EditBranch(AppSettings.BranchEntry? branch)
        {
            if (branch == null)
                return;

            var dialog = new AddBranchDialog(_settings.Repositories.ToList(), branch) { Owner = this };
            if (dialog.ShowDialog() != true)
                return;

            var result = _configurationService.UpdateBranch(
                _settings,
                branch,
                dialog.BranchName,
                dialog.SelectedRepository,
                dialog.IsManual);
            if (!result.IsSuccess)
            {
                _dialogs.ShowError(result.Message, "Duplicate Branch");
                return;
            }

            _viewModel.RefreshBranches();
            _ = LoadBranchCommitInfoAsync();

            if (dialog.PushToRemote)
                await PushBranchToRemoteAsync(dialog.BranchName, dialog.SelectedRepository);
        }

        private async Task PushBranchToRemoteAsync(string branchName, string repoName)
        {
            var repoPath = _settings.Repositories
                .FirstOrDefault(r => string.Equals(r.Name, repoName, StringComparison.OrdinalIgnoreCase))
                ?.Path;

            if (string.IsNullOrWhiteSpace(repoPath))
            {
                _dialogs.ShowError($"Could not find path for repository '{repoName}'.", "Push Failed");
                return;
            }

            var pushResult = await GitHelper.PushBranchAsync(repoPath, branchName).ConfigureAwait(true);
            if (pushResult.IsSuccess)
                _dialogs.ShowInformation($"Branch '{branchName}' pushed to remote successfully.", "Push Successful");
            else
                _dialogs.ShowError($"Push failed for '{branchName}':\n{pushResult.StdErr}", "Push Failed");
        }

        private void DeleteBranch(AppSettings.BranchEntry? branch)
        {
            if (branch == null)
                return;

            if (!_dialogs.ConfirmYesNo($"Are you sure you want to delete '{branch.BranchName}'?", "Confirm Delete"))
                return;

            var removeResult = _configurationService.RemoveBranch(_settings, branch);
            if (!removeResult.IsSuccess)
            {
                _dialogs.ShowError(removeResult.Message, "Invalid Branch");
                return;
            }

            _viewModel.RefreshBranches();
            _ = LoadBranchCommitInfoAsync();
        }

        private async System.Threading.Tasks.Task LoadBranchCommitInfoAsync()
        {
            _commitLoadCts.Cancel();
            _commitLoadCts = new System.Threading.CancellationTokenSource();
            var ct = _commitLoadCts.Token;

            // Snapshot the current branch view-models so we work on a stable list
            var items = _viewModel.Branches.ToList();

            foreach (var bvm in items)
            {
                if (ct.IsCancellationRequested) break;

                var repoPath = _settings.Repositories
                    .FirstOrDefault(r => string.Equals(r.Name, bvm.Repository, StringComparison.OrdinalIgnoreCase))
                    ?.Path;

                if (string.IsNullOrWhiteSpace(repoPath) || string.IsNullOrWhiteSpace(bvm.BranchName))
                    continue;

                try
                {
                    var info = await GitHelper.GetLastCommitForBranchAsync(repoPath, bvm.BranchName!, ct)
                        .ConfigureAwait(true);

                    if (!ct.IsCancellationRequested)
                        bvm.LastCommitInfo = info;
                }
                catch (OperationCanceledException) { break; }
                catch { /* silently skip if git fails for this branch */ }
            }
        }
    }
}
