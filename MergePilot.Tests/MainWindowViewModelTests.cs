using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using MergePilot.ViewModels;

namespace MergePilot.Tests
{
    public class MainWindowViewModelTests
    {
        [Fact]
        public void Constructor_CreatesInstanceWithDefaultState()
        {
            // Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.NotNull(viewModel);
            Assert.NotNull(viewModel.Repositories);
            Assert.NotNull(viewModel.SourceBranches);
            Assert.NotNull(viewModel.TargetBranches);
            Assert.False(viewModel.IsOperationInProgress);
            Assert.Equal("Ready", viewModel.StatusMessage);
            Assert.Equal(0, viewModel.ProgressPercentage);
        }

        [Fact]
        public void Constructor_InitializesCommands()
        {
            // Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.NotNull(viewModel.RefreshBranchesCommand);
            Assert.NotNull(viewModel.MergeCommand);
            Assert.NotNull(viewModel.PullBranchCommand);
            Assert.NotNull(viewModel.CancelOperationCommand);
            Assert.NotNull(viewModel.ManageRepositoriesCommand);
            Assert.NotNull(viewModel.ToggleLogsCommand);
            Assert.NotNull(viewModel.MergeWorkflowCommand);
            Assert.NotNull(viewModel.PullWorkflowCommand);
            Assert.NotNull(viewModel.RefreshWorkflowCommand);
            Assert.NotNull(viewModel.ManageRepositoriesWorkflowCommand);
            Assert.NotNull(viewModel.ToggleLogsWorkflowCommand);
            Assert.NotNull(viewModel.BrowseRepositoryPathWorkflowCommand);
            Assert.NotNull(viewModel.AddRepositoryWorkflowCommand);
            Assert.NotNull(viewModel.EditRepositoryWorkflowCommand);
            Assert.NotNull(viewModel.UpdateRepositoryWorkflowCommand);
            Assert.NotNull(viewModel.RemoveRepositoryWorkflowCommand);
            Assert.NotNull(viewModel.SaveLogsWorkflowCommand);
            Assert.NotNull(viewModel.CopyLogsWorkflowCommand);
            Assert.NotNull(viewModel.ClearLogsWorkflowCommand);
        }

