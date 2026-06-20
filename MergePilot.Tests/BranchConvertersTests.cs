using System.Globalization;
using System.Windows;
using System.Windows.Data;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchConvertersTests
    {
        [Fact]
        public void LevelToIndentConverter_ConvertsLevelToPixels()
        {
            var converter = new LevelToIndentConverter();

            var result = converter.Convert(2, typeof(double), null, CultureInfo.InvariantCulture);

            Assert.Equal(30, result);
        }

        [Fact]
        public void BoolToFontWeightConverter_UsesBoldForGroups()
        {
            var converter = new BoolToFontWeightConverter();

            var result = converter.Convert(true, typeof(FontWeight), null, CultureInfo.InvariantCulture);

            Assert.Equal(FontWeights.Bold, result);
        }

        [Fact]
        public void BoolToVisibilityConverter_CanInvertVisibility()
        {
            var converter = new BoolToVisibilityConverter();

            var result = converter.Convert(false, typeof(Visibility), "Invert", CultureInfo.InvariantCulture);

            Assert.Equal(Visibility.Visible, result);
        }

        [Fact]
        public void ConvertBack_ForDisplayConverters_ReturnsDoNothing()
        {
            Assert.Equal(
                Binding.DoNothing,
                new LevelToIndentConverter().ConvertBack(0, typeof(int), null, CultureInfo.InvariantCulture));
            Assert.Equal(
                Binding.DoNothing,
                new BoolToVisibilityConverter().ConvertBack(Visibility.Visible, typeof(bool), null, CultureInfo.InvariantCulture));
            Assert.Equal(
                Binding.DoNothing,
                new BoolToFontWeightConverter().ConvertBack(FontWeights.Bold, typeof(bool), null, CultureInfo.InvariantCulture));
            Assert.Equal(
                Binding.DoNothing,
                new WidthToBoolConverter().ConvertBack(true, typeof(double), null, CultureInfo.InvariantCulture));
        }
    }
}
