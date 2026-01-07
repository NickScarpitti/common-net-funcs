using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
public class CoreRunBatchesBenchmarks
{
	private readonly List<int> _items;
	private readonly List<int> _itemsWithDuplicates;
	private readonly List<int> _largeItemsWithDuplicates;

	public CoreRunBatchesBenchmarks()
	{
		_items = Enumerable.Range(1, 10000).ToList();

		// Create items with 20% duplicates
		_itemsWithDuplicates = new List<int>();
		for (int i = 0; i < 8000; i++)
		{
			_itemsWithDuplicates.Add(i);
		}
		for (int i = 0; i < 2000; i++)
		{
			_itemsWithDuplicates.Add(i % 8000);
		}

		// Create larger dataset with 30% duplicates
		_largeItemsWithDuplicates = new List<int>();
		for (int i = 0; i < 70000; i++)
		{
			_largeItemsWithDuplicates.Add(i);
		}
		for (int i = 0; i < 30000; i++)
		{
			_largeItemsWithDuplicates.Add(i % 70000);
		}
	}

	[Benchmark(Description = "RunBatchedProcess - no duplicates")]
	public bool RunBatchedProcess_NoDuplicates()
	{
		return _items.RunBatchedProcess(batch =>
		{
			// Simulate some work
			return batch.Any();
		}, batchSize: 1000, logProgress: false);
	}

	[Benchmark(Description = "RunBatchedProcess - with duplicates")]
	public bool RunBatchedProcess_WithDuplicates()
	{
		return _itemsWithDuplicates.RunBatchedProcess(batch =>
		{
			return batch.Any();
		}, batchSize: 1000, logProgress: false);
	}

	[Benchmark(Description = "RunBatchedProcess - large with duplicates")]
	public bool RunBatchedProcess_LargeWithDuplicates()
	{
		return _largeItemsWithDuplicates.RunBatchedProcess(batch =>
		{
			return batch.Any();
		}, batchSize: 10000, logProgress: false);
	}
}
