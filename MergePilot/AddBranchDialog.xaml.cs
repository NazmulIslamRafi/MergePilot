using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;
using MergePilot.ViewModels;

namespace MergePilot
{
    public partial class AddBranchDialog : Window
    {
        private readonly AppSettings.BranchEntry? _editingBranch;
        private readonly List<AppSettings.RepositoryEntry> _repositories;
        private CancellationTokenSource _loadCts = new();

        public string BranchName { get; set; } = string.Empty;
        public string SelectedRepository { get; set; } = string.Empty;
        public bool IsManual { get; private set; } = false;
        public bool PushToRemote { get; private set; } = false;

        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddBranchDialog(List<AppSettings.RepositoryEntry> repositories, AppSettings.BranchEntry? existingBranch = null)
        {
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            InitializeComponent();
            _repositories = repositories ?? new List<AppSettings.RepositoryEntry>();
            _editingBranch = existingBranch;

            RepositoryComboBox.ItemsSource = _repositories.Select(r => r.Name).ToList();

            // Attach the SelectionChanged handler FIRST so it triggers loading when editing
            RepositoryComboBox.SelectionChanged += async (s, e) =>
            {
                if (!IsManualMode())
                    await LoadRemoteBranchesAsync();
                else
                    ShowNoRepoPlaceholder(RepositoryComboBox.SelectedItem == null);
            };

            if (_editingBranch != null)
            {
                this.Title = "Edit Branch";

                if (_editingBranch.IsManual)
                {
                    // Manual mode: set checkbox first so SelectionChanged doesn't trigger a load
                    ManualEntryCheckBox.IsChecked = true;
                    SetManualMode(true);
                    RepositoryComboBox.SelectedItem = _editingBranch.Repository;
                    ManualBranchTextBox.Text = _editingBranch.BranchName ?? string.Empty;
                }
                else
                {
                    // Remote mode: selecting the repo triggers SelectionChanged → LoadRemoteBranchesAsync
                    RepositoryComboBox.SelectedItem = _editingBranch.Repository;
                }
            }
            else
            {
                // New branch: no auto-selection — show placeholder until user picks a repo
                ShowNoRepoPlaceholder(true);
            }
        }

        private bool IsManualMode() => ManualEntryCheckBox.IsChecked == true;

        private void ShowNoRepoPlaceholder(bool show)
        {
            if (BranchComboBox != null)
                BranchComboBox.Visibility = show ? Visibility.Collapsed : Visibility.Visible;
            if (NoRepoText != null)
                NoRepoText.Visibility = show ? Visibility.Visible : Visibility.Collapsed;
        }

        public void ManualEntryCheckBox_Changed(object sender, RoutedEventArgs e)
        {
            var isManual = ManualEntryCheckBox.IsChecked == true;
            SetManualMode(isManual);

            if (!isManual && RepositoryComboBox.SelectedItem != null)
                _ = LoadRemoteBranchesAsync();
            else if (!isManual)
                ShowNoRepoPlaceholder(true);
        }

        private void SetManualMode(bool isManual)
        {
            BranchComboBox.Visibility = isManual ? Visibility.Collapsed : Visibility.Visible;
            NoRepoText.Visibility = Visibility.Collapsed;
            LoadingText.Visibility = Visibility.Collapsed;
            LoadingIcon.Visibility = Visibility.Collapsed;
            ManualBranchTextBox.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;
            PushToRemoteCheckBox.Visibility = isManual ? Visibility.Visible : Visibility.Collapsed;

            if (isManual)
                ManualBranchTextBox.Focus();
        }

        private async Task LoadRemoteBranchesAsync()
        {
            _loadCts.Cancel();
            _loadCts = new CancellationTokenSource();
            var ct = _loadCts.Token;

            var repoName = RepositoryComboBox.SelectedItem?.ToString();
            if (string.IsNullOrEmpty(repoName))
            {
                ShowNoRepoPlaceholder(true);
                return;
            }

            var repoEntry = _repositories.FirstOrDefault(r => r.Name == repoName);
            if (repoEntry?.Path == null)
            {
                ShowNoRepoPlaceholder(true);
                return;
            }

            ShowNoRepoPlaceholder(false);
            BranchComboBox.IsEnabled = false;
            BranchComboBox.ItemsSource = null;
            LoadingText.Visibility = Visibility.Visible;
            LoadingIcon.Visibility = Visibility.Visible;
            ErrorMessage.Text = "";

            try
            {
                var branches = await GitHelper.GetRemoteBranchesAsync(repoEntry.Path, "origin", ct)
                    .ConfigureAwait(true);

                if (ct.IsCancellationRequested)
                    return;

                if (branches.Count == 0)
                {
                    ErrorMessage.Text = "No remote branches found.";
                    return;
                }

                BranchComboBox.ItemsSource = branches;

                if (_editingBranch?.BranchName != null && branches.Contains(_editingBranch.BranchName))
                    BranchComboBox.SelectedItem = _editingBranch.BranchName;
                else
                    BranchComboBox.SelectedIndex = 0;
            }
            catch (OperationCanceledException)
            {
                // superseded by a newer load
            }
            catch (Exception ex)
            {
                ErrorMessage.Text = $"Failed to load branches: {ex.Message}";
            }
            finally
            {
                if (!ct.IsCancellationRequested)
                {
                    LoadingText.Visibility = Visibility.Collapsed;
                    LoadingIcon.Visibility = Visibility.Collapsed;
                    BranchComboBox.IsEnabled = true;
                }
            }
        }

        private void Save()
        {
            ErrorMessage.Text = "";

            if (RepositoryComboBox.SelectedItem == null)
            {
                ErrorMessage.Text = "Repository selection is required.";
                return;
            }

            string branchName;

            if (IsManualMode())
            {
                branchName = ManualBranchTextBox.Text.Trim();
                if (string.IsNullOrWhiteSpace(branchName))
                {
                    ErrorMessage.Text = "Branch name is required.";
                    return;
                }

                if (!InputValidator.IsValidBranchName(branchName))
                {
                    ErrorMessage.Text = InputValidator.GetBranchNameError(branchName);
                    return;
                }

                IsManual = true;
                PushToRemote = PushToRemoteCheckBox.IsChecked == true;
            }
            else
            {
                branchName = BranchComboBox.SelectedItem?.ToString()?.Trim() ?? string.Empty;
                if (string.IsNullOrWhiteSpace(branchName))
                {
                    ErrorMessage.Text = "Branch selection is required.";
                    return;
                }

                IsManual = false;
                PushToRemote = false;
            }

            BranchName = branchName;
            SelectedRepository = RepositoryComboBox.SelectedItem?.ToString() ?? string.Empty;

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel()
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
