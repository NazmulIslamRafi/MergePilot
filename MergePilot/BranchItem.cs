namespace MergePilot
{
    /// <summary>
    /// Represents a branch or branch group in a hierarchical structure
    /// </summary>
    public class BranchItem
    {
        public string Name { get; set; }
        public string FullName { get; set; } // Full branch name (e.g., "0-Task/NewUpdate")
        public bool IsGroup { get; set; } // True if this is a folder/group, false if it's an actual branch
        public int Level { get; set; } // Indentation level
        public List<BranchItem> Children { get; set; } = new();
        public BranchItem Parent { get; set; } // Reference to parent for tri-state logic

        public BranchItem(string name, string fullName, bool isGroup = false, int level = 0, BranchItem parent = null)
        {
            Name = name;
            FullName = fullName;
            IsGroup = isGroup;
            Level = level;
            Parent = parent;
        }

        public override string ToString()
        {
            return IsGroup ? $"?? {Name}" : Name;
        }
    }

    /// <summary>
    /// Helper class to organize branches hierarchically
    /// </summary>
    public static class BranchOrganizer
    {
        /// <summary>
        /// Converts a flat list of branch names into a hierarchical structure
        /// </summary>
        public static List<BranchItem> OrganizeBranches(IEnumerable<string> branches)
        {
            var result = new List<BranchItem>();
            var groups = new Dictionary<string, BranchItem>();

            // Sort branches for consistent output
            var sortedBranches = branches.OrderBy(b => b).ToList();

            foreach (var branch in sortedBranches)
            {
                var parts = branch.Split('/');

                if (parts.Length == 1)
                {
                    // Leaf branch with no path
                    result.Add(new BranchItem(branch, branch, isGroup: false, level: 0, parent: null));
                }
                else
                {
                    // Branch with path
                    string groupPath = "";
                    BranchItem currentGroup = null;
                    List<BranchItem> parentList = result;

                    // Build the group hierarchy
                    for (int i = 0; i < parts.Length - 1; i++)
                    {
                        groupPath = i == 0 ? parts[i] : $"{groupPath}/{parts[i]}";

                        if (!groups.TryGetValue(groupPath, out currentGroup))
                        {
                            currentGroup = new BranchItem(parts[i], groupPath, isGroup: true, level: i, parent: null);
                            
                            // Set parent reference for tri-state logic
                            if (parentList.Count > 0)
                            {
                                var parentItem = parentList.FirstOrDefault(x => x.IsGroup && x.FullName == (i > 0 ? groupPath.Substring(0, groupPath.LastIndexOf('/')) : ""));
                                if (parentItem != null)
                                    currentGroup.Parent = parentItem;
                            }
                            
                            parentList.Add(currentGroup);
                            groups[groupPath] = currentGroup;
                        }

                        parentList = currentGroup.Children;
                    }

                    // Add the actual branch as a leaf
                    var leafName = parts[^1];
                    var leafItem = new BranchItem(leafName, branch, isGroup: false, level: parts.Length - 1, parent: currentGroup);
                    parentList.Add(leafItem);
                }
            }

            return result;
        }

        /// <summary>
        /// Flattens the hierarchical structure back to a list for display
        /// </summary>
        public static List<BranchItem> FlattenBranches(List<BranchItem> items)
        {
            var result = new List<BranchItem>();

            foreach (var item in items)
            {
                result.Add(item);
                if (item.IsGroup && item.Children.Count > 0)
                {
                    result.AddRange(FlattenBranches(item.Children));
                }
            }

            return result;
        }

        /// <summary>
        /// Extracts all leaf branch names (actual branches, not groups)
        /// </summary>
        public static List<string> GetLeafBranches(List<BranchItem> items)
        {
            var result = new List<string>();

            foreach (var item in items)
            {
                if (!item.IsGroup)
                {
                    result.Add(item.FullName);
                }
                else if (item.Children.Count > 0)
                {
                    result.AddRange(GetLeafBranches(item.Children));
                }
            }

            return result;
        }

        /// <summary>
        /// Gets all child branches (leaf nodes) for a given group
        /// </summary>
        public static List<string> GetChildBranches(BranchItem groupItem)
        {
            return GetLeafBranches(new List<BranchItem> { groupItem });
        }
    }
}
