using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;
using Xunit.Abstractions;

namespace MergePilot.Tests
{
    public class PerformanceScaleSmokeTests
    {
        private readonly ITestOutputHelper _output;

        public PerformanceScaleSmokeTests(ITestOutputHelper output)
        {
            _output = output;
        }

        [Fact]
        public void BranchDropdownProjection_WithTenThousandBranches_PreservesAllLeaves()
        {
            var service = new BranchDropdownProjectionService();
            var branches = Enumerable.Range(0, 10_000)
                .Select(index => $"feature/team-{index / 250}/area-{index / 25}/branch-{index}")
                .ToList();

            var projection = service.Build(branches, branches);

            Assert.Equal(10_000, CountLeaves(projection.SourceItems));
            Assert.Equal(10_000, CountLeaves(projection.TargetItems));
            Assert.Contains(projection.SourceItems, item => item.IsGroup && item.FullName == "feature/team-39");
            Assert.Contains(projection.TargetItems, item => item.IsGroup && item.FullName == "feature/team-39/area-399");
        }

        [Fact]
        public void LogBufferService_WithLargeLogVolume_DrainsDistinctLinesAndBoundsRetainedText()
        {
            var service = new LogBufferService { MaxChars = 10_000 };

            for (var index = 0; index < 5_000; index++)
            {
                service.AppendOutput($"line-{index}", status: true);
            }

            var batch = service.Drain();

            Assert.StartsWith("line-0\nline-1\n", batch.OutputText);
            Assert.EndsWith("line-4999\n", batch.OutputText);
            Assert.Equal(5_000, CountLines(batch.OutputText));
            Assert.True(service.OutputText.Length <= service.MaxChars);
            Assert.Contains("line-4999", service.OutputText);
        }

        [Fact]
        public void LogBufferService_WithLargeStreamAndExport_WritesLineSeparatedContent()
        {
            var root = Path.Combine(Path.GetTempPath(), "MergePilot.PerformanceLogs", Guid.NewGuid().ToString());
            Directory.CreateDirectory(root);

            try
            {
                var streamPath = Path.Combine(root, "stream.log");
                using var service = new LogBufferService { MaxChars = 100_000 };
                service.StartStreaming(streamPath);

                for (var index = 0; index < 1_000; index++)
                {
                    service.AppendOutput($"output-{index}", status: true);
                    if (index % 100 == 0)
                        service.AppendError($"error-{index}");
                }

                var batch = service.Drain();
                service.StopStreaming();
                var streamed = File.ReadAllText(streamPath);
                var export = service.BuildExport("==");

                Assert.Equal(1_000, CountLines(batch.OutputText));
                Assert.Equal(10, CountLines(batch.ErrorText));
                Assert.Contains("output-0\noutput-1\n", streamed);
                Assert.Contains("output-999\n", streamed);
                Assert.Contains("error-900\n", streamed);
                Assert.Contains("--- OUTPUT ---\n", export);
                Assert.Contains("output-999\n", export);
                Assert.Contains("--- ERRORS ---\n", export);
                Assert.Contains("error-900\n", export);
            }
            finally
            {
                ForceDeleteDirectory(root);
            }
        }

        [Fact]
        public async Task RepositoryService_WithMultipleLargeCachedRepositories_ReportsAggregateMetrics()
        {
            var service = new RepositoryService((repoPath, _) =>
            {
                var branches = Enumerable.Range(0, 250)
                    .Select(index => new BranchItem($"branch-{index}", $"{repoPath}/branch-{index}"));
                return Task.FromResult<IEnumerable<BranchItem>>(branches);
            });

            await service.GetBranchesAsync("C:\\repo-a");
            await service.GetBranchesAsync("C:\\repo-b");
            await service.GetBranchesAsync("C:\\repo-c");
            await service.GetBranchesAsync("C:\\repo-a");
            await service.GetBranchesAsync("C:\\repo-b");

            var metrics = service.GetCacheMetrics();

            Assert.Equal(3, metrics.CachedRepositoryCount);
            Assert.Equal(750, metrics.CachedBranchCount);
            Assert.Equal(2, metrics.CacheHits);
            Assert.Equal(3, metrics.CacheMisses);
            Assert.Equal(0.4, metrics.HitRate);
        }

        [Fact]
        public async Task RepositoryService_WithLocalGitRemote_ReportsCacheMetricsOnRepeat()
        {
            if (!IsGitAvailable())
            {
                _output.WriteLine("git was not available on PATH; local Git cache smoke was skipped.");
                return;
            }

            var root = Path.Combine(Path.GetTempPath(), "MergePilot.CacheSmoke", Guid.NewGuid().ToString());

            try
            {
                Directory.CreateDirectory(root);
                var branchCount = 80;
                var repoPath = CreateLocalGitRemoteFixture(root, branchCount);
                var service = new RepositoryService();

                var first = (await service.GetBranchesAsync(repoPath)).ToList();
                var second = (await service.GetBranchesAsync(repoPath)).ToList();
                var metrics = service.GetCacheMetrics();

                Assert.Equal(branchCount + 1, CountLeavesRecursive(first));
                Assert.Equal(CountLeavesRecursive(first), CountLeavesRecursive(second));
                Assert.Equal(1, metrics.CachedRepositoryCount);
                Assert.Equal(first.Count, metrics.CachedBranchCount);
                Assert.Equal(1, metrics.CacheHits);
                Assert.Equal(1, metrics.CacheMisses);
                Assert.Equal(0.5, metrics.HitRate);
            }
            finally
            {
                ForceDeleteDirectory(root);
            }
        }

