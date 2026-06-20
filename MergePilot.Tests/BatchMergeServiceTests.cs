using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests
{
    public class BatchMergeServiceTests
    {
        [Fact]
        public async Task MergeSelectedBranchesAsync_WithNoSources_ReportsErrorAndFinalSummary()
        {
            // Arrange
            var service = new BatchMergeService();
            var output = new List<string>();
            var errors = new List<string>();
            var callbacks = new BatchMergeCallbacks
            {
                Output = (message, _) => output.Add(message),
                Error = errors.Add
            };

            // Act
            var result = await service.MergeSelectedBranchesAsync(
                new BatchMergeRequest(new[] { "repo" }, Array.Empty<string>(), new[] { "main" }),
                callbacks);

            // Assert
            Assert.Empty(result.Successful);
            Assert.Empty(result.Skipped);
            Assert.Empty(result.Failed);
            Assert.Contains(errors, message => message.Contains("No source branches selected"));
            Assert.Contains(output, message => message.Contains("MERGE OPERATION"));
            Assert.Contains(output, message => message.Contains("Merging finished"));
        }

        [Fact]
        public async Task MergeSelectedBranchesAsync_WithNoTargets_ReportsErrorAndFinalSummary()
        {
            // Arrange
            var service = new BatchMergeService();
            var output = new List<string>();
            var errors = new List<string>();
            var callbacks = new BatchMergeCallbacks
            {
                Output = (message, _) => output.Add(message),
                Error = errors.Add
            };

            // Act
            var result = await service.MergeSelectedBranchesAsync(
                new BatchMergeRequest(new[] { "repo" }, new[] { "feature" }, Array.Empty<string>()),
                callbacks);

            // Assert
            Assert.Empty(result.Successful);
            Assert.Empty(result.Skipped);
            Assert.Empty(result.Failed);
            Assert.Contains(errors, message => message.Contains("No target branches selected"));
            Assert.Contains(output, message => message.Contains("Merging finished"));
        }

        [Fact]
        public async Task MergeSelectedBranchesAsync_WithNonGitRepository_RecordsFailure()
        {
            // Arrange
            var repoPath = Path.Combine(Path.GetTempPath(), "MergePilot.Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(repoPath);

            try
            {
                var service = new BatchMergeService();
                var errors = new List<string>();
                var callbacks = new BatchMergeCallbacks
                {
                    Output = (_, _) => { },
                    Error = errors.Add
                };

                // Act
                var result = await service.MergeSelectedBranchesAsync(
                    new BatchMergeRequest(new[] { repoPath }, new[] { "feature" }, new[] { "main" }),
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
        public async Task MergeSelectedBranchesAsync_WithNoRepositories_WritesZeroOperationSummary()
        {
            // Arrange
            var service = new BatchMergeService();
            var output = new List<string>();
            var callbacks = new BatchMergeCallbacks
            {
                Output = (message, _) => output.Add(message),
                Error = _ => { }
            };

            // Act
            var result = await service.MergeSelectedBranchesAsync(
                new BatchMergeRequest(Array.Empty<string>(), new[] { "feature" }, new[] { "main" }),
                callbacks);

            // Assert
            Assert.Empty(result.Successful);
            Assert.Empty(result.Skipped);
            Assert.Empty(result.Failed);
            Assert.Contains(output, message => message.Contains("Total: 0 operations processed"));
        }
    }
}
