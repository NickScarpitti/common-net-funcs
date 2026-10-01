using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

#region Models

public class OfAddress
{
	public string? Street { get; set; }
	public string? City { get; set; }
	public string? PostalCode { get; set; }
	public string? Country { get; set; }
}

public class OfDepartment
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public decimal Budget { get; set; }
	public List<string>? Roles { get; set; }
}

public class OfPlant
{
	public int Id { get; set; }
	public string? Code { get; set; }
	public string? Name { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedDate { get; set; }
	public OfAddress? Address { get; set; }
	public List<OfDepartment>? Departments { get; set; }
	public Dictionary<string, string>? Settings { get; set; }
}

public class OfSupplier
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? ContactEmail { get; set; }
	public decimal Rating { get; set; }
	public OfAddress? Address { get; set; }
	public List<string>? Certifications { get; set; }
}

public class OfContainer
{
	public int Id { get; set; }
	public string? Code { get; set; }
	public double Length { get; set; }
	public double Width { get; set; }
	public double Height { get; set; }
	public bool IsActive { get; set; }
	public OfSupplier? Supplier { get; set; }
}

public class OfPartNumber
{
	public int Id { get; set; }
	public string? Number { get; set; }
	public string? Description { get; set; }
	public decimal Weight { get; set; }
	public List<string>? Attributes { get; set; }
}

public class OfPartGroup
{
	public int Id { get; set; }
	public string? Number { get; set; }
	public string? Name { get; set; }
	public List<OfPartNumber>? PartNumbers { get; set; }
	public OfPlant? Plant { get; set; }
}

public class OfPdr
{
	public int Id { get; set; }
	public int Revision { get; set; }
	public string? PartGroupNumber { get; set; }
	public string? Status { get; set; }
	public string? Notes { get; set; }
	public decimal Cost { get; set; }
	public bool IsTemp { get; set; }
	public DateTime ModifiedDate { get; set; }
	public OfPlant? Plant { get; set; }
	public OfSupplier? Supplier { get; set; }
	public List<OfContainer>? Containers { get; set; }
	public Dictionary<string, string>? Options { get; set; }
}

#endregion

/// <summary>
/// Compares the "ObjectFill + TaskGroup" pattern with the vanilla "Task&lt;T&gt; + Task.WhenAll" pattern.
/// The simulated data source yields once before returning pre-built data, so both patterns pay the same data/async-hop cost
/// and the diff is purely the pattern overhead (extra state machines, locks, copies, task list).
/// </summary>
[MemoryDiagnoser]
public class CoreAsyncObjectFillPatternBenchmarks
{
	private List<OfPlant> plants = null!;
	private List<OfSupplier> suppliers = null!;
	private List<OfContainer> containers = null!;
	private List<string> naOptions = null!;
	private List<string> plantOptions = null!;
	private OfPdr pdr = null!;
	private OfPartGroup partGroup = null!;
	private OfPartNumber partNumber = null!;

	[Params(10, 1000)]
	public int ItemCount { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		plants = OfData.CreatePlants(ItemCount);
		suppliers = OfData.CreateSuppliers(ItemCount);
		containers = OfData.CreateContainers(ItemCount);
		naOptions = OfData.CreateStrings(ItemCount);
		plantOptions = OfData.CreateStrings(ItemCount);
		pdr = OfData.CreatePdr(containers);
		partGroup = OfData.CreatePartGroup();
		partNumber = OfData.CreatePartNumber();
	}

	private static async Task<T> Fetch<T>(T value)
	{
		await Task.Yield();
		return value;
	}

	// Pattern 1: two list results

	[Benchmark(Baseline = true, Description = "WhenAll - 2 lists")]
	public async Task<int> WhenAll_TwoLists()
	{
		Task<List<OfPlant>> plantsTask = Fetch(plants);
		Task<List<OfSupplier>> suppliersTask = Fetch(suppliers);

		await Task.WhenAll(plantsTask, suppliersTask);

		return plantsTask.Result.Count + suppliersTask.Result.Count;
	}

