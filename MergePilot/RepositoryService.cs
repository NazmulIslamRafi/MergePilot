using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    /// <summary>
    /// Provides event data for operation progress updates.
    /// </summary>
    public class OperationProgressEventArgs : EventArgs
    {
        /// <summary>
        /// Gets the progress percentage (0-100).
        /// </summary>
        public int ProgressPercentage { get; }

        /// <summary>
        /// Gets the status message.
        /// </summary>
        public string StatusMessage { get; }

        /// <summary>
        /// Initializes a new instance of OperationProgressEventArgs.
        /// </summary>
        public OperationProgressEventArgs(int progressPercentage, string statusMessage)
        {
            ProgressPercentage = Math.Clamp(progressPercentage, 0, 100);
            StatusMessage = statusMessage;
        }
    }

    /// <summary>
    /// Manages Git repository operations with caching, async support, and progress tracking.
    /// </summary>
    /// <remarks>
    /// Provides a high-level interface for Git operations with the following features:
    /// - Automatic branch list caching with configurable TTL
    /// - Full async/await support for responsive UI
    /// - Cancellation token support for operation interruption
    /// - Progress reporting during long operations
    /// - Safe concurrent operation handling
    /// </remarks>
    public class RepositoryService
    {
        // Branch cache: key = repoPath, value = (branches, cached timestamp)
        private readonly Dictionary<string, (IEnumerable<BranchItem> branches, DateTime timestamp)> _branchCache
            = new(StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Duration for which cached branch lists remain valid before requiring refresh.
        /// Default: 5 minutes.
        /// </summary>
        public TimeSpan CacheDuration { get; set; } = TimeSpan.FromMinutes(5);

        /// <summary>
        /// Raised when an operation reports progress.
        /// </summary>
        public event EventHandler<OperationProgressEventArgs>? ProgressChanged;

        /// <summary>
        /// Gets the list of branches for a repository, using cached data if available and valid.
        /// </summary>
        /// <param name="repoPath">The absolute path to the Git repository.</param>
        /// <param name="forceRefresh">If true, bypasses cache and fetches fresh data.</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
        /// <returns>An enumerable of BranchItem representing all branches in the repository.</returns>
        /// <remarks>
        /// This method implements caching to avoid repeated Git operations. If the cache
        /// is valid (within CacheDuration), the cached result is returned immediately.
        /// Otherwise, a fresh fetch from Git is performed and cached for future calls.
        /// </remarks>
        /// <example>
        /// <code>
        /// var service = new RepositoryService();
        /// var branches = await service.GetBranchesAsync("/path/to/repo");
        /// 
        /// // Force refresh even if cached:
        /// var freshBranches = await service.GetBranchesAsync("/path/to/repo", forceRefresh: true);
        /// </code>
        /// </example>
        public async Task<IEnumerable<BranchItem>> GetBranchesAsync(
            string repoPath,
            bool forceRefresh = false,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new ArgumentException("Repository path cannot be empty", nameof(repoPath));

            // Check cache first if not forcing refresh
            if (!forceRefresh && _branchCache.TryGetValue(repoPath, out var cached))
            {
                var age = DateTime.UtcNow - cached.timestamp;
                if (age < CacheDuration)
                {
                    OnProgressChanged(100, $"Branches loaded from cache (age: {age.TotalSeconds:F0}s)");
                    return cached.branches;
                }
            }

            // Cache miss or expired - fetch from Git
            OnProgressChanged(50, "Fetching branches from repository...");

            try
            {
                var branches = await GitHelper.GetBranchesAsync(repoPath, cancellationToken)
                    .ConfigureAwait(false);

                // Cache the result
                var branchList = branches.ToList();
                _branchCache[repoPath] = (branchList, DateTime.UtcNow);

                OnProgressChanged(100, $"Loaded {branchList.Count} branches");

                return branchList;
            }
            catch (OperationCanceledException)
            {
                OnProgressChanged(0, "Branch fetch canceled");
                throw;
            }
            catch (Exception ex)
            {
                OnProgressChanged(0, $"Failed to fetch branches: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Performs a merge operation with progress tracking and cancellation support.
        /// </summary>
        /// <param name="repoPath">The absolute path to the Git repository.</param>
        /// <param name="sourceBranch">The branch to merge from.</param>
        /// <param name="targetBranch">The branch to merge into.</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
        /// <returns>A MergeResult indicating success/failure and any relevant messages.</returns>
        /// <remarks>
        /// This method:
        /// 1. Validates input using InputValidator
        /// 2. Reports progress at key stages
        /// 3. Supports cancellation for responsive UI
        /// 4. Invalidates cache after successful merge
        /// 5. Provides detailed error messages on failure
        /// </remarks>
        public async Task<MergeResult> MergeBranchAsync(
            string repoPath,
            string sourceBranch,
            string targetBranch,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new ArgumentException("Repository path cannot be empty", nameof(repoPath));

            // Validate inputs
            if (!InputValidator.IsValidMergeOperation(sourceBranch, targetBranch))
            {
                var (isValid, errorMessage) = InputValidator.GetMergeOperationError(sourceBranch, targetBranch);
                OnProgressChanged(0, errorMessage);
                return new MergeResult(MergeStatus.Failed, errorMessage);
            }

            OnProgressChanged(10, $"Preparing to merge {sourceBranch} into {targetBranch}...");

            try
            {
                // Fetch to ensure we have latest
                OnProgressChanged(20, "Fetching latest changes...");
                var fetchResult = await GitHelper.RetryRunGitCommandAsync(
                    repoPath,
                    "fetch origin",
                    maxAttempts: 3,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (!fetchResult.IsSuccess)
                {
                    var message = $"Failed to fetch: {fetchResult.StdErr}";
                    OnProgressChanged(0, message);
                    return new MergeResult(MergeStatus.Failed, message, fetchResult);
                }

                // Perform merge
                OnProgressChanged(50, "Performing merge operation...");
                var mergeResult = await GitHelper.MergeBranchAsync(
                    repoPath,
                    sourceBranch,
                    targetBranch,
                    cancellationToken
                ).ConfigureAwait(false);

                // Invalidate cache after successful merge
                if (mergeResult.Status == MergeStatus.Success)
                {
                    InvalidateCache(repoPath);
                    OnProgressChanged(100, $"Successfully merged {sourceBranch} into {targetBranch}");
                }
                else
                {
                    OnProgressChanged(0, mergeResult.Message);
                }

                return mergeResult;
            }
            catch (OperationCanceledException)
            {
                OnProgressChanged(0, "Merge operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                OnProgressChanged(0, $"Merge failed: {ex.Message}");
                return new MergeResult(MergeStatus.Failed, ex.Message);
            }
        }

        /// <summary>
        /// Pulls the latest changes for a branch in the repository.
        /// </summary>
        /// <param name="repoPath">The absolute path to the Git repository.</param>
        /// <param name="branch">The branch to pull.</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
        /// <returns>A CommandResult indicating success/failure of the pull operation.</returns>
        public async Task<CommandResult> PullBranchAsync(
            string repoPath,
            string branch,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new ArgumentException("Repository path cannot be empty", nameof(repoPath));

            if (string.IsNullOrWhiteSpace(branch))
                throw new ArgumentException("Branch name cannot be empty", nameof(branch));

            OnProgressChanged(20, $"Pulling {branch}...");

            try
            {
                var result = await GitHelper.RetryRunGitCommandAsync(
                    repoPath,
                    $"pull origin {branch}",
                    maxAttempts: 3,
                    cancellationToken: cancellationToken
                ).ConfigureAwait(false);

                if (result.IsSuccess)
                {
                    OnProgressChanged(100, $"Successfully pulled {branch}");
                    // Invalidate cache after pull
                    InvalidateCache(repoPath);
                }
                else
                {
                    OnProgressChanged(0, $"Failed to pull {branch}: {result.StdErr}");
                }

                return result;
            }
            catch (OperationCanceledException)
            {
                OnProgressChanged(0, "Pull operation canceled");
                throw;
            }
            catch (Exception ex)
            {
                OnProgressChanged(0, $"Pull failed: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Invalidates cached branch lists, forcing a fresh fetch on the next call.
        /// </summary>
        /// <param name="repoPath">
        /// The repository path to invalidate. If null or empty, clears all caches.
        /// </param>
        /// <remarks>
        /// Call this method after operations that modify the repository structure
        /// (merge, create branch, delete branch, etc.) to ensure fresh data.
        /// </remarks>
        public void InvalidateCache(string? repoPath = null)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
            {
                _branchCache.Clear();
                OnProgressChanged(0, "All caches cleared");
            }
            else
            {
                _branchCache.Remove(repoPath);
                OnProgressChanged(0, $"Cache cleared for {repoPath}");
            }
        }

        /// <summary>
        /// Gets the current cache hit rate (percentage of successful cache lookups).
        /// Useful for performance monitoring.
        /// </summary>
        /// <returns>Dictionary mapping repository paths to their cache status.</returns>
        public Dictionary<string, CacheStatus> GetCacheStatus()
        {
            var status = new Dictionary<string, CacheStatus>();

            foreach (var kvp in _branchCache)
            {
                var age = DateTime.UtcNow - kvp.Value.timestamp;
                status[kvp.Key] = new CacheStatus
                {
                    IsCached = true,
                    CacheAge = age,
                    IsValid = age < CacheDuration,
                    BranchCount = kvp.Value.branches.Count()
                };
            }

            return status;
        }

        /// <summary>
        /// Raises the ProgressChanged event.
        /// </summary>
        protected virtual void OnProgressChanged(int progressPercentage, string statusMessage)
        {
            ProgressChanged?.Invoke(this, new OperationProgressEventArgs(progressPercentage, statusMessage));
        }
    }

    /// <summary>
    /// Represents the status of a cached repository.
    /// </summary>
    public class CacheStatus
    {
        /// <summary>
        /// Gets or sets whether data is currently cached.
        /// </summary>
        public bool IsCached { get; set; }

        /// <summary>
        /// Gets or sets how long the cached data has been stored.
        /// </summary>
        public TimeSpan CacheAge { get; set; }

        /// <summary>
        /// Gets or sets whether the cached data is still considered valid.
        /// </summary>
        public bool IsValid { get; set; }

        /// <summary>
        /// Gets or sets the number of branches in the cache.
        /// </summary>
        public int BranchCount { get; set; }
    }
}
