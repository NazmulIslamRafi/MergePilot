using Xunit;

namespace MergePilot.Tests
{
    public class LogDisplayCullPolicyTests
    {
        [Fact]
        public void GetLinesToRemove_WhenWithinLimit_ReturnsZero()
        {
            var linesToRemove = LogDisplayCullPolicy.GetLinesToRemove(
                displayedLineCount: 2_000,
                maxDisplayedLines: RichTextBoxLogRenderer.DefaultMaxDisplayedLines);

            Assert.Equal(0, linesToRemove);
        }

        [Fact]
        public void GetLinesToRemove_WithDefaultLimit_CullsBackToHysteresisTarget()
        {
            var linesToRemove = LogDisplayCullPolicy.GetLinesToRemove(
                displayedLineCount: 2_105,
                maxDisplayedLines: RichTextBoxLogRenderer.DefaultMaxDisplayedLines);

            Assert.Equal(205, linesToRemove);
            Assert.Equal(1_900, 2_105 - linesToRemove);
        }

        [Fact]
        public void GetLinesToRemove_WithSmallLimit_DoesNotOverCullBelowTargetWindow()
        {
            var linesToRemove = LogDisplayCullPolicy.GetLinesToRemove(
                displayedLineCount: 10,
                maxDisplayedLines: 5);

            Assert.Equal(6, linesToRemove);
            Assert.Equal(4, 10 - linesToRemove);
        }

        [Fact]
        public void GetLinesToRemove_WithDisabledLimit_RemovesDisplayedLines()
        {
            var linesToRemove = LogDisplayCullPolicy.GetLinesToRemove(
                displayedLineCount: 10,
                maxDisplayedLines: 0);

            Assert.Equal(10, linesToRemove);
        }
    }
}
