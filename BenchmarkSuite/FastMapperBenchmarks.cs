// #nullable enable
// using System;
// using System.Collections.Generic;
// using System.Collections.ObjectModel;
// using System.Linq;
// using BenchmarkDotNet.Attributes;
// using BenchmarkDotNet.Diagnosers;
// using BenchmarkDotNet.Jobs;
// using CommonNetFuncs.FastMap;
// using Microsoft.VSDiagnostics;

namespace BenchmarkSuite;

// [MediumRunJob(RuntimeMoniker.Net10_0)]
// [EventPipeProfiler(EventPipeProfile.CpuSampling)]
// [CPUUsageDiagnoser]
// public class FastMapperBenchmarks
// {
// 	private SimpleSource _simpleSource = null!;
// 	private ComplexSource _complexSource = null!;
// 	private List<SimpleSource> _simpleList = null!;
// 	private SimpleSource[] _simpleArray = null!;
// 	private Dictionary<string, SimpleSource> _complexDictionary = null!;
// 	private NestedSource _deeplyNestedSource = null!;
// 	private int[] _largeArray = null!;
// 	private List<ComplexSource> _largeComplexList = null!;
// 	private IReadOnlyCollection<SimpleSource> _readOnlyCollection = null!;
// 	[GlobalSetup]
// 	public void Setup()
// 	{
// 		// Simple source for basic property mapping
// 		_simpleSource = new SimpleSource
// 		{
// 			StringProp = "Test String Value",
// 			IntProp = 42,
// 			DateProp = DateTime.Now,
// 			DoubleProp = 3.14159,
// 			BoolProp = true,
// 			GuidProp = Guid.NewGuid()
// 		};
// 		// Complex source with nested objects and collections
// 		_complexSource = new ComplexSource
// 		{
// 			Name = "Complex Object",
// 			StringList = Enumerable.Range(0, 10).Select(i => $"Item {i}").ToList(),
// 			Dictionary = Enumerable.Range(0, 10).ToDictionary(i => $"Key{i}", i => i),
// 			NestedObject = new SimpleSource
// 			{
// 				StringProp = "Nested String",
// 				IntProp = 100,
// 				DateProp = DateTime.Now,
// 				DoubleProp = 2.71828,
// 				BoolProp = false,
// 				GuidProp = Guid.NewGuid()
// 			},
// 			NumberSet = Enumerable.Range(0, 10).ToHashSet(),
// 			StringQueue = new Queue<string>(Enumerable.Range(0, 10).Select(i => $"Queue {i}")),
// 			DoubleStack = new Stack<double>(Enumerable.Range(0, 10).Select(i => (double)i))
// 		};
// 		// Simple list for collection mapping
// 		_simpleList = Enumerable.Range(0, 100).Select(i => new SimpleSource { StringProp = $"String {i}", IntProp = i, DateProp = DateTime.Now.AddDays(i), DoubleProp = i * 1.5, BoolProp = i % 2 == 0, GuidProp = Guid.NewGuid() }).ToList();
// 		// Initialize array from list
// 		_simpleArray = _simpleList.ToArray();
// 		// Initialize readonly collection
// 		_readOnlyCollection = _simpleList.AsReadOnly();
// 		// Complex dictionary with nested objects
// 		_complexDictionary = Enumerable.Range(0, 50).ToDictionary(i => $"Key{i}", i => new SimpleSource { StringProp = $"Dict String {i}", IntProp = i, DateProp = DateTime.Now, DoubleProp = i * 2.0, BoolProp = i % 3 == 0, GuidProp = Guid.NewGuid() });
// 		// Deeply nested source
// 		_deeplyNestedSource = new NestedSource
// 		{
// 			Level1 = new Level1
// 			{
// 				Name = "Level 1",
// 				Level2 = new Level2
// 				{
// 					Name = "Level 2",
// 					Level3 = new Level3
// 					{
// 						Name = "Level 3",
// 						Values = Enumerable.Range(0, 5).ToList()
// 					}
// 				}
// 			}
// 		};
// 		// Large array for array mapping
// 		_largeArray = Enumerable.Range(0, 1000).ToArray();
// 		// Large complex list
// 		_largeComplexList = Enumerable.Range(0, 500).Select(i => new ComplexSource { Name = $"Complex {i}", StringList = Enumerable.Range(0, 5).Select(j => $"Item {i}-{j}").ToList(), Dictionary = Enumerable.Range(0, 5).ToDictionary(j => $"Key{i}-{j}", j => j), NestedObject = new SimpleSource { StringProp = $"Nested {i}", IntProp = i, DateProp = DateTime.Now, DoubleProp = i * 1.1, BoolProp = i % 2 == 0, GuidProp = Guid.NewGuid() }, NumberSet = Enumerable.Range(0, 5).ToHashSet(), StringQueue = new Queue<string>(Enumerable.Range(0, 5).Select(j => $"Queue {i}-{j}")), DoubleStack = new Stack<double>(Enumerable.Range(0, 5).Select(j => (double)j)) }).ToList();
// 		// Clear cache before benchmarks
// 		FastMapper.CacheManager.ClearAllCaches();
// 		FastMapper.CacheManager.SetUseLimitedCache(false);
// 	}

