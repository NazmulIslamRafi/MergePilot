using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace MergePilot
{
    /// <summary>
    /// Manages persisted repository and branch configuration stored in AppSettings.
    /// </summary>
    public class RepositoryConfigurationService
    {
        private readonly Func<string, CancellationToken, Task<IEnumerable<string>>> _remoteBranchProvider;

        public RepositoryConfigurationService(
            Func<string, CancellationToken, Task<IEnumerable<string>>>? remoteBranchProvider = null)
        {
            _remoteBranchProvider = remoteBranchProvider ?? GetRemoteBranchesAsync;
        }

        public RepositoryConfigurationResult AddRepository(
            AppSettings settings,
            string? name,
            string? path,
            string? remoteUrl = null,
            RepositoryPathRequirement pathRequirement = RepositoryPathRequirement.RequireGitRepository)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var validation = ValidateRepositoryInput(name, path, pathRequirement);
            if (!validation.IsSuccess)
                return validation;

            settings.Repositories ??= new List<AppSettings.RepositoryEntry>();
            var repository = new AppSettings.RepositoryEntry
            {
                Name = name!.Trim(),
                Path = path!.Trim(),
                RemoteUrl = NormalizeOptional(remoteUrl)
            };

            settings.Repositories.Add(repository);
            settings.Save();

            return RepositoryConfigurationResult.Success(repository, settings.Repositories.Count - 1);
        }

        public RepositoryConfigurationResult UpdateRepository(
            AppSettings settings,
            int index,
            string? name,
            string? path,
            string? remoteUrl = null,
            RepositoryPathRequirement pathRequirement = RepositoryPathRequirement.RequireGitRepository)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (!TryGetRepository(settings, index, out var repository))
                return RepositoryConfigurationResult.Failure("Selected repository was not found.");

            var validation = ValidateRepositoryInput(name, path, pathRequirement);
            if (!validation.IsSuccess)
                return validation;

            repository.Name = name!.Trim();
            repository.Path = path!.Trim();
            if (remoteUrl != null)
                repository.RemoteUrl = NormalizeOptional(remoteUrl);

            settings.Save();

            return RepositoryConfigurationResult.Success(repository, index);
        }

        public RepositoryConfigurationResult RemoveRepository(AppSettings settings, int index)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (!TryGetRepository(settings, index, out var repository))
                return RepositoryConfigurationResult.Failure("Selected repository was not found.");

            settings.Repositories.RemoveAt(index);
            settings.Save();

            return RepositoryConfigurationResult.Success(repository, index);
        }

        public RepositoryConfigurationResult AddBranch(
            AppSettings settings,
            string? branchName,
            string? repositoryName,
            bool isManual = false)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var validation = ValidateBranchInput(branchName, repositoryName);
            if (!validation.IsSuccess)
                return validation;

            settings.CustomBranches ??= new List<AppSettings.BranchEntry>();

            var trimmedName = branchName!.Trim();
            var trimmedRepo = repositoryName!.Trim();

            var duplicate = settings.CustomBranches.Any(b =>
                string.Equals(b.BranchName, trimmedName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Repository, trimmedRepo, StringComparison.OrdinalIgnoreCase));

            if (duplicate)
                return RepositoryConfigurationResult.Failure($"Branch '{trimmedName}' already exists in repository '{trimmedRepo}'.");

            var branch = new AppSettings.BranchEntry
            {
                BranchName = trimmedName,
                Repository = trimmedRepo,
                IsManual = isManual
            };

            settings.CustomBranches.Add(branch);
            settings.Save();

            return RepositoryConfigurationResult.Success(branch, settings.CustomBranches.Count - 1);
        }

        public RepositoryConfigurationResult UpdateBranch(
            AppSettings settings,
            AppSettings.BranchEntry branch,
            string? branchName,
            string? repositoryName,
            bool isManual = false)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(branch);

            if (settings.CustomBranches == null || !settings.CustomBranches.Contains(branch))
                return RepositoryConfigurationResult.Failure("Selected branch was not found.");

            var validation = ValidateBranchInput(branchName, repositoryName);
            if (!validation.IsSuccess)
                return validation;

            var trimmedName = branchName!.Trim();
            var trimmedRepo = repositoryName!.Trim();

            var duplicate = settings.CustomBranches.Any(b =>
                b != branch &&
                string.Equals(b.BranchName, trimmedName, StringComparison.OrdinalIgnoreCase) &&
                string.Equals(b.Repository, trimmedRepo, StringComparison.OrdinalIgnoreCase));

            if (duplicate)
                return RepositoryConfigurationResult.Failure($"Branch '{trimmedName}' already exists in repository '{trimmedRepo}'.");

            branch.BranchName = trimmedName;
            branch.Repository = trimmedRepo;
            branch.IsManual = isManual;
            settings.Save();

            return RepositoryConfigurationResult.Success(branch, settings.CustomBranches.IndexOf(branch));
        }

        public RepositoryConfigurationResult RemoveBranch(AppSettings settings, AppSettings.BranchEntry branch)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(branch);

            if (settings.CustomBranches == null || !settings.CustomBranches.Remove(branch))
                return RepositoryConfigurationResult.Failure("Selected branch was not found.");

            settings.Save();

            return RepositoryConfigurationResult.Success(branch);
        }

        public async Task<BranchRefreshResult> RefreshBranchesFromRepositoriesAsync(
            AppSettings settings,
            IEnumerable<AppSettings.RepositoryEntry> repositories,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(repositories);

            settings.CustomBranches ??= new List<AppSettings.BranchEntry>();
            var result = new BranchRefreshResult();

            foreach (var repository in repositories)
            {
                cancellationToken.ThrowIfCancellationRequested();

                if (repository == null || string.IsNullOrWhiteSpace(repository.Path))
                {
                    result.SkippedRepositories++;
                    continue;
                }

                try
                {
                    var branches = await _remoteBranchProvider(repository.Path, cancellationToken)
                        .ConfigureAwait(false);

                    foreach (var branchName in branches.Where(b => !string.IsNullOrWhiteSpace(b)))
                    {
                        var trimmedBranchName = branchName.Trim();
                        var exists = settings.CustomBranches.Any(b =>
                            string.Equals(b.BranchName, trimmedBranchName, StringComparison.OrdinalIgnoreCase) &&
                            string.Equals(b.Repository, repository.Name, StringComparison.OrdinalIgnoreCase));

                        if (exists)
                        {
                            result.DuplicateBranches++;
                            continue;
                        }

                        settings.CustomBranches.Add(new AppSettings.BranchEntry
                        {
                            BranchName = trimmedBranchName,
                            Repository = repository.Name
                        });
                        result.AddedBranches++;
                    }

                    result.RefreshedRepositories++;
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    result.FailedRepositories++;
                    result.Errors.Add($"{repository.Name ?? repository.Path}: {ex.Message}");
                }
            }

            if (result.AddedBranches > 0)
                settings.Save();

            return result;
        }

        public static bool HasGitRepositoryMarker(string? path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                var gitPath = Path.Combine(path.Trim(), ".git");
                return Directory.Exists(gitPath) || File.Exists(gitPath);
            }
            catch
            {
                return false;
            }
        }

        private static async Task<IEnumerable<string>> GetRemoteBranchesAsync(string path, CancellationToken cancellationToken)
        {
            return await GitHelper.GetRemoteBranchesAsync(path, "origin", cancellationToken)
                .ConfigureAwait(false);
        }

        private static RepositoryConfigurationResult ValidateRepositoryInput(
            string? name,
            string? path,
            RepositoryPathRequirement pathRequirement)
        {
            if (!InputValidator.IsValidRepositoryName(name ?? string.Empty))
                return RepositoryConfigurationResult.Failure("Repository name is required (max 255 characters).");

            if (string.IsNullOrWhiteSpace(path))
                return RepositoryConfigurationResult.Failure("Repository path cannot be empty.");

            if (pathRequirement == RepositoryPathRequirement.RequireGitRepository &&
                (!Directory.Exists(path.Trim()) || !HasGitRepositoryMarker(path)))
            {
                return RepositoryConfigurationResult.Failure("Invalid repository path. Path must exist and contain .git folder.");
            }

            return RepositoryConfigurationResult.Success();
        }

        private static RepositoryConfigurationResult ValidateBranchInput(string? branchName, string? repositoryName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                return RepositoryConfigurationResult.Failure("Branch name is required.");

            var trimmedBranchName = branchName.Trim();
            if (!InputValidator.IsValidBranchName(trimmedBranchName))
                return RepositoryConfigurationResult.Failure($"Invalid branch name: {InputValidator.GetBranchNameError(trimmedBranchName)}");

            if (string.IsNullOrWhiteSpace(repositoryName))
                return RepositoryConfigurationResult.Failure("Repository selection is required.");

            return RepositoryConfigurationResult.Success();
        }

        private static bool TryGetRepository(
            AppSettings settings,
            int index,
            out AppSettings.RepositoryEntry repository)
        {
            repository = null!;

            if (settings.Repositories == null || index < 0 || index >= settings.Repositories.Count)
                return false;

            repository = settings.Repositories[index];
            return repository != null;
        }

        private static string? NormalizeOptional(string? value)
        {
            var trimmed = value?.Trim();
            return string.IsNullOrEmpty(trimmed) ? null : trimmed;
        }
    }

    public enum RepositoryPathRequirement
    {
        RequireGitRepository,
        AllowMissingGitRepository
    }

    public class RepositoryConfigurationResult
    {
        public bool IsSuccess { get; }
        public string Message { get; }
        public object? Item { get; }
        public int Index { get; }

        private RepositoryConfigurationResult(bool isSuccess, string message, object? item = null, int index = -1)
        {
            IsSuccess = isSuccess;
            Message = message;
            Item = item;
            Index = index;
        }

        public static RepositoryConfigurationResult Success(object? item = null, int index = -1)
        {
            return new RepositoryConfigurationResult(true, string.Empty, item, index);
        }

        public static RepositoryConfigurationResult Failure(string message)
        {
            return new RepositoryConfigurationResult(false, message);
        }
    }

    public class BranchRefreshResult
    {
        public int RefreshedRepositories { get; set; }
        public int SkippedRepositories { get; set; }
        public int FailedRepositories { get; set; }
        public int AddedBranches { get; set; }
        public int DuplicateBranches { get; set; }
        public List<string> Errors { get; } = new();
    }
}
