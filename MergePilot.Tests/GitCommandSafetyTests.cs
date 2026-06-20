using Xunit;

namespace MergePilot.Tests
{
    public class GitCommandSafetyTests
    {
        [Theory]
        [InlineData("main")]
        [InlineData("feature/user-auth")]
        [InlineData("release/v1.2.3")]
        [InlineData("chore_update-deps")]
        [InlineData("feature/JIRA-123+api")]
        [InlineData("feature;legacy")]
        public void TryNormalizeBranchName_WithSafeBranch_ReturnsTrue(string branchName)
        {
            var result = GitCommandSafety.TryNormalizeBranchName(branchName, out var normalized, out var error);

            Assert.True(result);
            Assert.Equal(branchName, normalized);
            Assert.Empty(error);
        }

        [Theory]
        [InlineData("feature branch")]
        [InlineData("--upload-pack=bad")]
        [InlineData("feature\\bad")]
        [InlineData("feature\"bad")]
        [InlineData("feature//bad")]
        [InlineData("feature.lock")]
        [InlineData("@")]
        [InlineData("feature@{bad}")]
        public void TryNormalizeBranchName_WithUnsafeBranch_ReturnsFalse(string branchName)
        {
            var result = GitCommandSafety.TryNormalizeBranchName(branchName, out var normalized, out var error);

            Assert.False(result);
            Assert.Empty(normalized);
            Assert.NotEmpty(error);
        }

        [Theory]
        [InlineData("origin")]
        [InlineData("upstream")]
        [InlineData("team/remote-1")]
        [InlineData("origin+mirror")]
        public void TryNormalizeRemoteName_WithSafeRemote_ReturnsTrue(string remoteName)
        {
            var result = GitCommandSafety.TryNormalizeRemoteName(remoteName, out var normalized, out var error);

            Assert.True(result);
            Assert.Equal(remoteName, normalized);
            Assert.Empty(error);
        }

        [Theory]
        [InlineData("origin main")]
        [InlineData("--bad")]
        [InlineData("origin\\bad")]
        [InlineData("origin\"bad")]
        public void TryNormalizeRemoteName_WithUnsafeRemote_ReturnsFalse(string remoteName)
        {
            var result = GitCommandSafety.TryNormalizeRemoteName(remoteName, out var normalized, out var error);

            Assert.False(result);
            Assert.Empty(normalized);
            Assert.NotEmpty(error);
        }
    }
}
