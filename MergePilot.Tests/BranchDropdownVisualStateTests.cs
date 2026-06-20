using System;
using System.Linq;
using System.Threading;
using WpfComboBox = System.Windows.Controls.ComboBox;
using Xunit;

namespace MergePilot.Tests
{
    public class BranchDropdownVisualStateTests
    {
        [Fact]
        public void ResolveBranchItemContext_UsesObjectReferenceToDetermineRole()
        {
            RunOnStaThread(() =>
            {
                var sourceBranch = new BranchItem("feature", "feature/shared");
                var targetBranchWithSameName = new BranchItem("feature", "feature/shared");
                var sourceBox = new WpfComboBox();
                var targetBox = new WpfComboBox();
                sourceBox.Items.Add(sourceBranch);
                targetBox.Items.Add(targetBranchWithSameName);
                var visualState = new BranchDropdownVisualState();

                var sourceContext = visualState.ResolveBranchItemContext(
                    sourceBranch,
                    sourceBox,
                    targetBox);
                var targetContext = visualState.ResolveBranchItemContext(
                    targetBranchWithSameName,
                    sourceBox,
                    targetBox);

                Assert.NotNull(sourceContext);
                Assert.NotNull(targetContext);
                Assert.Equal(BranchSelectionRole.Source, sourceContext.Role);
                Assert.Equal(BranchSelectionRole.Target, targetContext.Role);
                Assert.Same(sourceBranch, sourceContext.Items.Single());
                Assert.Same(targetBranchWithSameName, targetContext.Items.Single());
            });
        }

        [Fact]
        public void ResolveRole_UsesComboBoxReference()
        {
            RunOnStaThread(() =>
            {
                var sourceBox = new WpfComboBox();
                var targetBox = new WpfComboBox();
                var unrelatedBox = new WpfComboBox();
                var visualState = new BranchDropdownVisualState();

                Assert.Equal(
                    BranchSelectionRole.Source,
                    visualState.ResolveRole(sourceBox, sourceBox, targetBox));
                Assert.Equal(
                    BranchSelectionRole.Target,
                    visualState.ResolveRole(targetBox, sourceBox, targetBox));
                Assert.Null(visualState.ResolveRole(unrelatedBox, sourceBox, targetBox));
            });
        }

        [Fact]
        public void GetBranchItems_ReturnsOnlyBranchItems()
        {
            RunOnStaThread(() =>
            {
                var branch = new BranchItem("main", "main");
                var comboBox = new WpfComboBox();
                comboBox.Items.Add(branch);
                comboBox.Items.Add("not a branch");
                var visualState = new BranchDropdownVisualState();

                var items = visualState.GetBranchItems(comboBox);

                Assert.Single(items);
                Assert.Same(branch, items[0]);
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
