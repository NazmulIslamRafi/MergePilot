using System;
using System.Linq;
using System.Threading;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using Xunit;

namespace MergePilot.Tests
{
    public class RichTextBoxLogRendererTests
    {
        [Fact]
        public void Append_CullsOldParagraphsWhenDisplayedLineLimitIsExceeded()
        {
            RunOnStaThread(() =>
            {
                var box = new RichTextBox();
                var renderer = new RichTextBoxLogRenderer(box)
                {
                    MaxDisplayedLines = 5
                };

                renderer.Append(string.Join("\n", Enumerable.Range(0, 10).Select(index => $"line-{index}")), Brushes.White);

                Assert.Equal(4, box.Document.Blocks.Count);
                Assert.Equal("line-6", GetParagraphText(box.Document.Blocks.FirstBlock));
                Assert.Equal("line-9", GetParagraphText(box.Document.Blocks.LastBlock));
            });
        }

        [Fact]
        public void Append_WithLargeLogVolume_KeepsVisibleParagraphsBounded()
        {
            RunOnStaThread(() =>
            {
                var box = new RichTextBox();
                var renderer = new RichTextBoxLogRenderer(box)
                {
                    MaxDisplayedLines = 200
                };

                renderer.Append(string.Join("\n", Enumerable.Range(0, 1_500).Select(index => $"line-{index}")), Brushes.White);

                Assert.Equal(180, box.Document.Blocks.Count);
                Assert.Equal("line-1320", GetParagraphText(box.Document.Blocks.FirstBlock));
                Assert.Equal("line-1499", GetParagraphText(box.Document.Blocks.LastBlock));
            });
        }

        [Fact]
        public void Append_UsesClassifierBrushForKnownLineKinds()
        {
            RunOnStaThread(() =>
            {
                var box = new RichTextBox();
                var renderer = new RichTextBoxLogRenderer(box);

                renderer.Append("Successfully merged branch", Brushes.White);

                var run = ((Paragraph)box.Document.Blocks.FirstBlock!).Inlines.OfType<Run>().Single();
                Assert.Same(ColorScheme.SuccessBrush, run.Foreground);
            });
        }

        [Fact]
        public void Clear_RemovesRenderedParagraphs()
        {
            RunOnStaThread(() =>
            {
                var box = new RichTextBox();
                var renderer = new RichTextBoxLogRenderer(box);
                renderer.Append("line", Brushes.White);

                renderer.Clear();

                Assert.Empty(box.Document.Blocks);
            });
        }

        private static string GetParagraphText(Block? block)
        {
            var paragraph = Assert.IsType<Paragraph>(block);
            return paragraph.Inlines.OfType<Run>().Single().Text;
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try
                {
                    action();
                }
                catch (Exception ex)
                {
                    exception = ex;
                }
            });

            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();

            if (exception != null)
                throw exception;
        }
    }
}
