using System.Diagnostics;

namespace MergePilot
{
    public static class GitHelper
    {
        private static async Task<string> RunGitCommandAsync(string repoPath, string arguments)
        {
            var tcs = new TaskCompletionSource<string>();
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = "git",
                    Arguments = arguments,
                    WorkingDirectory = repoPath,
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                },
                EnableRaisingEvents = true
            };

            string output = "";
            string error = "";

            process.OutputDataReceived += (s, e) => output += e.Data + "\n";
            process.ErrorDataReceived += (s, e) => error += e.Data + "\n";

            process.Exited += (s, e) =>
            {
                if (process.ExitCode == 0)
                    tcs.TrySetResult(output);
                else
                    tcs.TrySetException(new Exception($"Git error: {error}"));
                process.Dispose();
            };

            process.Start();
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();

            return await tcs.Task;
        }

        // Example: Merge from one branch into another
        public static async Task<string> MergeBranchAsync(string repoPath, string sourceBranch, string targetBranch)
        {
            await RunGitCommandAsync(repoPath, $"fetch origin {sourceBranch}");
            await RunGitCommandAsync(repoPath, $"fetch origin {targetBranch}");
            await RunGitCommandAsync(repoPath, $"checkout {targetBranch}");
            await RunGitCommandAsync(repoPath, $"pull origin {targetBranch}");
            await RunGitCommandAsync(repoPath, $"merge --no-ff {sourceBranch} -m \"Merge {sourceBranch} into {targetBranch}\"");
            await RunGitCommandAsync(repoPath, $"push origin {targetBranch}");

            return $"✅ Successfully merged {sourceBranch} → {targetBranch}";
        }
    }
}

