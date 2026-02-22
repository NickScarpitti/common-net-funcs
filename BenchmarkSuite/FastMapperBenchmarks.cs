using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using CommonNetFuncs.FastMap;
using Microsoft.VSDiagnostics;

using static CommonNetFuncs.FastMap.FastMapper;

namespace BenchmarkSuite;

[MediumRunJob(RuntimeMoniker.Net10_0)]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[CPUUsageDiagnoser]
[MemoryDiagnoser]
public class FastMapperBenchmarks
{
	private SimpleSource simpleSource = null!;
	private ComplexSource complexSource = null!;
	private List<SimpleSource> simpleList = null!;
	private SimpleSource[] simpleArray = null!;
	private Dictionary<string, SimpleSource> complexDictionary = null!;
	private NestedSource deeplyNestedSource = null!;
	private int[] largeArray = null!;
	private List<ComplexSource> largeComplexList = null!;
	private IReadOnlyCollection<SimpleSource> readOnlyCollection = null!;
	[GlobalSetup]
	public void Setup()
	{
		// Simple source for basic property mapping
		simpleSource = new SimpleSource
		{
			StringProp = "Test String Value",
			IntProp = 42,
			DateProp = DateTime.Now,
			DoubleProp = 3.14159,
			BoolProp = true,
			GuidProp = Guid.NewGuid()
		};
		// Complex source with nested objects and collections
		complexSource = new ComplexSource
		{
			Name = "Complex Object",
			StringList = Enumerable.Range(0, 10).Select(i => $"Item {i}").ToList(),
			Dictionary = Enumerable.Range(0, 10).ToDictionary(i => $"Key{i}", i => i),
			NestedObject = new SimpleSource
			{
				StringProp = "Nested String",
				IntProp = 100,
				DateProp = DateTime.Now,
				DoubleProp = 2.71828,
				BoolProp = false,
				GuidProp = Guid.NewGuid()
			},
			NumberSet = Enumerable.Range(0, 10).ToHashSet(),
			StringQueue = new Queue<string>(Enumerable.Range(0, 10).Select(i => $"Queue {i}")),
			DoubleStack = new Stack<double>(Enumerable.Range(0, 10).Select(i => (double)i))
		};
		// Simple list for collection mapping
		simpleList = Enumerable.Range(0, 100).Select(i => new SimpleSource { StringProp = $"String {i}", IntProp = i, DateProp = DateTime.Now.AddDays(i), DoubleProp = i * 1.5, BoolProp = i % 2 == 0, GuidProp = Guid.NewGuid() }).ToList();
		// Initialize array from list
		simpleArray = simpleList.ToArray();
		// Initialize readonly collection
		readOnlyCollection = simpleList.AsReadOnly();
		// Complex dictionary with nested objects
		complexDictionary = Enumerable.Range(0, 50).ToDictionary(i => $"Key{i}", i => new SimpleSource { StringProp = $"Dict String {i}", IntProp = i, DateProp = DateTime.Now, DoubleProp = i * 2.0, BoolProp = i % 3 == 0, GuidProp = Guid.NewGuid() });
		// Deeply nested source
		deeplyNestedSource = new NestedSource
		{
			Level1 = new Level1
			{
				Name = "Level 1",
				Level2 = new Level2
				{
					Name = "Level 2",
					Level3 = new Level3
					{
						Name = "Level 3",
						Values = Enumerable.Range(0, 5).ToList()
					}
				}
			}
		};
		// Large array for array mapping
		largeArray = Enumerable.Range(0, 1000).ToArray();
		// Large complex list
		largeComplexList = Enumerable.Range(0, 500).Select(i => new ComplexSource { Name = $"Complex {i}", StringList = Enumerable.Range(0, 5).Select(j => $"Item {i}-{j}").ToList(), Dictionary = Enumerable.Range(0, 5).ToDictionary(j => $"Key{i}-{j}", j => j), NestedObject = new SimpleSource { StringProp = $"Nested {i}", IntProp = i, DateProp = DateTime.Now, DoubleProp = i * 1.1, BoolProp = i % 2 == 0, GuidProp = Guid.NewGuid() }, NumberSet = Enumerable.Range(0, 5).ToHashSet(), StringQueue = new Queue<string>(Enumerable.Range(0, 5).Select(j => $"Queue {i}-{j}")), DoubleStack = new Stack<double>(Enumerable.Range(0, 5).Select(j => (double)j)) }).ToList();
		// Clear cache before benchmarks
		CacheManager.ClearAllCaches();
		CacheManager.SetUseLimitedCache(false);
	}

