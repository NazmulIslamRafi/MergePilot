using System;
using System.Threading;
using MaterialDesignThemes.Wpf;
using Xunit;
using WpfButton = System.Windows.Controls.Button;

namespace MergePilot.Tests
{
    public class ButtonProgressScopeTests
    {
        [Fact]
        public void Start_RestoresInitialEnabledStateByDefault()
        {
            RunOnStaThread(() =>
            {
                var button = new WpfButton { IsEnabled = false };

                using (ButtonProgressScope.Start(button))
                {
                    Assert.False(button.IsEnabled);
                    Assert.True(ButtonProgressAssist.GetIsIndeterminate(button));
                }

                Assert.False(button.IsEnabled);
                Assert.False(ButtonProgressAssist.GetIsIndeterminate(button));
            });
        }

        [Fact]
        public void Start_RestoresExplicitEnabledStateWhenProvided()
        {
            RunOnStaThread(() =>
            {
                var button = new WpfButton { IsEnabled = false };

                using (ButtonProgressScope.Start(button, restoreEnabled: true))
                {
                    Assert.False(button.IsEnabled);
                    Assert.True(ButtonProgressAssist.GetIsIndeterminate(button));
                }

                Assert.True(button.IsEnabled);
                Assert.False(ButtonProgressAssist.GetIsIndeterminate(button));
            });
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
