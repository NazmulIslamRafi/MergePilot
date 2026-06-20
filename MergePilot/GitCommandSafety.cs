using System;
using System.Linq;

namespace MergePilot
{
    internal static class GitCommandSafety
    {
        public static bool TryNormalizeBranchName(string? branchName, out string normalized, out string error)
        {
            normalized = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(branchName))
            {
                error = "Branch name cannot be empty.";
                return false;
            }

            var trimmed = branchName.Trim();
            if (trimmed.Any(char.IsWhiteSpace))
            {
                error = "Branch name cannot contain whitespace.";
                return false;
            }

            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                error = "Branch name cannot start with '-'.";
                return false;
            }

            if (trimmed == "@")
            {
                error = "Branch name cannot be '@'.";
                return false;
            }

            if (trimmed.Contains("@{", StringComparison.Ordinal))
            {
                error = "Branch name cannot contain '@{'.";
                return false;
            }

            if (trimmed.Contains('\\'))
            {
                error = "Branch name cannot contain backslashes.";
                return false;
            }

            if (trimmed.Contains('"'))
            {
                error = "Branch name cannot contain quote characters.";
                return false;
            }

            if (!InputValidator.IsValidBranchName(trimmed))
            {
                error = InputValidator.GetBranchNameError(trimmed);
                return false;
            }

            if (trimmed.Contains("//", StringComparison.Ordinal))
            {
                error = "Branch name cannot contain '//'.";
                return false;
            }

            if (trimmed.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                error = "Branch name cannot end with '.lock'.";
                return false;
            }

            if (!trimmed.All(IsAllowedRefCharacter))
            {
                error = "Branch name contains unsupported characters.";
                return false;
            }

            normalized = trimmed;
            return true;
        }

        public static bool TryNormalizeRemoteName(string? remoteName, out string normalized, out string error)
        {
            normalized = string.Empty;
            error = string.Empty;

            if (string.IsNullOrWhiteSpace(remoteName))
            {
                error = "Remote name cannot be empty.";
                return false;
            }

            var trimmed = remoteName.Trim();
            if (trimmed.Any(char.IsWhiteSpace))
            {
                error = "Remote name cannot contain whitespace.";
                return false;
            }

            if (trimmed.StartsWith("-", StringComparison.Ordinal))
            {
                error = "Remote name cannot start with '-'.";
                return false;
            }

            if (trimmed.Contains('\\'))
            {
                error = "Remote name cannot contain backslashes.";
                return false;
            }

            if (trimmed.Contains('"'))
            {
                error = "Remote name cannot contain quote characters.";
                return false;
            }

            if (!trimmed.All(IsAllowedRefCharacter))
            {
                error = "Remote name contains unsupported characters.";
                return false;
            }

            normalized = trimmed;
            return true;
        }

        private static bool IsAllowedRefCharacter(char value)
        {
            if (char.IsControl(value) || value == '\u007f')
                return false;

            const string invalidGitRefCharacters = " ~^:?*[";
            return !invalidGitRefCharacters.Contains(value);
        }
    }
}