	[GlobalCleanup]
	public static void Cleanup()
	{
		CacheManager.ClearAllCaches();
	}

	[Benchmark(Baseline = true)]
	public SimpleDestination SimplePropertyMapping_Cached()
	{
		return simpleSource.FastMap<SimpleSource, SimpleDestination>(useCache: true);
	}

	[Benchmark]
	public SimpleDestination SimplePropertyMapping_Uncached()
	{
		return simpleSource.FastMap<SimpleSource, SimpleDestination>(useCache: false);
	}

	[Benchmark]
	public ComplexDestination ComplexObjectMapping_Cached()
	{
		return complexSource.FastMap<ComplexSource, ComplexDestination>(useCache: true);
	}

	[Benchmark]
	public ComplexDestination ComplexObjectMapping_Uncached()
	{
		return complexSource.FastMap<ComplexSource, ComplexDestination>(useCache: false);
	}

	[Benchmark]
	public List<SimpleDestination> ListMapping_Cached()
	{
		return simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>(useCache: true);
	}

	[Benchmark]
	public List<SimpleDestination> ListMapping_Uncached()
	{
		return simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>(useCache: false);
	}

	[Benchmark]
	public SimpleDestination[] ArrayMapping_Cached()
	{
		return simpleArray.FastMap<SimpleSource[], SimpleDestination[]>(useCache: true);
	}

	[Benchmark]
	public SimpleDestination[] ArrayMapping_Uncached()
	{
		return simpleArray.FastMap<SimpleSource[], SimpleDestination[]>(useCache: false);
	}

	[Benchmark]
	public HashSet<int> HashSetMapping_Cached()
	{
		return complexSource.NumberSet.FastMap<HashSet<int>, HashSet<int>>(useCache: true);
	}

	[Benchmark]
	public HashSet<int> HashSetMapping_Uncached()
	{
		return complexSource.NumberSet.FastMap<HashSet<int>, HashSet<int>>(useCache: false);
	}

	[Benchmark]
	public Dictionary<string, SimpleDestination> DictionaryMapping_Cached()
	{
		return complexDictionary.FastMap<Dictionary<string, SimpleSource>, Dictionary<string, SimpleDestination>>(useCache: true);
	}

	[Benchmark]
	public Dictionary<string, SimpleDestination> DictionaryMapping_Uncached()
	{
		return complexDictionary.FastMap<Dictionary<string, SimpleSource>, Dictionary<string, SimpleDestination>>(useCache: false);
	}

	[Benchmark]
	public NestedDestination DeeplyNestedMapping_Cached()
	{
		return deeplyNestedSource.FastMap<NestedSource, NestedDestination>(useCache: true);
	}

	[Benchmark]
	public NestedDestination DeeplyNestedMapping_Uncached()
	{
		return deeplyNestedSource.FastMap<NestedSource, NestedDestination>(useCache: false);
	}

	[Benchmark]
	public int[] LargeArrayMapping_Cached()
	{
		return largeArray.FastMap<int[], int[]>(useCache: true);
	}

	[Benchmark]
	public int[] LargeArrayMapping_Uncached()
	{
		return largeArray.FastMap<int[], int[]>(useCache: false);
	}

