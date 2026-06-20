using System;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Evaluates whether production merge/pull workflows can run from current selections.
    /// </summary>
    public class WorkflowAvailabilityService
    {
        public WorkflowAvailability Evaluate(
            BranchSelectionState branchSelectionState,
            RepositorySelectionState repositorySelectionState)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);
            ArgumentNullException.ThrowIfNull(repositorySelectionState);

            var hasSelectedRepositories = repositorySelectionState.SelectedRepositoryPaths.Count > 0;
            var hasExactlyOneSourceBranch = branchSelectionState.HasExactlyOneSourceBranch;
            var hasSourceBranches = branchSelectionState.HasSourceBranches;
            var hasMergeCompatibleTargets = HasMergeCompatibleTargets(branchSelectionState);

            return new WorkflowAvailability(
                CanRunMergeWorkflow: hasSelectedRepositories && hasExactlyOneSourceBranch && hasMergeCompatibleTargets,
                CanRunPullWorkflow: hasSelectedRepositories && hasSourceBranches);
        }

        private static bool HasMergeCompatibleTargets(BranchSelectionState branchSelectionState)
        {
            if (!branchSelectionState.HasExactlyOneSourceBranch || !branchSelectionState.HasTargetBranches)
                return false;

            var sourceBranch = branchSelectionState.SelectedSourceBranches.Single();
            return branchSelectionState.SelectedTargetBranches.All(targetBranch =>
                !string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase));
        }
    }

    public readonly record struct WorkflowAvailability(
        bool CanRunMergeWorkflow,
        bool CanRunPullWorkflow);
}
