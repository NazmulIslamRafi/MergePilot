using System;
using WpfKeyEventHandler = System.Windows.Input.KeyEventHandler;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfWindow = System.Windows.Window;

namespace MergePilot
{
    /// <summary>
    /// Owns MainWindow event hook/unhook operations that are inherently WPF-specific.
    /// </summary>
    internal sealed class MainWindowEventHookService
    {
        public void HookBranchDropdowns(
            WpfComboBox? sourceBranchBox,
            WpfComboBox? targetBranchBox,
            EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            TryApply(() =>
            {
                if (sourceBranchBox != null)
                {
                    sourceBranchBox.DropDownOpened -= handler;
                    sourceBranchBox.DropDownOpened += handler;
                }

                if (targetBranchBox != null)
                {
                    targetBranchBox.DropDownOpened -= handler;
                    targetBranchBox.DropDownOpened += handler;
                }
            });
        }

        public void UnhookBranchDropdowns(
            WpfComboBox? sourceBranchBox,
            WpfComboBox? targetBranchBox,
            EventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            TryApply(() =>
            {
                if (sourceBranchBox != null)
                    sourceBranchBox.DropDownOpened -= handler;

                if (targetBranchBox != null)
                    targetBranchBox.DropDownOpened -= handler;
            });
        }

        public void HookPreviewKeyDown(WpfWindow? window, WpfKeyEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            TryApply(() =>
            {
                if (window == null)
                    return;

                window.PreviewKeyDown -= handler;
                window.PreviewKeyDown += handler;
            });
        }

        public void UnhookPreviewKeyDown(WpfWindow? window, WpfKeyEventHandler handler)
        {
            ArgumentNullException.ThrowIfNull(handler);

            TryApply(() =>
            {
                if (window != null)
                    window.PreviewKeyDown -= handler;
            });
        }

        private static void TryApply(Action action)
        {
            try
            {
                action();
            }
            catch
            {
                // Preserve best-effort WPF event hookup behavior from the existing window code.
            }
        }
    }
}