	[Benchmark]
	public List<ComplexDestination> LargeComplexListMapping_Cached()
	{
		return largeComplexList.FastMap<List<ComplexSource>, List<ComplexDestination>>(useCache: true);
	}

	[Benchmark]
	public List<ComplexDestination> LargeComplexListMapping_Uncached()
	{
		return largeComplexList.FastMap<List<ComplexSource>, List<ComplexDestination>>(useCache: false);
	}

	[Benchmark]
	public ReadOnlyCollection<SimpleDestination> ReadOnlyCollectionMapping_Cached()
	{
		return readOnlyCollection.FastMap<IReadOnlyCollection<SimpleSource>, ReadOnlyCollection<SimpleDestination>>(useCache: true);
	}

	[Benchmark]
	public ReadOnlyCollection<SimpleDestination> ReadOnlyCollectionMapping_Uncached()
	{
		return readOnlyCollection.FastMap<IReadOnlyCollection<SimpleSource>, ReadOnlyCollection<SimpleDestination>>(useCache: false);
	}

	[Benchmark]
	public Queue<string> QueueMapping_Cached()
	{
		return complexSource.StringQueue.FastMap<Queue<string>, Queue<string>>(useCache: true);
	}

	[Benchmark]
	public Queue<string> QueueMapping_Uncached()
	{
		return complexSource.StringQueue.FastMap<Queue<string>, Queue<string>>(useCache: false);
	}

	[Benchmark]
	public Stack<double> StackMapping_Cached()
	{
		return complexSource.DoubleStack.FastMap<Stack<double>, Stack<double>>(useCache: true);
	}

	[Benchmark]
	public Stack<double> StackMapping_Uncached()
	{
		return complexSource.DoubleStack.FastMap<Stack<double>, Stack<double>>(useCache: false);
	}
}

public sealed class SimpleSource
{
	public required string StringProp { get; set; }
	public int IntProp { get; set; }
	public DateTime DateProp { get; set; }
	public double DoubleProp { get; set; }
	public bool BoolProp { get; set; }
	public Guid GuidProp { get; set; }
}

public sealed class SimpleDestination
{
	public required string StringProp { get; set; }
	public int IntProp { get; set; }
	public DateTime DateProp { get; set; }
	public double DoubleProp { get; set; }
	public bool BoolProp { get; set; }
	public Guid GuidProp { get; set; }
}

public sealed class ComplexSource
{
	public required string Name { get; set; }
	public required List<string> StringList { get; set; }
	public required Dictionary<string, int> Dictionary { get; set; }
	public required SimpleSource NestedObject { get; set; }
	public required HashSet<int> NumberSet { get; set; }
	public required Queue<string> StringQueue { get; set; }
	public required Stack<double> DoubleStack { get; set; }
}

public sealed class ComplexDestination
{
	public required string Name { get; set; }
	public required List<string> StringList { get; set; }
	public required Dictionary<string, int> Dictionary { get; set; }
	public required SimpleDestination NestedObject { get; set; }
	public required HashSet<int> NumberSet { get; set; }
	public required Queue<string> StringQueue { get; set; }
	public required Stack<double> DoubleStack { get; set; }
}

public sealed class NestedSource
{
	public required Level1 Level1 { get; set; }
}

public sealed class NestedDestination
{
	public required Level1Dest Level1 { get; set; }
}

public sealed class Level1
{
	public required string Name { get; set; }
	public required Level2 Level2 { get; set; }
}

public sealed class Level1Dest
{
	public required string Name { get; set; }
	public required Level2Dest Level2 { get; set; }
}

public sealed class Level2
{
	public required string Name { get; set; }
	public required Level3 Level3 { get; set; }
}

public sealed class Level2Dest
{
	public required string Name { get; set; }
	public required Level3Dest Level3 { get; set; }
}

public sealed class Level3
{
	public required string Name { get; set; }
	public required List<int> Values { get; set; }
}

public sealed class Level3Dest
{
	public required string Name { get; set; }
	public required List<int> Values { get; set; }
}
