using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace MergePilot.Tests
{
    public class BatchOperationWorkflowRunnerTests : IDisposable
    {
        private readonly string _settingsDirectory;

        public BatchOperationWorkflowRunnerTests()
        {
            _settingsDirectory = Path.Combine(Path.GetTempPath(), "MergePilot.BatchOperationWorkflowRunner", Guid.NewGuid().ToString());
            Directory.CreateDirectory(_settingsDirectory);
            AppSettings.SettingsPathOverride = Path.Combine(_settingsDirectory, "settings.json");
        }

        public void Dispose()
        {
            AppSettings.SettingsPathOverride = null;

            if (Directory.Exists(_settingsDirectory))
                Directory.Delete(_settingsDirectory, recursive: true);
        }

        [Fact]
        public async Task RunMergeAsync_CreatesRequestPersistsLastBranchesAndInvokesRunner()
        {
            var settings = new AppSettings();
            BatchMergeRequest? capturedRequest = null;
            BatchMergeCallbacks? capturedCallbacks = null;
            CancellationToken capturedToken = default;
            using var cts = new CancellationTokenSource();
            var callbacks = new BatchMergeCallbacks();
            var expectedResult = new BatchMergeResult();
            var runner = new BatchOperationWorkflowRunner(
                mergeRunner: (request, callbackHooks, token) =>
                {
                    capturedRequest = request;
                    capturedCallbacks = callbackHooks;
                    capturedToken = token;
                    return Task.FromResult(expectedResult);
                });

            var result = await runner.RunMergeAsync(
                settings,
                new[] { "C:\\repo-a", "C:\\repo-b" },
                new[] { "feature/a" },
                new[] { "develop", "release" },
                callbacks,
                cts.Token);

            Assert.Same(expectedResult, result);
            Assert.Same(callbacks, capturedCallbacks);
            Assert.Equal(cts.Token, capturedToken);
            Assert.NotNull(capturedRequest);
            Assert.Equal(new[] { "C:\\repo-a", "C:\\repo-b" }, capturedRequest.RepositoryPaths);
            Assert.Equal(new[] { "feature/a" }, capturedRequest.SourceBranches);
            Assert.Equal(new[] { "develop", "release" }, capturedRequest.TargetBranches);
            Assert.Equal("feature/a", settings.LastSourceBranch);
            Assert.Equal("develop", settings.LastTargetBranch);
            Assert.True(File.Exists(AppSettings.SettingsPathOverride));
        }

        [Fact]
        public async Task RunPullAsync_CreatesRequestAndInvokesRunner()
        {
            BatchPullRequest? capturedRequest = null;
            BatchPullCallbacks? capturedCallbacks = null;
            CancellationToken capturedToken = default;
            using var cts = new CancellationTokenSource();
            var callbacks = new BatchPullCallbacks();
            var expectedResult = new BatchPullResult();
            var runner = new BatchOperationWorkflowRunner(
                pullRunner: (request, callbackHooks, token) =>
                {
                    capturedRequest = request;
                    capturedCallbacks = callbackHooks;
                    capturedToken = token;
                    return Task.FromResult(expectedResult);
                });

            var result = await runner.RunPullAsync(
                new[] { "C:\\repo-a", "C:\\repo-b" },
                new[] { "main", "feature/a" },
                callbacks,
                cts.Token);

            Assert.Same(expectedResult, result);
            Assert.Same(callbacks, capturedCallbacks);
            Assert.Equal(cts.Token, capturedToken);
            Assert.NotNull(capturedRequest);
            Assert.Equal(new[] { "C:\\repo-a", "C:\\repo-b" }, capturedRequest.RepositoryPaths);
            Assert.Equal(new[] { "main", "feature/a" }, capturedRequest.SourceBranches);
        }

        [Fact]
        public async Task RunMergeAsync_RequiresSettingsAndCallbacks()
        {
            var runner = new BatchOperationWorkflowRunner();
            var callbacks = new BatchMergeCallbacks();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                runner.RunMergeAsync(null!, Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), callbacks));

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                runner.RunMergeAsync(new AppSettings(), Array.Empty<string>(), Array.Empty<string>(), Array.Empty<string>(), null!));
        }

        [Fact]
        public async Task RunPullAsync_RequiresCallbacks()
        {
            var runner = new BatchOperationWorkflowRunner();

            await Assert.ThrowsAsync<ArgumentNullException>(() =>
                runner.RunPullAsync(Array.Empty<string>(), Array.Empty<string>(), null!));
        }
    }
}
