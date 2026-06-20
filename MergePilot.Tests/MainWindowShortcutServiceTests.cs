using System;
using System.Linq;
using System.Threading;
using System.Windows.Input;
using MergePilot.ViewModels;
using Xunit;

namespace MergePilot.Tests
{
    public class MainWindowShortcutServiceTests
    {
        [Fact]
        public void RegisterActionShortcuts_WithViewModel_UsesViewModelCommands()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowShortcutService();
                var viewModel = new MainWindowViewModel();
                var bindings = new InputBindingCollection();

                service.RegisterActionShortcuts(bindings, viewModel, new MainWindowShortcutActions());

                Assert.Same(viewModel.MergeWorkflowCommand, FindBinding(bindings, Key.M, ModifierKeys.Control).Command);
                Assert.Same(viewModel.PullWorkflowCommand, FindBinding(bindings, Key.P, ModifierKeys.Control).Command);
                Assert.Same(viewModel.RefreshWorkflowCommand, FindBinding(bindings, Key.R, ModifierKeys.Control).Command);
                Assert.Same(viewModel.ManageRepositoriesWorkflowCommand, FindBinding(bindings, Key.B, ModifierKeys.Control).Command);
                Assert.Same(viewModel.ToggleLogsWorkflowCommand, FindBinding(bindings, Key.L, ModifierKeys.Control).Command);
            });
        }

        [Fact]
        public void RegisterActionShortcuts_WithoutViewModel_UsesFallbackActions()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowShortcutService();
                var bindings = new InputBindingCollection();
                var mergeCalls = 0;
                var pullCalls = 0;
                var refreshCalls = 0;
                var manageCalls = 0;
                var toggleCalls = 0;

                service.RegisterActionShortcuts(
                    bindings,
                    viewModel: null,
                    new MainWindowShortcutActions
                    {
                        Merge = () => mergeCalls++,
                        Pull = () => pullCalls++,
                        Refresh = () => refreshCalls++,
                        ManageRepositories = () => manageCalls++,
                        ToggleLogs = () => toggleCalls++
                    });

                FindBinding(bindings, Key.M, ModifierKeys.Control).Command.Execute(null);
                FindBinding(bindings, Key.P, ModifierKeys.Control).Command.Execute(null);
                FindBinding(bindings, Key.R, ModifierKeys.Control).Command.Execute(null);
                FindBinding(bindings, Key.B, ModifierKeys.Control).Command.Execute(null);
                FindBinding(bindings, Key.L, ModifierKeys.Control).Command.Execute(null);

                Assert.Equal(1, mergeCalls);
                Assert.Equal(1, pullCalls);
                Assert.Equal(1, refreshCalls);
                Assert.Equal(1, manageCalls);
                Assert.Equal(1, toggleCalls);
            });
        }

        [Fact]
        public void RegisterLogShortcuts_WithViewModel_UsesViewModelCommands()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowShortcutService();
                var viewModel = new MainWindowViewModel();
                var bindings = new InputBindingCollection();

                service.RegisterLogShortcuts(bindings, viewModel, new MainWindowLogShortcutActions());

                Assert.Same(viewModel.SaveLogsWorkflowCommand, FindBinding(bindings, Key.S, ModifierKeys.Control).Command);
                Assert.Same(viewModel.CopyLogsWorkflowCommand, FindBinding(bindings, Key.D, ModifierKeys.Control | ModifierKeys.Shift).Command);
                Assert.Same(viewModel.ClearLogsWorkflowCommand, FindBinding(bindings, Key.K, ModifierKeys.Control).Command);
            });
        }

        [Fact]
        public void RegisterLogShortcuts_WithoutViewModel_UsesFallbackActions()
        {
            RunOnStaThread(() =>
            {
                var service = new MainWindowShortcutService();
                var bindings = new InputBindingCollection();
                var saveCalls = 0;
                var copyCalls = 0;
                var clearCalls = 0;

                service.RegisterLogShortcuts(
                    bindings,
                    viewModel: null,
                    new MainWindowLogShortcutActions
                    {
                        SaveLogs = () => saveCalls++,
                        CopyLogs = () => copyCalls++,
                        ClearLogs = () => clearCalls++
                    });

                FindBinding(bindings, Key.S, ModifierKeys.Control).Command.Execute(null);
                FindBinding(bindings, Key.D, ModifierKeys.Control | ModifierKeys.Shift).Command.Execute(null);
                FindBinding(bindings, Key.K, ModifierKeys.Control).Command.Execute(null);

                Assert.Equal(1, saveCalls);
                Assert.Equal(1, copyCalls);
                Assert.Equal(1, clearCalls);
            });
        }

        [Fact]
        public void HandlePreviewKeyDown_WithShiftE_TogglesLogsAndReturnsHandled()
        {
            var service = new MainWindowShortcutService();
            var calls = 0;

            var handled = service.HandlePreviewKeyDown(
                Key.E,
                Key.None,
                ModifierKeys.Shift,
                () => calls++);

            Assert.True(handled);
            Assert.Equal(1, calls);
        }

        [Fact]
        public void HandlePreviewKeyDown_WithNonShortcut_DoesNotToggleLogs()
        {
            var service = new MainWindowShortcutService();
            var calls = 0;

            var handled = service.HandlePreviewKeyDown(
                Key.E,
                Key.None,
                ModifierKeys.Control,
                () => calls++);

            Assert.False(handled);
            Assert.Equal(0, calls);
        }

        private static KeyBinding FindBinding(
            InputBindingCollection bindings,
            Key key,
            ModifierKeys modifiers)
        {
            return bindings
                .OfType<KeyBinding>()
                .Single(binding => binding.Key == key && binding.Modifiers == modifiers);
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
