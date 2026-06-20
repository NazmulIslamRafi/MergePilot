using System.Windows;
using System.Windows.Input;
using MergePilot.ViewModels;

namespace MergePilot
{
    public partial class AddRepositoryDialog : Window
    {
        private readonly AppSettings.RepositoryEntry? _editingRepo;
        private readonly IFilePickerService _filePickers;

        public string RepoName { get; set; } = string.Empty;
        public string RepoPath { get; set; } = string.Empty;
        public string RemoteUrl { get; set; } = string.Empty;
        public ICommand BrowseFolderCommand { get; }
        public ICommand SaveCommand { get; }
        public ICommand CancelCommand { get; }

        public AddRepositoryDialog(
            AppSettings.RepositoryEntry? existingRepo = null,
            IFilePickerService? filePickers = null)
        {
            _filePickers = filePickers ?? new WpfFilePickerService();
            BrowseFolderCommand = new RelayCommand(_ => BrowseFolder());
            SaveCommand = new RelayCommand(_ => Save());
            CancelCommand = new RelayCommand(_ => Cancel());

            InitializeComponent();
            _editingRepo = existingRepo;

            if (_editingRepo != null)
            {
                RepoNameTextBox.Text = _editingRepo.Name;
                RepoPathTextBox.Text = _editingRepo.Path;
                RemoteUrlTextBox.Text = _editingRepo.RemoteUrl;
                this.Title = "Edit Repository";
            }
        }

        private void BrowseFolder()
        {
            var selectedPath = _filePickers.SelectFolder(
                "Select a Git repository folder (must contain .git)",
                showNewFolderButton: false);
            if (!string.IsNullOrWhiteSpace(selectedPath))
            {
                RepoPathTextBox.Text = selectedPath;
                ErrorMessage.Text = "";
            }
        }

        private void Save()
        {
            ErrorMessage.Text = "";

            // Validate repository name
            if (!InputValidator.IsValidRepositoryName(RepoNameTextBox.Text))
            {
                ErrorMessage.Text = "Repository name is required (max 255 characters).";
                return;
            }

            // Validate repository path
            var repoPath = RepoPathTextBox.Text;
            if (!InputValidator.IsValidRepositoryPath(repoPath))
            {
                ErrorMessage.Text = InputValidator.GetRepositoryPathError(repoPath);
                return;
            }

            // Set result
            RepoName = RepoNameTextBox.Text;
            RepoPath = repoPath;
            RemoteUrl = RemoteUrlTextBox.Text;

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
