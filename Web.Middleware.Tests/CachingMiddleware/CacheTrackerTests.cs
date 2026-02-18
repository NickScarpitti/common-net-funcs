using CommonNetFuncs.Web.Middleware.CachingMiddleware;

namespace Web.Middleware.Tests.CachingMiddleware;

public sealed class CacheTrackerTests
{
	[Fact]
	public void Constructor_InitializesCollections()
	{
		// Act
		CacheTracker tracker = new();

		// Assert
		tracker.CacheTags.ShouldNotBeNull();
		tracker.CacheTags.ShouldBeEmpty();
	}

	[Fact]
	public void TrackEntry_AddsEntryWithCorrectMetadata()
	{
		// Arrange
		CacheTracker tracker = new();
		const string key = "test-key";
		const long size = 1024;

		// Act
		tracker.TrackEntry(key, size);
		IEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = tracker.GetEntries();

		// Assert
		entries.ShouldContain(e => e.Key == key);
		KeyValuePair<string, CacheTracker.CacheEntryMetadata> entry = entries.First(e => e.Key == key);
		entry.Value.Size.ShouldBe(size);
		entry.Value.TimeCreated.ShouldBeGreaterThan(DateTimeOffset.UtcNow.AddSeconds(-5));
	}

	[Fact]
	public void TrackEntry_OverwriteExistingEntry_UpdatesMetadata()
	{
		// Arrange
		CacheTracker tracker = new();
		const string key = "test-key";

		// Act
		tracker.TrackEntry(key, 100);
		DateTimeOffset firstTime = tracker.GetEntries().First(e => e.Key == key).Value.TimeCreated;

		Thread.Sleep(10); // Small delay to ensure time difference

		tracker.TrackEntry(key, 200);
		IEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = tracker.GetEntries();

		// Assert
		entries.Count(e => e.Key == key).ShouldBe(1);
		KeyValuePair<string, CacheTracker.CacheEntryMetadata> entry = entries.First(e => e.Key == key);
		entry.Value.Size.ShouldBe(200);
		entry.Value.TimeCreated.ShouldBeGreaterThan(firstTime);
	}

	[Fact]
	public void RemoveEntry_RemovesExistingEntry()
	{
		// Arrange
		CacheTracker tracker = new();
		const string key = "test-key";
		tracker.TrackEntry(key, 1024);

		// Act
		tracker.RemoveEntry(key);
		IEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = tracker.GetEntries();

		// Assert
		entries.ShouldNotContain(e => e.Key == key);
	}

	[Fact]
	public void RemoveEntry_NonExistentKey_DoesNotThrow()
	{
		// Arrange
		CacheTracker tracker = new();

		// Act & Assert
		Should.NotThrow(() => tracker.RemoveEntry("non-existent-key"));
	}

	[Fact]
	public void GetEntries_ReturnsSnapshot()
	{
		// Arrange
		CacheTracker tracker = new();
		tracker.TrackEntry("key1", 100);
		tracker.TrackEntry("key2", 200);

		// Act
		IEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = tracker.GetEntries();
		int initialCount = entries.Count();

		tracker.TrackEntry("key3", 300);
		int countAfterAdd = entries.Count();

		// Assert
		initialCount.ShouldBe(2);
		countAfterAdd.ShouldBe(2); // Snapshot should not change
	}

	[Fact]
	public void Clear_RemovesAllEntriesAndTags()
	{
		// Arrange
		CacheTracker tracker = new();
		tracker.TrackEntry("key1", 100);
		tracker.TrackEntry("key2", 200);
		tracker.CacheTags["tag1"] = ["key1"];
		tracker.CacheTags["tag2"] = ["key2"];

		// Act
		tracker.Clear();

		// Assert
		tracker.GetEntries().ShouldBeEmpty();
		tracker.CacheTags.ShouldBeEmpty();
	}

	[Fact]
	public void CacheTags_CanAddAndRetrieveTags()
	{
		// Arrange
		CacheTracker tracker = new();

		// Act
		tracker.CacheTags["tag1"] = ["key1", "key2"];
		tracker.CacheTags["tag2"] = ["key3"];

		// Assert
		tracker.CacheTags.Count.ShouldBe(2);
		tracker.CacheTags["tag1"].ShouldContain("key1");
		tracker.CacheTags["tag1"].ShouldContain("key2");
		tracker.CacheTags["tag2"].ShouldContain("key3");
	}

	[Fact]
	public void CacheTags_SupportsConcurrentAccess()
	{
		// Arrange
		CacheTracker tracker = new();
		List<Task> tasks = [];

		// Act
		for (int i = 0; i < 10; i++)
		{
			int capturedI = i;
			tasks.Add(Task.Run(() =>
			{
				tracker.CacheTags[$"tag{capturedI}"] = [$"key{capturedI}"];
			}));
		}

		Task.WaitAll([.. tasks]);

		// Assert
		tracker.CacheTags.Count.ShouldBe(10);
	}

	[Fact]
	public void TrackEntry_MultipleConcurrentCalls_AllRecorded()
	{
		// Arrange
		CacheTracker tracker = new();
		List<Task> tasks = [];

		// Act
		for (int i = 0; i < 100; i++)
		{
			int capturedI = i;
			tasks.Add(Task.Run(() =>
			{
				tracker.TrackEntry($"key{capturedI}", capturedI * 100);
			}));
		}

		Task.WaitAll([.. tasks]);

		// Assert
		IEnumerable<KeyValuePair<string, CacheTracker.CacheEntryMetadata>> entries = tracker.GetEntries();
		entries.Count().ShouldBe(100);
	}
}
