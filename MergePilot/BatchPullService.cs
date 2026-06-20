using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    /// <summary>
    /// Runs pull/fetch operations across selected repositories and source branches.
    /// </summary>
    public class BatchPullService
    {
        /// <summary>
        /// Pulls/fetches all requested branches in each selected repository.
        /// </summary>
        public async Task<BatchPullResult> PullSelectedBranchesAsync(
            BatchPullRequest request,
            BatchPullCallbacks callbacks,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(request);
            ArgumentNullException.ThrowIfNull(callbacks);

            var result = new BatchPullResult();
            var repoPaths = request.RepositoryPaths.Where(p => !string.IsNullOrWhiteSpace(p)).ToList();
            var sourceBranches = request.SourceBranches.Where(b => !string.IsNullOrWhiteSpace(b)).ToList();

            callbacks.WriteOutput(LogFormatter.FormatOperationStart("PULL / FETCH OPERATION"), status: true);

            if (sourceBranches.Count == 0)
            {
                callbacks.WriteError("❌ No source branches selected. Please select at least one source branch.");
                return result;
            }

            foreach (var repo in repoPaths)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var repoSuccessList = new List<string>();
                var repoSkipList = new List<string>();
                var repoFailList = new List<string>();

                callbacks.WriteOutput(LogFormatter.FormatProjectHeader(Path.GetFileName(repo), repo), status: true);

                if (!Directory.Exists(repo) || !Directory.Exists(Path.Combine(repo, ".git")))
                {
                    callbacks.WriteError($"❌ Repository path not found or not a git repo: {repo}");
                    result.Failed.Add($"{repo} | pull failed | repo not found");
                    continue;
                }

                foreach (var branch in sourceBranches)
                {
                    cancellationToken.ThrowIfCancellationRequested();

                    try
                    {
                        var branchResult = await GitHelper.EnsureBranchLatestAsync(repo, branch, cancellationToken);
                        if (branchResult.IsSuccess)
                        {
                            if (!string.IsNullOrEmpty(branchResult.Message) &&
                                branchResult.Message.Contains("already up-to-date", StringComparison.OrdinalIgnoreCase))
                            {
                                callbacks.WriteOutput($"⏭ {branchResult.Message}");
                                result.Skipped.Add($"{repo} | {branch}");
                                repoSkipList.Add(branch);
                            }
                            else
                            {
                                callbacks.WriteOutput($"✔ {branchResult.Message}");
                                result.Successful.Add($"{repo} | {branch}");
                                repoSuccessList.Add(branch);
                                callbacks.NotifyBranchUpdated(branch);
                            }
                        }
                        else
                        {
                            callbacks.WriteError($"❌ {branchResult.Message}");
                            result.Failed.Add($"{repo} | {branch} | {branchResult.Message}");
                            repoFailList.Add(branch);
                        }
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        callbacks.WriteError($"❌ Exception while updating '{branch}': {ex.Message}");
                        result.Failed.Add($"{repo} | {branch} | exception");
                        repoFailList.Add(branch);
                    }
                }

                callbacks.WriteOutput(
                    LogFormatter.FormatProjectSummary(Path.GetFileName(repo), repoSuccessList, repoSkipList, repoFailList),
                    status: true);
            }

            callbacks.WriteOutput(LogFormatter.FormatFinalSummary("PULL / FETCH OPERATION", result.Successful, result.Skipped, result.Failed), status: true);
            callbacks.WriteOutput("🎉 Pull operation finished.\n");

            return result;
        }
    }

    /// <summary>
    /// Request data for a batch pull/fetch operation.
    /// </summary>
    public class BatchPullRequest
    {
        public IReadOnlyList<string> RepositoryPaths { get; }
        public IReadOnlyList<string> SourceBranches { get; }

        public BatchPullRequest(IEnumerable<string> repositoryPaths, IEnumerable<string> sourceBranches)
        {
            RepositoryPaths = repositoryPaths?.ToList() ?? new List<string>();
            SourceBranches = sourceBranches?.ToList() ?? new List<string>();
        }
    }

    /// <summary>
    /// Callback hooks used by batch operations to report progress without depending on WPF controls.
    /// </summary>
    public class BatchPullCallbacks
    {
        public Action<string, bool>? Output { get; init; }
        public Action<string>? Error { get; init; }
        public Action<string>? BranchUpdated { get; init; }

        public void WriteOutput(string message, bool status = false) => Output?.Invoke(message, status);
        public void WriteError(string message) => Error?.Invoke(message);
        public void NotifyBranchUpdated(string branch) => BranchUpdated?.Invoke(branch);
    }

    /// <summary>
    /// Summary lists produced by a batch pull/fetch operation.
    /// </summary>
    public class BatchPullResult
    {
        public List<string> Successful { get; } = new();
        public List<string> Skipped { get; } = new();
        public List<string> Failed { get; } = new();
    }
}
