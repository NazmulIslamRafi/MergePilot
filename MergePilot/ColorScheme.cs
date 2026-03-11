using System.Windows.Media;

namespace MergePilot
{
    /// <summary>
    /// Color scheme matching the bash script style
    /// Maps to: GREEN, YELLOW, RED, BLUE from bash
    /// </summary>
    public static class ColorScheme
    {
        // Success - GREEN (\e[32m)
        public static readonly System.Windows.Media.Color SuccessColor = System.Windows.Media.Color.FromRgb(34, 197, 94);  // #22C55E
        public static readonly SolidColorBrush SuccessBrush = new SolidColorBrush(SuccessColor);
        public static readonly string SuccessHex = "#22C55E";

        // Warning/Skipped - YELLOW (\e[33m)
        public static readonly System.Windows.Media.Color WarningColor = System.Windows.Media.Color.FromRgb(234, 179, 8);  // #EAB308
        public static readonly SolidColorBrush WarningBrush = new SolidColorBrush(WarningColor);
        public static readonly string WarningHex = "#EAB308";

        // Error/Failed - RED (\e[31m)
        public static readonly System.Windows.Media.Color ErrorColor = System.Windows.Media.Color.FromRgb(239, 68, 68);   // #EF4444
        public static readonly SolidColorBrush ErrorBrush = new SolidColorBrush(ErrorColor);
        public static readonly string ErrorHex = "#EF4444";

        // Info/General - BLUE (\e[34m)
        public static readonly System.Windows.Media.Color InfoColor = System.Windows.Media.Color.FromRgb(59, 130, 246);   // #3B82F6
        public static readonly SolidColorBrush InfoBrush = new SolidColorBrush(InfoColor);
        public static readonly string InfoHex = "#3B82F6";

        // Secondary Info Color
        public static readonly System.Windows.Media.Color SecondaryInfoColor = System.Windows.Media.Color.FromRgb(96, 165, 250);  // #60A5FA
        public static readonly SolidColorBrush SecondaryInfoBrush = new SolidColorBrush(SecondaryInfoColor);
        public static readonly string SecondaryInfoHex = "#60A5FA";
    }
}
