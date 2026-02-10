using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Order;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[Orderer(SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
[SimpleJob(RuntimeMoniker.Net10_0)]
public class CopyBenchmarks
{
	private SimpleClass? simpleSource;
	private ComplexClass? complexSource;
	private NestedClass? nestedSource;
	private List<SimpleClass>? listSource;
	private Dictionary<string, SimpleClass>? dictSource;

	[GlobalSetup]
	public void Setup()
	{
		simpleSource = new SimpleClass
		{
			Id = 1,
			Name = "Test",
			Value = 42.5,
			IsActive = true,
			CreatedDate = DateTime.Now
		};

		complexSource = new ComplexClass
		{
			Id = 1,
			Title = "Complex Test",
			Description = "This is a complex test object",
			Count = 100,
			Price = 99.99m,
			IsEnabled = true,
			Tags = new List<string> { "tag1", "tag2", "tag3" },
			Metadata = new Dictionary<string, string> { { "key1", "value1" }, { "key2", "value2" } }
		};

		nestedSource = new NestedClass
		{
			Id = 1,
			Name = "Nested Test",
			Child = new SimpleClass
			{
				Id = 2,
				Name = "Child",
				Value = 10.5,
				IsActive = true,
				CreatedDate = DateTime.Now
			},
			Children = new List<SimpleClass>
			{
				new() { Id = 3, Name = "Child1", Value = 20.0, IsActive = true },
				new() { Id = 4, Name = "Child2", Value = 30.0, IsActive = false }
			}
		};

		listSource = new List<SimpleClass>();
		for (int i = 0; i < 100; i++)
		{
			listSource.Add(new SimpleClass
			{
				Id = i,
				Name = $"Item{i}",
				Value = i * 1.5,
				IsActive = i % 2 == 0,
				CreatedDate = DateTime.Now.AddDays(-i)
			});
		}

		dictSource = new Dictionary<string, SimpleClass>();
		for (int i = 0; i < 50; i++)
		{
			dictSource[$"key{i}"] = new SimpleClass
			{
				Id = i,
				Name = $"DictItem{i}",
				Value = i * 2.5,
				IsActive = i % 2 == 0,
				CreatedDate = DateTime.Now.AddDays(-i)
			};
		}
	}

	// Shallow Copy Benchmarks
	[Benchmark]
	public SimpleClass ShallowCopy_Simple_Cached()
	{
		return simpleSource!.CopyPropertiesToNew(useCache: true);
	}

	[Benchmark]
	public SimpleClass ShallowCopy_Simple_Uncached()
	{
		return simpleSource!.CopyPropertiesToNew(useCache: false);
	}

	[Benchmark]
	public ComplexClass ShallowCopy_Complex_Cached()
	{
		return complexSource!.CopyPropertiesToNew(useCache: true);
	}

	[Benchmark]
	public ComplexClass ShallowCopy_Complex_Uncached()
	{
		return complexSource!.CopyPropertiesToNew(useCache: false);
	}

	[Benchmark]
	public SimpleClassDto ShallowCopyDifferentType_Cached()
	{
		return simpleSource!.CopyPropertiesToNew<SimpleClass, SimpleClassDto>(useCache: true);
	}

	[Benchmark]
	public SimpleClassDto ShallowCopyDifferentType_Uncached()
	{
		return simpleSource!.CopyPropertiesToNew<SimpleClass, SimpleClassDto>(useCache: false);
	}

	// Recursive Copy Benchmarks
	[Benchmark]
	public NestedClass RecursiveCopy_Nested_Cached()
	{
		return nestedSource!.CopyPropertiesToNewRecursive<NestedClass, NestedClass>(maxDepth: -1, useCache: true);
	}

	[Benchmark]
	public NestedClass RecursiveCopy_Nested_Uncached()
	{
		return nestedSource!.CopyPropertiesToNewRecursive<NestedClass, NestedClass>(maxDepth: -1, useCache: false);
	}

	[Benchmark]
	public NestedClass RecursiveCopy_Nested_Depth1_Cached()
	{
		return nestedSource!.CopyPropertiesToNewRecursive<NestedClass, NestedClass>(maxDepth: 1, useCache: true);
	}

	[Benchmark]
	public NestedClass RecursiveCopy_Nested_Depth1_Uncached()
	{
		return nestedSource!.CopyPropertiesToNewRecursive<NestedClass, NestedClass>(maxDepth: 1, useCache: false);
	}

	// Collection Copy Benchmarks
	[Benchmark]
	public List<SimpleClass> RecursiveCopy_List_Cached()
	{
		return listSource!.CopyPropertiesToNewRecursive<List<SimpleClass>, List<SimpleClass>>(maxDepth: -1, useCache: true);
	}

	[Benchmark]
	public List<SimpleClass> RecursiveCopy_List_Uncached()
	{
		return listSource!.CopyPropertiesToNewRecursive<List<SimpleClass>, List<SimpleClass>>(maxDepth: -1, useCache: false);
	}

	[Benchmark]
	public Dictionary<string, SimpleClass> RecursiveCopy_Dictionary_Cached()
	{
		return dictSource!.CopyPropertiesToNewRecursive<Dictionary<string, SimpleClass>, Dictionary<string, SimpleClass>>(maxDepth: -1, useCache: true);
	}

	[Benchmark]
	public Dictionary<string, SimpleClass> RecursiveCopy_Dictionary_Uncached()
	{
		return dictSource!.CopyPropertiesToNewRecursive<Dictionary<string, SimpleClass>, Dictionary<string, SimpleClass>>(maxDepth: -1, useCache: false);
	}

	// CopyPropertiesTo Benchmarks
	[Benchmark]
	public void CopyPropertiesTo_Simple_Cached()
	{
		SimpleClass dest = new();
		simpleSource!.CopyPropertiesTo(dest, useCache: true);
	}

	[Benchmark]
	public void CopyPropertiesTo_Simple_Uncached()
	{
		SimpleClass dest = new();
		simpleSource!.CopyPropertiesTo(dest, useCache: false);
	}
}