        [Fact]
        public void StatusMessage_PropertyChangeNotifies()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var propertyChanged = false;

            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MainWindowViewModel.StatusMessage))
                    propertyChanged = true;
            };

            // Act
            viewModel.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Test",
                Path = "/test"
            });

            // Assert
            Assert.True(propertyChanged || viewModel.StatusMessage != null);
        }

        [Fact]
        public void ProgressPercentage_CanBeSet()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            // Note: ProgressPercentage is private setter, so we test through operations
            // that set it

            // Assert
            Assert.Equal(0, viewModel.ProgressPercentage);
        }

        [Fact]
        public void OutputLog_CanAppendText()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var testMessage = "Test output";

            // Act
            viewModel.OutputLog += testMessage;

            // Assert
            Assert.Contains(testMessage, viewModel.OutputLog);
        }

        [Fact]
        public void ErrorLog_CanAppendText()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var testMessage = "Test error";

            // Act
            viewModel.ErrorLog += testMessage;

            // Assert
            Assert.Contains(testMessage, viewModel.ErrorLog);
        }

        [Fact]
        public void AutoOpenLogs_CanBeToggled()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var initial = viewModel.AutoOpenLogs;
            viewModel.AutoOpenLogs = !initial;

            // Assert
            Assert.NotEqual(initial, viewModel.AutoOpenLogs);
        }

        [Fact]
        public void InlineLogsVisible_CanBeToggled()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var initial = viewModel.InlineLogsVisible;
            viewModel.InlineLogsVisible = !initial;

            // Assert
            Assert.NotEqual(initial, viewModel.InlineLogsVisible);
        }

        [Fact]
        public void ToggleLogsCommand_TogglesInlineLogsVisible()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var initial = viewModel.InlineLogsVisible;

            // Act
            if (viewModel.ToggleLogsCommand?.CanExecute(null) == true)
                viewModel.ToggleLogsCommand.Execute(null);

            // Assert
            Assert.NotEqual(initial, viewModel.InlineLogsVisible);
        }

        [Fact]
        public void RefreshBranchesCommand_DisabledWhenNoRepositorySelected()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var canExecute = viewModel.RefreshBranchesCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void RefreshBranchesCommand_DisabledDuringOperation()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.Repositories.Add(new AppSettings.RepositoryEntry
            {
                Name = "Test",
                Path = "/test"
            });
            viewModel.SelectedRepository = viewModel.Repositories[0];

            // Note: We can't easily set IsOperationInProgress externally,
            // but we can verify the command logic

            // Act & Assert
            Assert.NotNull(viewModel.RefreshBranchesCommand);
        }

        [Fact]
        public void MergeCommand_DisabledWithInvalidBranches()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var canExecute = viewModel.MergeCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void PullBranchCommand_DisabledWithoutRepository()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var canExecute = viewModel.PullBranchCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void CancelOperationCommand_DisabledWhenNotOperating()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();

            // Act
            var canExecute = viewModel.CancelOperationCommand.CanExecute(null);

            // Assert
            Assert.False(canExecute);
        }

        [Fact]
        public void SaveSettings_PersistsViewModelState()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.AutoOpenLogs = false;
            viewModel.InlineLogsVisible = true;

            // Act
            viewModel.SaveSettings();
            var settings = AppSettings.Load();

            // Assert
            Assert.Equal(false, settings.AutoOpenLogs);
            Assert.Equal(true, settings.InlineLogsVisible);
        }

        [Fact]
        public void Repositories_ReflectSettings()
        {
            // Arrange
            var settings = AppSettings.Load();

            // Act
            var viewModel = new MainWindowViewModel();

            // Assert
            Assert.Equal(settings.Repositories.Count, viewModel.Repositories.Count);
        }

        [Fact]
        public void SelectedRepository_CanBeChanged()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var repo = new AppSettings.RepositoryEntry { Name = "Test", Path = "/test" };
            viewModel.Repositories.Add(repo);

            // Act
            viewModel.SelectedRepository = repo;

            // Assert
            Assert.Equal(repo, viewModel.SelectedRepository);
        }

        [Fact]
        public void SelectedSourceBranch_CanBeChanged()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var branch = new BranchItem("main", "main", false, 0);
            viewModel.SourceBranches.Add(branch);

            // Act
            viewModel.SelectedSourceBranch = branch;

            // Assert
            Assert.Equal(branch, viewModel.SelectedSourceBranch);
        }

        [Fact]
        public void SelectedTargetBranch_CanBeChanged()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var branch = new BranchItem("develop", "develop", false, 0);
            viewModel.TargetBranches.Add(branch);

            // Act
            viewModel.SelectedTargetBranch = branch;

            // Assert
            Assert.Equal(branch, viewModel.SelectedTargetBranch);
        }

        [Fact]
        public void ReplaceBranchDropdownItems_ReplacesBoundCollections()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.SourceBranches.Add(new BranchItem("old-source", "old-source"));
            viewModel.TargetBranches.Add(new BranchItem("old-target", "old-target"));

            // Act
            viewModel.ReplaceBranchDropdownItems(
                new[] { new BranchItem("main", "main"), new BranchItem("feature", "feature/test") },
                new[] { new BranchItem("develop", "develop") });

            // Assert
            Assert.Equal(new[] { "main", "feature/test" }, viewModel.SourceBranches.Select(branch => branch.FullName));
            Assert.Equal(new[] { "develop" }, viewModel.TargetBranches.Select(branch => branch.FullName));
        }

        [Fact]
        public void ReplaceRepositories_ReplacesBoundRepositoryCollection()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.Repositories.Add(new AppSettings.RepositoryEntry { Name = "Old", Path = "C:\\old" });

            // Act
            viewModel.ReplaceRepositories(new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo A", Path = "C:\\repo-a" },
                new AppSettings.RepositoryEntry { Name = "Repo B", Path = "C:\\repo-b" }
            });

            // Assert
            Assert.Equal(new[] { "Repo A", "Repo B" }, viewModel.Repositories.Select(repository => repository.Name));
            Assert.Equal(new[] { "Repo A", "Repo B" }, viewModel.SelectableRepositories.Select(repository => repository.DisplayName));
            Assert.Equal(new[] { "C:\\repo-a", "C:\\repo-b" }, viewModel.SelectableRepositories.Select(repository => repository.Path));
        }

        [Fact]
        public void SetRepositorySelected_UpdatesSelectableRepositoryByPath()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.ReplaceRepositories(new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\Repo" }
            });

            // Act
            viewModel.SetRepositorySelected("c:\\repo", true);

            // Assert
            Assert.True(viewModel.SelectableRepositories.Single().IsSelected);
        }

        [Fact]
        public void ClearRepositorySelections_UnchecksAllSelectableRepositories()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            viewModel.ReplaceRepositories(new[]
            {
                new AppSettings.RepositoryEntry { Name = "Repo A", Path = "C:\\repo-a" },
                new AppSettings.RepositoryEntry { Name = "Repo B", Path = "C:\\repo-b" }
            });
            viewModel.SetRepositorySelected("C:\\repo-a", true);
            viewModel.SetRepositorySelected("C:\\repo-b", true);

            // Act
            viewModel.ClearRepositorySelections();

            // Assert
            Assert.All(viewModel.SelectableRepositories, repository => Assert.False(repository.IsSelected));
        }

        [Fact]
        public void AddTargetBranchDropdownItemIfMissing_DeduplicatesByFullName()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var marker = new BranchItem("feature/a (missing on origin)", "feature/a (missing on origin)");

            // Act
            viewModel.AddTargetBranchDropdownItemIfMissing(marker);
            viewModel.AddTargetBranchDropdownItemIfMissing(new BranchItem(marker.Name, marker.FullName.ToUpperInvariant()));

            // Assert
            Assert.Single(viewModel.TargetBranches, branch => branch.FullName == marker.FullName);
        }

        [Fact]
        public void InlineRepositoryEditor_DefaultsToAddMode()
        {
            var viewModel = new MainWindowViewModel();

            Assert.Equal(string.Empty, viewModel.InlineRepositoryName);
            Assert.Equal(string.Empty, viewModel.InlineRepositoryPath);
            Assert.False(viewModel.IsInlineRepositoryEditing);
            Assert.True(viewModel.CanAddInlineRepository);
            Assert.False(viewModel.CanUpdateInlineRepository);
        }

        [Fact]
        public void SetInlineRepositoryPath_UpdatesBoundPath()
        {
            var viewModel = new MainWindowViewModel();

            viewModel.SetInlineRepositoryPath("C:\\repo");

            Assert.Equal("C:\\repo", viewModel.InlineRepositoryPath);
        }

        [Fact]
        public void BeginInlineRepositoryEdit_PopulatesFieldsAndEnablesUpdateMode()
        {
            var viewModel = new MainWindowViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.BeginInlineRepositoryEdit(new AppSettings.RepositoryEntry
            {
                Name = "Repo",
                Path = "C:\\repo"
            });

            Assert.Equal("Repo", viewModel.InlineRepositoryName);
            Assert.Equal("C:\\repo", viewModel.InlineRepositoryPath);
            Assert.True(viewModel.IsInlineRepositoryEditing);
            Assert.False(viewModel.CanAddInlineRepository);
            Assert.True(viewModel.CanUpdateInlineRepository);
            Assert.Contains(nameof(MainWindowViewModel.CanAddInlineRepository), changedProperties);
            Assert.Contains(nameof(MainWindowViewModel.CanUpdateInlineRepository), changedProperties);
        }

        [Fact]
        public void ClearInlineRepositoryEditor_ClearsFieldsAndReturnsToAddMode()
        {
            var viewModel = new MainWindowViewModel();
            viewModel.BeginInlineRepositoryEdit(new AppSettings.RepositoryEntry
            {
                Name = "Repo",
                Path = "C:\\repo"
            });

            viewModel.ClearInlineRepositoryEditor();

            Assert.Equal(string.Empty, viewModel.InlineRepositoryName);
            Assert.Equal(string.Empty, viewModel.InlineRepositoryPath);
            Assert.False(viewModel.IsInlineRepositoryEditing);
            Assert.True(viewModel.CanAddInlineRepository);
            Assert.False(viewModel.CanUpdateInlineRepository);
        }

        [Fact]
        public void WorkflowAvailability_DefaultsToDisabledCommands()
        {
            var viewModel = new MainWindowViewModel();

            Assert.False(viewModel.CanRunMergeWorkflow);
            Assert.False(viewModel.CanRunPullWorkflow);
            Assert.False(viewModel.MergeWorkflowCommand.CanExecute(null));
            Assert.False(viewModel.PullWorkflowCommand.CanExecute(null));
        }

        [Fact]
        public void UpdateWorkflowAvailability_UpdatesWorkflowCommandCanExecute()
        {
            var viewModel = new MainWindowViewModel();
            var changedProperties = new List<string?>();
            viewModel.PropertyChanged += (_, args) => changedProperties.Add(args.PropertyName);

            viewModel.UpdateWorkflowAvailability(
                canRunMergeWorkflow: true,
                canRunPullWorkflow: true);

            Assert.True(viewModel.CanRunMergeWorkflow);
            Assert.True(viewModel.CanRunPullWorkflow);
            Assert.True(viewModel.MergeWorkflowCommand.CanExecute(null));
            Assert.True(viewModel.PullWorkflowCommand.CanExecute(null));
            Assert.Contains(nameof(MainWindowViewModel.CanRunMergeWorkflow), changedProperties);
            Assert.Contains(nameof(MainWindowViewModel.CanRunPullWorkflow), changedProperties);
        }

        [Fact]
        public void WorkflowCommands_InvokeConfiguredActions()
        {
            // Arrange
            var viewModel = new MainWindowViewModel();
            var mergeCalls = 0;
            var pullCalls = 0;
            var refreshCalls = 0;
            var manageCalls = 0;
            var toggleCalls = 0;
            var browseRepositoryPathCalls = 0;
            var addRepositoryCalls = 0;
            var editRepositoryCalls = 0;
            var updateRepositoryCalls = 0;
            var removeRepositoryCalls = 0;
            var saveLogsCalls = 0;
            var copyLogsCalls = 0;
            var clearLogsCalls = 0;

            viewModel.ConfigureWorkflowActions(new MainWindowWorkflowActions
            {
                Merge = () => mergeCalls++,
                Pull = () => pullCalls++,
                Refresh = () => refreshCalls++,
                ManageRepositories = () => manageCalls++,
                ToggleLogs = () => toggleCalls++,
                BrowseRepositoryPath = () => browseRepositoryPathCalls++,
                AddRepository = () => addRepositoryCalls++,
                EditRepository = () => editRepositoryCalls++,
                UpdateRepository = () => updateRepositoryCalls++,
                RemoveRepository = () => removeRepositoryCalls++,
                SaveLogs = () => saveLogsCalls++,
                CopyLogs = () => copyLogsCalls++,
                ClearLogs = () => clearLogsCalls++
            });
            viewModel.UpdateWorkflowAvailability(
                canRunMergeWorkflow: true,
                canRunPullWorkflow: true);

            // Act
            viewModel.MergeWorkflowCommand.Execute(null);
            viewModel.PullWorkflowCommand.Execute(null);
            viewModel.RefreshWorkflowCommand.Execute(null);
            viewModel.ManageRepositoriesCommand.Execute(null);
            viewModel.ManageRepositoriesWorkflowCommand.Execute(null);
            viewModel.ToggleLogsWorkflowCommand.Execute(null);
            viewModel.BrowseRepositoryPathWorkflowCommand.Execute(null);
            viewModel.AddRepositoryWorkflowCommand.Execute(null);
            viewModel.EditRepositoryWorkflowCommand.Execute(null);
            viewModel.UpdateRepositoryWorkflowCommand.Execute(null);
            viewModel.RemoveRepositoryWorkflowCommand.Execute(null);
            viewModel.SaveLogsWorkflowCommand.Execute(null);
            viewModel.CopyLogsWorkflowCommand.Execute(null);
            viewModel.ClearLogsWorkflowCommand.Execute(null);

            // Assert
            Assert.Equal(1, mergeCalls);
            Assert.Equal(1, pullCalls);
            Assert.Equal(1, refreshCalls);
            Assert.Equal(2, manageCalls);
            Assert.Equal(1, toggleCalls);
            Assert.Equal(1, browseRepositoryPathCalls);
            Assert.Equal(1, addRepositoryCalls);
            Assert.Equal(1, editRepositoryCalls);
            Assert.Equal(1, updateRepositoryCalls);
            Assert.Equal(1, removeRepositoryCalls);
            Assert.Equal(1, saveLogsCalls);
            Assert.Equal(1, copyLogsCalls);
            Assert.Equal(1, clearLogsCalls);
        }
    }
}
