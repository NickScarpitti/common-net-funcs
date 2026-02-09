using System;
using System.Collections.Generic;
using System.Linq;
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Jobs;
using CommonNetFuncs.FastMap;
using Mapster;
using Microsoft.VSDiagnostics;

// using Microsoft.VSDiagnostics; // For CPUUsageDiagnoser

namespace BenchmarkSuite;

/// <summary>
/// Comprehensive benchmarks comparing FastMapper, FasterMapper, Mapster, and Mapperly.
/// Measures speed, CPU efficiency, and memory usage across various mapping scenarios.
/// </summary>
[MediumRunJob(RuntimeMoniker.Net10_0)]
[MemoryDiagnoser]
[EventPipeProfiler(EventPipeProfile.CpuSampling)]
[CPUUsageDiagnoser] // Only works on Windows
public class MapperComparisonBenchmarks
{
	private SimpleSource simpleSource = null!;
	private ComplexSource complexSource = null!;
	private List<SimpleSource> simpleList = null!;
	private NestedSource deeplyNestedSource = null!;

	[GlobalSetup]
	public void Setup()
	{
		//Configure Mapster (global configuration)
		TypeAdapterConfig.GlobalSettings.NewConfig<SimpleSource, SimpleDestination>();
		TypeAdapterConfig.GlobalSettings.NewConfig<ComplexSource, ComplexDestination>();
		TypeAdapterConfig.GlobalSettings.NewConfig<NestedSource, NestedDestination>();
		TypeAdapterConfig.GlobalSettings.NewConfig<Level1, Level1Dest>();
		TypeAdapterConfig.GlobalSettings.NewConfig<Level2, Level2Dest>();
		TypeAdapterConfig.GlobalSettings.NewConfig<Level3, Level3Dest>();
		TypeAdapterConfig.GlobalSettings.Compile();

		// Initialize test data
		simpleSource = new SimpleSource
		{
			StringProp = "Test String Value",
			IntProp = 42,
			DateProp = DateTime.Now,
			DoubleProp = 3.14159,
			BoolProp = true,
			GuidProp = Guid.NewGuid()
		};

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

		simpleList = Enumerable.Range(0, 100).Select(i => new SimpleSource
		{
			StringProp = $"String {i}",
			IntProp = i,
			DateProp = DateTime.Now.AddDays(i),
			DoubleProp = i * 1.5,
			BoolProp = i % 2 == 0,
			GuidProp = Guid.NewGuid()
		}).ToList();

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

		// Clear FasterMapper cache
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.CacheManager.SetUseLimitedCache(false);
	}

	[GlobalCleanup]
	public static void Cleanup()
	{
		FastMapper.CacheManager.ClearAllCaches();
		FastMapper.ClearCache();
	}

	#region Simple Object Mapping

	[Benchmark(Description = "Simple - FastMapper")]
	public SimpleDestination SimpleMapping_FastMapper()
	{
		return simpleSource.FastMap<SimpleSource, SimpleDestination>(true);
	}

	[Benchmark(Description = "Simple - FasterMapper")]
	public SimpleDestination SimpleMapping_FasterMapper()
	{
		return simpleSource.FastMap<SimpleSource, SimpleDestination>();
	}

	[Benchmark(Description = "Simple - Mapster")]
	public SimpleDestination SimpleMapping_Mapster()
	{
		return simpleSource.Adapt<SimpleDestination>();
	}

	[Benchmark(Description = "Simple - Mapperly")]
	public SimpleDestination SimpleMapping_Mapperly()
	{
		return MapperlyMappers.MapSimpleSourceToDestination(simpleSource);
	}

	[Benchmark(Description = "Simple - Manual")]
	public SimpleDestination SimpleMapping_Manual()
	{
		return new SimpleDestination
		{
			StringProp = simpleSource.StringProp,
			IntProp = simpleSource.IntProp,
			DateProp = simpleSource.DateProp,
			DoubleProp = simpleSource.DoubleProp,
			BoolProp = simpleSource.BoolProp,
			GuidProp = simpleSource.GuidProp
		};
	}

	#endregion

	#region Complex Object Mapping

	[Benchmark(Description = "Complex - FastMapper")]
	public ComplexDestination ComplexMapping_FastMapper()
	{
		return complexSource.FastMap<ComplexSource, ComplexDestination>(true);
	}

	[Benchmark(Description = "Complex - FasterMapper")]
	public ComplexDestination ComplexMapping_FasterMapper()
	{
		return complexSource.FastMap<ComplexSource, ComplexDestination>();
	}

	[Benchmark(Description = "Complex - Mapster")]
	public ComplexDestination ComplexMapping_Mapster()
	{
		return complexSource.Adapt<ComplexDestination>();
	}

	[Benchmark(Description = "Complex - Mapperly")]
	public ComplexDestination ComplexMapping_Mapperly()
	{
		return MapperlyMappers.MapComplexSourceToDestination(complexSource);
	}

	#endregion

	#region List Mapping

	[Benchmark(Description = "List - FastMapper")]
	public List<SimpleDestination> ListMapping_FastMapper()
	{
		return simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>(true);
	}

	[Benchmark(Description = "List - FasterMapper")]
	public List<SimpleDestination> ListMapping_FasterMapper()
	{
		return simpleList.FastMap<List<SimpleSource>, List<SimpleDestination>>();
	}

	[Benchmark(Description = "List - Mapster")]
	public List<SimpleDestination> ListMapping_Mapster()
	{
		return simpleList.Adapt<List<SimpleDestination>>();
	}

	[Benchmark(Description = "List - Mapperly")]
	public List<SimpleDestination> ListMapping_Mapperly()
	{
		return MapperlyMappers.MapSimpleSourceList(simpleList);
	}

	#endregion

	#region Deeply Nested Mapping

	[Benchmark(Description = "Nested - FastMapper")]
	public NestedDestination NestedMapping_FastMapper()
	{
		return deeplyNestedSource.FastMap<NestedSource, NestedDestination>(true);
	}

	[Benchmark(Description = "Nested - FasterMapper")]
	public NestedDestination NestedMapping_FasterMapper()
	{
		return deeplyNestedSource.FastMap<NestedSource, NestedDestination>();
	}

	[Benchmark(Description = "Nested - Mapster")]
	public NestedDestination NestedMapping_Mapster()
	{
		return deeplyNestedSource.Adapt<NestedDestination>();
	}

	[Benchmark(Description = "Nested - Mapperly")]
	public NestedDestination NestedMapping_Mapperly()
	{
		return MapperlyMappers.MapNestedSourceToDestination(deeplyNestedSource);
	}

	#endregion
}

/// <summary>
/// Mapperly mapper using compile-time source generation.
/// The [Mapper] attribute triggers source generation to create the implementations.
/// </summary>
[Riok.Mapperly.Abstractions.Mapper]
public static partial class MapperlyMappers
{
	public static partial SimpleDestination MapSimpleSourceToDestination(SimpleSource source);
	public static partial ComplexDestination MapComplexSourceToDestination(ComplexSource source);
	public static partial List<SimpleDestination> MapSimpleSourceList(List<SimpleSource> source);
	public static partial NestedDestination MapNestedSourceToDestination(NestedSource source);
}
