using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Input;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// ViewModel for the repository and custom branch manager dialog.
    /// </summary>
    public class RepositoryManagerViewModel : ViewModelBase
    {
        private AppSettings _settings = new();
        private RepositoryManagerWorkflowActions _workflowActions = RepositoryManagerWorkflowActions.Empty;
        private bool _isRefreshingBranches;
        private string _branchSearchText = string.Empty;
        private string _repoSearchText = string.Empty;

        public RepositoryManagerViewModel()
        {
            Repositories = new ObservableCollection<AppSettings.RepositoryEntry>();
            Branches = new ObservableCollection<BranchEntryViewModel>();

            AddRepositoryCommand = new RelayCommand(_ => _workflowActions.AddRepository());
            EditRepositoryCommand = new RelayCommand(parameter =>
                _workflowActions.EditRepository(parameter as AppSettings.RepositoryEntry));
            DeleteRepositoryCommand = new RelayCommand(parameter =>
                _workflowActions.DeleteRepository(parameter as AppSettings.RepositoryEntry));
            RefreshBranchesCommand = new RelayCommand(
                _ => _workflowActions.RefreshBranches(),
                _ => CanRefreshBranches);
            AddBranchCommand = new RelayCommand(_ => _workflowActions.AddBranch());
            EditBranchCommand = new RelayCommand(parameter =>
                _workflowActions.EditBranch((parameter as BranchEntryViewModel)?.Entry));
            DeleteBranchCommand = new RelayCommand(parameter =>
                _workflowActions.DeleteBranch((parameter as BranchEntryViewModel)?.Entry));
            CloseCommand = new RelayCommand(_ => _workflowActions.Close());
        }

        public ObservableCollection<AppSettings.RepositoryEntry> Repositories { get; }
        public ObservableCollection<BranchEntryViewModel> Branches { get; }

        public string BranchSearchText
        {
            get => _branchSearchText;
            set { if (SetProperty(ref _branchSearchText, value)) RefreshBranches(); }
        }

        public string RepoSearchText
        {
            get => _repoSearchText;
            set { if (SetProperty(ref _repoSearchText, value)) RefreshRepositories(); }
        }

        public bool IsRefreshingBranches
        {
            get => _isRefreshingBranches;
            private set
            {
                if (SetProperty(ref _isRefreshingBranches, value))
                {
                    OnPropertyChanged(nameof(CanRefreshBranches));
                    OnPropertyChanged(nameof(RefreshBranchesButtonText));
                    CommandManager.InvalidateRequerySuggested();
                }
            }
        }

        public bool CanRefreshBranches => !IsRefreshingBranches;

        public string RefreshBranchesButtonText => IsRefreshingBranches
            ? "Refreshing..."
            : "Refresh Branches";

        public ICommand AddRepositoryCommand { get; }
        public ICommand EditRepositoryCommand { get; }
        public ICommand DeleteRepositoryCommand { get; }
        public ICommand RefreshBranchesCommand { get; }
        public ICommand AddBranchCommand { get; }
        public ICommand EditBranchCommand { get; }
        public ICommand DeleteBranchCommand { get; }
        public ICommand CloseCommand { get; }

        public void ConfigureWorkflowActions(RepositoryManagerWorkflowActions workflowActions)
        {
            _workflowActions = workflowActions ?? RepositoryManagerWorkflowActions.Empty;
        }

        public void LoadData(AppSettings settings)
        {
            _settings = settings ?? new AppSettings();
            RefreshRepositories();
            RefreshBranches();
        }

        public void RefreshRepositories()
        {
            Repositories.Clear();
            var repos = _settings.Repositories ?? Enumerable.Empty<AppSettings.RepositoryEntry>();
            if (!string.IsNullOrWhiteSpace(_repoSearchText))
            {
                repos = repos.Where(r =>
                    r.Name?.Contains(_repoSearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    r.Path?.Contains(_repoSearchText, StringComparison.OrdinalIgnoreCase) == true);
            }
            foreach (var repository in repos)
                Repositories.Add(repository);
        }

        public void RefreshBranches()
        {
            Branches.Clear();
            var branches = _settings.CustomBranches ?? Enumerable.Empty<AppSettings.BranchEntry>();
            if (!string.IsNullOrWhiteSpace(_branchSearchText))
            {
                branches = branches.Where(b =>
                    b.BranchName?.Contains(_branchSearchText, StringComparison.OrdinalIgnoreCase) == true ||
                    b.Repository?.Contains(_branchSearchText, StringComparison.OrdinalIgnoreCase) == true);
            }
            foreach (var branch in branches)
                Branches.Add(new BranchEntryViewModel(branch));
        }

        public IReadOnlyList<string> GetRepositoryNames()
        {
            return Repositories
                .Select(repository => repository.Name)
                .Where(name => !string.IsNullOrWhiteSpace(name))
                .Select(name => name!)
                .ToList();
        }

        public void SetRefreshingBranches(bool isRefreshing)
        {
            IsRefreshingBranches = isRefreshing;
        }
    }
}