	[Benchmark(Description = "ObjectFill + TaskGroup - 2 lists")]
	public async Task<int> ObjectFillTaskGroup_TwoLists()
	{
		List<OfPlant> plantsResult = [];
		List<OfSupplier> suppliersResult = [];

		TaskGroup taskGroup = new(
		[
			plantsResult.ObjectFill(Fetch(plants)),
			suppliersResult.ObjectFill(Fetch(suppliers))
		]);
		await taskGroup.RunTasks();

		return plantsResult.Count + suppliersResult.Count;
	}

	[Benchmark(Description = "ObjectFill + Task.WhenAll - 2 lists")]
	public async Task<int> ObjectFillWhenAll_TwoLists()
	{
		List<OfPlant> plantsResult = [];
		List<OfSupplier> suppliersResult = [];

		await Task.WhenAll(plantsResult.ObjectFill(Fetch(plants)), suppliersResult.ObjectFill(Fetch(suppliers)));

		return plantsResult.Count + suppliersResult.Count;
	}

	// Pattern 2: six mixed results (single objects, lists, string lists)

	[Benchmark(Description = "WhenAll - 6 mixed")]
	public async Task<int> WhenAll_SixMixed()
	{
		Task<OfPdr> pdrTask = Fetch(pdr);
		Task<OfPartGroup> partGroupTask = Fetch(partGroup);
		Task<List<OfContainer>> containersTask = Fetch(containers);
		Task<OfPartNumber> partNumberTask = Fetch(partNumber);
		Task<List<string>> naCommonTask = Fetch(naOptions);
		Task<List<string>> plantCommonTask = Fetch(plantOptions);

		await Task.WhenAll(pdrTask, partGroupTask, containersTask, partNumberTask, naCommonTask, plantCommonTask);

		return pdrTask.Result.Id + partGroupTask.Result.Id + containersTask.Result.Count + partNumberTask.Result.Id + naCommonTask.Result.Count + plantCommonTask.Result.Count;
	}

	[Benchmark(Description = "ObjectFill + TaskGroup - 6 mixed")]
	public async Task<int> ObjectFillTaskGroup_SixMixed()
	{
		OfPdr pdrResult = new();
		OfPartGroup partGroupResult = new();
		List<OfContainer> containersResult = [];
		OfPartNumber partNumberResult = new();
		List<string> naCommonResult = [];
		List<string> plantCommonResult = [];

		TaskGroup taskGroup = new(
		[
			pdrResult.ObjectFill(Fetch(pdr)),
			partGroupResult.ObjectFill(Fetch(partGroup)),
			containersResult.ObjectFill(Fetch(containers)),
			partNumberResult.ObjectFill(Fetch(partNumber)),
			naCommonResult.ObjectFill(Fetch(naOptions)),
			plantCommonResult.ObjectFill(Fetch(plantOptions))
		]);
		await taskGroup.RunTasks();

		return pdrResult.Id + partGroupResult.Id + containersResult.Count + partNumberResult.Id + naCommonResult.Count + plantCommonResult.Count;
	}
}

/// <summary>
/// Compares each ObjectFill target type against awaiting the task and using the result directly.
/// </summary>
[MemoryDiagnoser]
public class CoreAsyncObjectFillFamilyBenchmarks
{
	private List<OfPlant> plants = null!;
	private HashSet<OfPlant> plantSet = null!;
	private OfPdr pdr = null!;
	private DataTable templateTable = null!;
	private byte[] streamBytes = null!;
	private readonly SemaphoreSlim semaphore = new(4);

	[Params(10, 1000)]
	public int ItemCount { get; set; }

	[GlobalSetup]
	public void Setup()
	{
		plants = OfData.CreatePlants(ItemCount);
		plantSet = [.. plants];
		pdr = OfData.CreatePdr(OfData.CreateContainers(10));

		templateTable = new DataTable();
		templateTable.Columns.Add("Id", typeof(int));
		templateTable.Columns.Add("Name", typeof(string));
		templateTable.Columns.Add("Value", typeof(decimal));
		for (int i = 0; i < ItemCount; i++)
		{
			templateTable.Rows.Add(i, $"Row {i}", i * 1.5m);
		}

		streamBytes = new byte[ItemCount * 256];
		new System.Random(42).NextBytes(streamBytes);
	}

	[GlobalCleanup]
	public void Cleanup()
	{
		templateTable.Dispose();
		semaphore.Dispose();
	}

