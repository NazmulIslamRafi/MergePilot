using System;
using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Tracks repository checkbox selection independently from WPF controls.
    /// </summary>
    public class RepositorySelectionState
    {
        private readonly HashSet<string> _selectedRepositoryPaths = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> SelectedRepositoryPaths => _selectedRepositoryPaths;

        public void SetSelected(string? repositoryPath, bool isSelected)
        {
            if (string.IsNullOrWhiteSpace(repositoryPath))
                return;

            var normalizedPath = repositoryPath.Trim();
            if (isSelected)
                _selectedRepositoryPaths.Add(normalizedPath);
            else
                _selectedRepositoryPaths.Remove(normalizedPath);
        }

        public List<string> GetSelectedRepositories()
        {
            return _selectedRepositoryPaths.ToList();
        }

        public void Clear()
        {
            _selectedRepositoryPaths.Clear();
        }
    }
}
