using System;
using Xunit;

namespace MergePilot.Tests
{
    public class InputValidatorTests
    {
        #region Branch Name Validation Tests

        [Theory]
        [InlineData("main")]
        [InlineData("develop")]
        [InlineData("feature/user-auth")]
        [InlineData("bugfix/issue-123")]
        [InlineData("release/v1.0.0")]
        [InlineData("chore_update-deps")]
        [InlineData("v1.2.3")]
        public void IsValidBranchName_WithValidNames_ReturnsTrue(string branchName)
        {
            // Arrange & Act
            bool result = InputValidator.IsValidBranchName(branchName);

            // Assert
            Assert.True(result, $"'{branchName}' should be a valid branch name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        [InlineData("branch@name")]
        [InlineData("branch~name")]
        [InlineData("branch^name")]
        [InlineData("branch:name")]
        [InlineData("branch?name")]
        [InlineData("branch[name")]
        [InlineData("branch]name")]
        [InlineData("branch..name")]
        [InlineData(".branch")]
        [InlineData("/branch")]
        [InlineData("branch/")]
        public void IsValidBranchName_WithInvalidNames_ReturnsFalse(string branchName)
        {
            // Arrange & Act
            bool result = InputValidator.IsValidBranchName(branchName);

            // Assert
            Assert.False(result, $"'{branchName}' should be invalid");
        }

        [Fact]
        public void IsValidBranchName_WithNull_ReturnsFalse()
        {
            // Act
            bool result = InputValidator.IsValidBranchName(null!);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void GetBranchNameError_WithInvalidName_ReturnsErrorMessage()
        {
            // Arrange
            string invalidBranch = "branch@name";

            // Act
            string error = InputValidator.GetBranchNameError(invalidBranch);

            // Assert
            Assert.NotEmpty(error);
            Assert.Contains("invalid", error, StringComparison.OrdinalIgnoreCase);
        }

        #endregion

        #region Repository Name Validation Tests

        [Theory]
        [InlineData("MyRepository")]
        [InlineData("my-repo")]
        [InlineData("my_repo")]
        [InlineData("repo-123")]
        [InlineData("A")] // Single char is valid
        public void IsValidRepositoryName_WithValidNames_ReturnsTrue(string repoName)
        {
            // Arrange & Act
            bool result = InputValidator.IsValidRepositoryName(repoName);

            // Assert
            Assert.True(result, $"'{repoName}' should be a valid repository name");
        }

        [Theory]
        [InlineData("")]
        [InlineData("   ")]
        public void IsValidRepositoryName_WithEmptyNames_ReturnsFalse(string repoName)
        {
            // Arrange & Act
            bool result = InputValidator.IsValidRepositoryName(repoName);

            // Assert
            Assert.False(result, $"'{repoName}' should be invalid (empty)");
        }

        [Fact]
        public void IsValidRepositoryName_WithNull_ReturnsFalse()
        {
            // Act
            bool result = InputValidator.IsValidRepositoryName(null!);

            // Assert
            Assert.False(result);
        }

        #endregion

        #region Repository Path Validation Tests

        [Fact]
        public void IsValidRepositoryPath_WithNonExistentPath_ReturnsFalse()
        {
            // Arrange
            string nonExistentPath = "/nonexistent/path/that/does/not/exist";

            // Act
            bool result = InputValidator.IsValidRepositoryPath(nonExistentPath);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void IsValidRepositoryPath_WithNullOrEmptyPath_ReturnsFalse()
        {
            // Arrange & Act & Assert
            Assert.False(InputValidator.IsValidRepositoryPath(null));
            Assert.False(InputValidator.IsValidRepositoryPath(""));
            Assert.False(InputValidator.IsValidRepositoryPath("   "));
        }

        [Fact]
        public void GetRepositoryPathError_WithInvalidPath_ReturnsErrorMessage()
        {
            // Arrange
            string invalidPath = "/nonexistent/path";

            // Act
            string error = InputValidator.GetRepositoryPathError(invalidPath);

            // Assert
            Assert.NotEmpty(error);
            Assert.True(
                error.Contains("does not exist", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("not found", StringComparison.OrdinalIgnoreCase) ||
                error.Contains("invalid", StringComparison.OrdinalIgnoreCase)
            );
        }

        #endregion

        #region Merge Operation Validation Tests

        [Fact]
        public void IsValidMergeOperation_WithSameSourceAndTarget_ReturnsFalse()
        {
            // Arrange
            string branch = "main";

            // Act
            bool result = InputValidator.IsValidMergeOperation(branch, branch);

            // Assert
            Assert.False(result, "Cannot merge a branch into itself");
        }

        [Fact]
        public void IsValidMergeOperation_WithValidSourceAndTarget_ReturnsTrue()
        {
            // Arrange
            string source = "feature/new-feature";
            string target = "develop";

            // Act
            bool result = InputValidator.IsValidMergeOperation(source, target);

            // Assert
            Assert.True(result, "Valid source and target branches should pass validation");
        }

        [Theory]
        [InlineData("")]
        [InlineData("source")]
        public void IsValidMergeOperation_WithMissingTarget_ReturnsFalse(string target)
        {
            // Act
            bool result = InputValidator.IsValidMergeOperation("source", target);

            // Assert
            Assert.False(result, "Missing or empty target branch should be invalid");
        }

        [Theory]
        [InlineData("")]
        [InlineData("target")]
        public void IsValidMergeOperation_WithMissingSource_ReturnsFalse(string source)
        {
            // Act
            bool result = InputValidator.IsValidMergeOperation(source, "target");

            // Assert
            Assert.False(result, "Missing or empty source branch should be invalid");
        }

        [Fact]
        public void GetMergeOperationError_WithInvalidOperation_ReturnsErrorMessage()
        {
            // Arrange
            string branch = "main";

            // Act
            var (isValid, errorMessage) = InputValidator.GetMergeOperationError(branch, branch);

            // Assert
            Assert.False(isValid);
            Assert.NotEmpty(errorMessage);
            Assert.Contains("itself", errorMessage, StringComparison.OrdinalIgnoreCase);
        }

        #endregion
    }
}
