using System.Collections.Concurrent;
using CommonNetFuncs.FastMap;

namespace FastMap.Tests;

// Define a collection to ensure tests don't run in parallel with other test classes
[CollectionDefinition("FasterMapperConcurrency", DisableParallelization = true)]
public class FasterMapperConcurrencyCollection;

[Collection("FasterMapperConcurrency")] // Run these tests serially
public sealed class FasterMapperConcurrencyTests
{
	public sealed class SimpleSource
	{
		public required string StringProp { get; set; }
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
	}

	public sealed class SimpleDestination
	{
		public required string StringProp { get; set; }
		public int IntProp { get; set; }
		public DateTime DateProp { get; set; }
	}

	public sealed class ComplexSource
	{
		public required string Name { get; set; }
		public required List<string> Items { get; set; }
	}

	public sealed class ComplexDestination
	{
		public required string Name { get; set; }
		public required List<string> Items { get; set; }
	}

	[Fact]
	public void ConcurrentMapping_MultipleMappings_NoExceptions()
	{
		// Arrange
		const int threadCount = 10;
		const int operationsPerThread = 1000;
		ConcurrentBag<Exception> exceptions = [];
		List<Thread> threads = [];

		// Act - Create multiple threads that perform mapping simultaneously
		for (int i = 0; i < threadCount; i++)
		{
			int threadId = i;
			Thread thread = new(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						SimpleSource source = new()
						{
							StringProp = $"Thread{threadId}-Op{j}",
							IntProp = (threadId * 1000) + j,
							DateProp = DateTime.Now
						};

						SimpleDestination result = source.FasterMap<SimpleSource, SimpleDestination>();

						if (result.StringProp != source.StringProp || result.IntProp != source.IntProp)
						{
							throw new InvalidOperationException($"Mapping produced incorrect results on thread {threadId}");
						}
					}
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			});
			threads.Add(thread);
			thread.Start();
		}

		// Wait for all threads to complete
		foreach (Thread thread in threads)
		{
			thread.Join();
		}

		// Assert
		exceptions.ShouldBeEmpty();
	}

	[Fact]
	public void ConcurrentMapping_DifferentTypes_NoExceptions()
	{
		// Arrange
		const int threadCount = 8;
		const int operationsPerThread = 500;
		ConcurrentBag<Exception> exceptions = [];
		List<Thread> threads = [];

		// Act - Create threads that map different types concurrently
		for (int i = 0; i < threadCount; i++)
		{
			int threadId = i;
			Thread thread = new(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						// Alternate between simple and complex mappings
						if ((threadId + j) % 2 == 0)
						{
							SimpleSource source = new()
							{
								StringProp = $"Simple{threadId}-{j}",
								IntProp = j,
								DateProp = DateTime.Now
							};

							SimpleDestination result = source.FasterMap<SimpleSource, SimpleDestination>();
							if (result.StringProp != source.StringProp)
							{
								throw new InvalidOperationException("Simple mapping failed");
							}
						}
						else
						{
							ComplexSource source = new()
							{
								Name = $"Complex{threadId}-{j}",
								Items = [$"item{j}"]
							};

							ComplexDestination result = source.FasterMap<ComplexSource, ComplexDestination>();
							if (result.Name != source.Name || result.Items.Count != source.Items.Count)
							{
								throw new InvalidOperationException("Complex mapping failed");
							}
						}
					}
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			});
			threads.Add(thread);
			thread.Start();
		}

		// Wait for all threads to complete
		foreach (Thread thread in threads)
		{
			thread.Join();
		}

		// Assert
		exceptions.ShouldBeEmpty();
	}

	[Fact]
	public void ConcurrentMapping_ListMappings_NoExceptions()
	{
		// Arrange
		const int threadCount = 6;
		const int operationsPerThread = 200;
		ConcurrentBag<Exception> exceptions = [];
		List<Thread> threads = [];

		// Act
		for (int i = 0; i < threadCount; i++)
		{
			int threadId = i;
			Thread thread = new(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						List<SimpleSource> source =
						[
							new() { StringProp = $"T{threadId}-{j}-A", IntProp = j, DateProp = DateTime.Now },
							new() { StringProp = $"T{threadId}-{j}-B", IntProp = j + 1, DateProp = DateTime.Now.AddDays(1) },
							new() { StringProp = $"T{threadId}-{j}-C", IntProp = j + 2, DateProp = DateTime.Now.AddDays(2) }
						];

						List<SimpleDestination> result = source.FasterMap<List<SimpleSource>, List<SimpleDestination>>();

						if (result.Count != 3)
						{
							throw new InvalidOperationException($"Expected 3 items, got {result.Count}");
						}

						for (int k = 0; k < source.Count; k++)
						{
							if (result[k].StringProp != source[k].StringProp || result[k].IntProp != source[k].IntProp)
							{
								throw new InvalidOperationException($"List mapping produced incorrect results");
							}
						}
					}
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			});
			threads.Add(thread);
			thread.Start();
		}

		// Wait for all threads to complete
		foreach (Thread thread in threads)
		{
			thread.Join();
		}

		// Assert
		exceptions.ShouldBeEmpty();
	}

	[Fact]
	public async Task ConcurrentMapping_ParallelAsyncOperations_NoExceptions()
	{
		// Arrange
		const int parallelOperations = 100;
		ConcurrentBag<Exception> exceptions = [];

		// Act
		await Parallel.ForEachAsync(Enumerable.Range(0, parallelOperations), async (i, _) =>
		{
			try
			{
				await Task.Run(() =>
				{
					SimpleSource source = new()
					{
						StringProp = $"Async-{i}",
						IntProp = i,
						DateProp = DateTime.Now
					};

					SimpleDestination result = source.FasterMap<SimpleSource, SimpleDestination>();

					if (result.StringProp != source.StringProp || result.IntProp != source.IntProp)
					{
						throw new InvalidOperationException("Async mapping failed");
					}
				});
			}
			catch (Exception ex)
			{
				exceptions.Add(ex);
			}
		});

		// Assert
		exceptions.ShouldBeEmpty();
	}

	[Fact]
	public void RapidMapping_HighThroughput_Succeeds()
	{
		// Arrange
		const int iterations = 10000;
		SimpleSource source = new()
		{
			StringProp = "HighThroughput",
			IntProp = 42,
			DateProp = DateTime.Now
		};

		// Act & Assert - Should complete without exception
		for (int i = 0; i < iterations; i++)
		{
			SimpleDestination result = source.FasterMap<SimpleSource, SimpleDestination>();
			result.StringProp.ShouldBe(source.StringProp);
		}
	}

	[Fact]
	public void ConcurrentMapping_ArrayMappings_NoExceptions()
	{
		// Arrange
		const int threadCount = 4;
		const int operationsPerThread = 300;
		ConcurrentBag<Exception> exceptions = [];
		List<Thread> threads = [];

		// Act
		for (int i = 0; i < threadCount; i++)
		{
			int threadId = i;
			Thread thread = new(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						SimpleSource[] source =
						[
							new() { StringProp = $"T{threadId}-{j}-X", IntProp = j, DateProp = DateTime.Now },
							new() { StringProp = $"T{threadId}-{j}-Y", IntProp = j + 100, DateProp = DateTime.Now.AddHours(1) }
						];

						SimpleDestination[] result = source.FasterMap<SimpleSource[], SimpleDestination[]>();

						if (result.Length != 2)
						{
							throw new InvalidOperationException($"Expected 2 items, got {result.Length}");
						}

						for (int k = 0; k < source.Length; k++)
						{
							if (result[k].StringProp != source[k].StringProp || result[k].IntProp != source[k].IntProp)
							{
								throw new InvalidOperationException("Array mapping produced incorrect results");
							}
						}
					}
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			});
			threads.Add(thread);
			thread.Start();
		}

		// Wait for all threads to complete
		foreach (Thread thread in threads)
		{
			thread.Join();
		}

		// Assert
		exceptions.ShouldBeEmpty();
	}

	[Fact]
	public void ConcurrentMapping_DictionaryMappings_NoExceptions()
	{
		// Arrange
		const int threadCount = 4;
		const int operationsPerThread = 200;
		ConcurrentBag<Exception> exceptions = [];
		List<Thread> threads = [];

		// Act
		for (int i = 0; i < threadCount; i++)
		{
			int threadId = i;
			Thread thread = new(() =>
			{
				try
				{
					for (int j = 0; j < operationsPerThread; j++)
					{
						Dictionary<string, int> source = new()
						{
							[$"key{threadId}-{j}-A"] = j,
							[$"key{threadId}-{j}-B"] = j + 1
						};

						Dictionary<string, int> result = source.FasterMap<Dictionary<string, int>, Dictionary<string, int>>();

						if (result.Count != source.Count)
						{
							throw new InvalidOperationException($"Expected {source.Count} items, got {result.Count}");
						}

						foreach (KeyValuePair<string, int> kvp in source)
						{
							if (!result.TryGetValue(kvp.Key, out int value) || value != kvp.Value)
							{
								throw new InvalidOperationException("Dictionary mapping produced incorrect results");
							}
						}
					}
				}
				catch (Exception ex)
				{
					exceptions.Add(ex);
				}
			});
			threads.Add(thread);
			thread.Start();
		}

		// Wait for all threads to complete
		foreach (Thread thread in threads)
		{
			thread.Join();
		}

		// Assert
		exceptions.ShouldBeEmpty();
	}
}
