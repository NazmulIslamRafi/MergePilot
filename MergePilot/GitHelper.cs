using System;
using System.Diagnostics;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    public record CommandResult(int ExitCode, string StdOut, string StdErr)
    {
        public bool IsSuccess => ExitCode == 0;
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
                return string.IsNullOrWhiteSpace(first) ? "origin" : first!;
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
                var res = await RunGitCommandAsync(repoPath, $"ls-remote --heads {remoteName} {branch}", TimeSpan.FromSeconds(20), cancellationToken).ConfigureAwait(false);
                return res.IsSuccess && !string.IsNullOrWhiteSpace(res.StdOut);
            }
            catch
            {
                return false;
            }
        }
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

                return new CommandResult(exitCode, stdout.TrimEnd('\r', '\n'), stderr.TrimEnd('\r', '\n'));
            }
            catch (OperationCanceledException) when (linkedCts.IsCancellationRequested)
            {
                try { if (!process.HasExited) process.Kill(); } catch { }
                return new CommandResult(-1, string.Empty, "Command canceled or timed out.");
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
            CommandResult lastResult = new CommandResult(-1, string.Empty, string.Empty);

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
            // 1) check if branch exists locally
            var showRef = await RunGitCommandAsync(repoPath, $"show-ref --verify --quiet refs/heads/{branch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!showRef.IsSuccess)
            {
                // create branch locally from origin
                var fetchCreate = await RetryRunGitCommandAsync(repoPath, $"fetch origin {branch}:{branch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
                if (!fetchCreate.IsSuccess)
                    return new BranchUpdateResult(false, $"Failed to create branch '{branch}' from origin: {fetchCreate.StdErr}");

                return new BranchUpdateResult(true, $"Created '{branch}' from origin.");
            }

            // 2) get local SHA
            var localShaRes = await RunGitCommandAsync(repoPath, $"rev-parse {branch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!localShaRes.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to get local SHA for '{branch}': {localShaRes.StdErr}");
            var localSha = localShaRes.StdOut.Trim();

            // 3) get remote SHA
            var remoteRes = await RunGitCommandAsync(repoPath, $"ls-remote origin {branch}", TimeSpan.FromSeconds(30), cancellationToken).ConfigureAwait(false);
            if (!remoteRes.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to get remote SHA for '{branch}': {remoteRes.StdErr}");

            var remoteOut = remoteRes.StdOut.Trim();
            if (string.IsNullOrWhiteSpace(remoteOut))
                return new BranchUpdateResult(false, $"Remote branch '{branch}' not found on origin.");

            var remoteSha = remoteOut.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries)[0].Trim();

            if (localSha.Equals(remoteSha, StringComparison.OrdinalIgnoreCase))
                return new BranchUpdateResult(true, $"'{branch}' is already up-to-date ({(localSha.Length >= 7 ? localSha.Substring(0, 7) : localSha)}).");

            // 4) fetch latest into local branch
            var fetch = await RetryRunGitCommandAsync(repoPath, $"fetch --no-tags origin {branch}:{branch}", 3, TimeSpan.FromSeconds(2), TimeSpan.FromMinutes(2), cancellationToken).ConfigureAwait(false);
            if (!fetch.IsSuccess)
                return new BranchUpdateResult(false, $"Failed to fetch/update '{branch}': {fetch.StdErr}");

            return new BranchUpdateResult(true, $"Updated '{branch}' -> {(remoteSha.Length >= 7 ? remoteSha.Substring(0, 7) : remoteSha)}.");
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

            // Default timeouts and retry settings
            TimeSpan perAttemptTimeout = TimeSpan.FromMinutes(2);
            int retryAttempts = 3;
            TimeSpan retryDelay = TimeSpan.FromSeconds(2);

            // 1) fetch origin source
            var fetchSource = await RetryRunGitCommandAsync(repoPath, $"fetch origin {sourceBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!fetchSource.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Failed to fetch source branch '{sourceBranch}': {fetchSource.StdErr}", fetchSource);

            // 2) fetch origin target
            var fetchTarget = await RetryRunGitCommandAsync(repoPath, $"fetch origin {targetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!fetchTarget.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Failed to fetch target branch '{targetBranch}': {fetchTarget.StdErr}", fetchTarget);

            // 3) checkout target
            var checkout = await RunGitCommandAsync(repoPath, $"checkout {targetBranch}", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!checkout.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Checkout failed for '{targetBranch}': {checkout.StdErr}", checkout);

            // 4) pull origin target (--no-edit)
            var pull = await RetryRunGitCommandAsync(repoPath, $"pull --no-edit origin {targetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!pull.IsSuccess)
                return new MergeResult(MergeStatus.Failed, $"Pull failed for '{targetBranch}': {pull.StdErr}", pull);

            // 5) check if source is already merged into target
            // git merge-base --is-ancestor origin/<source> HEAD  => exit code 0 means ancestor (already merged)
            var mergeBaseCheck = await RunGitCommandAsync(repoPath, $"merge-base --is-ancestor origin/{sourceBranch} HEAD", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (mergeBaseCheck.IsSuccess)
            {
                return new MergeResult(MergeStatus.Skipped, $"Source '{sourceBranch}' is already merged into '{targetBranch}'.");
            }

            // 6) merge origin/source into target --no-edit
            var merge = await RunGitCommandAsync(repoPath, $"merge origin/{sourceBranch} --no-edit", perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!merge.IsSuccess)
            {
                // Detect conflict hints in output/stderr
                var combined = (merge.StdOut + "\n" + merge.StdErr).ToLowerInvariant();
                if (combined.Contains("conflict") || combined.Contains("merge conflict"))
                {
                    return new MergeResult(MergeStatus.Conflict, $"Merge conflict while merging '{sourceBranch}' into '{targetBranch}':\n{merge.StdErr}\n{merge.StdOut}", merge);
                }

                return new MergeResult(MergeStatus.Failed, $"Merge failed: {merge.StdErr}\n{merge.StdOut}", merge);
            }

            // 7) push origin target (with retry)
            var push = await RetryRunGitCommandAsync(repoPath, $"push origin {targetBranch}", retryAttempts, retryDelay, perAttemptTimeout, cancellationToken).ConfigureAwait(false);
            if (!push.IsSuccess)
            {
                return new MergeResult(MergeStatus.Failed, $"Push failed for '{targetBranch}': {push.StdErr}", push);
            }

            return new MergeResult(MergeStatus.Success, $"Successfully merged {sourceBranch} → {targetBranch}", push);
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
    }
}

