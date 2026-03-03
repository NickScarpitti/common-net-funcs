using System.Collections.Concurrent;
using CommonNetFuncs.Web.Common.CachingSupportClasses;
using Microsoft.Extensions.Caching.Memory;

namespace Web.Common.Tests.CachingSupportClasses;

public class CacheMetricsTests
{
	[Fact]
	public void CacheMetrics_Constructor_WithoutCacheTags_InitializesEmptyDictionary()
	{
		// Act
		CacheMetrics metrics = new();

		// Assert
		metrics.CacheTags.ShouldNotBeNull();
		metrics.CacheTags.ShouldBeEmpty();
	}

	[Fact]
	public void CacheMetrics_Constructor_WithCacheTags_UsesSameInstance()
	{
		// Arrange
		ConcurrentDictionary<string, HashSet<string>> cacheTags = new();
		cacheTags.TryAdd("tag1", ["key1"]);

		// Act
		CacheMetrics metrics = new(cacheTags);

		// Assert
		metrics.CacheTags.ShouldBeSameAs(cacheTags);
		metrics.CacheTags.Count.ShouldBe(1);
	}

	[Fact]
	public void IncrementHits_IncreasesHitCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementHits();

		// Assert
		metrics.CacheHits().ShouldBe(3);
	}

	[Fact]
	public void IncrementMisses_IncreasesMissCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementMisses();
		metrics.IncrementMisses();

		// Assert
		metrics.CacheMisses().ShouldBe(2);
	}

	[Fact]
	public void IncrementCacheEntryCount_IncreasesCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();

		// Assert
		metrics.CurrentCacheEntryCount().ShouldBe(2);
	}

	[Fact]
	public void DecrementCacheEntryCount_DecreasesCount()
	{
		// Arrange
		CacheMetrics metrics = new();
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();

		// Act
		metrics.DecrementCacheEntryCount();

		// Assert
		metrics.CurrentCacheEntryCount().ShouldBe(2);
	}

	[Fact]
	public void DecrementCacheEntryCount_DoesNotGoBelowZero()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.DecrementCacheEntryCount();
		metrics.DecrementCacheEntryCount();

		// Assert
		metrics.CurrentCacheEntryCount().ShouldBe(0);
	}

	[Fact]
	public void AddToSize_IncreasesSize()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.AddToSize(1024);
		metrics.AddToSize(2048);

		// Assert
		metrics.CurrentCacheSize().ShouldBe(3072);
	}

	[Fact]
	public void AddToSize_WithNegativeValue_AddsAbsoluteValue()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.AddToSize(-1024);

		// Assert
		metrics.CurrentCacheSize().ShouldBe(1024);
	}

	[Fact]
	public void SubtractFromSize_DecreasesSize()
	{
		// Arrange
		CacheMetrics metrics = new();
		metrics.AddToSize(5000);

		// Act
		metrics.SubtractFromSize(2000);

		// Assert
		metrics.CurrentCacheSize().ShouldBe(3000);
	}

	[Fact]
	public void SubtractFromSize_WithNegativeValue_SubtractsAbsoluteValue()
	{
		// Arrange
		CacheMetrics metrics = new();
		metrics.AddToSize(5000);

		// Act
		metrics.SubtractFromSize(-2000);

		// Assert
		metrics.CurrentCacheSize().ShouldBe(3000);
	}

	[Fact]
	public void SubtractFromSize_CanResultInNegativeSize()
	{
		// Arrange
		CacheMetrics metrics = new();
		metrics.AddToSize(1000);

		// Act
		metrics.SubtractFromSize(2000);

		// Assert
		// Note: The implementation checks if currentCacheSize > 0 before subtracting,
		// but it still allows going negative if the subtraction is larger
		metrics.CurrentCacheSize().ShouldBe(-1000);
	}

	[Fact]
	public void IncrementEviction_WithRemovedReason_IncreasesRemovedCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementEviction(EvictionReason.Removed);
		metrics.IncrementEviction(EvictionReason.Removed);

		// Assert
		metrics.EvictedDueToRemoved().ShouldBe(2);
	}

	[Fact]
	public void IncrementEviction_WithCapacityReason_IncreasesCapacityCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Capacity);

		// Assert
		metrics.EvictedDueToCapacity().ShouldBe(3);
	}

	[Fact]
	public void IncrementEviction_WithOtherReasons_DoesNotIncrementCounters()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementEviction(EvictionReason.Expired);
		metrics.IncrementEviction(EvictionReason.TokenExpired);
		metrics.IncrementEviction(EvictionReason.Replaced);
		metrics.IncrementEviction(EvictionReason.None);

		// Assert
		metrics.EvictedDueToRemoved().ShouldBe(0);
		metrics.EvictedDueToCapacity().ShouldBe(0);
	}

	[Fact]
	public void IncrementSkippedDueToSize_IncreasesCount()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementSkippedDueToSize();
		metrics.IncrementSkippedDueToSize();

		// Assert
		metrics.SkippedDueToSize().ShouldBe(2);
	}

	[Fact]
	public void Clear_ResetsAllMetrics()
	{
		// Arrange
		CacheMetrics metrics = new();
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementMisses();
		metrics.AddToSize(5000);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Removed);
		metrics.IncrementSkippedDueToSize();
		metrics.CacheTags.TryAdd("tag1", ["key1"]);

		// Act
		metrics.Clear();

		// Assert
		metrics.CacheHits().ShouldBe(0);
		metrics.CacheMisses().ShouldBe(0);
		metrics.CurrentCacheSize().ShouldBe(0);
		metrics.CurrentCacheEntryCount().ShouldBe(0);
		metrics.EvictedDueToCapacity().ShouldBe(0);
		metrics.EvictedDueToRemoved().ShouldBe(0);
		metrics.SkippedDueToSize().ShouldBe(0);
		metrics.CacheTags.ShouldBeEmpty();
	}

	[Fact]
	public void CacheTags_CanAddAndRetrieveTags()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.CacheTags.TryAdd("tag1", ["key1", "key2"]);
		metrics.CacheTags.TryAdd("tag2", ["key3"]);

		// Assert
		metrics.CacheTags.Count.ShouldBe(2);
		metrics.CacheTags["tag1"].Count.ShouldBe(2);
		metrics.CacheTags["tag2"].Count.ShouldBe(1);
	}

	[Fact]
	public void ConcurrentOperations_ShouldBeThreadSafe()
	{
		// Arrange
		CacheMetrics metrics = new();
		const int taskCount = 100;
		const int opsPerTask = 100;

		// Act
		Parallel.For(0, taskCount, _ =>
		{
			for (int i = 0; i < opsPerTask; i++)
			{
				metrics.IncrementHits();
				metrics.IncrementMisses();
				metrics.IncrementCacheEntryCount();
				metrics.AddToSize(10);
			}
		});

		// Assert
		metrics.CacheHits().ShouldBe(taskCount * opsPerTask);
		metrics.CacheMisses().ShouldBe(taskCount * opsPerTask);
		metrics.CurrentCacheEntryCount().ShouldBe(taskCount * opsPerTask);
		metrics.CurrentCacheSize().ShouldBe(taskCount * opsPerTask * 10);
	}

	[Fact]
	public void InitialState_AllMetricsAreZero()
	{
		// Act
		CacheMetrics metrics = new();

		// Assert
		metrics.CacheHits().ShouldBe(0);
		metrics.CacheMisses().ShouldBe(0);
		metrics.CurrentCacheSize().ShouldBe(0);
		metrics.CurrentCacheEntryCount().ShouldBe(0);
		metrics.EvictedDueToCapacity().ShouldBe(0);
		metrics.EvictedDueToRemoved().ShouldBe(0);
		metrics.SkippedDueToSize().ShouldBe(0);
	}

	[Fact]
	public void MixedOperations_ProduceCorrectResults()
	{
		// Arrange
		CacheMetrics metrics = new();

		// Act
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementHits();
		metrics.IncrementMisses();
		metrics.AddToSize(1024);
		metrics.AddToSize(2048);
		metrics.IncrementCacheEntryCount();
		metrics.IncrementCacheEntryCount();
		metrics.DecrementCacheEntryCount();
		metrics.SubtractFromSize(500);
		metrics.IncrementEviction(EvictionReason.Capacity);
		metrics.IncrementEviction(EvictionReason.Removed);
		metrics.IncrementSkippedDueToSize();

		// Assert
		metrics.CacheHits().ShouldBe(3);
		metrics.CacheMisses().ShouldBe(1);
		metrics.CurrentCacheSize().ShouldBe(2572); // 1024 + 2048 - 500
		metrics.CurrentCacheEntryCount().ShouldBe(1); // 2 - 1
		metrics.EvictedDueToCapacity().ShouldBe(1);
		metrics.EvictedDueToRemoved().ShouldBe(1);
		metrics.SkippedDueToSize().ShouldBe(1);
	}
}
