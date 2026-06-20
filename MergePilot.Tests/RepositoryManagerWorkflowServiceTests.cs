using System;
using System.Threading;
using System.Windows;
using Xunit;

namespace MergePilot.Tests
{
    public class RepositoryManagerWorkflowServiceTests
    {
        [Fact]
        public void ShowAndReload_ShowsDialogWithCurrentSettingsAndReturnsLoadedSettings()
        {
            var currentSettings = new AppSettings();
            var loadedSettings = new AppSettings();
            var dialog = new FakeRepositoryManagerDialogService();
            var service = new RepositoryManagerWorkflowService(
                dialog,
                () => loadedSettings);

            var result = service.ShowAndReload(currentSettings);

            Assert.Same(loadedSettings, result);
            Assert.Same(currentSettings, dialog.Settings);
            Assert.Equal(1, dialog.ShowCalls);
        }

        [Fact]
        public void ShowAndReload_PassesOwnerToDialog()
        {
            RunOnStaThread(() =>
            {
                var currentSettings = new AppSettings();
                Window? capturedOwner = null;
                var owner = new Window();
                var service = new RepositoryManagerWorkflowService(
                    new DelegateRepositoryManagerDialogService((settings, dialogOwner) => capturedOwner = dialogOwner),
                    () => new AppSettings());

                try
                {
                    service.ShowAndReload(currentSettings, owner);

                    Assert.Same(owner, capturedOwner);
                }
                finally
                {
                    owner.Close();
                }
            });
        }

        private static void RunOnStaThread(Action action)
        {
            Exception? exception = null;
            var thread = new Thread(() =>
            {
                try { action(); }
                catch (Exception ex) { exception = ex; }
            });
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            thread.Join();
            if (exception != null)
                throw exception;
        }

        [Fact]
        public void ShowAndReload_RequiresSettings()
        {
            var service = new RepositoryManagerWorkflowService(
                new FakeRepositoryManagerDialogService(),
                () => new AppSettings());

            Assert.Throws<ArgumentNullException>(() => service.ShowAndReload(null!));
        }

        private sealed class FakeRepositoryManagerDialogService : IRepositoryManagerDialogService
        {
            public int ShowCalls { get; private set; }
            public AppSettings? Settings { get; private set; }

            public void ShowRepositoryManager(AppSettings settings, Window? owner)
            {
                ShowCalls++;
                Settings = settings;
            }
        }

        private sealed class DelegateRepositoryManagerDialogService : IRepositoryManagerDialogService
        {
            private readonly Action<AppSettings, Window?> _show;

            public DelegateRepositoryManagerDialogService(Action<AppSettings, Window?> show)
            {
                _show = show;
            }

            public void ShowRepositoryManager(AppSettings settings, Window? owner)
            {
                _show(settings, owner);
            }
        }
    }
}
