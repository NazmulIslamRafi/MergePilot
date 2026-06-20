using System;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// Delegates production MainWindow workflows behind ViewModel commands.
    /// </summary>
    public sealed class MainWindowWorkflowActions
    {
        public static MainWindowWorkflowActions Empty { get; } = new();

        public Action Merge { get; init; } = NoOp;
        public Action Pull { get; init; } = NoOp;
        public Action Refresh { get; init; } = NoOp;
        public Action ManageRepositories { get; init; } = NoOp;
        public Action ToggleLogs { get; init; } = NoOp;
        public Action BrowseRepositoryPath { get; init; } = NoOp;
        public Action AddRepository { get; init; } = NoOp;
        public Action EditRepository { get; init; } = NoOp;
        public Action UpdateRepository { get; init; } = NoOp;
        public Action RemoveRepository { get; init; } = NoOp;
        public Action SaveLogs { get; init; } = NoOp;
        public Action CopyLogs { get; init; } = NoOp;
        public Action ClearLogs { get; init; } = NoOp;

        private static void NoOp()
        {
        }
    }
}
