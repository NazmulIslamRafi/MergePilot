namespace MergePilot
{
    /// <summary>
    /// Classifies log lines so renderers can apply consistent visual treatment.
    /// </summary>
    public static class LogLineClassifier
    {
        public static LogLineKind Classify(string? line)
        {
            if (string.IsNullOrEmpty(line))
                return LogLineKind.Default;

            if (line.Contains("✅") || line.Contains("✔") || line.Contains("SUCCESSFUL") ||
                line.Contains("Updated") || line.Contains("created") || line.Contains("Successfully"))
            {
                return LogLineKind.Success;
            }

            if (line.Contains("⏭") || line.Contains("SKIPPED") || line.Contains("Skipped") ||
                line.Contains("skipped") || line.Contains("already up-to-date") ||
                line.Contains("⚠") || line.Contains("WARNING") || line.Contains("Warning"))
            {
                return LogLineKind.Warning;
            }

            if (line.Contains("❌") || line.Contains("FAILED") || line.Contains("failed") ||
                line.Contains("Error") || line.Contains("error") || line.Contains("Exception"))
            {
                return LogLineKind.Error;
            }

            if (line.Contains("🚀") || line.Contains("📁") || line.Contains("🔀") ||
                line.Contains("📥") || line.Contains("Processing") || line.Contains("Attempt") ||
                line.Contains("Timestamp") || line.Contains("Path:") || line.Contains("🔄"))
            {
                return LogLineKind.Info;
            }

            return LogLineKind.Default;
        }
    }

    public enum LogLineKind
    {
        Default,
        Success,
        Warning,
        Error,
        Info
    }
}
