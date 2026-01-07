using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.DeepClone;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class DeepCloneBenchmarks
{
	private SimpleClass? _simpleObject;
	private ComplexClass? _complexObject;
	private NestedClass? _nestedObject;
	private int[]? _intArray;
	private List<string>? _stringList;
	private Dictionary<string, object>? _dictionary;

	[GlobalSetup]
	public void Setup()
	{
		// Simple object with basic types
		_simpleObject = new SimpleClass
		{
			Id = 42,
			Name = "Test Object",
			Value = 123.456,
			IsActive = true
		};

		// Complex object with various field types
		_complexObject = new ComplexClass
		{
			Id = 100,
			Name = "Complex Test",
			Numbers = new List<int> { 1, 2, 3, 4, 5 },
			Metadata = new Dictionary<string, string>
			{
				{ "key1", "value1" },
				{ "key2", "value2" },
				{ "key3", "value3" }
			},
			Nested = new SimpleClass { Id = 1, Name = "Nested", Value = 99.9, IsActive = false }
		};

		// Deeply nested object
		_nestedObject = new NestedClass
		{
			Level = 1,
			Child2 = new NestedClass
			{
				Level = 2,
				Child2 = new NestedClass
				{
					Level = 3,
					Child2 = new NestedClass
					{
						Level = 4,
						Child = null
					}
				}
			}
		};

		// Array
		_intArray = Enumerable.Range(0, 100).ToArray();

		// List
		_stringList = Enumerable.Range(0, 50).Select(i => $"Item_{i}").ToList();

		// Dictionary
		_dictionary = Enumerable.Range(0, 20).ToDictionary(i => $"key_{i}", i => (object)i);
	}

	[Benchmark]
	public SimpleClass? CloneSimpleObject()
	{
		return _simpleObject.DeepClone();
	}

	[Benchmark]
	public SimpleClass? CloneSimpleObjectNoCache()
	{
		return _simpleObject.DeepClone(useCache: false);
	}

	[Benchmark]
	public ComplexClass? CloneComplexObject()
	{
		return _complexObject.DeepClone();
	}

	[Benchmark]
	public ComplexClass? CloneComplexObjectNoCache()
	{
		return _complexObject.DeepClone(useCache: false);
	}

	[Benchmark]
	public NestedClass? CloneNestedObject()
	{
		return _nestedObject.DeepClone();
	}

	[Benchmark]
	public int[]? CloneArray()
	{
		return _intArray.DeepClone();
	}

	[Benchmark]
	public List<string>? CloneList()
	{
		return _stringList.DeepClone();
	}

	[Benchmark]
	public Dictionary<string, object>? CloneDictionary()
	{
		return _dictionary.DeepClone();
	}
}
