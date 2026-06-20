using System;

namespace MergePilot
{
    /// <summary>
    /// Persists close-time MainWindow state and releases close-time resources.
    /// </summary>
    public class MainWindowClosingService
    {
        public void SaveAndDispose(
            AppSettings settings,
            double logFontSize,
            IDisposable logBuffer)
        {
            ArgumentNullException.ThrowIfNull(settings);
            ArgumentNullException.ThrowIfNull(logBuffer);

            settings.LogFontSize = logFontSize;
            settings.Save();
            logBuffer.Dispose();
        }
    }
}
