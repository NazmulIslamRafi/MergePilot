using System;
using System.Threading;
using System.Windows.Controls;
using Xunit;

namespace MergePilot.Tests
{
    public class RefreshWorkflowVisualStateTests
    {
        [Fact]
        public void Apply_ClearsSelectionsAndRestoresBranchText()
        {
            RunOnStaThread(() =>
            {
                var repositoryList = new ListView();
                repositoryList.Items.Add("Repo A");
                repositoryList.SelectedIndex = 0;

                var sourceBranchBox = CreateSelectedComboBox();
                var targetBranchBox = CreateSelectedComboBox();
                var refresh = RefreshWorkflowResult.Empty(
                    "refreshed",
                    "feature/source",
                    "release/target");
                var visualState = new RefreshWorkflowVisualState();

                visualState.Apply(
                    refresh,
                    repositoryList,
                    sourceBranchBox,
                    targetBranchBox);

                Assert.Equal(-1, repositoryList.SelectedIndex);
                Assert.Equal(-1, sourceBranchBox.SelectedIndex);
                Assert.Equal(-1, targetBranchBox.SelectedIndex);
                Assert.Equal("feature/source", sourceBranchBox.Text);
                Assert.Equal("release/target", targetBranchBox.Text);
            });
        }

        [Fact]
        public void Apply_WithNullControls_DoesNotThrow()
        {
            var visualState = new RefreshWorkflowVisualState();
            var refresh = RefreshWorkflowResult.Empty("refreshed");

            visualState.Apply(
                refresh,
                repositoryList: null,
                sourceBranchBox: null,
                targetBranchBox: null);
        }

        private static ComboBox CreateSelectedComboBox()
        {
            var comboBox = new ComboBox();
            comboBox.Items.Add("main");
            comboBox.SelectedIndex = 0;
            return comboBox;
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