	private static async Task<T> Fetch<T>(T value)
	{
		await Task.Yield();
		return value;
	}

	private async Task<DataTable> FetchTable()
	{
		await Task.Yield();
		return templateTable.Copy();
	}

	private async Task<MemoryStream> FetchStream()
	{
		await Task.Yield();
		return new MemoryStream(streamBytes);
	}

	// Single object (copy properties into existing instance)

	[Benchmark(Baseline = true, Description = "Object - await")]
	public async Task<int> Object_Await()
	{
		OfPdr result = await Fetch(pdr);
		return result.Id;
	}

	[Benchmark(Description = "Object - ObjectFill")]
	public async Task<int> Object_ObjectFill()
	{
		OfPdr result = new();
		await result.ObjectFill(Fetch(pdr));
		return result.Id;
	}

	[Benchmark(Description = "Object - ObjectFill (Func)")]
	public async Task<int> Object_ObjectFillFunc()
	{
		OfPdr result = new();
		await result.ObjectFill(() => Fetch(pdr));
		return result.Id;
	}

	// List

	[Benchmark(Description = "List - await")]
	public async Task<int> List_Await()
	{
		List<OfPlant> result = await Fetch(plants);
		return result.Count;
	}

	[Benchmark(Description = "List - ObjectFill")]
	public async Task<int> List_ObjectFill()
	{
		List<OfPlant> result = [];
		await result.ObjectFill(Fetch(plants));
		return result.Count;
	}

	[Benchmark(Description = "List - ObjectFill (Func)")]
	public async Task<int> List_ObjectFillFunc()
	{
		List<OfPlant> result = [];
		await result.ObjectFill(() => Fetch(plants));
		return result.Count;
	}

	[Benchmark(Description = "List - ObjectFill (Func + semaphore)")]
	public async Task<int> List_ObjectFillFuncSemaphore()
	{
		List<OfPlant> result = [];
		await result.ObjectFill(() => Fetch(plants), semaphore);
		return result.Count;
	}

	[Benchmark(Description = "List - manual semaphore + await")]
	public async Task<int> List_ManualSemaphoreAwait()
	{
		await semaphore.WaitAsync();
		try
		{
			List<OfPlant> result = await Fetch(plants);
			return result.Count;
		}
		finally
		{
			semaphore.Release();
		}
	}

	// HashSet

	[Benchmark(Description = "HashSet - await")]
	public async Task<int> HashSet_Await()
	{
		HashSet<OfPlant> result = await Fetch(plantSet);
		return result.Count;
	}

	[Benchmark(Description = "HashSet - ObjectFill")]
	public async Task<int> HashSet_ObjectFill()
	{
		HashSet<OfPlant> result = [];
		await result.ObjectFill(Fetch(plantSet));
		return result.Count;
	}

	// ConcurrentBag

	[Benchmark(Description = "ConcurrentBag - await")]
	public async Task<int> ConcurrentBag_Await()
	{
		List<OfPlant> result = await Fetch(plants);
		return result.Count;
	}

	[Benchmark(Description = "ConcurrentBag - ObjectFill")]
	public async Task<int> ConcurrentBag_ObjectFill()
	{
		ConcurrentBag<OfPlant> result = [];
		await result.ObjectFill(Fetch(plants));
		return result.Count;
	}

	// ConcurrentDictionary (single keyed entry)

	[Benchmark(Description = "ConcurrentDictionary - await")]
	public async Task<int> ConcurrentDictionary_Await()
	{
		ConcurrentDictionary<int, OfPlant?> result = [];
		result[1] = await Fetch(plants[0]);
		return result.Count;
	}

	[Benchmark(Description = "ConcurrentDictionary - ObjectFill")]
	public async Task<int> ConcurrentDictionary_ObjectFill()
	{
		ConcurrentDictionary<int, OfPlant?> result = [];
		await result.ObjectFill(1, Fetch(plants[0]));
		return result.Count;
	}

	// DataTable

	[Benchmark(Description = "DataTable - await")]
	public async Task<int> DataTable_Await()
	{
		using DataTable result = await FetchTable();
		return result.Rows.Count;
	}

