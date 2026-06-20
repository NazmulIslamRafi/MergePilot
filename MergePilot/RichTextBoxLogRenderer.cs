using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows.Media;
using WpfBrush = System.Windows.Media.Brush;
using WpfRichTextBox = System.Windows.Controls.RichTextBox;

namespace MergePilot
{
    /// <summary>
    /// Renders buffered log text into a RichTextBox and keeps the visible log size bounded.
    /// </summary>
    public class RichTextBoxLogRenderer
    {
        public const int DefaultMaxDisplayedLines = 2000;

        private readonly WpfRichTextBox _box;
        private ScrollViewer? _scrollViewer;
        private double _targetScrollOffset;
        private int _displayedLineCount;

        public int MaxDisplayedLines { get; set; } = DefaultMaxDisplayedLines;

        public RichTextBoxLogRenderer(WpfRichTextBox box)
        {
            _box = box ?? throw new ArgumentNullException(nameof(box));
            _box.Document?.Blocks.Clear();
        }

        public void Append(string text, WpfBrush? foreground)
        {
            try
            {
                if (_box.Document == null || string.IsNullOrEmpty(text))
                    return;

                _box.BeginChange();

                var lines = text.Split(new[] { "\n" }, StringSplitOptions.None);
                foreach (var line in lines)
                {
                    if (string.IsNullOrEmpty(line))
                        continue;

                    var paragraph = new Paragraph
                    {
                        Margin = new Thickness(0),
                        Padding = new Thickness(0),
                        LineHeight = 1.0
                    };

                    var run = new Run(line)
                    {
                        Foreground = GetBrush(line, foreground)
                    };

                    paragraph.Inlines.Add(run);
                    _box.Document.Blocks.Add(paragraph);
                    _displayedLineCount++;
                }

                _box.EndChange();
                _targetScrollOffset = double.MaxValue;

                if (_displayedLineCount > MaxDisplayedLines)
                    CullOldLogLines();
            }
            catch
            {
            }
        }

        public void Clear()
        {
            try
            {
                _box.Document?.Blocks.Clear();
                _displayedLineCount = 0;
                _targetScrollOffset = 0;
            }
            catch
            {
            }
        }

        public void UpdateSmoothScroll()
        {
            try
            {
                if (_box.Document == null)
                    return;

                var scrollViewer = GetScrollViewer();
                if (scrollViewer == null)
                    return;

                var scrollableHeight = scrollViewer.ScrollableHeight;
                if (scrollableHeight <= 0)
                    return;

                var currentOffset = scrollViewer.VerticalOffset;
                if (_targetScrollOffset == double.MaxValue)
                    _targetScrollOffset = scrollableHeight;

                var diff = _targetScrollOffset - currentOffset;
                if (Math.Abs(diff) > 0.5)
                {
                    scrollViewer.ScrollToVerticalOffset(currentOffset + diff * 0.06);
                }
                else if (Math.Abs(diff) > 0)
                {
                    scrollViewer.ScrollToEnd();
                }
            }
            catch
            {
            }
        }

        private void CullOldLogLines()
        {
            try
            {
                var linesToRemove = LogDisplayCullPolicy.GetLinesToRemove(_displayedLineCount, MaxDisplayedLines);
                if (linesToRemove <= 0)
                    return;

                _box.BeginChange();
                for (var i = 0; i < linesToRemove && _box.Document.Blocks.Count > 0; i++)
                {
                    var firstBlock = _box.Document.Blocks.FirstBlock;
                    if (firstBlock != null)
                        _box.Document.Blocks.Remove(firstBlock);
                }
                _box.EndChange();

                _displayedLineCount = _box.Document.Blocks.Count;
            }
            catch
            {
            }
        }

        private static WpfBrush GetBrush(string line, WpfBrush? fallback)
        {
            return LogLineClassifier.Classify(line) switch
            {
                LogLineKind.Success => ColorScheme.SuccessBrush,
                LogLineKind.Warning => ColorScheme.WarningBrush,
                LogLineKind.Error => ColorScheme.ErrorBrush,
                LogLineKind.Info => ColorScheme.InfoBrush,
                _ => fallback ?? System.Windows.Media.Brushes.White
            };
        }

        private ScrollViewer? GetScrollViewer()
        {
            _scrollViewer ??= FindScrollViewer(_box);
            return _scrollViewer;
        }

        private static ScrollViewer? FindScrollViewer(DependencyObject obj)
        {
            for (var i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                var child = VisualTreeHelper.GetChild(obj, i);
                if (child is ScrollViewer scrollViewer)
                    return scrollViewer;

                var found = FindScrollViewer(child);
                if (found != null)
                    return found;
            }

            return null;
        }
    }
}
