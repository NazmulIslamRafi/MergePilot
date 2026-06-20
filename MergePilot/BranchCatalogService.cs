using System;
using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Builds branch catalogs from settings and manages recently used branches.
    /// </summary>
    public class BranchCatalogService
    {
        public const int DefaultMaxRecentBranches = 100;

        public List<string> GetInitialBranchCatalog(AppSettings settings)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var catalog = new List<string>();
            var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            AddDistinct(catalog, seen, settings.CustomBranches?.Select(b => b.BranchName));
            AddDistinct(catalog, seen, settings.RecentBranches);

            return catalog;
        }

        public List<string> GetCustomBranchesForRepository(AppSettings settings, string repoPath)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(repoPath))
                return new List<string>();

            var repoName = settings.Repositories?
                .FirstOrDefault(r => string.Equals(r.Path, repoPath, StringComparison.OrdinalIgnoreCase))
                ?.Name;

            if (string.IsNullOrWhiteSpace(repoName))
                return new List<string>();

            var branches = settings.CustomBranches?
                .Where(b =>
                    !string.IsNullOrWhiteSpace(b.BranchName) &&
                    string.Equals(b.Repository, repoName, StringComparison.OrdinalIgnoreCase))
                .Select(b => b.BranchName)
                ?? Enumerable.Empty<string?>();

            var result = new List<string>();
            AddDistinct(result, new HashSet<string>(StringComparer.OrdinalIgnoreCase), branches);
            return result;
        }

        public bool AddRecentBranch(AppSettings settings, string? branch, int maxRecentBranches = DefaultMaxRecentBranches)
        {
            ArgumentNullException.ThrowIfNull(settings);

            if (string.IsNullOrWhiteSpace(branch))
                return false;

            settings.RecentBranches ??= new List<string>();
            var trimmedBranch = branch.Trim();

            if (settings.RecentBranches.Contains(trimmedBranch, StringComparer.OrdinalIgnoreCase))
                return false;

            settings.RecentBranches.Add(trimmedBranch);

            if (maxRecentBranches > 0 && settings.RecentBranches.Count > maxRecentBranches)
                settings.RecentBranches.RemoveRange(0, settings.RecentBranches.Count - maxRecentBranches);

            settings.Save();
            return true;
        }

        private static void AddDistinct(
            List<string> target,
            HashSet<string> seen,
            IEnumerable<string?>? values)
        {
            if (values == null)
                return;

            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value))
                    continue;

                var trimmed = value.Trim();
                if (seen.Add(trimmed))
                    target.Add(trimmed);
            }
        }
    }
}
