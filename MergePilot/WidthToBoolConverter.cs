using System;
using System.Globalization;
using System.Windows.Data;

namespace MergePilot
{
    // Converts a numeric ActualWidth into a bool indicating whether width is less than Threshold
    public class WidthToBoolConverter : IValueConverter
    {
        public double Threshold { get; set; } = 800;

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            try
            {
                if (value is double d)
                {
                    return d <= Threshold;
                }
            }
            catch { }
            return false;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotSupportedException();
        }
    }
}
