using System;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// Delegates RepositoryManager dialog workflows behind ViewModel commands.
    /// </summary>
    public sealed class RepositoryManagerWorkflowActions
    {
        public static RepositoryManagerWorkflowActions Empty { get; } = new();

        public Action AddRepository { get; init; } = NoOp;
        public Action<AppSettings.RepositoryEntry?> EditRepository { get; init; } = _ => NoOp();
        public Action<AppSettings.RepositoryEntry?> DeleteRepository { get; init; } = _ => NoOp();
        public Action RefreshBranches { get; init; } = NoOp;
        public Action AddBranch { get; init; } = NoOp;
        public Action<AppSettings.BranchEntry?> EditBranch { get; init; } = _ => NoOp();
        public Action<AppSettings.BranchEntry?> DeleteBranch { get; init; } = _ => NoOp();
        public Action Close { get; init; } = NoOp;

        private static void NoOp()
        {
        }
    }
}
