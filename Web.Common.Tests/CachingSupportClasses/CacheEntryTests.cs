using CommonNetFuncs.Web.Common.CachingSupportClasses;

namespace Web.Common.Tests.CachingSupportClasses;

public class CacheEntryTests
{
	[Fact]
	public void CacheEntry_DefaultConstructor_InitializesEmptyCollections()
	{
		// Act
		CacheEntry entry = new();

		// Assert
		entry.Data.ShouldNotBeNull();
		entry.Data.ShouldBeEmpty();
		entry.Tags.ShouldNotBeNull();
		entry.Tags.ShouldBeEmpty();
		entry.Headers.ShouldNotBeNull();
		entry.Headers.ShouldBeEmpty();
		entry.CompressionType.ShouldBe((short)0);
	}

	[Fact]
	public void CacheEntry_CanSetData()
	{
		// Arrange
		CacheEntry entry = new();
		byte[] data = [1, 2, 3, 4, 5];

		// Act
		entry.Data = data;

		// Assert
		entry.Data.ShouldBe(data);
		entry.Data.Length.ShouldBe(5);
	}

	[Fact]
	public void CacheEntry_CanSetTags()
	{
		// Arrange
		CacheEntry entry = new();
		HashSet<string> tags = ["tag1", "tag2", "tag3"];

		// Act
		entry.Tags = tags;

		// Assert
		entry.Tags.ShouldBe(tags);
		entry.Tags.Count.ShouldBe(3);
	}

	[Fact]
	public void CacheEntry_CanSetHeaders()
	{
		// Arrange
		CacheEntry entry = new();
		Dictionary<string, string> headers = new()
		{
			{ "Content-Type", "application/json" },
			{ "Cache-Control", "max-age=3600" }
		};

		// Act
		entry.Headers = headers;

		// Assert
		entry.Headers.ShouldBe(headers);
		entry.Headers.Count.ShouldBe(2);
	}

	[Fact]
	public void CacheEntry_CanSetCompressionType()
	{
		// Arrange
		CacheEntry entry = new();

		// Act
		entry.CompressionType = 1;

		// Assert
		entry.CompressionType.ShouldBe((short)1);
	}

	[Fact]
	public void CacheEntry_CanInitializeWithObjectInitializer()
	{
		// Act
		CacheEntry entry = new()
		{
			Data = [1, 2, 3],
			Tags = ["tag1", "tag2"],
			Headers = new Dictionary<string, string> { { "Content-Type", "text/plain" } },
			CompressionType = 2
		};

		// Assert
		entry.Data.Length.ShouldBe(3);
		entry.Tags.Count.ShouldBe(2);
		entry.Headers.Count.ShouldBe(1);
		entry.CompressionType.ShouldBe((short)2);
	}

	[Fact]
	public void CacheEntry_Tags_CanAddAndRemoveTags()
	{
		// Arrange
		CacheEntry entry = new();

		// Act
		entry.Tags.Add("tag1");
		entry.Tags.Add("tag2");
		entry.Tags.Add("tag3");
		bool removed = entry.Tags.Remove("tag2");

		// Assert
		removed.ShouldBeTrue();
		entry.Tags.Count.ShouldBe(2);
		entry.Tags.ShouldContain("tag1");
		entry.Tags.ShouldContain("tag3");
		entry.Tags.ShouldNotContain("tag2");
	}

	[Fact]
	public void CacheEntry_Headers_CanAddAndRemoveHeaders()
	{
		// Arrange
		CacheEntry entry = new();

		// Act
		entry.Headers.Add("Content-Type", "application/json");
		entry.Headers.Add("Content-Length", "1024");
		bool removed = entry.Headers.Remove("Content-Length");

		// Assert
		removed.ShouldBeTrue();
		entry.Headers.Count.ShouldBe(1);
		entry.Headers.ContainsKey("Content-Type").ShouldBeTrue();
		entry.Headers.ContainsKey("Content-Length").ShouldBeFalse();
	}

