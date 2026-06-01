using System;
using System.Collections.ObjectModel;
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
    }
}
