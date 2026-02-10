using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
public class CoreRunBatchesBenchmarks
{
	private readonly List<int> items;
	private readonly List<int> itemsWithDuplicates;
	private readonly List<int> largeItemsWithDuplicates;

	public CoreRunBatchesBenchmarks()
	{
		items = Enumerable.Range(1, 10000).ToList();

		// Create items with 20% duplicates
		itemsWithDuplicates = new List<int>();
		for (int i = 0; i < 8000; i++)
		{
			itemsWithDuplicates.Add(i);
		}
		for (int i = 0; i < 2000; i++)
		{
			itemsWithDuplicates.Add(i % 8000);
		}

		// Create larger dataset with 30% duplicates
		largeItemsWithDuplicates = new List<int>();
		for (int i = 0; i < 70000; i++)
		{
			largeItemsWithDuplicates.Add(i);
		}
		for (int i = 0; i < 30000; i++)
		{
			largeItemsWithDuplicates.Add(i % 70000);
		}
	}

	[Benchmark(Description = "RunBatchedProcess - no duplicates")]
	public bool RunBatchedProcess_NoDuplicates()
	{
		return items.RunBatchedProcess(batch =>
		{
			// Simulate some work
			return batch.Any();
		}, batchSize: 1000, logProgress: false);
	}

	[Benchmark(Description = "RunBatchedProcess - with duplicates")]
	public bool RunBatchedProcess_WithDuplicates()
	{
		return itemsWithDuplicates.RunBatchedProcess(batch => batch.Any(), batchSize: 1000, logProgress: false);
	}

	[Benchmark(Description = "RunBatchedProcess - large with duplicates")]
	public bool RunBatchedProcess_LargeWithDuplicates()
	{
		return largeItemsWithDuplicates.RunBatchedProcess(batch => batch.Any(), batchSize: 10000, logProgress: false);
	}
}
