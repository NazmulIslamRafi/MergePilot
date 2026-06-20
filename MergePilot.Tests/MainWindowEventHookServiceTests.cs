using System;
using System.Threading;
using System.Windows.Controls;
using WpfKeyEventHandler = System.Windows.Input.KeyEventHandler;
using Xunit;

namespace MergePilot.Tests
{
    public class MainWindowEventHookServiceTests
    {
        [Fact]
        public void HookBranchDropdowns_HooksEachDropdownOnceAndUnhooks()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowEventHookService();
                var source = new TestComboBox();
                var target = new TestComboBox();
                var calls = 0;
                EventHandler handler = (_, _) => calls++;

                service.HookBranchDropdowns(source, target, handler);
                service.HookBranchDropdowns(source, target, handler);

                source.TriggerDropDownOpened();
                target.TriggerDropDownOpened();

                Assert.Equal(2, calls);

                service.UnhookBranchDropdowns(source, target, handler);
                source.TriggerDropDownOpened();
                target.TriggerDropDownOpened();

                Assert.Equal(2, calls);
            });
        }

        private sealed class TestComboBox : ComboBox
        {
            public void TriggerDropDownOpened() => OnDropDownOpened(EventArgs.Empty);
        }

        [Fact]
        public void HookMethods_WithNullControls_DoNotThrow()
        {
            var service = new MainWindowEventHookService();
            EventHandler dropdownHandler = (_, _) => { };
            WpfKeyEventHandler keyHandler = (_, _) => { };

            service.HookBranchDropdowns(null, null, dropdownHandler);
            service.UnhookBranchDropdowns(null, null, dropdownHandler);
            service.HookPreviewKeyDown(null, keyHandler);
            service.UnhookPreviewKeyDown(null, keyHandler);
        }

        [Fact]
        public void HookMethods_RequireHandlers()
        {
            var service = new MainWindowEventHookService();

            Assert.Throws<ArgumentNullException>(() => service.HookBranchDropdowns(null, null, null!));
            Assert.Throws<ArgumentNullException>(() => service.UnhookBranchDropdowns(null, null, null!));
            Assert.Throws<ArgumentNullException>(() => service.HookPreviewKeyDown(null, null!));
            Assert.Throws<ArgumentNullException>(() => service.UnhookPreviewKeyDown(null, null!));
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
