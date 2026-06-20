using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    /// <summary>
    /// Coordinates production batch merge and pull request creation with the batch services.
    /// </summary>
    public class BatchOperationWorkflowRunner
    {
        private readonly BatchOperationRequestFactory _requestFactory;
        private readonly Func<BatchMergeRequest, BatchMergeCallbacks, CancellationToken, Task<BatchMergeResult>> _mergeRunner;
        private readonly Func<BatchPullRequest, BatchPullCallbacks, CancellationToken, Task<BatchPullResult>> _pullRunner;

        public BatchOperationWorkflowRunner(
            BatchOperationRequestFactory? requestFactory = null,
            Func<BatchMergeRequest, BatchMergeCallbacks, CancellationToken, Task<BatchMergeResult>>? mergeRunner = null,
            Func<BatchPullRequest, BatchPullCallbacks, CancellationToken, Task<BatchPullResult>>? pullRunner = null)
        {
            _requestFactory = requestFactory ?? new BatchOperationRequestFactory();
            _mergeRunner = mergeRunner ?? RunDefaultMergeAsync;
            _pullRunner = pullRunner ?? RunDefaultPullAsync;
        }

        public Task<BatchMergeResult> RunMergeAsync(
            AppSettings settings,
            IEnumerable<string> repositoryPaths,
            IEnumerable<string> sourceBranches,
            IEnumerable<string> targetBranches,
            BatchMergeCallbacks callbacks,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(callbacks);

            var request = _requestFactory.CreateMergeRequest(
                settings,
                repositoryPaths,
                sourceBranches,
                targetBranches);

            return _mergeRunner(request, callbacks, cancellationToken);
        }

        public Task<BatchPullResult> RunPullAsync(
            IEnumerable<string> repositoryPaths,
            IEnumerable<string> sourceBranches,
            BatchPullCallbacks callbacks,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(callbacks);

            var request = _requestFactory.CreatePullRequest(
                repositoryPaths,
                sourceBranches);

            return _pullRunner(request, callbacks, cancellationToken);
        }

        private static Task<BatchMergeResult> RunDefaultMergeAsync(
            BatchMergeRequest request,
            BatchMergeCallbacks callbacks,
            CancellationToken cancellationToken)
        {
            return new BatchMergeService().MergeSelectedBranchesAsync(
                request,
                callbacks,
                cancellationToken);
        }

        private static Task<BatchPullResult> RunDefaultPullAsync(
            BatchPullRequest request,
            BatchPullCallbacks callbacks,
            CancellationToken cancellationToken)
        {
            return new BatchPullService().PullSelectedBranchesAsync(
                request,
                callbacks,
                cancellationToken);
        }
    }
}
