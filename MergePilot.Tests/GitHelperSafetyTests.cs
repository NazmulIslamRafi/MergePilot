using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests
{
    public class GitHelperSafetyTests : IDisposable
    {
        private readonly string _tempDirectory;

        public GitHelperSafetyTests()
        {
            _tempDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.GitHelperSafety", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_tempDirectory);
        }

        public void Dispose()
        {
            if (Directory.Exists(_tempDirectory))
                Directory.Delete(_tempDirectory, recursive: true);
        }

        [Fact]
        public async Task EnsureBranchLatestAsync_WithUnsafeBranch_ReturnsFailureBeforeGitRuns()
        {
            var result = await GitHelper.EnsureBranchLatestAsync("Z:\\repo-does-not-exist", "--upload-pack=bad");

            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid branch", result.Message);
        }

        [Fact]
        public async Task PullBranchAsync_WithUnsafeBranch_ReturnsFailureBeforeGitRuns()
        {
            var result = await GitHelper.PullBranchAsync("Z:\\repo-does-not-exist", "feature branch");

            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid branch", result.StdErr);
            Assert.Equal(GitCommandErrorKind.Validation, result.ErrorKind);
        }

        [Fact]
        public async Task PushBranchAsync_WithUnsafeBranch_ReturnsFailureBeforeGitRuns()
        {
            var result = await GitHelper.PushBranchAsync("Z:\\repo-does-not-exist", "feature\"branch");

            Assert.False(result.IsSuccess);
            Assert.Contains("Invalid branch", result.StdErr);
            Assert.Equal(GitCommandErrorKind.Validation, result.ErrorKind);
        }

        [Fact]
        public async Task RemoteBranchExistsAsync_WithUnsafeRemote_ReturnsFalseBeforeGitRuns()
        {
            var exists = await GitHelper.RemoteBranchExistsAsync("Z:\\repo-does-not-exist", "--bad", "main");

            Assert.False(exists);
        }

        [Fact]
        public async Task MergeBranchAsync_WithUnsafeSource_ReturnsFailureBeforeGitRuns()
        {
            var result = await GitHelper.MergeBranchAsync("Z:\\repo-does-not-exist", "feature branch", "main", CancellationToken.None);

            Assert.Equal(MergeStatus.Failed, result.Status);
            Assert.Contains("Invalid source branch", result.Message);
        }

        [Fact]
        public async Task BatchPullService_WithUnsafeBranch_ReportsFailedBranch()
        {
            var repoPath = CreateGitRepositoryDirectory();
            var errors = new System.Collections.Generic.List<string>();
            var service = new BatchPullService();

            var result = await service.PullSelectedBranchesAsync(
                new BatchPullRequest(new[] { repoPath }, new[] { "feature branch" }),
                new BatchPullCallbacks
                {
                    Output = (_, _) => { },
                    Error = errors.Add
                });

            Assert.Empty(result.Successful);
            Assert.Single(result.Failed);
            Assert.Contains("Invalid branch", errors[0]);
        }

        private string CreateGitRepositoryDirectory()
        {
            var repoPath = Path.Combine(_tempDirectory, "repo");
            Directory.CreateDirectory(repoPath);
            Directory.CreateDirectory(Path.Combine(repoPath, ".git"));
            return repoPath;
        }
    }
}