	[Fact]
	public void CacheEntry_Data_CanBeModified()
	{
		// Arrange
		CacheEntry entry = new() { Data = [1, 2, 3] };

		// Act
		entry.Data = [4, 5, 6, 7, 8];

		// Assert
		entry.Data.Length.ShouldBe(5);
		entry.Data[0].ShouldBe((byte)4);
		entry.Data[4].ShouldBe((byte)8);
	}

	[Fact]
	public void CacheEntry_EmptyData_IsValid()
	{
		// Arrange & Act
		CacheEntry entry = new() { Data = [] };

		// Assert
		entry.Data.ShouldNotBeNull();
		entry.Data.ShouldBeEmpty();
	}

	[Fact]
	public void CacheEntry_LargeData_CanBeStored()
	{
		// Arrange
		byte[] largeData = new byte[1024 * 1024]; // 1MB
		Array.Fill(largeData, (byte)255);

		// Act
		CacheEntry entry = new() { Data = largeData };

		// Assert
		entry.Data.Length.ShouldBe(1024 * 1024);
		entry.Data[0].ShouldBe((byte)255);
	}

	[Fact]
	public void CacheEntry_Tags_AreCaseSensitive()
	{
		// Arrange
		CacheEntry entry = new();

		// Act
		entry.Tags.Add("Tag1");
		entry.Tags.Add("tag1");
		entry.Tags.Add("TAG1");

		// Assert
		entry.Tags.Count.ShouldBe(3);
	}

	[Fact]
	public void CacheEntry_Headers_PreservesCaseInKeys()
	{
		// Arrange
		CacheEntry entry = new();

		// Act
		entry.Headers.Add("Content-Type", "application/json");
		entry.Headers.Add("content-type", "text/plain"); // Will throw if case-sensitive

		// Assert - Dictionary is case-sensitive by default
		Should.Throw<ArgumentException>(() => entry.Headers.Add("Content-Type", "text/html"));
	}

	[Fact]
	public void CacheEntry_MultiplePropertiesSet_MaintainsIndependence()
	{
		// Arrange
		CacheEntry entry = new()
		{
			Data = [1, 2, 3],
			Tags = ["tag1"],
			Headers = new Dictionary<string, string> { { "Key1", "Value1" } },
			CompressionType = 5
		};

		// Act - Modify one property
		entry.Tags.Add("tag2");

		// Assert - Other properties unchanged
		entry.Data.Length.ShouldBe(3);
		entry.Tags.Count.ShouldBe(2);
		entry.Headers.Count.ShouldBe(1);
		entry.CompressionType.ShouldBe((short)5);
	}

	[Fact]
	public void CacheEntry_CompressionType_AcceptsVariousValues()
	{
		// Arrange
		CacheEntry entry = new();

		// Act & Assert
		entry.CompressionType = 0;
		entry.CompressionType.ShouldBe((short)0);

		entry.CompressionType = 100;
		entry.CompressionType.ShouldBe((short)100);

		entry.CompressionType = short.MaxValue;
		((int)entry.CompressionType).ShouldBe(short.MaxValue);

		entry.CompressionType = short.MinValue;
		((int)entry.CompressionType).ShouldBe(short.MinValue);
	}

	[Fact]
	public void CacheEntry_CanReplaceCollectionReferences()
	{
		// Arrange
		CacheEntry entry = new()
		{
			Tags = ["old1", "old2"],
			Headers = new Dictionary<string, string> { { "Old", "Value" } }
		};

		// Act
		entry.Tags = ["new1", "new2", "new3"];
		entry.Headers = new Dictionary<string, string> { { "New", "NewValue" } };

		// Assert
		entry.Tags.Count.ShouldBe(3);
		entry.Tags.ShouldContain("new1");
		entry.Tags.ShouldNotContain("old1");
		entry.Headers.Count.ShouldBe(1);
		entry.Headers.ContainsKey("New").ShouldBeTrue();
		entry.Headers.ContainsKey("Old").ShouldBeFalse();
	}
}
