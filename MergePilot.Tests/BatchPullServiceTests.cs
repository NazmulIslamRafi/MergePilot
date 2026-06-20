using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests
{
    public class BatchPullServiceTests
    {
        [Fact]
        public async Task PullSelectedBranchesAsync_WithNoBranches_ReportsError()
        {
            // Arrange
            var service = new BatchPullService();
            var output = new List<string>();
            var errors = new List<string>();
            var callbacks = new BatchPullCallbacks
            {
                Output = (message, _) => output.Add(message),
                Error = errors.Add
            };

            // Act
            var result = await service.PullSelectedBranchesAsync(
                new BatchPullRequest(new[] { "repo" }, Array.Empty<string>()),
                callbacks);

            // Assert
            Assert.Empty(result.Successful);
            Assert.Empty(result.Skipped);
            Assert.Empty(result.Failed);
            Assert.Contains(output, message => message.Contains("PULL / FETCH OPERATION"));
            Assert.Contains(errors, message => message.Contains("No source branches selected"));
        }

        [Fact]
        public async Task PullSelectedBranchesAsync_WithNonGitRepository_RecordsFailure()
        {
            // Arrange
            var repoPath = Path.Combine(Path.GetTempPath(), "MergePilot.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(repoPath);

            try
            {
                var service = new BatchPullService();
                var errors = new List<string>();
                var callbacks = new BatchPullCallbacks
                {
                    Output = (_, _) => { },
                    Error = errors.Add
                };

                // Act
                var result = await service.PullSelectedBranchesAsync(
                    new BatchPullRequest(new[] { repoPath }, new[] { "main" }),
                    callbacks);

                // Assert
                Assert.Empty(result.Successful);
                Assert.Empty(result.Skipped);
                Assert.Single(result.Failed);
                Assert.Contains("repo not found", result.Failed[0]);
                Assert.Contains(errors, message => message.Contains("not a git repo"));
            }
            finally
            {
                if (Directory.Exists(repoPath))
                    Directory.Delete(repoPath, recursive: true);
            }
        }

        [Fact]
        public async Task PullSelectedBranchesAsync_WithNoRepositories_WritesFinalSummary()
        {
            // Arrange
            var service = new BatchPullService();
            var output = new List<string>();
            var callbacks = new BatchPullCallbacks
            {
                Output = (message, _) => output.Add(message),
                Error = _ => { }
            };

            // Act
            var result = await service.PullSelectedBranchesAsync(
                new BatchPullRequest(Array.Empty<string>(), new[] { "main" }),
                callbacks);

            // Assert
            Assert.Empty(result.Successful);
            Assert.Empty(result.Skipped);
            Assert.Empty(result.Failed);
            Assert.Contains(output, message => message.Contains("Total: 0 operations processed"));
        }
    }
}
