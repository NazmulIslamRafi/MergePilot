using System;

namespace MergePilot
{
    /// <summary>
    /// Creates batch-operation callbacks while keeping UI dispatch and interaction wiring in one place.
    /// </summary>
    public sealed class BatchOperationCallbackFactory
    {
        private readonly Action<Action> _invoke;
        private readonly Action<string, bool> _output;
        private readonly Action<string> _error;
        private readonly Action<string> _branchUpdated;
        private readonly IMergeInteractionService _mergeInteractions;

        public BatchOperationCallbackFactory(
            Action<Action> invoke,
            Action<string, bool> output,
            Action<string> error,
            Action<string> branchUpdated,
            IMergeInteractionService mergeInteractions)
        {
            _invoke = invoke ?? throw new ArgumentNullException(nameof(invoke));
            _output = output ?? throw new ArgumentNullException(nameof(output));
            _error = error ?? throw new ArgumentNullException(nameof(error));
            _branchUpdated = branchUpdated ?? throw new ArgumentNullException(nameof(branchUpdated));
            _mergeInteractions = mergeInteractions ?? throw new ArgumentNullException(nameof(mergeInteractions));
        }

        public BatchMergeCallbacks CreateMergeCallbacks()
        {
            return new BatchMergeCallbacks
            {
                Output = (message, status) => _invoke(() => _output(message, status)),
                Error = message => _invoke(() => _error(message)),
                MissingTargetConfirmation = context => InvokeWithResult(() =>
                    _mergeInteractions.ConfirmMissingTargetBranch(context)),
                ConflictResolution = context => InvokeWithResult(() =>
                    _mergeInteractions.ResolveMergeConflict(context))
            };
        }

        public BatchPullCallbacks CreatePullCallbacks()
        {
            return new BatchPullCallbacks
            {
                Output = (message, status) => _invoke(() => _output(message, status)),
                Error = message => _invoke(() => _error(message)),
                BranchUpdated = branch => _invoke(() => _branchUpdated(branch))
            };
        }

        private T InvokeWithResult<T>(Func<T> action)
        {
            T result = default!;
            _invoke(() => result = action());
            return result;
        }
    }
}
