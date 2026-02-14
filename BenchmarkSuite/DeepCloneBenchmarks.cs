using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.DeepClone;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[MinColumn, MaxColumn, MeanColumn, MedianColumn]
public class DeepCloneBenchmarks
{
	private SimpleClass? simpleObject;
	private ComplexClass? complexObject;
	private NestedClass? nestedObject;
	private int[]? intArray;
	private List<string>? stringList;
	private Dictionary<string, object>? dictionary;

	[GlobalSetup]
	public void Setup()
	{
		// Simple object with basic types
		simpleObject = new SimpleClass
		{
			Id = 42,
			Name = "Test Object",
			Value = 123.456,
			IsActive = true
		};

		// Complex object with various field types
		complexObject = new ComplexClass
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
		nestedObject = new NestedClass
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
		intArray = Enumerable.Range(0, 100).ToArray();

		// List
		stringList = Enumerable.Range(0, 50).Select(i => $"Item_{i}").ToList();

		// Dictionary
		dictionary = Enumerable.Range(0, 20).ToDictionary(i => $"key_{i}", i => (object)i);
	}

	[Benchmark]
	public SimpleClass? CloneSimpleObject()
	{
		return simpleObject.DeepClone();
	}

	[Benchmark]
	public SimpleClass? CloneSimpleObjectNoCache()
	{
		return simpleObject.DeepClone(useCache: false);
	}

	[Benchmark]
	public ComplexClass? CloneComplexObject()
	{
		return complexObject.DeepClone();
	}

	[Benchmark]
	public ComplexClass? CloneComplexObjectNoCache()
	{
		return complexObject.DeepClone(useCache: false);
	}

	[Benchmark]
	public NestedClass? CloneNestedObject()
	{
		return nestedObject.DeepClone();
	}

	[Benchmark]
	public int[]? CloneArray()
	{
		return intArray.DeepClone();
	}

	[Benchmark]
	public List<string>? CloneList()
	{
		return stringList.DeepClone();
	}

	[Benchmark]
	public Dictionary<string, object>? CloneDictionary()
	{
		return dictionary.DeepClone();
	}
}
