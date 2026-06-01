using System;
using System.Windows.Input;
using System.Threading.Tasks;

namespace MergePilot.ViewModels
{
    /// <summary>
    /// A basic implementation of ICommand for synchronous operations.
    /// Allows binding WPF commands to ViewModel methods without code-behind.
    /// </summary>
    /// <remarks>
    /// Provides a way to execute methods from XAML bindings. Supports optional
    /// CanExecute predicate for command availability checking.
    /// 
    /// Example usage:
    /// <code>
    /// public ICommand RefreshCommand { get; }
    /// 
    /// public MyViewModel()
    /// {
    ///     RefreshCommand = new RelayCommand(
    ///         execute: _ => RefreshData(),
    ///         canExecute: _ => !_isLoading
    ///     );
    /// }
    /// </code>
    /// </remarks>
    public class RelayCommand : ICommand
    {
        private readonly Action<object?> _execute;
        private readonly Predicate<object?>? _canExecute;

        /// <summary>
        /// Raised when CanExecute status changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of RelayCommand.
        /// </summary>
        /// <param name="execute">The method to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional predicate to determine if command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if execute is null.</exception>
        public RelayCommand(Action<object?> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute), "Execute method cannot be null");
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Command parameter from the binding.</param>
        /// <returns>True if command can execute; otherwise false.</returns>
        public bool CanExecute(object? parameter)
        {
            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// Executes the command.
        /// </summary>
        /// <param name="parameter">Command parameter from the binding.</param>
        public void Execute(object? parameter)
        {
            _execute(parameter);
        }
    }

    /// <summary>
    /// An implementation of ICommand for asynchronous operations.
    /// Automatically manages execution state and prevents concurrent executions.
    /// </summary>
    /// <remarks>
    /// Handles async/await operations with automatic command state management.
    /// While an async command is executing, CanExecute returns false to prevent
    /// concurrent operations. Useful for long-running operations like Git commands.
    /// 
    /// Example usage:
    /// <code>
    /// public ICommand MergeCommand { get; }
    /// 
    /// public MyViewModel()
    /// {
    ///     MergeCommand = new AsyncRelayCommand(
    ///         execute: param => PerformMergeAsync(),
    ///         canExecute: _ => !_isOperationInProgress
    ///     );
    /// }
    /// 
    /// private async Task PerformMergeAsync()
    /// {
    ///     _isOperationInProgress = true;
    ///     try
    ///     {
    ///         await GitHelper.MergeBranchAsync(...);
    ///     }
    ///     finally
    ///     {
    ///         _isOperationInProgress = false;
    ///     }
    /// }
    /// </code>
    /// </remarks>
    public class AsyncRelayCommand : ICommand
    {
        private readonly Func<object?, Task> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Raised when CanExecute status changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of AsyncRelayCommand.
        /// </summary>
        /// <param name="execute">The async method to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional predicate to determine if command can execute.</param>
        /// <exception cref="ArgumentNullException">Thrown if execute is null.</exception>
        public AsyncRelayCommand(Func<object?, Task> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute), "Execute method cannot be null");
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        /// <param name="parameter">Command parameter from the binding.</param>
        /// <returns>
        /// True if command can execute and is not currently executing; otherwise false.
        /// This prevents concurrent executions of the async operation.
        /// </returns>
        public bool CanExecute(object? parameter)
        {
            // Prevent concurrent execution
            if (_isExecuting)
                return false;

            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// Executes the async command.
        /// </summary>
        /// <param name="parameter">Command parameter from the binding.</param>
        /// <remarks>
        /// This method automatically manages the _isExecuting flag to prevent
        /// concurrent executions. The command will not be executable while it's running.
        /// </remarks>
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute(parameter).ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }

    /// <summary>
    /// An implementation of ICommand for generic asynchronous operations with return values.
    /// </summary>
    /// <typeparam name="T">The return type of the async operation.</typeparam>
    /// <remarks>
    /// Similar to AsyncRelayCommand but returns a Task&lt;T&gt; instead of just Task.
    /// This is useful when you need to capture the result of an async operation.
    /// </remarks>
    public class AsyncRelayCommand<T> : ICommand
    {
        private readonly Func<object?, Task<T>> _execute;
        private readonly Predicate<object?>? _canExecute;
        private bool _isExecuting;

        /// <summary>
        /// Raised when CanExecute status changes.
        /// </summary>
        public event EventHandler? CanExecuteChanged
        {
            add { CommandManager.RequerySuggested += value; }
            remove { CommandManager.RequerySuggested -= value; }
        }

        /// <summary>
        /// Initializes a new instance of AsyncRelayCommand&lt;T&gt;.
        /// </summary>
        /// <param name="execute">The async method to execute when the command is invoked.</param>
        /// <param name="canExecute">Optional predicate to determine if command can execute.</param>
        public AsyncRelayCommand(Func<object?, Task<T>> execute, Predicate<object?>? canExecute = null)
        {
            _execute = execute ?? throw new ArgumentNullException(nameof(execute));
            _canExecute = canExecute;
        }

        /// <summary>
        /// Determines whether the command can execute in its current state.
        /// </summary>
        public bool CanExecute(object? parameter)
        {
            if (_isExecuting)
                return false;

            return _canExecute?.Invoke(parameter) ?? true;
        }

        /// <summary>
        /// Executes the async command.
        /// </summary>
        public async void Execute(object? parameter)
        {
            if (!CanExecute(parameter))
                return;

            _isExecuting = true;
            CommandManager.InvalidateRequerySuggested();

            try
            {
                await _execute(parameter).ConfigureAwait(false);
            }
            finally
            {
                _isExecuting = false;
                CommandManager.InvalidateRequerySuggested();
            }
        }
    }
}
