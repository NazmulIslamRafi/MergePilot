using System;
using System.Windows.Threading;

namespace MergePilot
{
    /// <summary>
    /// Owns MainWindow DispatcherTimer creation and shutdown.
    /// </summary>
    internal sealed class MainWindowTimerService
    {
        public DispatcherTimer StartTimer(
            TimeSpan interval,
            EventHandler tick,
            DispatcherPriority priority = DispatcherPriority.Background)
        {
            ArgumentNullException.ThrowIfNull(tick);

            var timer = new DispatcherTimer(priority)
            {
                Interval = interval
            };
            timer.Tick += tick;
            timer.Start();
            return timer;
        }

        public void Stop(DispatcherTimer? timer)
        {
            try
            {
                timer?.Stop();
            }
            catch
            {
                // Preserve best-effort window shutdown behavior.
            }
        }
    }
}
