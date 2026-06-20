using System;
using System.IO;
using System.Linq;

namespace MergePilot
{
    /// <summary>
    /// Provides input validation for Git operations and repository management.
    /// Prevents injection attacks, path traversal, and invalid data.
    /// </summary>
    public static class InputValidator
    {
        /// <summary>
        /// Validates a Git branch name according to Git naming rules.
        /// </summary>
        /// <param name="branch">The branch name to validate.</param>
        /// <returns>True if branch name is valid; otherwise, false.</returns>
        /// <remarks>
        /// Git branch names must not:
        /// - Be empty or whitespace
        /// - Contain special characters: @ ~ ^ : ? [ ]
        /// - Contain consecutive dots (..)
        /// - Start or end with dots (.)
        /// - Start or end with slashes (/)
        /// </remarks>
        public static bool IsValidBranchName(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return false;

            // Check for invalid characters (including whitespace — git rejects spaces in branch names)
            if (branch.Any(c => char.IsWhiteSpace(c)))
                return false;

            const string invalidChars = "@~^:?[]";
            if (branch.Any(c => invalidChars.Contains(c)))
                return false;

            // Check for consecutive dots
            if (branch.Contains(".."))
                return false;

            // Check for leading/trailing dots
            if (branch.StartsWith(".") || branch.EndsWith("."))
                return false;

            // Check for leading/trailing slashes
            if (branch.StartsWith("/") || branch.EndsWith("/"))
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation error message for an invalid branch name.
        /// </summary>
        /// <param name="branch">The branch name that failed validation.</param>
        /// <returns>A descriptive error message explaining why the name is invalid.</returns>
        public static string GetBranchNameError(string branch)
        {
            if (string.IsNullOrWhiteSpace(branch))
                return "Branch name cannot be empty.";

            if (branch.Any(char.IsWhiteSpace))
                return "Branch name cannot contain whitespace characters.";

            const string invalidChars = "@~^:?[]";
            var foundInvalid = branch.FirstOrDefault(c => invalidChars.Contains(c));
            if (foundInvalid != '\0')
                return $"Branch name contains invalid character: '{foundInvalid}'";

            if (branch.Contains(".."))
                return "Branch name cannot contain '..'";

            if (branch.StartsWith(".") || branch.EndsWith("."))
                return "Branch name cannot start or end with '.'";

            if (branch.StartsWith("/") || branch.EndsWith("/"))
                return "Branch name cannot start or end with '/'";

            return "Branch name is invalid.";
        }

        /// <summary>
        /// Validates a repository path exists and contains a Git repository.
        /// </summary>
        /// <param name="path">The path to validate.</param>
        /// <returns>True if path is a valid Git repository; otherwise, false.</returns>
        /// <remarks>
        /// A valid repository path must:
        /// - Exist as a directory
        /// - Contain a .git folder or .git file
        /// </remarks>
        public static bool IsValidRepositoryPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return false;

            try
            {
                // Check if directory exists
                if (!Directory.Exists(path))
                    return false;

                // Check if .git folder exists
                var gitPath = Path.Combine(path, ".git");
                if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
                    return false;

                // Ensure the path can be normalized by the platform.
                Path.GetFullPath(path);

                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Gets validation error message for an invalid repository path.
        /// </summary>
        /// <param name="path">The path that failed validation.</param>
        /// <returns>A descriptive error message explaining why the path is invalid.</returns>
        public static string GetRepositoryPathError(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
                return "Repository path cannot be empty.";

            if (!Directory.Exists(path))
                return $"Directory does not exist: {path}";

            var gitPath = Path.Combine(path, ".git");
            if (!Directory.Exists(gitPath) && !File.Exists(gitPath))
                return "Directory does not contain a .git folder. Is this a Git repository?";

            try
            {
                Path.GetFullPath(path);
            }
            catch (Exception ex)
            {
                return $"Invalid path: {ex.Message}";
            }

            return "Repository path is invalid.";
        }

        /// <summary>
        /// Validates a repository name is not empty.
        /// </summary>
        /// <param name="name">The repository name to validate.</param>
        /// <returns>True if name is valid; otherwise, false.</returns>
        public static bool IsValidRepositoryName(string name)
        {
            return !string.IsNullOrWhiteSpace(name) && name.Length <= 255;
        }

        /// <summary>
        /// Validates that a branch operation has both source and target branches specified.
        /// </summary>
        /// <param name="sourceBranch">The source branch name.</param>
        /// <param name="targetBranch">The target branch name.</param>
        /// <returns>True if both branches are valid and different; otherwise, false.</returns>
        public static bool IsValidMergeOperation(string sourceBranch, string targetBranch)
        {
            if (!IsValidBranchName(sourceBranch) || !IsValidBranchName(targetBranch))
                return false;

            // Prevent merging a branch into itself
            if (string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
                return false;

            return true;
        }

        /// <summary>
        /// Gets validation result with error details.
        /// </summary>
        /// <param name="sourceBranch">The source branch name.</param>
        /// <param name="targetBranch">The target branch name.</param>
        /// <returns>A tuple of (isValid, errorMessage).</returns>
        public static (bool isValid, string errorMessage) GetMergeOperationError(
            string sourceBranch, 
            string targetBranch)
        {
            if (string.IsNullOrWhiteSpace(sourceBranch))
                return (false, "Source branch is required.");

            if (string.IsNullOrWhiteSpace(targetBranch))
                return (false, "Target branch is required.");

            if (!IsValidBranchName(sourceBranch))
                return (false, $"Invalid source branch: {GetBranchNameError(sourceBranch)}");

            if (!IsValidBranchName(targetBranch))
                return (false, $"Invalid target branch: {GetBranchNameError(targetBranch)}");

            if (string.Equals(sourceBranch, targetBranch, StringComparison.OrdinalIgnoreCase))
                return (false, "Cannot merge a branch into itself.");

            return (true, "");
        }
    }
}
