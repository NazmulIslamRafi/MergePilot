using System;
using System.Collections.Generic;

namespace MergePilot
{
    /// <summary>
    /// Coordinates non-WPF state reset for the MainWindow refresh workflow.
    /// </summary>
    public class RefreshWorkflowService
    {
        public RefreshWorkflowResult ResetSelections(
            BranchSelectionState branchSelectionState,
            RepositorySelectionState repositorySelectionState,
            string? sourceBranchText = null,
            string? targetBranchText = null)
        {
            ArgumentNullException.ThrowIfNull(branchSelectionState);
            ArgumentNullException.ThrowIfNull(repositorySelectionState);

            branchSelectionState.ClearSelections();
            branchSelectionState.ClearBranchCatalogs();
            repositorySelectionState.Clear();

            return RefreshWorkflowResult.Empty(
                "UI refreshed (selections preserved where possible).",
                NormalizeRestoredText(sourceBranchText),
                NormalizeRestoredText(targetBranchText));
        }

        private static string? NormalizeRestoredText(string? text)
        {
            return string.IsNullOrWhiteSpace(text)
                ? null
                : text;
        }
    }

    public record RefreshWorkflowResult(
        IReadOnlyList<BranchItem> SourceItems,
        IReadOnlyList<BranchItem> TargetItems,
        string? SourceBranchTextToRestore,
        string? TargetBranchTextToRestore,
        string Message)
    {
        public static RefreshWorkflowResult Empty(
            string message,
            string? sourceBranchTextToRestore = null,
            string? targetBranchTextToRestore = null)
        {
            return new RefreshWorkflowResult(
                Array.Empty<BranchItem>(),
                Array.Empty<BranchItem>(),
                sourceBranchTextToRestore,
                targetBranchTextToRestore,
                message);
        }
    }
}
