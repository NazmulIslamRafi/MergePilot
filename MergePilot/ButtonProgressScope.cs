using System;
using MaterialDesignThemes.Wpf;
using WpfButton = System.Windows.Controls.Button;

namespace MergePilot
{
    /// <summary>
    /// Temporarily disables a button and shows indeterminate progress.
    /// </summary>
    public sealed class ButtonProgressScope : IDisposable
    {
        private readonly WpfButton _button;
        private readonly bool _restoreEnabled;
        private bool _disposed;

        private ButtonProgressScope(WpfButton button, bool? restoreEnabled)
        {
            _button = button ?? throw new ArgumentNullException(nameof(button));

            _restoreEnabled = InvokeOnButton(() =>
            {
                var enabledStateToRestore = restoreEnabled ?? _button.IsEnabled;
                _button.IsEnabled = false;
                ButtonProgressAssist.SetIsIndeterminate(_button, true);

                return enabledStateToRestore;
            });
        }

        public static ButtonProgressScope Start(WpfButton button)
        {
            return new ButtonProgressScope(button, restoreEnabled: null);
        }

        public static ButtonProgressScope Start(WpfButton button, bool restoreEnabled)
        {
            return new ButtonProgressScope(button, restoreEnabled);
        }

        public void Dispose()
        {
            if (_disposed)
                return;

            _disposed = true;
            InvokeOnButton(() =>
            {
                ButtonProgressAssist.SetIsIndeterminate(_button, false);
                _button.IsEnabled = _restoreEnabled;
            });
        }

        private void InvokeOnButton(Action action)
        {
            InvokeOnButton<object?>(() =>
            {
                action();
                return null;
            });
        }

        private T InvokeOnButton<T>(Func<T> action)
        {
            if (_button.Dispatcher.CheckAccess())
                return action();

            return _button.Dispatcher.Invoke(action);
        }
    }
}
