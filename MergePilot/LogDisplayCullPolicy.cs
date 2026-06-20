using System;

namespace MergePilot
{
    /// <summary>
    /// Calculates bounded log display culling targets for visible log renderers.
    /// </summary>
    public static class LogDisplayCullPolicy
    {
        public const int DefaultCullHysteresisLines = 100;

        public static int GetLinesToRemove(int displayedLineCount, int maxDisplayedLines)
        {
            if (displayedLineCount <= 0 || maxDisplayedLines <= 0)
                return Math.Max(0, displayedLineCount);

            if (displayedLineCount <= maxDisplayedLines)
                return 0;

            return displayedLineCount - GetTargetLineCount(maxDisplayedLines);
        }

        public static int GetTargetLineCount(int maxDisplayedLines)
        {
            if (maxDisplayedLines <= 0)
                return 0;

            var hysteresis = Math.Min(
                DefaultCullHysteresisLines,
                Math.Max(1, maxDisplayedLines / 10));

            return Math.Max(0, maxDisplayedLines - hysteresis);
        }
    }
}
