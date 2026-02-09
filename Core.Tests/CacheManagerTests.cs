using System.Collections.ObjectModel;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class CacheManagerTests
{
	[Fact]
	public void Constructor_Should_Initialize_Properties()
	{
		// Arrange & Act
		CacheManager<int, string> cacheManager = new(42);

		// Assert
		cacheManager.GetCache().ShouldNotBeNull();
		cacheManager.GetLimitedCache().ShouldNotBeNull();
		cacheManager.GetLimitedCacheSize().ShouldBe(42);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(100)]
	public void SetLimitedCacheSize_Should_Change_Size_And_Clear_LimitedCache(int newSize)
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "a");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);

		// Act
		cacheManager.SetLimitedCacheSize(newSize);

		// Assert
		cacheManager.GetLimitedCacheSize().ShouldBe(newSize);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void GetLimitedCache_Should_Return_Readonly_Cache()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);

		// Act
		cacheManager.TryAddLimitedCache(1, "a");

		// Assert
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
		cacheManager.GetLimitedCache().ShouldBeOfType<ReadOnlyDictionary<int, string?>>();
		cacheManager.GetLimitedCache().Keys.First().ShouldBe(1);
		cacheManager.GetLimitedCache().Values.First().ShouldBe("a");
	}

	[Fact]
	public void GetCache_Should_Return_Readonly_Cache()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);

		// Act
		cacheManager.TryAddCache(1, "a");

		// Assert
		cacheManager.GetCache().Count.ShouldBe(1);
		cacheManager.GetCache().ShouldBeOfType<ReadOnlyDictionary<int, string?>>();
		cacheManager.GetCache().Keys.First().ShouldBe(1);
		cacheManager.GetCache().Values.First().ShouldBe("a");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SetUseLimitedCache_Should_Set_UseLimitedCache_And_Clear_Caches(bool useLimited)
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(3);
		cacheManager.TryAddCache(1, "a");
		cacheManager.TryAddCache(2, "b");

		// Act
		cacheManager.SetUseLimitedCache(useLimited);

		// Assert
		cacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
		cacheManager.GetCache().Count.ShouldBe(0);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
		if (!useLimited)
		{
			cacheManager.GetLimitedCacheSize().ShouldBe(1);
		}
		else
		{
			cacheManager.GetLimitedCacheSize().ShouldBe(3);
		}
	}

	[Fact]
	public void ClearCache_Should_Clear_ConcurrentDictionary()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "a");
		cacheManager.GetCache().Count.ShouldBe(1);

		// Act
		cacheManager.ClearCache();

		// Assert
		cacheManager.GetCache().Count.ShouldBe(0);
	}

	[Fact]
	public void ClearLimitedCache_Should_Clear_FixedLRUDictionary()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.TryAddLimitedCache(1, "a");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);

		// Act
		cacheManager.ClearLimitedCache();

		// Assert
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void ClearAllCaches_Should_Clear_Both_Caches()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "a");
		cacheManager.TryAddCache(2, "b");

		// Act
		cacheManager.ClearAllCaches();

		// Assert
		cacheManager.GetCache().Count.ShouldBe(0);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void GetLimitedCacheSize_Should_Return_Current_Size()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(7);

		// Act
		int size = cacheManager.GetLimitedCacheSize();

		// Assert
		size.ShouldBe(7);
	}

	[Fact]
	public void IsUsingLimitedCache_Should_Return_UseLimitedCache()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.SetUseLimitedCache(false);

		// Act & Assert
		cacheManager.IsUsingLimitedCache().ShouldBeFalse();
		cacheManager.SetUseLimitedCache(true);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();
	}

	[Fact]
	public void ThreadSafety_SetLimitedCacheSize_And_SetUseLimitedCache_Should_Not_Throw()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(10);

		// Act & Assert
		Exception? exception1 = Record.Exception(() => Parallel.For(0, 100, i => cacheManager.SetLimitedCacheSize(i + 1)));
		Exception? exception2 = Record.Exception(() => Parallel.For(0, 100, i => cacheManager.SetUseLimitedCache(i % 2 == 0)));

		exception1.ShouldBeNull();
		exception2.ShouldBeNull();
	}

	#region GetOrAdd Tests

	[Fact]
	public void GetOrAddCache_Should_Add_New_Key_And_Return_Value()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();

		// Act
		string result = cacheManager.GetOrAddCache(1, k => $"Value{k}");

		// Assert
		result.ShouldBe("Value1");
		cacheManager.GetCache().Count.ShouldBe(1);
		cacheManager.GetCache()[1].ShouldBe("Value1");
	}

	[Fact]
	public void GetOrAddCache_Should_Return_Existing_Value_When_Key_Exists()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "Original");

		// Act
		string result = cacheManager.GetOrAddCache(1, k => "New");

		// Assert
		result.ShouldBe("Original");
		cacheManager.GetCache().Count.ShouldBe(1);
	}

	[Fact]
	public void GetOrAddLimitedCache_Should_Add_New_Key_And_Return_Value()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);

		// Act
		string result = cacheManager.GetOrAddLimitedCache(1, k => $"Value{k}");

		// Assert
		result.ShouldBe("Value1");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
		cacheManager.GetLimitedCache()[1].ShouldBe("Value1");
	}

	[Fact]
	public void GetOrAddLimitedCache_Should_Return_Existing_Value_When_Key_Exists()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "Original");

		// Act
		string result = cacheManager.GetOrAddLimitedCache(1, k => "New");

		// Assert
		result.ShouldBe("Original");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
	}

	#endregion

	#region TryAdd Duplicate Key Tests

	[Fact]
	public void TryAddCache_Should_Return_False_When_Key_Already_Exists()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "First");

		// Act
		bool result = cacheManager.TryAddCache(1, "Second");

		// Assert
		result.ShouldBeFalse();
		cacheManager.GetCache()[1].ShouldBe("First");
	}

	[Fact]
	public void TryAddLimitedCache_Should_Return_False_When_Key_Already_Exists()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "First");

		// Act
		bool result = cacheManager.TryAddLimitedCache(1, "Second");

		// Assert
		result.ShouldBeFalse();
		cacheManager.GetLimitedCache()[1].ShouldBe("First");
	}

	#endregion

	#region SetLimitedCacheSize Edge Cases

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public void SetLimitedCacheSize_WithSizeLessThanOne_ShouldDisableLimitedCache(int size)
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(10);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();

		// Act
		cacheManager.SetLimitedCacheSize(size);

		// Assert
		cacheManager.IsUsingLimitedCache().ShouldBeFalse();
		cacheManager.GetLimitedCacheSize().ShouldBe(size);
	}

	[Fact]
	public void SetLimitedCacheSize_WhenUseLimitedCacheIsFalse_ShouldNotReinitializeLimitedCache()
	{
		// Arrange
		CacheManager<int, string> cacheManager = new(10);
		cacheManager.SetUseLimitedCache(false);
		cacheManager.TryAddLimitedCache(1, "a");
		int countBefore = cacheManager.GetLimitedCache().Count;

		// Act
		cacheManager.SetLimitedCacheSize(20);

		// Assert
		cacheManager.GetLimitedCacheSize().ShouldBe(20);
		cacheManager.GetLimitedCache().Count.ShouldBe(countBefore);
	}

	#endregion
}

