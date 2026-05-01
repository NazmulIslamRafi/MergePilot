using System.IO;
using System.Windows;
using System.Windows.Forms;

namespace MergePilot
{
    public partial class AddRepositoryDialog : Window
    {
        private AppSettings.RepositoryEntry _editingRepo;

        public string RepoName { get; set; }
        public string RepoPath { get; set; }
        public string RemoteUrl { get; set; }

        public AddRepositoryDialog(AppSettings.RepositoryEntry existingRepo = null)
        {
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

        private void BrowseFolder_Click(object sender, RoutedEventArgs e)
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Select a Git repository folder (must contain .git)";
                dialog.ShowNewFolderButton = false;

                if (dialog.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    RepoPathTextBox.Text = dialog.SelectedPath;
                    ErrorMessage.Text = "";
                }
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";

            // Validate inputs
            if (string.IsNullOrWhiteSpace(RepoNameTextBox.Text))
            {
                ErrorMessage.Text = "Repository name is required.";
                return;
            }

            if (string.IsNullOrWhiteSpace(RepoPathTextBox.Text))
            {
                ErrorMessage.Text = "Repository path is required.";
                return;
            }

            if (!Directory.Exists(RepoPathTextBox.Text))
            {
                ErrorMessage.Text = "Directory does not exist.";
                return;
            }

            if (!Directory.Exists(Path.Combine(RepoPathTextBox.Text, ".git")))
            {
                ErrorMessage.Text = "Directory does not contain a .git folder. Is this a valid Git repository?";
                return;
            }

            // Set result
            RepoName = RepoNameTextBox.Text;
            RepoPath = RepoPathTextBox.Text;
            RemoteUrl = RemoteUrlTextBox.Text;

            this.DialogResult = true;
            this.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}
