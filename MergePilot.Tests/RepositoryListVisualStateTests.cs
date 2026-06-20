using System;
using System.Threading;
using WpfListView = System.Windows.Controls.ListView;
using Xunit;

namespace MergePilot.Tests
{
    public class RepositoryListVisualStateTests
    {
        [Fact]
        public void GetSelection_ReturnsSelectedIndexAndRepository()
        {
            RunOnStaThread(() =>
            {
                var repository = new AppSettings.RepositoryEntry
                {
                    Name = "Repo",
                    Path = "C:\\repo"
                };
                var listView = new WpfListView();
                listView.Items.Add(repository);
                listView.SelectedIndex = 0;
                var visualState = new RepositoryListVisualState();

                var selection = visualState.GetSelection(listView);

                Assert.True(selection.HasIndexSelection);
                Assert.True(selection.HasRepository);
                Assert.Equal(0, selection.Index);
                Assert.Same(repository, selection.Repository);
            });
        }

        [Fact]
        public void SelectIndexIfAvailable_OnlySelectsValidIndex()
        {
            RunOnStaThread(() =>
            {
                var listView = new WpfListView();
                listView.Items.Add("Repo A");
                listView.Items.Add("Repo B");
                var visualState = new RepositoryListVisualState();

                visualState.SelectIndexIfAvailable(listView, 1);
                Assert.Equal(1, listView.SelectedIndex);

                visualState.SelectIndexIfAvailable(listView, 5);
                Assert.Equal(1, listView.SelectedIndex);
            });
        }

        [Fact]
        public void ConfigureSingleSelection_ReplacesExistingHandlerBeforeAdding()
        {
            RunOnStaThread(() =>
            {
                var calls = 0;
                var listView = new WpfListView();
                listView.Items.Add("Repo A");
                var visualState = new RepositoryListVisualState();

                System.Windows.Controls.SelectionChangedEventHandler handler = (_, _) => calls++;
                visualState.ConfigureSingleSelection(listView, handler);
                visualState.ConfigureSingleSelection(listView, handler);

                listView.SelectedIndex = 0;

                Assert.Equal(System.Windows.Controls.SelectionMode.Single, listView.SelectionMode);
                Assert.Equal(1, calls);
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
