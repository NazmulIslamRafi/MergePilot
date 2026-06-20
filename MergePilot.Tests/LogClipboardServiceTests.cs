using System;
using Xunit;

namespace MergePilot.Tests
{
    public class LogClipboardServiceTests
    {
        [Fact]
        public void CopyToClipboard_CopiesExportText()
        {
            var logBuffer = new LogBufferService();
            logBuffer.AppendOutput("output-line", status: true);
            logBuffer.AppendError("error-line", new DateTime(2026, 6, 6, 9, 15, 0));
            var clipboard = new StubClipboardService();
            var service = new LogClipboardService();

            service.CopyToClipboard(logBuffer, clipboard, "==");

            Assert.Contains("--- OUTPUT ---", clipboard.Text);
            Assert.Contains("output-line\n", clipboard.Text);
            Assert.Contains("--- ERRORS ---", clipboard.Text);
            Assert.Contains("[2026-06-06 09:15:00] error-line\n", clipboard.Text);
        }

        [Fact]
        public void CopyToClipboard_RequiresDependencies()
        {
            var service = new LogClipboardService();
            var logBuffer = new LogBufferService();
            var clipboard = new StubClipboardService();

            Assert.Throws<ArgumentNullException>(() => service.CopyToClipboard(null!, clipboard, "=="));
            Assert.Throws<ArgumentNullException>(() => service.CopyToClipboard(logBuffer, null!, "=="));
        }

        private sealed class StubClipboardService : IClipboardService
        {
            public string Text { get; private set; } = string.Empty;

            public void SetText(string text)
            {
                Text = text;
            }
        }
    }
}
