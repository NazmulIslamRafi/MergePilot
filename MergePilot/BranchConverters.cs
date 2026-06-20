using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace MergePilot
{
    /// <summary>
    /// Converts branch level to indentation width
    /// </summary>
    public class LevelToIndentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int level)
            {
                return level * 15; // 15 pixels per level
            }
            return 0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts boolean to visibility (with optional inversion)
    /// </summary>
    public class BoolToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isGroup = value is bool b && b;
            bool invert = parameter is string s && s.Equals("Invert", StringComparison.OrdinalIgnoreCase);

            // Show checkbox for leaf nodes (IsGroup = false)
            // If invert parameter is set, invert the logic
            if (invert)
            {
                return isGroup ? Visibility.Collapsed : Visibility.Visible;
            }
            else
            {
                return isGroup ? Visibility.Visible : Visibility.Collapsed;
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts boolean to font weight (bold for groups, normal for leaves)
    /// </summary>
    public class BoolToFontWeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            bool isGroup = value is bool b && b;
            return isGroup ? FontWeights.Bold : FontWeights.Normal;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

    /// <summary>
    /// Converts window width to boolean (true if width > threshold)
    /// </summary>
    public class WidthToBoolConverter : IValueConverter
    {
        public int Threshold { get; set; } = 900;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is double width && parameter is string thresholdStr && int.TryParse(thresholdStr, out int threshold))
            {
                return width > threshold;
            }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return System.Windows.Data.Binding.DoNothing;
        }
    }

}


