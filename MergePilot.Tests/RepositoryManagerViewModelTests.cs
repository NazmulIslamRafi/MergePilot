using System.Linq;
using MergePilot.ViewModels;
using Xunit;

namespace MergePilot.Tests
{
    public class RepositoryManagerViewModelTests
    {
        [Fact]
        public void LoadData_PopulatesRepositoryAndBranchCollections()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" });
            settings.CustomBranches.Add(new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" });
            var viewModel = new RepositoryManagerViewModel();

            viewModel.LoadData(settings);

            Assert.Single(viewModel.Repositories);
            Assert.Single(viewModel.Branches);
            Assert.Equal("Repo", viewModel.Repositories[0].Name);
            Assert.Equal("main", viewModel.Branches[0].BranchName);
        }

        [Fact]
        public void GetRepositoryNames_ReturnsNonEmptyNames()
        {
            var settings = new AppSettings();
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" });
            settings.Repositories.Add(new AppSettings.RepositoryEntry { Name = " ", Path = "C:\\empty" });
            var viewModel = new RepositoryManagerViewModel();
            viewModel.LoadData(settings);

            var names = viewModel.GetRepositoryNames();

            Assert.Equal(new[] { "Repo" }, names);
        }

        [Fact]
        public void RefreshState_UpdatesButtonTextAndCanExecute()
        {
            var viewModel = new RepositoryManagerViewModel();

            viewModel.SetRefreshingBranches(true);

            Assert.True(viewModel.IsRefreshingBranches);
            Assert.False(viewModel.CanRefreshBranches);
            Assert.Equal("Refreshing...", viewModel.RefreshBranchesButtonText);
            Assert.False(viewModel.RefreshBranchesCommand.CanExecute(null));

            viewModel.SetRefreshingBranches(false);

            Assert.True(viewModel.CanRefreshBranches);
            Assert.Equal("Refresh Branches", viewModel.RefreshBranchesButtonText);
            Assert.True(viewModel.RefreshBranchesCommand.CanExecute(null));
        }

        [Fact]
        public void Commands_InvokeConfiguredActionsWithParameters()
        {
            var viewModel = new RepositoryManagerViewModel();
            var repository = new AppSettings.RepositoryEntry { Name = "Repo", Path = "C:\\repo" };
            var branch = new AppSettings.BranchEntry { BranchName = "main", Repository = "Repo" };
            var calls = new[]
            {
                ("addRepo", 0),
                ("editRepo", 0),
                ("deleteRepo", 0),
                ("refresh", 0),
                ("addBranch", 0),
                ("editBranch", 0),
                ("deleteBranch", 0),
                ("close", 0)
            }.ToDictionary(item => item.Item1, item => item.Item2);

            viewModel.ConfigureWorkflowActions(new RepositoryManagerWorkflowActions
            {
                AddRepository = () => calls["addRepo"]++,
                EditRepository = selected =>
                {
                    Assert.Same(repository, selected);
                    calls["editRepo"]++;
                },
                DeleteRepository = selected =>
                {
                    Assert.Same(repository, selected);
                    calls["deleteRepo"]++;
                },
                RefreshBranches = () => calls["refresh"]++,
                AddBranch = () => calls["addBranch"]++,
                EditBranch = selected =>
                {
                    Assert.Same(branch, selected);
                    calls["editBranch"]++;
                },
                DeleteBranch = selected =>
                {
                    Assert.Same(branch, selected);
                    calls["deleteBranch"]++;
                },
                Close = () => calls["close"]++
            });

            viewModel.AddRepositoryCommand.Execute(null);
            viewModel.EditRepositoryCommand.Execute(repository);
            viewModel.DeleteRepositoryCommand.Execute(repository);
            viewModel.RefreshBranchesCommand.Execute(null);
            viewModel.AddBranchCommand.Execute(null);
            viewModel.EditBranchCommand.Execute(branch);
            viewModel.DeleteBranchCommand.Execute(branch);
            viewModel.CloseCommand.Execute(null);

            Assert.All(calls.Values, count => Assert.Equal(1, count));
        }
    }
}
