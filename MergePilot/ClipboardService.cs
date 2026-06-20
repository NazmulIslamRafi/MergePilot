using System;
using WpfClipboard = System.Windows.Clipboard;

namespace MergePilot
{
    public interface IClipboardService
    {
        void SetText(string text);
    }

    public sealed class WpfClipboardService : IClipboardService
    {
        public void SetText(string text)
        {
            WpfClipboard.SetText(text);
        }
    }

    /// <summary>
    /// Coordinates copying exported log text to the clipboard.
    /// </summary>
    public class LogClipboardService
    {
        public void CopyToClipboard(
            LogBufferService logBufferService,
            IClipboardService clipboardService,
            string sectionSeparator)
        {
            ArgumentNullException.ThrowIfNull(logBufferService);
            ArgumentNullException.ThrowIfNull(clipboardService);

            clipboardService.SetText(logBufferService.BuildExport(sectionSeparator));
        }
    }
}