#region CacheManagerFifo Tests

public sealed class CacheManagerFifoTests
{
	[Fact]
	public void Constructor_Should_Initialize_Properties()
	{
		// Arrange & Act
		CacheManagerFifo<int, string> cacheManager = new(42);

		// Assert
		cacheManager.GetCache().ShouldNotBeNull();
		cacheManager.GetLimitedCache().ShouldNotBeNull();
		cacheManager.GetLimitedCacheSize().ShouldBe(42);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();
	}

	[Theory]
	[InlineData(1)]
	[InlineData(10)]
	[InlineData(100)]
	public void SetLimitedCacheSize_Should_Change_Size_And_Clear_LimitedCache(int newSize)
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "a");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);

		// Act
		cacheManager.SetLimitedCacheSize(newSize);

		// Assert
		cacheManager.GetLimitedCacheSize().ShouldBe(newSize);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void GetLimitedCache_Should_Return_Readonly_Cache()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);

		// Act
		cacheManager.TryAddLimitedCache(1, "a");

		// Assert
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
		cacheManager.GetLimitedCache().ShouldBeOfType<ReadOnlyDictionary<int, string?>>();
		cacheManager.GetLimitedCache().Keys.First().ShouldBe(1);
		cacheManager.GetLimitedCache().Values.First().ShouldBe("a");
	}

	[Fact]
	public void GetCache_Should_Return_Readonly_Cache()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);

		// Act
		cacheManager.TryAddCache(1, "a");

		// Assert
		cacheManager.GetCache().Count.ShouldBe(1);
		cacheManager.GetCache().ShouldBeOfType<ReadOnlyDictionary<int, string?>>();
		cacheManager.GetCache().Keys.First().ShouldBe(1);
		cacheManager.GetCache().Values.First().ShouldBe("a");
	}

	[Theory]
	[InlineData(true)]
	[InlineData(false)]
	public void SetUseLimitedCache_Should_Set_UseLimitedCache_And_Clear_Caches(bool useLimited)
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(3);
		cacheManager.TryAddCache(1, "a");
		cacheManager.TryAddCache(2, "b");

		// Act
		cacheManager.SetUseLimitedCache(useLimited);

		// Assert
		cacheManager.IsUsingLimitedCache().ShouldBe(useLimited);
		cacheManager.GetCache().Count.ShouldBe(0);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
		if (!useLimited)
		{
			cacheManager.GetLimitedCacheSize().ShouldBe(1);
		}
		else
		{
			cacheManager.GetLimitedCacheSize().ShouldBe(3);
		}
	}

	[Fact]
	public void ClearCache_Should_Clear_ConcurrentDictionary()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "a");
		cacheManager.GetCache().Count.ShouldBe(1);

		// Act
		cacheManager.ClearCache();

		// Assert
		cacheManager.GetCache().Count.ShouldBe(0);
	}

	[Fact]
	public void ClearLimitedCache_Should_Clear_FixedFifoDictionary()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.TryAddLimitedCache(1, "a");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);

		// Act
		cacheManager.ClearLimitedCache();

		// Assert
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void ClearAllCaches_Should_Clear_Both_Caches()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "a");
		cacheManager.TryAddCache(2, "b");

		// Act
		cacheManager.ClearAllCaches();

		// Assert
		cacheManager.GetCache().Count.ShouldBe(0);
		cacheManager.GetLimitedCache().Count.ShouldBe(0);
	}

	[Fact]
	public void GetLimitedCacheSize_Should_Return_Current_Size()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(7);

		// Act
		int size = cacheManager.GetLimitedCacheSize();

		// Assert
		size.ShouldBe(7);
	}

	[Fact]
	public void IsUsingLimitedCache_Should_Return_UseLimitedCache()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.SetUseLimitedCache(false);

		// Act & Assert
		cacheManager.IsUsingLimitedCache().ShouldBeFalse();
		cacheManager.SetUseLimitedCache(true);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();
	}

	[Fact]
	public void ThreadSafety_SetLimitedCacheSize_And_SetUseLimitedCache_Should_Not_Throw()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(10);

		// Act & Assert
		Exception? exception1 = Record.Exception(() => Parallel.For(0, 100, i => cacheManager.SetLimitedCacheSize(i + 1)));
		Exception? exception2 = Record.Exception(() => Parallel.For(0, 100, i => cacheManager.SetUseLimitedCache(i % 2 == 0)));

		exception1.ShouldBeNull();
		exception2.ShouldBeNull();
	}

	[Fact]
	public void GetOrAddCache_Should_Add_New_Key_And_Return_Value()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();

		// Act
		string result = cacheManager.GetOrAddCache(1, k => $"Value{k}");

		// Assert
		result.ShouldBe("Value1");
		cacheManager.GetCache().Count.ShouldBe(1);
		cacheManager.GetCache()[1].ShouldBe("Value1");
	}

	[Fact]
	public void GetOrAddCache_Should_Return_Existing_Value_When_Key_Exists()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "Original");

		// Act
		string result = cacheManager.GetOrAddCache(1, k => "New");

		// Assert
		result.ShouldBe("Original");
		cacheManager.GetCache().Count.ShouldBe(1);
	}

	[Fact]
	public void GetOrAddLimitedCache_Should_Add_New_Key_And_Return_Value()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);

		// Act
		string result = cacheManager.GetOrAddLimitedCache(1, k => $"Value{k}");

		// Assert
		result.ShouldBe("Value1");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
		cacheManager.GetLimitedCache()[1].ShouldBe("Value1");
	}

	[Fact]
	public void GetOrAddLimitedCache_Should_Return_Existing_Value_When_Key_Exists()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "Original");

		// Act
		string result = cacheManager.GetOrAddLimitedCache(1, k => "New");

		// Assert
		result.ShouldBe("Original");
		cacheManager.GetLimitedCache().Count.ShouldBe(1);
	}

	[Fact]
	public void TryAddCache_Should_Return_False_When_Key_Already_Exists()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new();
		cacheManager.TryAddCache(1, "First");

		// Act
		bool result = cacheManager.TryAddCache(1, "Second");

		// Assert
		result.ShouldBeFalse();
		cacheManager.GetCache()[1].ShouldBe("First");
	}

	[Fact]
	public void TryAddLimitedCache_Should_Return_False_When_Key_Already_Exists()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(5);
		cacheManager.TryAddLimitedCache(1, "First");

		// Act
		bool result = cacheManager.TryAddLimitedCache(1, "Second");

		// Assert
		result.ShouldBeFalse();
		cacheManager.GetLimitedCache()[1].ShouldBe("First");
	}

	[Theory]
	[InlineData(0)]
	[InlineData(-1)]
	[InlineData(-100)]
	public void SetLimitedCacheSize_WithSizeLessThanOne_ShouldDisableLimitedCache(int size)
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(10);
		cacheManager.IsUsingLimitedCache().ShouldBeTrue();

		// Act
		cacheManager.SetLimitedCacheSize(size);

		// Assert
		cacheManager.IsUsingLimitedCache().ShouldBeFalse();
		cacheManager.GetLimitedCacheSize().ShouldBe(size);
	}

	[Fact]
	public void SetLimitedCacheSize_WhenUseLimitedCacheIsFalse_ShouldNotReinitializeLimitedCache()
	{
		// Arrange
		CacheManagerFifo<int, string> cacheManager = new(10);
		cacheManager.SetUseLimitedCache(false);
		cacheManager.TryAddLimitedCache(1, "a");
		int countBefore = cacheManager.GetLimitedCache().Count;

		// Act
		cacheManager.SetLimitedCacheSize(20);

		// Assert
		cacheManager.GetLimitedCacheSize().ShouldBe(20);
		cacheManager.GetLimitedCache().Count.ShouldBe(countBefore);
	}
}

#endregion
