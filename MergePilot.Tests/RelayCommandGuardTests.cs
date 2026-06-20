using Xunit;
using MergePilot.ViewModels;

namespace MergePilot.Tests
{
    public class RelayCommandGuardTests
    {
        [Fact]
        public void RelayCommand_Execute_DoesNotRunWhenCanExecuteIsFalse()
        {
            var calls = 0;
            var command = new RelayCommand(_ => calls++, _ => false);

            command.Execute(null);

            Assert.Equal(0, calls);
        }

        [Fact]
        public void RelayCommand_Execute_RunsWhenCanExecuteIsTrue()
        {
            var calls = 0;
            var command = new RelayCommand(_ => calls++, _ => true);

            command.Execute(null);

            Assert.Equal(1, calls);
        }

        [Fact]
        public void RelayCommand_Execute_RunsWhenCanExecuteIsNull()
        {
            var calls = 0;
            var command = new RelayCommand(_ => calls++);

            command.Execute(null);

            Assert.Equal(1, calls);
        }
    }
}
