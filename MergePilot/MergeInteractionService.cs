using System;

namespace MergePilot
{
    public interface IMergeInteractionService
    {
        bool ConfirmMissingTargetBranch(MissingTargetBranchContext context);
        MergeConflictResolution ResolveMergeConflict(MergeConflictContext context);
    }

    public sealed class MergeInteractionService : IMergeInteractionService
    {
        private readonly IUserDialogService _dialogs;
        private readonly IRepositoryFolderOpener _folderOpener;
        private readonly Action<string> _error;

        public MergeInteractionService(
            IUserDialogService dialogs,
            IRepositoryFolderOpener folderOpener,
            Action<string> error)
        {
            _dialogs = dialogs ?? throw new ArgumentNullException(nameof(dialogs));
            _folderOpener = folderOpener ?? throw new ArgumentNullException(nameof(folderOpener));
            _error = error ?? throw new ArgumentNullException(nameof(error));
        }

        public bool ConfirmMissingTargetBranch(MissingTargetBranchContext context)
        {
            return _dialogs.ConfirmYesNo(
                $"Target branch '{context.TargetBranch}' was not found on '{context.RemoteName}' for repo '{context.RepositoryPath}'.\n\nDo you want to continue anyway?",
                "Target branch not found",
                UserDialogIcon.Warning);
        }

        public MergeConflictResolution ResolveMergeConflict(MergeConflictContext context)
        {
            var message =
                $"Merge conflict while merging '{context.SourceBranch}' into '{context.TargetBranch}' for repo '{context.RepositoryPath}'.\n\n" +
                "Choose 'Yes' to open repository folder and resolve manually, then press OK to continue.\n" +
                "Choose 'No' to abort merge and skip this branch.";

            var shouldResolve = _dialogs.ConfirmYesNo(message, "Merge Conflict", UserDialogIcon.Warning);
            if (!shouldResolve)
                return MergeConflictResolution.Abort;

            if (!_folderOpener.TryOpen(context.RepositoryPath))
                _error($"⚠ Could not open explorer for: {context.RepositoryPath}");

            var resolved = _dialogs.ConfirmOkCancel(
                "Resolve conflicts in the opened repository (stage & commit). Click OK when done, or Cancel to abort & skip.",
                "Resolve Conflicts",
                UserDialogIcon.Information);

            return resolved
                ? MergeConflictResolution.ContinueAfterManualResolution
                : MergeConflictResolution.Abort;
        }
    }
}
