using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using MahApps.Metro.Controls;

namespace MergePilot
{
    /// <summary>
    /// Repository and Branch Manager Window
    /// </summary>
    public partial class RepositoryManager : MetroWindow
    {
        private AppSettings _settings;
        private ObservableCollection<AppSettings.RepositoryEntry> _repositories;
        private ObservableCollection<AppSettings.BranchEntry> _branches;

        public RepositoryManager()
        {
            InitializeComponent();
            _repositories = new ObservableCollection<AppSettings.RepositoryEntry>();
            _branches = new ObservableCollection<AppSettings.BranchEntry>();
        }

        public void LoadData(AppSettings settings)
        {
            _settings = settings;
            RefreshRepositories();
            RefreshBranches();
        }

        private void RefreshRepositories()
        {
            _repositories.Clear();
            if (_settings?.Repositories != null)
            {
                foreach (var repo in _settings.Repositories)
                {
                    _repositories.Add(repo);
                }
            }
            RepositoriesList.ItemsSource = _repositories;
        }

        private void RefreshBranches()
        {
            _branches.Clear();
            if (_settings?.CustomBranches != null)
            {
                foreach (var branch in _settings.CustomBranches)
                {
                    _branches.Add(branch);
                }
            }
            BranchesList.ItemsSource = _branches;
        }

        private void AddRepo_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddRepositoryDialog();
            if (dialog.ShowDialog() == true)
            {
                var newRepo = new AppSettings.RepositoryEntry
                {
                    Name = dialog.RepoName,
                    Path = dialog.RepoPath,
                    RemoteUrl = dialog.RemoteUrl
                };

                // Validate path exists
                if (!Directory.Exists(newRepo.Path) || !Directory.Exists(Path.Combine(newRepo.Path, ".git")))
                {
                    System.Windows.MessageBox.Show("Invalid repository path. Path must exist and contain .git folder.", 
                        "Invalid Path", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }

                _settings.Repositories.Add(newRepo);
                _settings.Save();
                RefreshRepositories();
                System.Windows.MessageBox.Show($"Repository '{newRepo.Name}' added successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }

        private void EditRepo_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var repo = btn?.DataContext as AppSettings.RepositoryEntry;
            if (repo == null) return;

            var dialog = new AddRepositoryDialog(repo);
            if (dialog.ShowDialog() == true)
            {
                repo.Name = dialog.RepoName;
                repo.Path = dialog.RepoPath;
                repo.RemoteUrl = dialog.RemoteUrl;

                _settings.Save();
                RefreshRepositories();
            }
        }

        private void DeleteRepo_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var repo = btn?.DataContext as AppSettings.RepositoryEntry;
            if (repo == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete '{repo.Name}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _settings.Repositories.Remove(repo);
                _settings.Save();
                RefreshRepositories();
            }
        }

        private async void RefreshBranches_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            btn.IsEnabled = false;
            btn.Content = "⏳ Refreshing...";

            try
            {
                foreach (var repo in _repositories)
                {
                    await LoadBranchesFromRepository(repo);
                }
                System.Windows.MessageBox.Show("Branches refreshed successfully!", "Success", MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                System.Windows.MessageBox.Show($"Error refreshing branches: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                btn.IsEnabled = true;
                btn.Content = "🔄 Refresh Branches";
            }
        }

        private async Task LoadBranchesFromRepository(AppSettings.RepositoryEntry repo)
        {
            try
            {
                var branches = await GitHelper.GetRemoteBranchesAsync(repo.Path);
                foreach (var branch in branches)
                {
                    if (!_settings.CustomBranches.Any(b => b.BranchName == branch && b.Repository == repo.Name))
                    {
                        _settings.CustomBranches.Add(new AppSettings.BranchEntry
                        {
                            BranchName = branch,
                            Repository = repo.Name
                        });
                    }
                }
            }
            catch { }
        }

        private void AddBranch_Click(object sender, RoutedEventArgs e)
        {
            var dialog = new AddBranchDialog(_repositories.Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList());
            if (dialog.ShowDialog() == true)
            {
                var newBranch = new AppSettings.BranchEntry
                {
                    BranchName = dialog.BranchName,
                    Repository = dialog.SelectedRepository
                };

                _settings.CustomBranches.Add(newBranch);
                _settings.Save();
                RefreshBranches();
            }
        }

        private void EditBranch_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var branch = btn?.DataContext as AppSettings.BranchEntry;
            if (branch == null) return;

            var dialog = new AddBranchDialog(
                _repositories.Select(r => r.Name).Where(n => !string.IsNullOrWhiteSpace(n)).ToList(),
                branch);
            if (dialog.ShowDialog() == true)
            {
                branch.BranchName = dialog.BranchName;
                branch.Repository = dialog.SelectedRepository;
                _settings.Save();
                RefreshBranches();
            }
        }

        private void DeleteBranch_Click(object sender, RoutedEventArgs e)
        {
            var btn = sender as System.Windows.Controls.Button;
            var branch = btn?.DataContext as AppSettings.BranchEntry;
            if (branch == null) return;

            var result = System.Windows.MessageBox.Show($"Are you sure you want to delete '{branch.BranchName}'?", 
                "Confirm Delete", MessageBoxButton.YesNo, MessageBoxImage.Question);
            if (result == MessageBoxResult.Yes)
            {
                _settings.CustomBranches.Remove(branch);
                _settings.Save();
                RefreshBranches();
            }
        }

        private void Close_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
