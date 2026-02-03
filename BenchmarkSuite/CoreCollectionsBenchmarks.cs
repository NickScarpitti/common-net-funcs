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
	private List<int> _intList = null!;
	private List<string> _stringList = null!;
	private ConcurrentBag<int> _concurrentBag = null!;
	private int[] _intArray = null!;
	private Dictionary<int, string> _dictionary = null!;

	[GlobalSetup]
	public void Setup()
	{
		_intList = Enumerable.Range(0, 100).ToList();
		_stringList = Enumerable.Range(0, 100).Select(i => $"Item_{i}").ToList();
		_concurrentBag = new ConcurrentBag<int>(_intList);
		_intArray = _intList.ToArray();
		_dictionary = _intList.ToDictionary(i => i, i => $"Value_{i}");
	}

	[Benchmark]
	public bool AnyFast_List()
	{
		return _intList.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_Array()
	{
		return _intArray.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_ConcurrentBag()
	{
		return _concurrentBag.AnyFast();
	}

	[Benchmark]
	public bool AnyFast_Dictionary()
	{
		return _dictionary.AnyFast();
	}

	[Benchmark]
	public List<string> SelectNonEmpty()
	{
		return _stringList.SelectNonEmpty().ToList();
	}

	[Benchmark]
	public List<int> SelectNonNull()
	{
		return _intList.SelectNonNull().ToList();
	}

	[Benchmark]
	public void SetValue()
	{
		_intList.SetValue(_ => { });
	}

	[Benchmark]
	public void AddRange_ConcurrentBag()
	{
		ConcurrentBag<int> bag = new();
		bag.AddRange(_intList);
	}

	[Benchmark]
	public void AddRange_HashSet()
	{
		HashSet<int> set = new();
		set.AddRange(_intList);
	}

	[Benchmark]
	public static List<int> SingleToList()
	{
		return 42.SingleToList();
	}
}
