using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreCollectionsBenchmarks
{
	private List<int> intList = null!;
	private List<string> stringList = null!;
	private ConcurrentBag<int> concurrentBag = null!;
	private int[] intArray = null!;
	private Dictionary<int, string> dictionary = null!;

	[GlobalSetup]
	public void Setup()
	{
		intList = Enumerable.Range(0, 100).ToList();
		stringList = Enumerable.Range(0, 100).Select(i => $"Item_{i}").ToList();
		concurrentBag = new ConcurrentBag<int>(intList);
		intArray = intList.ToArray();
		dictionary = intList.ToDictionary(i => i, i => $"Value_{i}");
	}

	[Benchmark]
	public bool AnyFast_List()
	{
		return intList.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_Array()
	{
		return intArray.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_ConcurrentBag()
	{
		return concurrentBag.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_Dictionary()
	{
		return dictionary.AnyFast();
	}

	[Benchmark]
	public List<string> SelectNonEmpty()
	{
		return stringList.SelectNonEmpty().ToList();
	}

	[Benchmark]
	public List<int> SelectNonNull()
	{
		return intList.SelectNonNull().ToList();
	}

	[Benchmark]
	public void SetValue()
	{
		intList.SetValue(_ => { });
	}

	[Benchmark]
	public void AddRange_ConcurrentBag()
	{
		ConcurrentBag<int> bag = new();
		bag.AddRange(intList);
	}

	[Benchmark]
	public void AddRange_HashSet()
	{
		HashSet<int> set = new();
		set.AddRange(intList);
	}

	[Benchmark]
	public static List<int> SingleToList()
	{
		return 42.SingleToList();
	}
}
