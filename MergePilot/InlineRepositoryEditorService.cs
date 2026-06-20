using System;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Coordinates inline repository editor decisions without depending on WPF controls.
    /// </summary>
    public class InlineRepositoryEditorService
    {
        private readonly RepositoryConfigurationService _repositoryConfigurationService;

        public InlineRepositoryEditorService(RepositoryConfigurationService repositoryConfigurationService)
        {
            _repositoryConfigurationService = repositoryConfigurationService
                ?? throw new ArgumentNullException(nameof(repositoryConfigurationService));
        }

        public InlineRepositorySelection ResolveSelectedRepository(
            AppSettings settings,
            int selectedIndex,
            AppSettings.RepositoryEntry? selectedRepository)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var resolvedIndex = selectedIndex;
            var repository = GetRepositoryAtIndex(settings, selectedIndex);

            if (selectedRepository != null && !string.IsNullOrWhiteSpace(selectedRepository.Path))
            {
                var repositoryByPath = settings.Repositories
                    .Select((entry, index) => new { Entry = entry, Index = index })
                    .FirstOrDefault(candidate =>
                        string.Equals(
                            candidate.Entry.Path,
                            selectedRepository.Path,
                            StringComparison.OrdinalIgnoreCase));

                if (repositoryByPath != null)
                {
                    repository = repositoryByPath.Entry;
                    resolvedIndex = repositoryByPath.Index;
                }
            }

            return new InlineRepositorySelection(resolvedIndex, repository);
        }

        public bool RequiresMissingGitConfirmation(string? repositoryPath)
        {
            return !RepositoryConfigurationService.HasGitRepositoryMarker(repositoryPath);
        }

        public RepositoryConfigurationResult AddRepository(
            AppSettings settings,
            string? name,
            string? path,
            bool allowMissingGitRepository)
        {
            return _repositoryConfigurationService.AddRepository(
                settings,
                name,
                path,
                pathRequirement: GetPathRequirement(allowMissingGitRepository));
        }

        public RepositoryConfigurationResult UpdateRepository(
            AppSettings settings,
            int selectedIndex,
            string? name,
            string? path,
            bool allowMissingGitRepository)
        {
            return _repositoryConfigurationService.UpdateRepository(
                settings,
                selectedIndex,
                name,
                path,
                pathRequirement: GetPathRequirement(allowMissingGitRepository));
        }

        public RepositoryConfigurationResult RemoveRepository(AppSettings settings, int selectedIndex)
        {
            return _repositoryConfigurationService.RemoveRepository(settings, selectedIndex);
        }

        private static AppSettings.RepositoryEntry? GetRepositoryAtIndex(AppSettings settings, int selectedIndex)
        {
            if (selectedIndex < 0 || selectedIndex >= settings.Repositories.Count)
                return null;

            return settings.Repositories[selectedIndex];
        }

        private static RepositoryPathRequirement GetPathRequirement(bool allowMissingGitRepository)
        {
            return allowMissingGitRepository
                ? RepositoryPathRequirement.AllowMissingGitRepository
                : RepositoryPathRequirement.RequireGitRepository;
        }
    }

    public readonly record struct InlineRepositorySelection(
        int Index,
        AppSettings.RepositoryEntry? Repository)
    {
        public bool HasRepository => Repository != null;
    }
}
