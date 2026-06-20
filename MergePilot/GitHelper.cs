using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    public record CommandResult(
        int ExitCode,
        string StdOut,
        string StdErr,
        GitCommandErrorKind ErrorKind = GitCommandErrorKind.None)
    {
        public bool IsSuccess => ExitCode == 0;
    }

    public enum GitCommandErrorKind
    {
        None,
        Validation,
        Repository,
        Authentication,
        Remote,
        Timeout,
        Conflict,
        Network,
        Unknown
    }

    public static class GitCommandErrorClassifier
    {
        public static GitCommandErrorKind Classify(int exitCode, string? stdout, string? stderr)
        {
            if (exitCode == 0)
                return GitCommandErrorKind.None;

            var combined = $"{stdout} {stderr}".ToLowerInvariant();

            if (ContainsAny(combined, "timed out", "timeout", "command canceled"))
                return GitCommandErrorKind.Timeout;

            if (ContainsAny(combined, "merge conflict", "automatic merge failed", "conflict"))
                return GitCommandErrorKind.Conflict;

            if (ContainsAny(combined, "authentication failed", "permission denied", "publickey", "access denied", "could not read from remote repository"))
                return GitCommandErrorKind.Authentication;

            if (ContainsAny(combined, "could not resolve host", "failed to connect", "network is unreachable", "connection timed out"))
                return GitCommandErrorKind.Network;

            if (ContainsAny(combined, "not a git repository", "repository path not found"))
                return GitCommandErrorKind.Repository;

            if (ContainsAny(combined, "does not appear to be a git repository", "repository not found", "remote ref does not exist", "couldn't find remote ref", "remote origin already exists"))
                return GitCommandErrorKind.Remote;

            return GitCommandErrorKind.Unknown;
        }

        private static bool ContainsAny(string value, params string[] patterns)
        {
            return patterns.Any(pattern => value.Contains(pattern, StringComparison.OrdinalIgnoreCase));
        }
    }

    public enum MergeStatus
    {
        Success,
        Skipped,
        Conflict,
        Failed
    }

    public record MergeResult(MergeStatus Status, string Message, CommandResult? LastCommandResult = null);

    public static class GitHelper
    {
        /// <summary>
        /// Returns the default remote name for the repository (first remote from `git remote`), falls back to "origin" when none found.
        /// </summary>
        public static async Task<string> GetDefaultRemoteNameAsync(string repoPath, CancellationToken cancellationToken = default)
        {
            try
            {
                var res = await RunGitCommandAsync(repoPath, "remote", TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                if (!res.IsSuccess || string.IsNullOrWhiteSpace(res.StdOut))
                    return "origin";

                var first = res.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries).Select(s => s.Trim()).FirstOrDefault();
                return GitCommandSafety.TryNormalizeRemoteName(first, out var remoteName, out _)
                    ? remoteName
                    : "origin";
            }
            catch
            {
                return "origin";
            }
        }

        /// <summary>
        /// Returns true if the specified branch exists on the given remote for the repository (via ls-remote).
        /// </summary>
        public static async Task<bool> RemoteBranchExistsAsync(string repoPath, string remoteName, string branch, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(branch)) return false;
            try
            {
                if (!GitCommandSafety.TryNormalizeRemoteName(remoteName, out var safeRemoteName, out _) ||
                    !GitCommandSafety.TryNormalizeBranchName(branch, out var safeBranch, out _))
                    return false;

                var res = await RunGitCommandAsync(repoPath, $"ls-remote --heads {safeRemoteName} {safeBranch}", TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
                return res.IsSuccess && !string.IsNullOrWhiteSpace(res.StdOut);
            }
            catch
            {
                return false;
            }
        }
        /// <summary>
        /// Executes a Git command in the specified repository with proper error handling.
        /// </summary>
        /// <param name="repoPath">The absolute path to the Git repository root directory.</param>
        /// <param name="arguments">The Git command arguments (e.g., "status", "fetch origin main").</param>
        /// <param name="timeout">Optional timeout for command execution. Defaults to 2 minutes per attempt.</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
        /// <returns>
        /// A <see cref="CommandResult"/> containing the exit code, stdout, and stderr.
        /// Check <see cref="CommandResult.IsSuccess"/> to determine if execution was successful (exit code 0).
        /// </returns>
        /// <exception cref="ArgumentException">Thrown if repoPath is null or empty.</exception>
        /// <exception cref="DirectoryNotFoundException">Thrown if repoPath does not exist.</exception>
        /// <remarks>
        /// This method properly manages process lifetime and captures both standard output
        /// and error streams without deadlock using async operations. Process is killed if
        /// timeout is exceeded or cancellation is requested.
        /// </remarks>
        /// <example>
        /// <code>
        /// var result = await GitHelper.RunGitCommandAsync(
        ///     "/path/to/repo",
        ///     "fetch origin",
        ///     TimeSpan.FromMinutes(5)
        /// );
        ///
        /// if (result.IsSuccess)
        /// {
        ///     Console.WriteLine($"Success: {result.StdOut}");
        /// }
        /// else
        /// {
        ///     Console.WriteLine($"Error: {result.StdErr}");
        /// }
        /// </code>
        /// </example>
        internal static async Task<CommandResult> RunGitCommandAsync(
            string repoPath,
            string arguments,
            TimeSpan? timeout = null,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(repoPath))
                throw new ArgumentException("repoPath is required", nameof(repoPath));
            if (!Directory.Exists(repoPath))
                throw new DirectoryNotFoundException($"Repository path not found: {repoPath}");

            using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            if (timeout.HasValue && timeout.Value != Timeout.InfiniteTimeSpan)
                linkedCts.CancelAfter(timeout.Value);

            var psi = new ProcessStartInfo
            {
                FileName = "git",
                Arguments = arguments,
                WorkingDirectory = repoPath,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using var process = new Process { StartInfo = psi, EnableRaisingEvents = true };
            try
            {
                if (!process.Start())
                    throw new Exception("Failed to start git process.");

                // Read streams asynchronously to avoid deadlocks
                var stdOutTask = process.StandardOutput.ReadToEndAsync();
                var stdErrTask = process.StandardError.ReadToEndAsync();
                var waitTask = process.WaitForExitAsync(linkedCts.Token);

                await Task.WhenAll(waitTask, stdOutTask, stdErrTask).ConfigureAwait(false);

                var stdout = await stdOutTask.ConfigureAwait(false) ?? string.Empty;
                var stderr = await stdErrTask.ConfigureAwait(false) ?? string.Empty;
                var exitCode = process.ExitCode;

                var normalizedStdOut = stdout.TrimEnd('\r', '\n');
                var normalizedStdErr = stderr.TrimEnd('\r', '\n');
                return new CommandResult(
                    exitCode,
                    normalizedStdOut,
                    normalizedStdErr,
                    GitCommandErrorClassifier.Classify(exitCode, normalizedStdOut, normalizedStdErr));
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                return new CommandResult(-1, string.Empty, "Command canceled or timed out.", GitCommandErrorKind.Timeout);
            }
        }

        /// <summary>
        /// Retries a git command up to <paramref name="maxAttempts"/> times when the command returns a non-zero exit code.
        /// Returns the last CommandResult.
        /// </summary>
        public static async Task<CommandResult> RetryRunGitCommandAsync(
            string repoPath,
            string arguments,
            int maxAttempts = 3,
            TimeSpan? delayBetweenAttempts = null,
            TimeSpan? timeoutPerAttempt = null,
            CancellationToken cancellationToken = default)
        {
            delayBetweenAttempts ??= TimeSpan.FromSeconds(2);
            CommandResult lastResult = new CommandResult(-1, string.Empty, string.Empty, GitCommandErrorKind.Unknown);

            for (int attempt = 1; attempt <= maxAttempts; attempt++)
            {
                lastResult = await RunGitCommandAsync(repoPath, arguments, timeoutPerAttempt, cancellationToken).ConfigureAwait(false);
                if (lastResult.IsSuccess)
                    return lastResult;

                if (attempt < maxAttempts)
                    await Task.Delay(delayBetweenAttempts.Value, cancellationToken).ConfigureAwait(false);
            }

            return lastResult;
        }

        /// <summary>
        /// Result for branch update operations.
        /// </summary>
        public class BranchUpdateResult
        {
            public bool IsSuccess { get; }
            public string Message { get; }

            public BranchUpdateResult(bool isSuccess, string message)
            {
                IsSuccess = isSuccess;
                Message = message;
            }
        }

        /// <summary>
        /// Ensures the specified branch exists locally and is up-to-date with origin.
        /// - If the branch does not exist locally, attempts to fetch/create it from origin.
        /// - If the branch exists, compares local SHA with origin and fetches latest if out-of-date.
        /// Returns a BranchUpdateResult with a friendly message and success flag.
        /// </summary>
        public static async Task<BranchUpdateResult> EnsureBranchLatestAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeBranchName(branch, out var safeBranch, out var branchError))
                return new BranchUpdateResult(false, $"Invalid branch '{branch}': {branchError}");

            // 1) check if branch exists locally
            var showRef = await RunGitCommandAsync(repoPath, $"show-ref --verify --quiet refs/heads/{safeBranch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!showRef.IsSuccess)
            {
                // create branch locally from origin
                var fetchCreate = await RetryRunGitCommandAsync(repoPath, $"fetch origin {safeBranch}:{safeBranch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
                if (!fetchCreate.IsSuccess)
                    return new BranchUpdateResult(false, $"Failed to create branch '{safeBranch}' from origin: {fetchCreate.StdErr}");

                return new BranchUpdateResult(true, $"Created '{safeBranch}' from origin.");
            }

            // 2) get local SHA
            var localShaRes = await RunGitCommandAsync(repoPath, $"rev-parse {safeBranch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!localShaRes.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to get local SHA for '{safeBranch}': {localShaRes.StdErr}");
            var localSha = localShaRes.StdOut.Trim();

            // 3) get remote SHA
            var remoteRes = await RunGitCommandAsync(repoPath, $"ls-remote origin {safeBranch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!remoteRes.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to get remote SHA for '{safeBranch}': {remoteRes.StdErr}");

            var remoteOut = remoteRes.StdOut.Trim();
            if (string.IsNullOrWhiteSpace(remoteOut))
                return new BranchUpdateResult(false, $"Remote branch '{safeBranch}' not found on origin.");

            var remoteSha = remoteOut.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            if (string.IsNullOrWhiteSpace(remoteSha))
                return new BranchUpdateResult(false, $"Failed to parse remote SHA for '{safeBranch}'.");

            if (string.Equals(localSha, remoteSha, StringComparison.OrdinalIgnoreCase))
            {
                var shortSha = localSha?.Length > 7 ? localSha[..7] : localSha;

                return new BranchUpdateResult(true, $"'{safeBranch}' is already up-to-date ({shortSha}).");
            }

            // 4) fetch latest into local branch
            var fetch = await RetryRunGitCommandAsync(repoPath, $"fetch --no-tags origin {safeBranch}:{safeBranch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
            if (!fetch.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to fetch/update '{safeBranch}': {fetch.StdErr}");

            return new BranchUpdateResult(true, $"Updated '{safeBranch}' -> {(remoteSha.Length >= 7 ? remoteSha.Substring(0, 7) : remoteSha)}.");
        }

        /// <summary>
        /// Merges sourceBranch into targetBranch following:
        /// - fetch origin source & target (with retries)
        /// - checkout target
        /// - pull origin target (--no-edit) (with retries)
        /// - skip if origin/source already merged into target (merge-base --is-ancestor)
        /// - merge origin/source (--no-edit)
        /// - push origin target (with retries)
        /// Returns a MergeResult with status and helpful message.
        /// </summary>
        public static async Task<MergeResult> MergeBranchAsync(
            string repoPath,
            string sourceBranch,
            string targetBranch,
            CancellationToken cancellationToken = default)
        {
            // Basic validation
            if (string.IsNullOrWhiteSpace(sourceBranch))
                return new MergeResult(MergeStatus.Failed, "Source branch is empty.");

            if (string.IsNullOrWhiteSpace(targetBranch))
                return new MergeResult(MergeStatus.Failed, "Target branch is empty.");

            if (!GitCommandSafety.TryNormalizeBranchName(sourceBranch, out var safeSourceBranch, out var sourceError))
                return new MergeResult(MergeStatus.Failed, $"Invalid source branch '{sourceBranch}': {sourceError}");

            if (!GitCommandSafety.TryNormalizeBranchName(targetBranch, out var safeTargetBranch, out var targetError))
                return new MergeResult(MergeStatus.Failed, $"Invalid target branch '{targetBranch}': {targetError}");

            // Default timeouts and retry settings
            TimeSpan perAttemptTimeout = TimeSpan.FromMinutes(2);
            int retryAttempts = 3;
            TimeSpan retryDelay = TimeSpan.FromSeconds(2);

            // 1) fetch origin source
            var fetchSource = await RetryRunGitCommandAsync(repoPath, $"fetch origin {safeSourceBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!fetchSource.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Failed to fetch source branch '{safeSourceBranch}': {fetchSource.StdErr}", fetchSource);

            // 2) fetch origin target
            var fetchTarget = await RetryRunGitCommandAsync(repoPath, $"fetch origin {safeTargetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!fetchTarget.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Failed to fetch target branch '{safeTargetBranch}': {fetchTarget.StdErr}", fetchTarget);

            // 3) checkout target
            var checkout = await RunGitCommandAsync(repoPath, $"checkout {safeTargetBranch}", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!checkout.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Checkout failed for '{safeTargetBranch}': {checkout.StdErr}", checkout);

            // 4) pull origin target (--no-edit)
            var pull = await RetryRunGitCommandAsync(repoPath, $"pull --no-edit origin {safeTargetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!pull.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Pull failed for '{safeTargetBranch}': {pull.StdErr}", pull);

            // 5) check if source is already merged into target
            // git merge-base --is-ancestor origin/<source> HEAD  => exit code 0 means ancestor (already merged)
            var mergeBaseCheck = await RunGitCommandAsync(repoPath, $"merge-base --is-ancestor origin/{safeSourceBranch} HEAD", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (mergeBaseCheck.IsSuccess)
            {
                return new MergeResult(MergeStatus.Skipped, $"Source '{safeSourceBranch}' is already merged into '{safeTargetBranch}'.");
            }

            // 6) merge origin/source into target --no-edit
            var merge = await RunGitCommandAsync(repoPath, $"merge origin/{safeSourceBranch} --no-edit", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!merge.IsSuccess)
            {
                // Detect conflict hints in output/stderr
                var combined = (merge.StdOut + "\n" + merge.StdErr).ToLowerInvariant();
                if (combined.Contains("conflict") || combined.Contains("merge conflict"))
                {
                    return new MergeResult(MergeStatus.Conflict, $"Merge conflict while merging '{safeSourceBranch}' into '{safeTargetBranch}':\n{merge.StdErr}\n{merge.StdOut}", merge);
                }

                return new MergeResult(MergeStatus.Failed, $"Merge failed: {merge.StdErr}\n{merge.StdOut}", merge);
            }

            // 7) push origin target (with retry)
            var push = await RetryRunGitCommandAsync(repoPath, $"push origin {safeTargetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!push.IsSuccess)
            {
                return new MergeResult(MergeStatus.Failed, $"Push failed for '{safeTargetBranch}': {push.StdErr}", push);
            }

            return new MergeResult(MergeStatus.Success, $"Successfully merged {safeSourceBranch} → {safeTargetBranch}", push);
        }

        /// <summary>
        /// Abort an in-progress merge (wrapper for 'git merge --abort').
        /// </summary>
        public static async Task<CommandResult> AbortMergeAsync(string repoPath, CancellationToken cancellationToken = default)
        {
            return await RunGitCommandAsync(repoPath, "merge --abort", TimeSpan.FromMinutes(1), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Quick check for git availability (git --version).
        /// </summary>
        public static async Task<bool> IsGitAvailableAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                var result = await RunGitCommandAsync(".", "--version", TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Get list of local branches for a repository.
        /// </summary>
        public static async Task<List<string>> GetLocalBranchesAsync(string repoPath, CancellationToken cancellationToken = default)
        {
            var branches = new List<string>();
            try
            {
                var result = await RunGitCommandAsync(repoPath, "branch --list", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        var branch = line.TrimStart(new[] { '*', ' ' }).Trim();
                        if (!string.IsNullOrWhiteSpace(branch))
                            branches.Add(branch);
                    }
                }
            }
            catch { }
            return branches;
        }

        /// <summary>
        /// Get list of remote branches for a repository.
        /// </summary>
        public static async Task<List<string>> GetRemoteBranchesAsync(string repoPath, string remoteName = "origin", CancellationToken cancellationToken = default)
        {
            var branches = new List<string>();
            try
            {
                if (!GitCommandSafety.TryNormalizeRemoteName(remoteName, out var safeRemoteName, out _))
                    return branches;

                var result = await RunGitCommandAsync(repoPath, $"ls-remote --heads {safeRemoteName}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                if (result.IsSuccess)
                {
                    foreach (var line in result.StdOut.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries))
                    {
                        // Format: <sha>\trefs/heads/<branch>
                        var parts = line.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);
                        if (parts.Length >= 2)
                        {
                            var refPart = parts[1];
                            var prefix = "refs/heads/";
                            if (refPart.StartsWith(prefix))
                            {
                                var branch = refPart.Substring(prefix.Length).Trim();
                                if (!string.IsNullOrWhiteSpace(branch))
                                    branches.Add(branch);
                            }
                        }
                    }
                }
            }
            catch { }
            return branches;
        }

        /// <summary>
        /// Returns a one-line last-commit summary for a branch, e.g. "a1b2c3 • 2h ago • fix login bug".
        /// Tries origin/branch first, then local branch. Returns "—" when no commit is found.
        /// </summary>
        public static async Task<string> GetLastCommitForBranchAsync(string repoPath, string branchName, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeBranchName(branchName, out var safeBranch, out _))
                return "—";

            const string fmt = "--format=%h • %ar • %s";

            // Try remote branch (origin/<branch>) first
            var remote = await RunGitCommandAsync(repoPath, $"log origin/{safeBranch} -1 {fmt}", TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            if (remote.IsSuccess && !string.IsNullOrWhiteSpace(remote.StdOut))
                return remote.StdOut.Trim();

            // Fall back to local branch
            var local = await RunGitCommandAsync(repoPath, $"log {safeBranch} -1 {fmt}", TimeSpan.FromSeconds(10), cancellationToken).ConfigureAwait(false);
            if (local.IsSuccess && !string.IsNullOrWhiteSpace(local.StdOut))
                return local.StdOut.Trim();

            return "—";
        }

        /// <summary>
        /// Checkout a specific branch.
        /// </summary>
        public static async Task<CommandResult> CheckoutBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeBranchName(branch, out var safeBranch, out var error))
                return new CommandResult(-1, string.Empty, $"Invalid branch '{branch}': {error}", GitCommandErrorKind.Validation);

            return await RunGitCommandAsync(repoPath, $"checkout {safeBranch}", TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Gets all branches (both local and remote) organized hierarchically as BranchItem objects.
        /// </summary>
        /// <param name="repoPath">The absolute path to the Git repository.</param>
        /// <param name="cancellationToken">Cancellation token for operation cancellation.</param>
        /// <returns>An enumerable of BranchItem representing the hierarchical branch structure.</returns>
        /// <remarks>
        /// This method combines local and remote branches into a single hierarchical structure.
        /// Branches are organized by their prefix (e.g., "feature/", "bugfix/") with separators.
        /// Groups are automatically created for common prefixes.
        /// </remarks>
        public static async Task<IEnumerable<BranchItem>> GetBranchesAsync(
            string repoPath,
            CancellationToken cancellationToken = default)
        {
            try
            {
                // Fetch both local and remote branches concurrently
                var localTask = GetLocalBranchesAsync(repoPath, cancellationToken);
                var remoteTask = GetRemoteBranchesAsync(repoPath, "origin", cancellationToken);

                await Task.WhenAll(localTask, remoteTask).ConfigureAwait(false);

                var localBranches = await localTask.ConfigureAwait(false);
                var remoteBranches = await remoteTask.ConfigureAwait(false);

                // Combine and organize into hierarchy
                var allBranches = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                allBranches.UnionWith(localBranches);
                allBranches.UnionWith(remoteBranches);

                return OrganizeBranchesHierarchically(allBranches.OrderBy(b => b));
            }
            catch
            {
                return Enumerable.Empty<BranchItem>();
            }
        }

        /// <summary>
        /// Organizes a flat list of branch names into a hierarchical BranchItem structure.
        /// </summary>
        private static IEnumerable<BranchItem> OrganizeBranchesHierarchically(IEnumerable<string> branches)
        {
            var result = new List<BranchItem>();
            var groupDict = new Dictionary<string, BranchItem>(StringComparer.OrdinalIgnoreCase);

            foreach (var branch in branches)
            {
                var parts = branch.Split('/');

                if (parts.Length == 1)
                {
                    // Simple branch without prefix
                    result.Add(new BranchItem(branch, branch, isGroup: false, level: 0));
                }
                else
                {
                    // Create group hierarchy
                    var groupPath = "";
                    BranchItem? currentParent = null;
                    int level = 0;

                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        groupPath = i == 0 ? parts[i] : $"{groupPath}/{parts[i]}";

                        if (!groupDict.TryGetValue(groupPath, out var group))
                        {
                            group = new BranchItem(parts[i], groupPath, isGroup: true, level: level, parent: currentParent);
                            groupDict[groupPath] = group;

                            if (currentParent == null)
                                result.Add(group);
                            else
                                currentParent.Children.Add(group);
                        }

                        currentParent = group;
                        level++;
                    }

                    // Add the actual branch to its parent group
                    if (currentParent != null)
                    {
                        var leafBranch = new BranchItem(parts[parts.Length - 1], branch, isGroup: false, level: level, parent: currentParent);
                        currentParent.Children.Add(leafBranch);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Pull a branch from origin with --no-edit.
        /// </summary>
        public static async Task<CommandResult> PullBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeBranchName(branch, out var safeBranch, out var error))
                return new CommandResult(-1, string.Empty, $"Invalid branch '{branch}': {error}", GitCommandErrorKind.Validation);

            return await RetryRunGitCommandAsync(repoPath, $"pull --no-edit origin {safeBranch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Check if a branch is already merged using merge-base.
        /// </summary>
        public static async Task<bool> IsBranchMergedAsync(string repoPath, string remoteName, string sourceBranch, string targetBranch, CancellationToken cancellationToken = default)
        {
            try
            {
                if (!GitCommandSafety.TryNormalizeRemoteName(remoteName, out var safeRemoteName, out _) ||
                    !GitCommandSafety.TryNormalizeBranchName(sourceBranch, out var safeSourceBranch, out _) ||
                    !GitCommandSafety.TryNormalizeBranchName(targetBranch, out var safeTargetBranch, out _))
                    return false;

                var result = await RunGitCommandAsync(repoPath, $"merge-base --is-ancestor {safeRemoteName}/{safeSourceBranch} {safeTargetBranch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
                return result.IsSuccess;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Merge a branch into current branch with --no-edit.
        /// </summary>
        public static async Task<CommandResult> MergeBranchSimpleAsync(string repoPath, string remoteName, string sourceBranch, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeRemoteName(remoteName, out var safeRemoteName, out var remoteError))
                return new CommandResult(-1, string.Empty, $"Invalid remote '{remoteName}': {remoteError}", GitCommandErrorKind.Validation);

            if (!GitCommandSafety.TryNormalizeBranchName(sourceBranch, out var safeSourceBranch, out var branchError))
                return new CommandResult(-1, string.Empty, $"Invalid branch '{sourceBranch}': {branchError}", GitCommandErrorKind.Validation);

            return await RunGitCommandAsync(repoPath, $"merge {safeRemoteName}/{safeSourceBranch} --no-edit", TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Merge a branch with retry logic.
        /// </summary>
        public static async Task<CommandResult> MergeBranchAsync(string repoPath, string remoteName, string sourceBranch, bool noEdit = true, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeRemoteName(remoteName, out var safeRemoteName, out var remoteError))
                return new CommandResult(-1, string.Empty, $"Invalid remote '{remoteName}': {remoteError}", GitCommandErrorKind.Validation);

            if (!GitCommandSafety.TryNormalizeBranchName(sourceBranch, out var safeSourceBranch, out var branchError))
                return new CommandResult(-1, string.Empty, $"Invalid branch '{sourceBranch}': {branchError}", GitCommandErrorKind.Validation);

            var noEditFlag = noEdit ? "--no-edit" : string.Empty;
            return await RunGitCommandAsync(repoPath, $"merge {safeRemoteName}/{safeSourceBranch} {noEditFlag}".Trim(), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        }

        /// <summary>
        /// Push a branch to origin.
        /// </summary>
        public static async Task<CommandResult> PushBranchAsync(string repoPath, string branch, CancellationToken cancellationToken = default)
        {
            if (!GitCommandSafety.TryNormalizeBranchName(branch, out var safeBranch, out var error))
                return new CommandResult(-1, string.Empty, $"Invalid branch '{branch}': {error}", GitCommandErrorKind.Validation);

            return await RetryRunGitCommandAsync(repoPath, $"push origin {safeBranch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
        }
    }
}