        [Fact]
        public async Task RepositoryConfigurationService_WithLocalGitRemote_RefreshesAndDeduplicatesOnRepeat()
        {
            if (!IsGitAvailable())
            {
                _output.WriteLine("git was not available on PATH; local Git refresh smoke was skipped.");
                return;
            }

            var root = Path.Combine(Path.GetTempPath(), "MergePilot.RefreshSmoke", Guid.NewGuid().ToString());
            var previousSettingsOverride = AppSettings.SettingsPathOverride;
            AppSettings.SettingsPathOverride = Path.Combine(root, "settings.json");

            try
            {
                Directory.CreateDirectory(root);
                var branchCount = 120;
                var repoPath = CreateLocalGitRemoteFixture(root, branchCount);
                var settings = new AppSettings();
                settings.CustomBranches.Add(new AppSettings.BranchEntry
                {
                    BranchName = "main",
                    Repository = "LocalFixture"
                });
                var repositories = new[]
                {
                    new AppSettings.RepositoryEntry
                    {
                        Name = "LocalFixture",
                        Path = repoPath
                    }
                };

                var service = new RepositoryConfigurationService();
                var firstStopwatch = Stopwatch.StartNew();
                var first = await service.RefreshBranchesFromRepositoriesAsync(settings, repositories);
                firstStopwatch.Stop();

                var secondStopwatch = Stopwatch.StartNew();
                var second = await service.RefreshBranchesFromRepositoriesAsync(settings, repositories);
                secondStopwatch.Stop();

                _output.WriteLine(
                    $"Local Git refresh smoke: first={firstStopwatch.ElapsedMilliseconds}ms, second={secondStopwatch.ElapsedMilliseconds}ms, branches={branchCount}.");

                Assert.Equal(1, first.RefreshedRepositories);
                Assert.Equal(branchCount, first.AddedBranches);
                Assert.Equal(1, first.DuplicateBranches);
                Assert.Equal(1, second.RefreshedRepositories);
                Assert.Equal(0, second.AddedBranches);
                Assert.Equal(branchCount + 1, second.DuplicateBranches);
                Assert.Contains(settings.CustomBranches, b =>
                    b.Repository == "LocalFixture" &&
                    b.BranchName == "feature/team-00/area-00/branch-0000");
                Assert.Contains(settings.CustomBranches, b =>
                    b.Repository == "LocalFixture" &&
                    b.BranchName == "feature/team-00/area-04/branch-0119");
                Assert.True(File.Exists(AppSettings.SettingsPathOverride));
            }
            finally
            {
                AppSettings.SettingsPathOverride = previousSettingsOverride;

                ForceDeleteDirectory(root);
            }
        }

        private static int CountLeaves(IEnumerable<BranchItem> items)
        {
            return items.Count(item => !item.IsGroup);
        }

        private static int CountLeavesRecursive(IEnumerable<BranchItem> items)
        {
            return items.Sum(item => item.IsGroup
                ? CountLeavesRecursive(item.Children)
                : 1);
        }

        private static int CountLines(string text)
        {
            return text.Split('\n').Count(line => line.Length > 0);
        }

        private static string CreateLocalGitRemoteFixture(string root, int branchCount)
        {
            var repoPath = Path.Combine(root, "repository");
            var remotePath = Path.Combine(root, "remote.git");
            Directory.CreateDirectory(repoPath);
            Directory.CreateDirectory(remotePath);

            RunGit(repoPath, "init");
            RunGit(repoPath, "config", "user.email", "mergepilot-tests@example.local");
            RunGit(repoPath, "config", "user.name", "MergePilot Tests");
            File.WriteAllText(Path.Combine(repoPath, "README.md"), "# LocalFixture");
            RunGit(repoPath, "add", "README.md");
            RunGit(repoPath, "commit", "-m", "Initial commit");
            RunGit(repoPath, "branch", "-M", "main");
            RunGit(root, "init", "--bare", remotePath);
            RunGit(repoPath, "remote", "add", "origin", remotePath);
            RunGit(repoPath, "push", "origin", "main");

            var commitHash = RunGit(repoPath, "rev-parse", "HEAD").Trim();
            for (var index = 0; index < branchCount; index++)
            {
                RunGit(
                    root,
                    $"--git-dir={remotePath}",
                    "update-ref",
                    $"refs/heads/{NewBranchName(index)}",
                    commitHash);
            }

            return repoPath;
        }

        private static bool IsGitAvailable()
        {
            try
            {
                RunGit(Directory.GetCurrentDirectory(), "--version");
                return true;
            }
            catch
            {
                return false;
            }
        }

        private static string RunGit(string workingDirectory, params string[] arguments)
        {
            var startInfo = new ProcessStartInfo("git")
            {
                WorkingDirectory = workingDirectory,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            foreach (var argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = Process.Start(startInfo)
                ?? throw new InvalidOperationException("Unable to start git.");

            var stdout = process.StandardOutput.ReadToEnd();
            var stderr = process.StandardError.ReadToEnd();
            process.WaitForExit();

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    $"git {string.Join(" ", arguments)} failed with exit code {process.ExitCode}: {stderr}");
            }

            return stdout;
        }

        private static string NewBranchName(int index)
        {
            var team = index / 250;
            var area = index / 25;
            return $"feature/team-{team:D2}/area-{area:D2}/branch-{index:D4}";
        }

        private static void ForceDeleteDirectory(string path)
        {
            if (!Directory.Exists(path))
                return;

            foreach (var file in Directory.GetFiles(path, "*", SearchOption.AllDirectories))
            {
                File.SetAttributes(file, FileAttributes.Normal);
            }

            Directory.Delete(path, recursive: true);
        }
    }
}
