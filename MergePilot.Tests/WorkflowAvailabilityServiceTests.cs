using System;
using Xunit;

namespace MergePilot.Tests
{
    public class WorkflowAvailabilityServiceTests
    {
        [Fact]
        public void Evaluate_WithBranchesButNoRepository_DisablesWorkflows()
        {
            var branchState = new BranchSelectionState();
            branchState.SetBranchSelected(BranchSelectionRole.Source, "feature", true);
            branchState.SetBranchSelected(BranchSelectionRole.Target, "main", true);
            var service = new WorkflowAvailabilityService();

            var availability = service.Evaluate(branchState, new RepositorySelectionState());

            Assert.False(availability.CanRunMergeWorkflow);
            Assert.False(availability.CanRunPullWorkflow);
        }

        [Fact]
        public void Evaluate_WithRepositoryOneSourceAndTarget_EnablesMergeAndPull()
        {
            var branchState = new BranchSelectionState();
            var repositoryState = new RepositorySelectionState();
            repositoryState.SetSelected("C:\\repo", true);
            branchState.SetBranchSelected(BranchSelectionRole.Source, "feature", true);
            branchState.SetBranchSelected(BranchSelectionRole.Target, "main", true);
            var service = new WorkflowAvailabilityService();

            var availability = service.Evaluate(branchState, repositoryState);

            Assert.True(availability.CanRunMergeWorkflow);
            Assert.True(availability.CanRunPullWorkflow);
        }

        [Fact]
        public void Evaluate_WithMultipleSources_DisablesMergeButAllowsPull()
        {
            var branchState = new BranchSelectionState();
            var repositoryState = new RepositorySelectionState();
            repositoryState.SetSelected("C:\\repo", true);
            branchState.SetBranchSelected(BranchSelectionRole.Source, "feature/a", true);
            branchState.SetBranchSelected(BranchSelectionRole.Source, "feature/b", true);
            branchState.SetBranchSelected(BranchSelectionRole.Target, "main", true);
            var service = new WorkflowAvailabilityService();

            var availability = service.Evaluate(branchState, repositoryState);

            Assert.False(availability.CanRunMergeWorkflow);
            Assert.True(availability.CanRunPullWorkflow);
        }

        [Fact]
        public void Evaluate_WithSameSourceAndTarget_DisablesMergeButAllowsPull()
        {
            var branchState = new BranchSelectionState();
            var repositoryState = new RepositorySelectionState();
            repositoryState.SetSelected("C:\\repo", true);
            branchState.SetBranchSelected(BranchSelectionRole.Source, "main", true);
            branchState.SetBranchSelected(BranchSelectionRole.Target, "MAIN", true);
            var service = new WorkflowAvailabilityService();

            var availability = service.Evaluate(branchState, repositoryState);

            Assert.False(availability.CanRunMergeWorkflow);
            Assert.True(availability.CanRunPullWorkflow);
        }

        [Fact]
        public void Evaluate_WithNullState_Throws()
        {
            var service = new WorkflowAvailabilityService();

            Assert.Throws<ArgumentNullException>(() =>
                service.Evaluate(null!, new RepositorySelectionState()));
            Assert.Throws<ArgumentNullException>(() =>
                service.Evaluate(new BranchSelectionState(), null!));
        }
    }
}
