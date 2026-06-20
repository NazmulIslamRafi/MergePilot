using System;
using System.Threading;
using Xunit;

namespace MergePilot.Tests
{
    public class MainWindowTimerServiceTests
    {
        [Fact]
        public void StartTimer_StartsTimerWithInterval()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowTimerService();
                EventHandler handler = (_, _) => { };

                var timer = service.StartTimer(TimeSpan.FromMilliseconds(123), handler);

                try
                {
                    Assert.True(timer.IsEnabled);
                    Assert.Equal(TimeSpan.FromMilliseconds(123), timer.Interval);
                }
                finally
                {
                    timer.Stop();
                }
            });
        }

        [Fact]
        public void Stop_DisablesTimerAndAllowsNull()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowTimerService();
                var timer = service.StartTimer(TimeSpan.FromSeconds(1), (_, _) => { });

                service.Stop(timer);
                service.Stop(null);

                Assert.False(timer.IsEnabled);
            });
        }

        [Fact]
        public void StartTimer_RequiresTickHandler()
        {
            var service = new MainWindowTimerService();

            Assert.Throws<ArgumentNullException>(() =>
                service.StartTimer(TimeSpan.FromMilliseconds(1), null!));
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
