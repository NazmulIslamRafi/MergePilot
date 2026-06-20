using Xunit;

namespace MergePilot.Tests
{
    public class GitCommandErrorClassifierTests
    {
        [Theory]
        [InlineData(0, "", "", GitCommandErrorKind.None)]
        [InlineData(-1, "", "Command canceled or timed out.", GitCommandErrorKind.Timeout)]
        [InlineData(1, "", "CONFLICT (content): Merge conflict in file.txt", GitCommandErrorKind.Conflict)]
        [InlineData(128, "", "Permission denied (publickey). Could not read from remote repository.", GitCommandErrorKind.Authentication)]
        [InlineData(128, "", "fatal: 'origin' does not appear to be a git repository", GitCommandErrorKind.Remote)]
        [InlineData(128, "", "fatal: not a git repository (or any of the parent directories): .git", GitCommandErrorKind.Repository)]
        [InlineData(128, "", "fatal: unable to access url: Could not resolve host: example.com", GitCommandErrorKind.Network)]
        [InlineData(1, "", "some unexpected git failure", GitCommandErrorKind.Unknown)]
        public void Classify_ReturnsExpectedKind(
            int exitCode,
            string stdout,
            string stderr,
            GitCommandErrorKind expectedKind)
        {
            var kind = GitCommandErrorClassifier.Classify(exitCode, stdout, stderr);

            Assert.Equal(expectedKind, kind);
        }

        [Fact]
        public void CommandResult_WithDefaultErrorKind_RemainsSourceCompatible()
        {
            var result = new CommandResult(0, "ok", "");

            Assert.True(result.IsSuccess);
            Assert.Equal(GitCommandErrorKind.None, result.ErrorKind);
        }
    }
}
