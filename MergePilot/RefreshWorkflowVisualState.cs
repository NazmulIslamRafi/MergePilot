using System;
using WpfComboBox = System.Windows.Controls.ComboBox;
using WpfListView = System.Windows.Controls.ListView;

namespace MergePilot
{
    /// <summary>
    /// Applies WPF control state changes for the refresh workflow.
    /// </summary>
    internal sealed class RefreshWorkflowVisualState
    {
        public void Apply(
            RefreshWorkflowResult refresh,
            WpfListView? repositoryList,
            WpfComboBox? sourceBranchBox,
            WpfComboBox? targetBranchBox)
        {
            ArgumentNullException.ThrowIfNull(refresh);

            TryApply(() =>
            {
                if (repositoryList != null)
                    repositoryList.SelectedIndex = -1;
            });

            ApplyBranchBox(sourceBranchBox, refresh.SourceBranchTextToRestore);
            ApplyBranchBox(targetBranchBox, refresh.TargetBranchTextToRestore);
        }

        private static void ApplyBranchBox(WpfComboBox? branchBox, string? restoredText)
        {
            TryApply(() =>
            {
                if (branchBox == null)
                    return;

                branchBox.SelectedIndex = -1;
                if (restoredText != null)
                    branchBox.Text = restoredText;
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
                // Keep refresh best-effort, matching the existing WPF event-handler behavior.
            }
        }
    }
}