	[Benchmark(Description = "DataTable - ObjectFill")]
	public async Task<int> DataTable_ObjectFill()
	{
		using DataTable result = new();
		await result.ObjectFill(FetchTable());
		return result.Rows.Count;
	}

	// MemoryStream

	[Benchmark(Description = "MemoryStream - await")]
	public async Task<long> MemoryStream_Await()
	{
		await using MemoryStream result = await FetchStream();
		return result.Length;
	}

	[Benchmark(Description = "MemoryStream - ObjectFill")]
	public async Task<long> MemoryStream_ObjectFill()
	{
		await using MemoryStream result = new();
		await result.ObjectFill(FetchStream());
		return result.Length;
	}
}

internal static class OfData
{
	public static List<OfPlant> CreatePlants(int count)
	{
		List<OfPlant> list = new(count);
		for (int i = 0; i < count; i++)
		{
			list.Add(new OfPlant
			{
				Id = i,
				Code = $"P{i:D4}",
				Name = $"Plant {i}",
				IsActive = i % 2 == 0,
				CreatedDate = DateTime.UnixEpoch.AddDays(i),
				Address = new OfAddress { Street = $"{i} Main St", City = "Springfield", PostalCode = $"{10000 + i}", Country = "US" },
				Departments =
				[
					new OfDepartment { Id = i * 10, Name = "Assembly", Budget = 1000m * i, Roles = ["Operator", "Lead"] },
					new OfDepartment { Id = (i * 10) + 1, Name = "Quality", Budget = 500m * i, Roles = ["Inspector"] }
				],
				Settings = new Dictionary<string, string> { ["Region"] = "NA", ["Shift"] = "Day", ["Tier"] = $"{i % 3}" }
			});
		}
		return list;
	}

	public static List<OfSupplier> CreateSuppliers(int count)
	{
		List<OfSupplier> list = new(count);
		for (int i = 0; i < count; i++)
		{
			list.Add(new OfSupplier
			{
				Id = i,
				Name = $"Supplier {i}",
				ContactEmail = $"supplier{i}@example.com",
				Rating = i % 5,
				Address = new OfAddress { Street = $"{i} Industrial Way", City = "Detroit", PostalCode = $"{48000 + i}", Country = "US" },
				Certifications = ["ISO9001", "IATF16949"]
			});
		}
		return list;
	}

	public static List<OfContainer> CreateContainers(int count)
	{
		List<OfSupplier> supplierPool = CreateSuppliers(Math.Max(1, Math.Min(count, 10)));
		List<OfContainer> list = new(count);
		for (int i = 0; i < count; i++)
		{
			list.Add(new OfContainer
			{
				Id = i,
				Code = $"C{i:D4}",
				Length = 10 + i,
				Width = 5 + i,
				Height = 3 + i,
				IsActive = true,
				Supplier = supplierPool[i % supplierPool.Count]
			});
		}
		return list;
	}

	public static List<string> CreateStrings(int count)
	{
		List<string> list = new(count);
		for (int i = 0; i < count; i++)
		{
			list.Add($"Option {i}");
		}
		return list;
	}

	public static OfPartNumber CreatePartNumber()
	{
		return new OfPartNumber { Id = 7, Number = "PN-0007", Description = "Test part", Weight = 12.5m, Attributes = ["Fragile", "Stackable"] };
	}

	public static OfPartGroup CreatePartGroup()
	{
		return new OfPartGroup
		{
			Id = 3,
			Number = "PG-0003",
			Name = "Part group",
			PartNumbers = [CreatePartNumber(), CreatePartNumber()],
			Plant = CreatePlants(1)[0]
		};
	}

	public static OfPdr CreatePdr(List<OfContainer> containers)
	{
		return new OfPdr
		{
			Id = 42,
			Revision = 5,
			PartGroupNumber = "PG-0003",
			Status = "Active",
			Notes = "Benchmark PDR",
			Cost = 123.45m,
			IsTemp = true,
			ModifiedDate = DateTime.UnixEpoch,
			Plant = CreatePlants(1)[0],
			Supplier = CreateSuppliers(1)[0],
			Containers = containers,
			Options = new Dictionary<string, string> { ["A"] = "1", ["B"] = "2" }
		};
	}
}
