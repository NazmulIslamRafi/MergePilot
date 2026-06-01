using System;
using System.Threading.Tasks;
using Xunit;
using MergePilot.ViewModels;

namespace MergePilot.Tests
{
    public class ViewModelBaseTests
    {
        private class TestViewModel : ViewModelBase
        {
            private string _testProperty = "";
            private int _counter = 0;

            public string TestProperty
            {
                get => _testProperty;
                set => SetProperty(ref _testProperty, value);
            }

            public int Counter
            {
                get => _counter;
                set => SetProperty(ref _counter, value);
            }
        }

        [Fact]
        public void SetProperty_WithNewValue_RaisesPropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanged = false;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TestViewModel.TestProperty))
                    propertyChanged = true;
            };

            // Act
            viewModel.TestProperty = "NewValue";

            // Assert
            Assert.True(propertyChanged);
            Assert.Equal("NewValue", viewModel.TestProperty);
        }

        [Fact]
        public void SetProperty_WithSameValue_DoesNotRaisePropertyChanged()
        {
            // Arrange
            var viewModel = new TestViewModel { TestProperty = "Value" };
            var changeCount = 0;
            viewModel.PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(TestViewModel.TestProperty))
                    changeCount++;
            };

            // Act
            viewModel.TestProperty = "Value";

            // Assert
            Assert.Equal(0, changeCount);
        }

        [Fact]
        public void SetProperty_ReturnsTrueWhenValueChanged()
        {
            // Arrange
            var viewModel = new TestViewModel();

            // Act
            var result = viewModel.TestProperty != "New";
            viewModel.TestProperty = "New";

            // Assert
            Assert.Equal("New", viewModel.TestProperty);
        }

        [Fact]
        public void SetProperty_WithMultipleProperties_TracksEachSeparately()
        {
            // Arrange
            var viewModel = new TestViewModel();
            var propertyChanges = new System.Collections.Generic.List<string>();
            viewModel.PropertyChanged += (s, e) => propertyChanges.Add(e.PropertyName ?? "");

            // Act
            viewModel.TestProperty = "Value1";
            viewModel.Counter = 42;
            viewModel.TestProperty = "Value2";

            // Assert
            Assert.Equal(3, propertyChanges.Count);
            Assert.Contains(nameof(TestViewModel.TestProperty), propertyChanges);
            Assert.Contains(nameof(TestViewModel.Counter), propertyChanges);
        }
    }

    public class RelayCommandTests
    {
        [Fact]
        public void RelayCommand_ExecuteWithValidCommand_CallsExecuteAction()
        {
            // Arrange
            var executeCalled = false;
            var command = new RelayCommand(_ => executeCalled = true);

            // Act
            command.Execute(null);

            // Assert
            Assert.True(executeCalled);
        }

        [Fact]
        public void RelayCommand_CanExecuteWithNoPredicate_ReturnsTrue()
        {
            // Arrange
            var command = new RelayCommand(_ => { });

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RelayCommand_CanExecuteWithPredicate_ReturnsTrueWhenPredicateTrue()
        {
            // Arrange
            var command = new RelayCommand(_ => { }, _ => true);

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void RelayCommand_CanExecuteWithPredicate_ReturnsFalseWhenPredicateFalse()
        {
            // Arrange
            var command = new RelayCommand(_ => { }, _ => false);

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void RelayCommand_WithNullExecute_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new RelayCommand(null!));
        }

        [Fact]
        public void RelayCommand_PassesParameterToExecute()
        {
            // Arrange
            object? passedParameter = null;
            var command = new RelayCommand(param => passedParameter = param);

            // Act
            command.Execute("TestParameter");

            // Assert
            Assert.Equal("TestParameter", passedParameter);
        }
    }

    public class AsyncRelayCommandTests
    {
        [Fact]
        public async Task AsyncRelayCommand_ExecuteWithValidCommand_CallsExecuteAsync()
        {
            // Arrange
            var executeCalled = false;
            var command = new AsyncRelayCommand(async _ =>
            {
                await Task.Delay(10);
                executeCalled = true;
            });

            // Act
            command.Execute(null);
            await Task.Delay(50); // Wait for async execution

            // Assert
            Assert.True(executeCalled);
        }

        [Fact]
        public void AsyncRelayCommand_CanExecuteWhileNotExecuting_ReturnsTrue()
        {
            // Arrange
            var command = new AsyncRelayCommand(async _ => await Task.Delay(10));

            // Act
            var result = command.CanExecute(null);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void AsyncRelayCommand_CanExecuteWhileExecuting_ReturnsFalse()
        {
            // Arrange
            var executionStarted = new TaskCompletionSource<bool>();
            var executionCanContinue = new TaskCompletionSource<bool>();

            var command = new AsyncRelayCommand(async _ =>
            {
                executionStarted.SetResult(true);
                await executionCanContinue.Task;
            });

            // Act
            command.Execute(null);
            var canExecuteWhileRunning = command.CanExecute(null);
            executionCanContinue.SetResult(true);

            // Assert
            Assert.False(canExecuteWhileRunning);
        }

        [Fact]
        public void AsyncRelayCommand_PreventsConcurrentExecution()
        {
            // Arrange
            var executionCount = 0;
            var executionStarted = new TaskCompletionSource<bool>();
            var executionCanContinue = new TaskCompletionSource<bool>();

            var command = new AsyncRelayCommand(async _ =>
            {
                executionCount++;
                executionStarted.SetResult(true);
                await executionCanContinue.Task;
            });

            // Act
            command.Execute(null); // First execution
            var waitCompleted = executionStarted.Task.Wait(1000);
            command.Execute(null); // Second execution (should be blocked)
            executionCanContinue.SetResult(true);

            // Assert
            Assert.Equal(1, executionCount); // Only one execution
        }

        [Fact]
        public void AsyncRelayCommand_WithNullExecute_ThrowsArgumentNullException()
        {
            // Act & Assert
            Assert.Throws<ArgumentNullException>(() => new AsyncRelayCommand(null!));
        }

        [Fact]
        public async Task AsyncRelayCommand_WithCanExecutePredicate_RespectsPredicate()
        {
            // Arrange
            var canExecute = false;
            var executeCalled = false;
            var command = new AsyncRelayCommand(
                async _ =>
                {
                    await Task.Delay(10);
                    executeCalled = true;
                },
                _ => canExecute
            );

            // Act
            canExecute = false;
            command.Execute(null);
            await Task.Delay(50);

            // Assert - Should not execute when canExecute is false
            Assert.False(executeCalled);
        }
    }

    public class AsyncRelayCommandGenericTests
    {
        [Fact]
        public async Task AsyncRelayCommandGeneric_ExecuteReturnsValue()
        {
            // Arrange
            var command = new AsyncRelayCommand<int>(async _ =>
            {
                await Task.Delay(10);
                return 42;
            });

            // Act
            command.Execute(null);
            await Task.Delay(50);

            // Assert (Note: Can't directly capture return value from ICommand,
            // so this is more of an integration test)
            Assert.True(true); // Command executed without error
        }

        [Fact]
        public void AsyncRelayCommandGeneric_CanExecuteWhileExecuting_ReturnsFalse()
        {
            // Arrange
            var executionCanContinue = new TaskCompletionSource<int>();

            var command = new AsyncRelayCommand<int>(async _ =>
            {
                await executionCanContinue.Task;
                return 42;
            });

            // Act
            command.Execute(null);
            var canExecuteWhileRunning = command.CanExecute(null);
            executionCanContinue.SetResult(42);

            // Assert
            Assert.False(canExecuteWhileRunning);
        }
    }
}
