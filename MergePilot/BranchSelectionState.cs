using System;
using System.Collections.Generic;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Tracks branch selections and branch catalogs independently from WPF controls.
    /// </summary>
    public class BranchSelectionState
    {
        private readonly HashSet<string> _selectedSourceBranches = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _selectedTargetBranches = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _sourceBranchCatalog = new(StringComparer.OrdinalIgnoreCase);
        private readonly HashSet<string> _targetBranchCatalog = new(StringComparer.OrdinalIgnoreCase);

        public IReadOnlySet<string> SelectedSourceBranches => _selectedSourceBranches;
        public IReadOnlySet<string> SelectedTargetBranches => _selectedTargetBranches;
        public IReadOnlySet<string> SourceBranchCatalog => _sourceBranchCatalog;
        public IReadOnlySet<string> TargetBranchCatalog => _targetBranchCatalog;

        public bool HasExactlyOneSourceBranch => _selectedSourceBranches.Count == 1;
        public bool HasSourceBranches => _selectedSourceBranches.Count > 0;
        public bool HasTargetBranches => _selectedTargetBranches.Count > 0;

        public List<string> GetSelectedBranches(BranchSelectionRole role)
        {
            return GetSelectedSet(role).ToList();
        }

        public IReadOnlySet<string> GetSelectedSet(BranchSelectionRole role)
        {
            return role == BranchSelectionRole.Source
                ? _selectedSourceBranches
                : _selectedTargetBranches;
        }

        public void SetBranchSelected(BranchSelectionRole role, string? branchName, bool isSelected)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                return;

            var selectedSet = role == BranchSelectionRole.Source
                ? _selectedSourceBranches
                : _selectedTargetBranches;

            if (isSelected)
                selectedSet.Add(branchName);
            else
                selectedSet.Remove(branchName);
        }

        public void SetBranchesSelected(BranchSelectionRole role, IEnumerable<string> branchNames, bool isSelected)
        {
            foreach (var branchName in branchNames ?? Enumerable.Empty<string>())
            {
                SetBranchSelected(role, branchName, isSelected);
            }
        }

        public void ClearSelections()
        {
            _selectedSourceBranches.Clear();
            _selectedTargetBranches.Clear();
        }

        public void ClearBranchCatalogs()
        {
            _sourceBranchCatalog.Clear();
            _targetBranchCatalog.Clear();
        }

        public void AddBranchToCatalogs(string? branchName)
        {
            if (string.IsNullOrWhiteSpace(branchName))
                return;

            _sourceBranchCatalog.Add(branchName);
            _targetBranchCatalog.Add(branchName);
        }

        public bool? GetGroupCheckedState(BranchSelectionRole role, IEnumerable<string> leafBranchNames)
        {
            var leafBranches = (leafBranchNames ?? Enumerable.Empty<string>())
                .Where(b => !string.IsNullOrWhiteSpace(b))
                .ToList();

            if (leafBranches.Count == 0)
                return false;

            var selectedSet = GetSelectedSet(role);
            var checkedCount = leafBranches.Count(selectedSet.Contains);

            if (checkedCount == leafBranches.Count)
                return true;

            if (checkedCount == 0)
                return false;

            return null;
        }
    }

    public enum BranchSelectionRole
    {
        Source,
        Target
    }
}
