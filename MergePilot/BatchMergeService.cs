using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    /// <summary>
    /// Runs merge operations across selected repositories, source branches, and target branches.
    /// </summary>
    public class BatchMergeService
    {
        /// <summary>
        /// Merges each selected source branch into each selected target branch for every selected repository.
        /// </summary>
        public async Task<BatchMergeResult> MergeSelectedBranchesAsync(
            BatchMergeRequest request,
            BatchMergeCallbacks callbacks,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(callbacks);

            var result = new BatchMergeResult();
            var sources = request.SourceBranches.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            var targets = request.TargetBranches.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();
            var repoPaths = request.RepositoryPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();

            callbacks.WriteOutput(LogFormatter.FormatOperationStart("MERGE OPERATION"), status: true);

            if (sources.Count == 0)
            {
                callbacks.WriteError("❌ No source branches selected. Please select at least one source branch.");
                WriteFinalSummary(callbacks, result);
                return result;
            }

            if (targets.Count == 0)
            {
                callbacks.WriteError("❌ No target branches selected. Please select at least one target branch.");
                WriteFinalSummary(callbacks, result);
                return result;
            }

            foreach (var repo in repoPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                callbacks.WriteOutput(LogFormatter.FormatProjectHeader(Path.GetFileName(repo), repo), status: true);

                if (!Directory.Exists(repo) || !Directory.Exists(Path.Combine(repo, ".git")))
                {
                    callbacks.WriteError($"❌ Repository path not found or not a git repo: {repo}");
                    result.Failed.Add($"{repo} | all branches | repo not found");
                    continue;
                }

                var repoSuccessList = new List<string>();
                var repoSkipList = new List<string>();
                var repoFailList = new List<string>();

                foreach (var source in sources)
                {
                    foreach (var targetBranch in targets)
                    {
                        cancellationToken.ThrowIfCancellationRequested();

                        callbacks.WriteOutput(LogFormatter.FormatBranchOperationHeader(source, targetBranch), status: true);

                        try
                        {
                            var remoteName = await GitHelper.GetDefaultRemoteNameAsync(repo, cancellationToken);
                            var existsOnRemote = await GitHelper.RemoteBranchExistsAsync(repo, remoteName, targetBranch, cancellationToken);
                            if (!existsOnRemote)
                            {
                                var shouldContinue = callbacks.ConfirmMissingTarget(new MissingTargetBranchContext(repo, remoteName, targetBranch));
                                if (!shouldContinue)
                                {
                                    callbacks.WriteOutput("⏭ Skipped: target branch not found on remote");
                                    result.Failed.Add($"{repo} | {targetBranch} | target-not-found");
                                    repoFailList.Add($"{source}→{targetBranch}");
                                    continue;
                                }
                            }

                            var mergeResult = await GitHelper.MergeBranchAsync(repo, source, targetBranch, cancellationToken);
                            await HandleMergeResultAsync(
                                repo,
                                source,
                                targetBranch,
                                mergeResult,
                                result,
                                repoSuccessList,
                                repoSkipList,
                                repoFailList,
                                callbacks,
                                cancellationToken);
                        }
                        catch (Exception ex) when (ex is not OperationCanceledException)
                        {
                            callbacks.WriteError($"❌ Exception for {targetBranch}: {ex.Message}");
                            result.Failed.Add($"{repo} | {targetBranch} | exception");
                            repoFailList.Add($"{source}→{targetBranch}");
                        }
                    }
                }

                callbacks.WriteOutput(LogFormatter.FormatProjectSummary(Path.GetFileName(repo), repoSuccessList, repoSkipList, repoFailList), status: true);
            }

            WriteFinalSummary(callbacks, result);
            return result;
        }

        private static async Task HandleMergeResultAsync(
            string repo,
            string source,
            string targetBranch,
            MergeResult mergeResult,
            BatchMergeResult result,
            List<string> repoSuccessList,
            List<string> repoSkipList,
            List<string> repoFailList,
            BatchMergeCallbacks callbacks,
            CancellationToken cancellationToken)
        {
            switch (mergeResult.Status)
            {
                case MergeStatus.Success:
                    callbacks.WriteOutput($"✔ {mergeResult.Message}");
                    result.Successful.Add($"{repo} | {source}→{targetBranch}");
                    repoSuccessList.Add($"{source}→{targetBranch}");
                    break;

                case MergeStatus.Skipped:
                    callbacks.WriteOutput($"⏭ {mergeResult.Message}");
                    result.Skipped.Add($"{repo} | {source}→{targetBranch}");
                    repoSkipList.Add($"{source}→{targetBranch}");
                    break;

                case MergeStatus.Conflict:
                    await HandleConflictAsync(
                        repo,
                        source,
                        targetBranch,
                        mergeResult,
                        result,
                        repoSuccessList,
                        repoFailList,
                        callbacks,
                        cancellationToken);
                    break;

                case MergeStatus.Failed:
                    callbacks.WriteError($"❌ {mergeResult.Message}");
                    result.Failed.Add($"{repo} | {source}→{targetBranch} | failed");
                    repoFailList.Add($"{source}→{targetBranch}");
                    break;
            }
        }

        private static async Task HandleConflictAsync(
            string repo,
            string source,
            string targetBranch,
            MergeResult mergeResult,
            BatchMergeResult result,
            List<string> repoSuccessList,
            List<string> repoFailList,
            BatchMergeCallbacks callbacks,
            CancellationToken cancellationToken)
        {
            callbacks.WriteError($"❌ Conflict: {mergeResult.Message}");

            var resolution = callbacks.ResolveConflict(new MergeConflictContext(repo, source, targetBranch, mergeResult.Message));
            if (resolution != MergeConflictResolution.ContinueAfterManualResolution)
            {
                var abort = await GitHelper.AbortMergeAsync(repo, cancellationToken);
                callbacks.WriteError($"❌ Merge aborted for {targetBranch}: {abort.StdErr}");
                result.Failed.Add($"{repo} | {targetBranch} | merge conflict (aborted)");
                repoFailList.Add($"{source}→{targetBranch}");
                return;
            }

            var add = await GitHelper.RetryRunGitCommandAsync(repo, "add -A", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1), cancellationToken);
            if (!add.IsSuccess)
            {
                callbacks.WriteError($"❌ 'git add' failed: {add.StdErr}");
                result.Failed.Add($"{repo} | {targetBranch} | add failed");
                repoFailList.Add($"{source}→{targetBranch}");
                return;
            }

            var commit = await GitHelper.RetryRunGitCommandAsync(repo, "commit -m \"Resolve merge conflicts by user\"", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(1), cancellationToken);
            if (!commit.IsSuccess)
            {
                callbacks.WriteOutput($"ℹ 'git commit' returned: {commit.StdErr} {commit.StdOut}");
            }

            var push = await GitHelper.PushBranchAsync(repo, targetBranch, cancellationToken);
            if (!push.IsSuccess)
            {
                callbacks.WriteError($"❌ Push failed after manual resolution: {push.StdErr}");
                result.Failed.Add($"{repo} | {targetBranch} | push failed after resolution");
                repoFailList.Add($"{source}→{targetBranch}");
                return;
            }

            callbacks.WriteOutput($"✔ Manual resolution pushed: {targetBranch}");
            result.Successful.Add($"{repo} | {source}→{targetBranch}");
            repoSuccessList.Add($"{source}→{targetBranch}");
        }

        private static void WriteFinalSummary(BatchMergeCallbacks callbacks, BatchMergeResult result)
        {
            callbacks.WriteOutput(LogFormatter.FormatFinalSummary("MERGE OPERATION", result.Successful, result.Skipped, result.Failed), status: true);
            callbacks.WriteOutput("🎉 Merging finished.");
        }
    }

    /// <summary>
    /// Request data for a batch merge operation.
    /// </summary>
    public class BatchMergeRequest
    {
        public IReadOnlyList<string> RepositoryPaths { get; }
        public IReadOnlyList<string> SourceBranches { get; }
        public IReadOnlyList<string> TargetBranches { get; }

        public BatchMergeRequest(
            IEnumerable<string> repositoryPaths,
            IEnumerable<string> sourceBranches,
            IEnumerable<string> targetBranches)
        {
            RepositoryPaths = repositoryPaths?.ToList() ?? new List<string>();
            SourceBranches = sourceBranches?.ToList() ?? new List<string>();
            TargetBranches = targetBranches?.ToList() ?? new List<string>();
        }
    }

    /// <summary>
    /// Callback hooks used by batch merge operations without depending on WPF controls.
    /// </summary>
    public class BatchMergeCallbacks
    {
        public Action<string, bool>? Output { get; init; }
        public Action<string>? Error { get; init; }
        public Func<MissingTargetBranchContext, bool>? MissingTargetConfirmation { get; init; }
        public Func<MergeConflictContext, MergeConflictResolution>? ConflictResolution { get; init; }

        public void WriteOutput(string message, bool status = false) => Output?.Invoke(message, status);
        public void WriteError(string message) => Error?.Invoke(message);
        public bool ConfirmMissingTarget(MissingTargetBranchContext context) => MissingTargetConfirmation?.Invoke(context) ?? false;
        public MergeConflictResolution ResolveConflict(MergeConflictContext context) => ConflictResolution?.Invoke(context) ?? MergeConflictResolution.Abort;
    }

    /// <summary>
    /// Context for a missing target branch confirmation.
    /// </summary>
    public record MissingTargetBranchContext(string RepositoryPath, string RemoteName, string TargetBranch);

    /// <summary>
    /// Context for a merge conflict resolution prompt.
    /// </summary>
    public record MergeConflictContext(string RepositoryPath, string SourceBranch, string TargetBranch, string Message);

    /// <summary>
    /// User's chosen response to a merge conflict.
    /// </summary>
    public enum MergeConflictResolution
    {
        Abort,
        ContinueAfterManualResolution
    }

    /// <summary>
    /// Summary lists produced by a batch merge operation.
    /// </summary>
    public class BatchMergeResult
    {
        public List<string> Successful { get; } = new();
        public List<string> Skipped { get; } = new();
        public List<string> Failed { get; } = new();
    }
}
