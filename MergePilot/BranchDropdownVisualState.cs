using System;
using System.Collections.Generic;
using System.Linq;
using WpfComboBox = System.Windows.Controls.ComboBox;

namespace MergePilot
{
    /// <summary>
    /// Resolves WPF branch dropdown event context without mutating branch selection state.
    /// </summary>
    internal sealed class BranchDropdownVisualState
    {
        public BranchDropdownVisualContext? ResolveBranchItemContext(
            BranchItem branchItem,
            WpfComboBox? sourceBranchBox,
            WpfComboBox? targetBranchBox)
        {
            ArgumentNullException.ThrowIfNull(branchItem);

            if (ContainsItemByReference(sourceBranchBox, branchItem))
                return new BranchDropdownVisualContext(BranchSelectionRole.Source, GetBranchItems(sourceBranchBox));

            if (ContainsItemByReference(targetBranchBox, branchItem))
                return new BranchDropdownVisualContext(BranchSelectionRole.Target, GetBranchItems(targetBranchBox));

            return null;
        }

        public BranchSelectionRole? ResolveRole(
            WpfComboBox comboBox,
            WpfComboBox? sourceBranchBox,
            WpfComboBox? targetBranchBox)
        {
            ArgumentNullException.ThrowIfNull(comboBox);

            if (ReferenceEquals(comboBox, sourceBranchBox))
                return BranchSelectionRole.Source;

            if (ReferenceEquals(comboBox, targetBranchBox))
                return BranchSelectionRole.Target;

            return null;
        }

        public IReadOnlyList<BranchItem> GetBranchItems(WpfComboBox? comboBox)
        {
            return comboBox?.Items.OfType<BranchItem>().ToArray()
                ?? Array.Empty<BranchItem>();
        }

        private static bool ContainsItemByReference(WpfComboBox? comboBox, BranchItem item)
        {
            if (comboBox?.Items == null)
                return false;

            foreach (var comboItem in comboBox.Items)
            {
                if (ReferenceEquals(comboItem, item))
                    return true;
            }

            return false;
        }
    }

    internal sealed record BranchDropdownVisualContext(
        BranchSelectionRole Role,
        IReadOnlyList<BranchItem> Items);
}
