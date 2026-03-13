using System;
using System.Collections.Generic;
using System.Text;
using System.Windows.Media;

namespace MergePilot
{
    /// <summary>
    /// Formats log output to match bash script style with timestamps, sections, and summaries
    /// </summary>
    public static class LogFormatter
    {
        // Separators
        private const string SectionSeparator = "===============================================";
        private const string SubSectionSeparator = "-----------------------------------------------";

        /// <summary>
        /// Returns a tuple of (text, color) for RichTextBox coloring
        /// </summary>
        public static (string text, SolidColorBrush color) ColoredText(string text, SolidColorBrush color)
        {
            return (text, color);
        }

        /// <summary>
        /// Returns success colored text (GREEN - #22C55E)
        /// </summary>
        public static (string text, SolidColorBrush color) SuccessText(string text)
        {
            return (text, ColorScheme.SuccessBrush);
        }

        /// <summary>
        /// Returns warning colored text (YELLOW - #EAB308)
        /// </summary>
        public static (string text, SolidColorBrush color) WarningText(string text)
        {
            return (text, ColorScheme.WarningBrush);
        }

        /// <summary>
        /// Returns error colored text (RED - #EF4444)
        /// </summary>
        public static (string text, SolidColorBrush color) ErrorText(string text)
        {
            return (text, ColorScheme.ErrorBrush);
        }

        /// <summary>
        /// Returns info colored text (BLUE - #3B82F6)
        /// </summary>
        public static (string text, SolidColorBrush color) InfoText(string text)
        {
            return (text, ColorScheme.InfoBrush);
        }

        /// <summary>
        /// Format operation start banner
        /// </summary>
        public static string FormatOperationStart(string operationType)
        {
            var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"$ {operationType} : " + $"[{timestamp}]\n");
            sb.Append(SectionSeparator + "\n");
            return sb.ToString();
        }

        /// <summary>
        /// Format project/repository section header
        /// </summary>
        public static string FormatProjectHeader(string projectName, string repoPath)
        {
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"📁 {projectName} => " + $"Path: {repoPath}\n");
            sb.Append(SectionSeparator + "\n");
            return sb.ToString();
        }

        /// <summary>
        /// Format branch operation sub-header
        /// </summary>
        public static string FormatBranchOperationHeader(string sourceBranch, string targetBranch)
        {
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"🔀 {sourceBranch} → {targetBranch}\n");
            sb.Append(SectionSeparator + "\n");
            return sb.ToString();
        }

        /// <summary>
        /// Format pull/fetch operation sub-header
        /// </summary>
        public static string FormatPullOperationHeader(string branchName)
        {
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"📥 Pulling : {branchName}\n");
            sb.Append(SectionSeparator + "\n");
            return sb.ToString();
        }

        /// <summary>
        /// Format final comprehensive summary report
        /// </summary>
        public static string FormatFinalSummary(string title, List<string> successList, List<string> skipList, List<string> failList)
        {
            var sb = new StringBuilder();
            sb.Append(SectionSeparator + "\n");
            sb.Append($"📊 {title} : " + $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}]\n");
            sb.Append(SectionSeparator + "\n");

            // Successful operations
            sb.Append($"✅ SUCCESSFUL ({successList.Count})  :\n");
            if (successList.Count > 0)
            {
                foreach (var item in successList)
                {
                    sb.Append($" ✔ {item}\n");
                }
            }

            // Skipped operations
            sb.Append($"⏭ SKIPPED ({skipList.Count}):\n");
            if (skipList.Count > 0)
            {
                foreach (var item in skipList)
                {
                    sb.Append($" ⏭ {item}\n");
                }
            }

            // Failed operations
            sb.Append($"❌ FAILED ({failList.Count}):\n");
            if (failList.Count > 0)
            {
                foreach (var item in failList)
                {
                    sb.Append($" ❌ {item}\n");
                }
            }

            sb.Append(SectionSeparator + "\n");
            sb.Append($"Total: {successList.Count + skipList.Count + failList.Count} operations processed.\n");
            sb.Append(SectionSeparator + "\n");

            return sb.ToString();
        }

        /// <summary>
        /// Format project-level summary
        /// </summary>
        public static string FormatProjectSummary(string projectName, 
            List<string> successBranches, List<string> skippedBranches, List<string> failedBranches)
        {
            var sb = new StringBuilder();
            sb.Append("\n" + SectionSeparator + "\n");
            sb.Append($"📋 Summary for {projectName} \n");
            sb.Append(SectionSeparator + "\n");

            if (successBranches.Count > 0)
            {
                sb.Append($"✅ Updated ({successBranches.Count}):\n");
                foreach (var branch in successBranches)
                {
                    sb.Append($" ✔ {branch}\n");
                }
            }

            if (skippedBranches.Count > 0)
            {
                sb.Append($"⏭ Skipped - Already Up-to-date ({skippedBranches.Count}):\n");
                foreach (var branch in skippedBranches)
                {
                    sb.Append($" ⏭ {branch}\n");
                }
            }

            if (failedBranches.Count > 0)
            {
                sb.Append($"❌ Failed ({failedBranches.Count}):\n");
                foreach (var branch in failedBranches)
                {
                    sb.Append($" ❌ {branch}\n");
                }
            }

            if (failedBranches.Count == 0 && successBranches.Count == 0 && skippedBranches.Count == 0)
            {
                sb.Append(SectionSeparator + "\n");
                sb.Append("ℹ️ No operations to process.\n");
                sb.Append(SectionSeparator + "\n");
            }
            else if (failedBranches.Count == 0)
            {
                sb.Append(SectionSeparator + "\n");
                sb.Append("ℹ️ All operations completed successfully!\n");
                sb.Append(SectionSeparator + "\n");
            }

            return sb.ToString();
        }

        /// <summary>
        /// Format inline operation status
        /// </summary>
        public static string FormatSuccess(string message) => $"✅ {message}";
        public static string FormatSkipped(string message) => $"⏭ {message}";
        public static string FormatError(string message) => $"❌ {message}";
        public static string FormatInfo(string message) => $"ℹ️ {message}";
        public static string FormatWarning(string message) => $"⚠️ {message}";
        public static string FormatProgress(string message) => $"▶️ {message}";
    }
}

