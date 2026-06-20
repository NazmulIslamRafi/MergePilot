using System;
using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Builds batch operation requests and owns small settings side effects related to request preparation.
    /// </summary>
    public class BatchOperationRequestFactory
    {
        public BatchMergeRequest CreateMergeRequest(
            AppSettings settings,
            IEnumerable<string> repositoryPaths,
            IEnumerable<string> sourceBranches,
            IEnumerable<string> targetBranches)
        {
            ArgumentNullException.ThrowIfNull(settings);

            var repositories = Materialize(repositoryPaths);
            var sources = Materialize(sourceBranches);
            var targets = Materialize(targetBranches);

            if (sources.Count > 0 && targets.Count > 0)
            {
                settings.LastSourceBranch = string.Join("|", sources);
                settings.LastTargetBranch = targets[0];
                settings.Save();
            }

            return new BatchMergeRequest(repositories, sources, targets);
        }

        public BatchPullRequest CreatePullRequest(
            IEnumerable<string> repositoryPaths,
            IEnumerable<string> sourceBranches)
        {
            return new BatchPullRequest(
                Materialize(repositoryPaths),
                Materialize(sourceBranches));
        }

        private static List<string> Materialize(IEnumerable<string>? values)
        {
            return values?
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v.Trim())
                .ToList()
                ?? new List<string>();
        }
    }
}
