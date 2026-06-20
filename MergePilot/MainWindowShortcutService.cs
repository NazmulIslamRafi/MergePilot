using System;
using System.Windows.Input;
using MergePilot.ViewModels;

namespace MergePilot
{
    /// <summary>
    /// Registers MainWindow keyboard shortcuts and handles shortcut-specific preview keys.
    /// </summary>
    public class MainWindowShortcutService
    {
        public void RegisterActionShortcuts(
            InputBindingCollection inputBindings,
            MainWindowViewModel? viewModel,
            MainWindowShortcutActions fallbackActions)
        {
            ArgumentNullException.ThrowIfNull(inputBindings);
            ArgumentNullException.ThrowIfNull(fallbackActions);

            if (viewModel != null)
            {
                inputBindings.Add(new KeyBinding(viewModel.MergeWorkflowCommand, Key.M, ModifierKeys.Control));
                inputBindings.Add(new KeyBinding(viewModel.PullWorkflowCommand, Key.P, ModifierKeys.Control));
                inputBindings.Add(new KeyBinding(viewModel.RefreshWorkflowCommand, Key.R, ModifierKeys.Control));
                inputBindings.Add(new KeyBinding(viewModel.ManageRepositoriesWorkflowCommand, Key.B, ModifierKeys.Control));
                inputBindings.Add(new KeyBinding(viewModel.ToggleLogsWorkflowCommand, Key.L, ModifierKeys.Control));
                return;
            }

            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.Merge()), Key.M, ModifierKeys.Control));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.Pull()), Key.P, ModifierKeys.Control));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.Refresh()), Key.R, ModifierKeys.Control));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.ManageRepositories()), Key.B, ModifierKeys.Control));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.ToggleLogs()), Key.L, ModifierKeys.Control));
        }

        public void RegisterLogShortcuts(
            InputBindingCollection inputBindings,
            MainWindowViewModel? viewModel,
            MainWindowLogShortcutActions fallbackActions)
        {
            ArgumentNullException.ThrowIfNull(inputBindings);
            ArgumentNullException.ThrowIfNull(fallbackActions);

            if (viewModel != null)
            {
                inputBindings.Add(new KeyBinding(viewModel.SaveLogsWorkflowCommand, Key.S, ModifierKeys.Control));
                inputBindings.Add(new KeyBinding(viewModel.CopyLogsWorkflowCommand, Key.D, ModifierKeys.Control | ModifierKeys.Shift));
                inputBindings.Add(new KeyBinding(viewModel.ClearLogsWorkflowCommand, Key.K, ModifierKeys.Control));
                return;
            }

            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.SaveLogs()), Key.S, ModifierKeys.Control));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.CopyLogs()), Key.D, ModifierKeys.Control | ModifierKeys.Shift));
            inputBindings.Add(new KeyBinding(new RelayCommand(_ => fallbackActions.ClearLogs()), Key.K, ModifierKeys.Control));
        }

        public bool HandlePreviewKeyDown(
            Key key,
            Key systemKey,
            ModifierKeys modifiers,
            Action toggleLogs)
        {
            ArgumentNullException.ThrowIfNull(toggleLogs);

            var effectiveKey = key == Key.System ? systemKey : key;
            if (effectiveKey == Key.E && (modifiers & ModifierKeys.Shift) == ModifierKeys.Shift)
            {
                toggleLogs();
                return true;
            }

            return false;
        }
    }

    public sealed class MainWindowShortcutActions
    {
        public Action Merge { get; init; } = NoOp;
        public Action Pull { get; init; } = NoOp;
        public Action Refresh { get; init; } = NoOp;
        public Action ManageRepositories { get; init; } = NoOp;
        public Action ToggleLogs { get; init; } = NoOp;

        private static void NoOp()
        {
        }
    }

    public sealed class MainWindowLogShortcutActions
    {
        public Action SaveLogs { get; init; } = NoOp;
        public Action CopyLogs { get; init; } = NoOp;
        public Action ClearLogs { get; init; } = NoOp;

        private static void NoOp()
        {
        }
    }
}