// 	[GlobalCleanup]
// 	public void Cleanup()
// 	{
// 		FastMapper.CacheManager.ClearAllCaches();
// 	}

// 	[Benchmark(Baseline = true)]
// 	public SimpleDestination SimplePropertyMapping_Cached()
// 	{
// 		return _simpleSource.FastMap<SimpleSource, SimpleDestination>(useCache: true);
// 	}

// 	[Benchmark]
// 	public SimpleDestination SimplePropertyMapping_Uncached()
// 	{
// 		return _simpleSource.FastMap<SimpleSource, SimpleDestination>(useCache: false);
// 	}

// 	[Benchmark]
// 	public ComplexDestination ComplexObjectMapping_Cached()
// 	{
// 		return _complexSource.FastMap<ComplexSource, ComplexDestination>(useCache: true);
// 	}

// 	[Benchmark]
// 	public ComplexDestination ComplexObjectMapping_Uncached()
// 	{
// 		return _complexSource.FastMap<ComplexSource, ComplexDestination>(useCache: false);
// 	}

// 	[Benchmark]
// 	public List<SimpleDestination> ListMapping_Cached()
// 	{
// 		return _simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public List<SimpleDestination> ListMapping_Uncached()
// 	{
// 		return _simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public SimpleDestination[] ArrayMapping_Cached()
// 	{
// 		return _simpleArray.FastMap<SimpleSource[], SimpleDestination[]>(useCache: true);
// 	}

// 	[Benchmark]
// 	public SimpleDestination[] ArrayMapping_Uncached()
// 	{
// 		return _simpleArray.FastMap<SimpleSource[], SimpleDestination[]>(useCache: false);
// 	}

// 	[Benchmark]
// 	public HashSet<int> HashSetMapping_Cached()
// 	{
// 		return _complexSource.NumberSet.FastMap<HashSet<int>, HashSet<int>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public HashSet<int> HashSetMapping_Uncached()
// 	{
// 		return _complexSource.NumberSet.FastMap<HashSet<int>, HashSet<int>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public Dictionary<string, SimpleDestination> DictionaryMapping_Cached()
// 	{
// 		return _complexDictionary.FastMap<Dictionary<string, SimpleSource>, Dictionary<string, SimpleDestination>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public Dictionary<string, SimpleDestination> DictionaryMapping_Uncached()
// 	{
// 		return _complexDictionary.FastMap<Dictionary<string, SimpleSource>, Dictionary<string, SimpleDestination>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public NestedDestination DeeplyNestedMapping_Cached()
// 	{
// 		return _deeplyNestedSource.FastMap<NestedSource, NestedDestination>(useCache: true);
// 	}

// 	[Benchmark]
// 	public NestedDestination DeeplyNestedMapping_Uncached()
// 	{
// 		return _deeplyNestedSource.FastMap<NestedSource, NestedDestination>(useCache: false);
// 	}

// 	[Benchmark]
// 	public int[] LargeArrayMapping_Cached()
// 	{
// 		return _largeArray.FastMap<int[], int[]>(useCache: true);
// 	}

// 	[Benchmark]
// 	public int[] LargeArrayMapping_Uncached()
// 	{
// 		return _largeArray.FastMap<int[], int[]>(useCache: false);
// 	}

// 	[Benchmark]
// 	public List<ComplexDestination> LargeComplexListMapping_Cached()
// 	{
// 		return _largeComplexList.FastMap<List<ComplexSource>, List<ComplexDestination>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public List<ComplexDestination> LargeComplexListMapping_Uncached()
// 	{
// 		return _largeComplexList.FastMap<List<ComplexSource>, List<ComplexDestination>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public ReadOnlyCollection<SimpleDestination> ReadOnlyCollectionMapping_Cached()
// 	{
// 		return _readOnlyCollection.FastMap<IReadOnlyCollection<SimpleSource>, ReadOnlyCollection<SimpleDestination>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public ReadOnlyCollection<SimpleDestination> ReadOnlyCollectionMapping_Uncached()
// 	{
// 		return _readOnlyCollection.FastMap<IReadOnlyCollection<SimpleSource>, ReadOnlyCollection<SimpleDestination>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public Queue<string> QueueMapping_Cached()
// 	{
// 		return _complexSource.StringQueue.FastMap<Queue<string>, Queue<string>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public Queue<string> QueueMapping_Uncached()
// 	{
// 		return _complexSource.StringQueue.FastMap<Queue<string>, Queue<string>>(useCache: false);
// 	}

// 	[Benchmark]
// 	public Stack<double> StackMapping_Cached()
// 	{
// 		return _complexSource.DoubleStack.FastMap<Stack<double>, Stack<double>>(useCache: true);
// 	}

// 	[Benchmark]
// 	public Stack<double> StackMapping_Uncached()
// 	{
// 		return _complexSource.DoubleStack.FastMap<Stack<double>, Stack<double>>(useCache: false);
// 	}
// }

// Test model classes
using System;
using System.Collections.Generic;

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
