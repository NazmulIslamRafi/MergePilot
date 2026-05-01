using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;

namespace MergePilot
{
    public partial class AddBranchDialog : Window
    {
        private AppSettings.BranchEntry _editingBranch;
        private List<string> _repositories;

        public string BranchName { get; set; }
        public string SelectedRepository { get; set; }

        public AddBranchDialog(List<string> repositories, AppSettings.BranchEntry existingBranch = null)
        {
            InitializeComponent();
            _repositories = repositories;
            _editingBranch = existingBranch;

            // Populate repository combo
            RepositoryComboBox.ItemsSource = repositories;
            RepositoryComboBox.SelectedIndex = 0;

            if (_editingBranch != null)
            {
                BranchNameTextBox.Text = _editingBranch.BranchName;
                RepositoryComboBox.SelectedItem = _editingBranch.Repository;
                this.Title = "Edit Branch";
            }
        }

        private void Save_Click(object sender, RoutedEventArgs e)
        {
            ErrorMessage.Text = "";

            // Validate inputs
            if (string.IsNullOrWhiteSpace(BranchNameTextBox.Text))
            {
                ErrorMessage.Text = "Branch name is required.";
                return;
            }

            if (RepositoryComboBox.SelectedItem == null)
            {
                ErrorMessage.Text = "Repository selection is required.";
                return;
            }

            BranchName = BranchNameTextBox.Text.Trim();
            SelectedRepository = RepositoryComboBox.SelectedItem.ToString();

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
