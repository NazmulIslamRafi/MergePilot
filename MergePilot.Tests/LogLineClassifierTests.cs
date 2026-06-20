using Xunit;

namespace MergePilot.Tests
{
    public class LogLineClassifierTests
    {
        [Theory]
        [InlineData("✔ Updated main")]
        [InlineData("SUCCESSFUL (2)")]
        [InlineData("Successfully merged")]
        public void Classify_WithSuccessPattern_ReturnsSuccess(string line)
        {
            Assert.Equal(LogLineKind.Success, LogLineClassifier.Classify(line));
        }

        [Theory]
        [InlineData("⚠ warning")]
        [InlineData("WARNING: skipped")]
        [InlineData("SKIPPED (1)")]
        [InlineData("Warning: heads up")]
        [InlineData("main is already up-to-date")]
        [InlineData("⏭ Skipped branch")]
        public void Classify_WithWarningPattern_ReturnsWarning(string line)
        {
            Assert.Equal(LogLineKind.Warning, LogLineClassifier.Classify(line));
        }

        [Theory]
        [InlineData("❌ failed")]
        [InlineData("FAILED (1)")]
        [InlineData("Exception while updating")]
        public void Classify_WithErrorPattern_ReturnsError(string line)
        {
            Assert.Equal(LogLineKind.Error, LogLineClassifier.Classify(line));
        }

        [Theory]
        [InlineData("🚀 Starting")]
        [InlineData("Processing repo")]
        [InlineData("Timestamp: 2026-06-02")]
        public void Classify_WithInfoPattern_ReturnsInfo(string line)
        {
            Assert.Equal(LogLineKind.Info, LogLineClassifier.Classify(line));
        }

        [Fact]
        public void Classify_WithPlainText_ReturnsDefault()
        {
            Assert.Equal(LogLineKind.Default, LogLineClassifier.Classify("plain output"));
        }
    }
}
