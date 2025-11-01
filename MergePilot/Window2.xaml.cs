using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;
using MahApps.Metro.Controls;
using MessageBox = System.Windows.MessageBox;

namespace MergePilot
{
    /// <summary>
    /// Interaction logic for Window2.xaml
    /// </summary>
    public partial class Window2 : MetroWindow
    {
        public Window2()
        {
            try
            {
                InitializeComponent();
            }
            catch (XamlParseException ex)
            {
                MessageBox.Show($"XAML Parse Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Initialization Error: {ex.Message}", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void Merge_Click(object sender, RoutedEventArgs e)
        {
            // Implement merge logic here
        }

        public class HalfWidthConverter : IValueConverter
        {
            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double width)
                {
                    return width * 0.5; // Returns half the width of the parent Grid
                }
                return value;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }

        public class WidthLessThanConverter : IValueConverter
        {
            public double Threshold { get; set; } = 700; // Threshold for width comparison

            public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            {
                if (value is double width)
                {
                    return width <= Threshold;
                }
                return false;
            }

            public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            {
                throw new NotImplementedException();
            }
        }
    }
}