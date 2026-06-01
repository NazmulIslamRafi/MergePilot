using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using Moq;
using MergePilot;

namespace MergePilot.Tests
{
    public class RepositoryServiceTests
    {
        [Fact]
        public void Constructor_CreatesInstanceWithDefaultCacheDuration()
        {
            // Act
            var service = new RepositoryService();

            // Assert
            Assert.NotNull(service);
            Assert.Equal(TimeSpan.FromMinutes(5), service.CacheDuration);
        }

        [Fact]
        public async Task GetBranchesAsync_WithNullRepoPath_ThrowsArgumentException()
        {
            // Arrange
            var service = new RepositoryService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetBranchesAsync(null!)
            );
        }

        [Fact]
        public async Task GetBranchesAsync_WithEmptyRepoPath_ThrowsArgumentException()
        {
            // Arrange
            var service = new RepositoryService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.GetBranchesAsync("")
            );
        }

        [Fact]
        public async Task MergeBranchAsync_WithNullRepoPath_ThrowsArgumentException()
        {
            // Arrange
            var service = new RepositoryService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.MergeBranchAsync(null!, "source", "target")
            );
        }

        [Fact]
        public async Task MergeBranchAsync_WithSameBranchNames_ReturnsFailed()
        {
            // Arrange
            var service = new RepositoryService();

            // Act
            var result = await service.MergeBranchAsync("/repo", "main", "main");

            // Assert
            Assert.NotNull(result);
            Assert.Equal(MergeStatus.Failed, result.Status);
            Assert.Contains("itself", result.Message, StringComparison.OrdinalIgnoreCase);
        }

        [Fact]
        public async Task PullBranchAsync_WithNullRepoPath_ThrowsArgumentException()
        {
            // Arrange
            var service = new RepositoryService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.PullBranchAsync(null!, "main")
            );
        }

        [Fact]
        public async Task PullBranchAsync_WithNullBranch_ThrowsArgumentException()
        {
            // Arrange
            var service = new RepositoryService();

            // Act & Assert
            await Assert.ThrowsAsync<ArgumentException>(
                () => service.PullBranchAsync("/repo", null!)
            );
        }

        [Fact]
        public void InvalidateCache_WithNullPath_ClearsAllCaches()
        {
            // Arrange
            var service = new RepositoryService();

            // Manually add to cache (simulated)
            var initialStatus = new Dictionary<string, CacheStatus>();

            // Act
            service.InvalidateCache(null);
            var statusAfter = service.GetCacheStatus();

            // Assert
            Assert.Empty(statusAfter);
        }

        [Fact]
        public void InvalidateCache_WithSpecificPath_RemovesThatPath()
        {
            // Arrange
            var service = new RepositoryService();
            var repoPath = "/test/repo";

            // Act
            service.InvalidateCache(repoPath);

            // Assert
            var status = service.GetCacheStatus();
            Assert.DoesNotContain(repoPath, status.Keys);
        }

        [Fact]
        public void ProgressChanged_EventRaises()
        {
            // Arrange
            var service = new RepositoryService();
            var eventRaised = false;
            var receivedPercentage = 0;
            var receivedMessage = "";

            service.ProgressChanged += (s, e) =>
            {
                eventRaised = true;
                receivedPercentage = e.ProgressPercentage;
                receivedMessage = e.StatusMessage;
            };

            // Act - Invalidate cache to trigger event
            service.InvalidateCache(null);

            // Assert
            Assert.True(eventRaised);
            Assert.Equal(0, receivedPercentage);
            Assert.NotEmpty(receivedMessage);
        }

        [Fact]
        public void CacheDuration_CanBeModified()
        {
            // Arrange
            var service = new RepositoryService();
            var newDuration = TimeSpan.FromMinutes(10);

            // Act
            service.CacheDuration = newDuration;

            // Assert
            Assert.Equal(newDuration, service.CacheDuration);
        }

        [Fact]
        public void GetCacheStatus_WithEmptyCache_ReturnsEmptyDictionary()
        {
            // Arrange
            var service = new RepositoryService();

            // Act
            var status = service.GetCacheStatus();

            // Assert
            Assert.NotNull(status);
            Assert.Empty(status);
        }
    }

    public class OperationProgressEventArgsTests
    {
        [Fact]
        public void Constructor_WithValidPercentage_StoresValue()
        {
            // Arrange & Act
            var args = new OperationProgressEventArgs(50, "Loading...");

            // Assert
            Assert.Equal(50, args.ProgressPercentage);
            Assert.Equal("Loading...", args.StatusMessage);
        }

        [Fact]
        public void Constructor_WithPercentageAbove100_Clamps()
        {
            // Arrange & Act
            var args = new OperationProgressEventArgs(150, "Complete");

            // Assert
            Assert.Equal(100, args.ProgressPercentage);
        }

        [Fact]
        public void Constructor_WithNegativePercentage_Clamps()
        {
            // Arrange & Act
            var args = new OperationProgressEventArgs(-50, "Error");

            // Assert
            Assert.Equal(0, args.ProgressPercentage);
        }

        [Fact]
        public void Constructor_WithNullMessage_Stores()
        {
            // Arrange & Act
            var args = new OperationProgressEventArgs(50, null!);

            // Assert
            Assert.Equal(50, args.ProgressPercentage);
            Assert.Null(args.StatusMessage);
        }
    }

    public class CacheStatusTests
    {
        [Fact]
        public void CacheStatus_PropertiesCanBeSet()
        {
            // Arrange & Act
            var status = new CacheStatus
            {
                IsCached = true,
                CacheAge = TimeSpan.FromSeconds(30),
                IsValid = true,
                BranchCount = 5
            };

            // Assert
            Assert.True(status.IsCached);
            Assert.Equal(TimeSpan.FromSeconds(30), status.CacheAge);
            Assert.True(status.IsValid);
            Assert.Equal(5, status.BranchCount);
        }

        [Fact]
        public void CacheStatus_DefaultValues()
        {
            // Arrange & Act
            var status = new CacheStatus();

            // Assert
            Assert.False(status.IsCached);
            Assert.Equal(TimeSpan.Zero, status.CacheAge);
            Assert.False(status.IsValid);
            Assert.Equal(0, status.BranchCount);
        }
    }
}
