using System;
using Xunit;

namespace MergePilot.Tests
{
    public class RefreshWorkflowServiceTests
    {
        [Fact]
        public void ResetSelections_ClearsBranchAndRepositoryState()
        {
            var branchState = new BranchSelectionState();
            var repositoryState = new RepositorySelectionState();
            branchState.SetBranchSelected(BranchSelectionRole.Source, "main", true);
            branchState.SetBranchSelected(BranchSelectionRole.Target, "develop", true);
            branchState.AddBranchToCatalogs("main");
            repositoryState.SetSelected("C:\\repo", true);
            var service = new RefreshWorkflowService();

            var result = service.ResetSelections(
                branchState,
                repositoryState,
                sourceBranchText: "feature/source",
                targetBranchText: "release/target");

            Assert.Empty(branchState.SelectedSourceBranches);
            Assert.Empty(branchState.SelectedTargetBranches);
            Assert.Empty(branchState.SourceBranchCatalog);
            Assert.Empty(branchState.TargetBranchCatalog);
            Assert.Empty(repositoryState.SelectedRepositoryPaths);
            Assert.Empty(result.SourceItems);
            Assert.Empty(result.TargetItems);
            Assert.Equal("feature/source", result.SourceBranchTextToRestore);
            Assert.Equal("release/target", result.TargetBranchTextToRestore);
            Assert.Equal("UI refreshed (selections preserved where possible).", result.Message);
        }

        [Fact]
        public void ResetSelections_WithBlankBranchText_DoesNotRequestTextRestore()
        {
            var service = new RefreshWorkflowService();

            var result = service.ResetSelections(
                new BranchSelectionState(),
                new RepositorySelectionState(),
                sourceBranchText: " ",
                targetBranchText: null);

            Assert.Null(result.SourceBranchTextToRestore);
            Assert.Null(result.TargetBranchTextToRestore);
        }

        [Fact]
        public void ResetSelections_WithNullBranchState_Throws()
        {
            var service = new RefreshWorkflowService();

            Assert.Throws<ArgumentNullException>(() =>
                service.ResetSelections(null!, new RepositorySelectionState()));
        }

        [Fact]
        public void ResetSelections_WithNullRepositoryState_Throws()
        {
            var service = new RefreshWorkflowService();

            Assert.Throws<ArgumentNullException>(() =>
                service.ResetSelections(new BranchSelectionState(), null!));
        }
    }
}
