using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using ZLinq;

namespace BenchmarkSuite;

/// <summary>
/// Compares System.Linq against ZLinq for the LINQ chain shapes actually used across CommonNetFuncs
/// (Where+ToList, Where+Select+ToList, OrderBy+ToArray, Where+Max, Where+SelectMany+FirstOrDefault, Any/All,
/// Skip+First, Intersect+Count) across int, string, and custom reference-type (class/generic) sources
/// to decide whether adopting ZLinq is worth the churn before rolling it out broadly.
/// </summary>
[RankColumn]
[MemoryDiagnoser]
[MediumRunJob(RuntimeMoniker.Net10_0)]
public class ZLinqComparisonBenchmarks
{
	// Simple reference type used to verify ZLinq works with classes/generics, not just primitives.
	// Must be public: BenchmarkDotNet requires public benchmark methods, which requires a public return type here.
	public sealed class SampleItem
	{
		public int Id { get; init; }

		public string Name { get; init; } = null!;
	}

	[Params(10, 1000, 10000)]
	public int N;

	private List<int> intList = null!;
	private int[] intArray = null!;
	private List<string> stringList = null!;
	private string[] stringArray = null!;
	private List<SampleItem> itemList = null!;
	private SampleItem[] itemArray = null!;
	private List<List<int>> nestedIntLists = null!;
	private int[][] nestedIntArrays = null!;
	private List<List<string>> nestedStringLists = null!;
	private string[][] nestedStringArrays = null!;
	private List<List<SampleItem>> nestedItemLists = null!;
	private SampleItem[][] nestedItemArrays = null!;

	[GlobalSetup]
	public void Setup()
	{
		intList = Enumerable.Range(0, N).ToList();
		intArray = intList.ToArray();
		stringList = intList.Select(i => $"Item_{i}").ToList();
		stringArray = stringList.ToArray();
		itemList = intList.Select(i => new SampleItem { Id = i, Name = $"Item_{i}" }).ToList();
		itemArray = itemList.ToArray();
		nestedIntLists = intList.Select(i => Enumerable.Range(0, 5).Select(j => i * 5 + j).ToList()).ToList();
		nestedIntArrays = intList.Select(i => Enumerable.Range(0, 5).Select(j => i * 5 + j).ToArray()).ToArray();
		nestedStringLists = intList.Select(i => Enumerable.Range(0, 5).Select(j => $"Item_{i * 5 + j}").ToList()).ToList();
		nestedStringArrays = intList.Select(i => Enumerable.Range(0, 5).Select(j => $"Item_{i * 5 + j}").ToArray()).ToArray();
		nestedItemLists = intList.Select(i => Enumerable.Range(0, 5).Select(j => new SampleItem { Id = (i * 5) + j, Name = $"Item_{(i * 5) + j}" }).ToList()).ToList();
		nestedItemArrays = intList.Select(i => Enumerable.Range(0, 5).Select(j => new SampleItem { Id = (i * 5) + j, Name = $"Item_{(i * 5) + j}" }).ToArray()).ToArray();
	}

	#region Where_ToList

	[Benchmark(Baseline = true)]
	public List<int> Linq_ListInt_Where_ToList()
	{
		return intList.Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> ZLinq_ListInt_Where_ToList()
	{
		return intList.AsValueEnumerable().Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> Linq_ArrayInt_Where_ToList()
	{
		return intArray.Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> ZLinq_ArrayInt_Where_ToList()
	{
		return intArray.AsValueEnumerable().Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> Linq_ListString_Where_ToList()
	{
		return stringList.Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ListString_Where_ToList()
	{
		return stringList.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> Linq_NestedStringLists_Where_ToList()
	{
		return nestedStringLists.SelectMany(x => x).Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_NestedStringLists_Where_ToList()
	{
		return nestedStringLists.AsValueEnumerable().SelectMany(x => x).Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> Linq_NestedStringArrays_Where_ToList()
	{
		return nestedStringArrays.SelectMany(x => x).Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_NestedStringArrays_Where_ToList()
	{
		return nestedStringArrays.AsValueEnumerable().SelectMany(x => x).Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> Linq_NestedIntLists_Where_ToList()
	{
		return nestedIntLists.SelectMany(x => x).Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> ZLinq_NestedIntLists_Where_ToList()
	{
		return nestedIntLists.AsValueEnumerable().SelectMany(x => x).Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> Linq_NestedIntArrays_Where_ToList()
	{
		return nestedIntArrays.SelectMany(x => x).Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<int> ZLinq_NestedIntArrays_Where_ToList()
	{
		return nestedIntArrays.AsValueEnumerable().SelectMany(x => x).Where(x => x % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> Linq_ArrayString_Where_ToList()
	{
		return stringArray.Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ArrayString_Where_ToList()
	{
		return stringArray.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> Linq_ListClass_Where_ToList()
	{
		return itemList.Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> ZLinq_ListClass_Where_ToList()
	{
		return itemList.AsValueEnumerable().Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> Linq_ArrayClass_Where_ToList()
	{
		return itemArray.Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> ZLinq_ArrayClass_Where_ToList()
	{
		return itemArray.AsValueEnumerable().Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> Linq_NestedClassLists_Where_ToList()
	{
		return nestedItemLists.SelectMany(x => x).Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> ZLinq_NestedClassLists_Where_ToList()
	{
		return nestedItemLists.AsValueEnumerable().SelectMany(x => x).Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> Linq_NestedClassArrays_Where_ToList()
	{
		return nestedItemArrays.SelectMany(x => x).Where(x => x.Id % 2 == 0).ToList();
	}

	[Benchmark]
	public List<SampleItem> ZLinq_NestedClassArrays_Where_ToList()
	{
		return nestedItemArrays.AsValueEnumerable().SelectMany(x => x).Where(x => x.Id % 2 == 0).ToList();
	}

	#endregion Where_ToList

	#region Where_Select_ToList

	[Benchmark]
	public List<string> Linq_ListInt_Where_Select_ToList()
	{
		return intList.Where(x => x % 2 == 0).Select(x => x.ToString()).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ListInt_Where_Select_ToList()
	{
		return intList.AsValueEnumerable().Where(x => x % 2 == 0).Select(x => x.ToString()).ToList();
	}

	[Benchmark]
	public List<string> Linq_ArrayInt_Where_Select_ToList()
	{
		return intArray.Where(x => x % 2 == 0).Select(x => x.ToString()).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ArrayInt_Where_Select_ToList()
	{
		return intArray.AsValueEnumerable().Where(x => x % 2 == 0).Select(x => x.ToString()).ToList();
	}

	[Benchmark]
	public List<string> Linq_ListString_Where_Select_ToList()
	{
		return stringList.Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).Select(x => x.ToUpperInvariant()).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ListString_Where_Select_ToList()
	{
		return stringList.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).Select(x => x.ToUpperInvariant()).ToList();
	}

	[Benchmark]
	public List<string> Linq_ArrayString_Where_Select_ToList()
	{
		return stringArray.Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).Select(x => x.ToUpperInvariant()).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ArrayString_Where_Select_ToList()
	{
		return stringArray.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 2 == 0).Select(x => x.ToUpperInvariant()).ToList();
	}

	[Benchmark]
	public List<string> Linq_ListClass_Where_Select_ToList()
	{
		return itemList.Where(x => x.Id % 2 == 0).Select(x => x.Name).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ListClass_Where_Select_ToList()
	{
		return itemList.AsValueEnumerable().Where(x => x.Id % 2 == 0).Select(x => x.Name).ToList();
	}

	[Benchmark]
	public List<string> Linq_ArrayClass_Where_Select_ToList()
	{
		return itemArray.Where(x => x.Id % 2 == 0).Select(x => x.Name).ToList();
	}

	[Benchmark]
	public List<string> ZLinq_ArrayClass_Where_Select_ToList()
	{
		return itemArray.AsValueEnumerable().Where(x => x.Id % 2 == 0).Select(x => x.Name).ToList();
	}

	#endregion Where_Select_ToList

	#region OrderBy_ToArray

	[Benchmark]
	public int[] Linq_ListInt_OrderBy_ToArray()
	{
		return intList.OrderBy(x => x).ToArray();
	}

	[Benchmark]
	public int[] ZLinq_ListInt_OrderBy_ToArray()
	{
		return intList.AsValueEnumerable().OrderBy(x => x).ToArray();
	}

	[Benchmark]
	public int[] Linq_ArrayInt_OrderBy_ToArray()
	{
		return intArray.OrderBy(x => x).ToArray();
	}

	[Benchmark]
	public int[] ZLinq_ArrayInt_OrderBy_ToArray()
	{
		return intArray.AsValueEnumerable().OrderBy(x => x).ToArray();
	}

	[Benchmark]
	public string[] Linq_ListString_OrderBy_ToArray()
	{
		return stringList.OrderBy(x => x, StringComparer.Ordinal).ToArray();
	}

	[Benchmark]
	public string[] ZLinq_ListString_OrderBy_ToArray()
	{
		return stringList.AsValueEnumerable().OrderBy(x => x, StringComparer.Ordinal).ToArray();
	}

	[Benchmark]
	public string[] Linq_ArrayString_OrderBy_ToArray()
	{
		return stringArray.OrderBy(x => x, StringComparer.Ordinal).ToArray();
	}

	[Benchmark]
	public string[] ZLinq_ArrayString_OrderBy_ToArray()
	{
		return stringArray.AsValueEnumerable().OrderBy(x => x, StringComparer.Ordinal).ToArray();
	}

	[Benchmark]
	public SampleItem[] Linq_ListClass_OrderBy_ToArray()
	{
		return itemList.OrderBy(x => x.Id).ToArray();
	}

	[Benchmark]
	public SampleItem[] ZLinq_ListClass_OrderBy_ToArray()
	{
		return itemList.AsValueEnumerable().OrderBy(x => x.Id).ToArray();
	}

	[Benchmark]
	public SampleItem[] Linq_ArrayClass_OrderBy_ToArray()
	{
		return itemArray.OrderBy(x => x.Id).ToArray();
	}

	[Benchmark]
	public SampleItem[] ZLinq_ArrayClass_OrderBy_ToArray()
	{
		return itemArray.AsValueEnumerable().OrderBy(x => x.Id).ToArray();
	}

	#endregion OrderBy_ToArray

	#region Where_Max

	[Benchmark]
	public int Linq_ListInt_Where_Max()
	{
		return intList.Where(x => x % 3 == 0).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int ZLinq_ListInt_Where_Max()
	{
		return intList.AsValueEnumerable().Where(x => x % 3 == 0).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int Linq_ArrayInt_Where_Max()
	{
		return intArray.Where(x => x % 3 == 0).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int ZLinq_ArrayInt_Where_Max()
	{
		return intArray.AsValueEnumerable().Where(x => x % 3 == 0).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public string Linq_ListString_Where_Max()
	{
		return stringList.Where(x => int.Parse(x.Split('_')[1]) % 3 == 0).DefaultIfEmpty(string.Empty).Max()!;
	}

	[Benchmark]
	public string ZLinq_ListString_Where_Max()
	{
		return stringList.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 3 == 0).DefaultIfEmpty(string.Empty).Max()!;
	}

	[Benchmark]
	public string Linq_ArrayString_Where_Max()
	{
		return stringArray.Where(x => int.Parse(x.Split('_')[1]) % 3 == 0).DefaultIfEmpty(string.Empty).Max()!;
	}

	[Benchmark]
	public string ZLinq_ArrayString_Where_Max()
	{
		return stringArray.AsValueEnumerable().Where(x => int.Parse(x.Split('_')[1]) % 3 == 0).DefaultIfEmpty(string.Empty).Max()!;
	}

	[Benchmark]
	public int Linq_ListClass_Where_Max()
	{
		return itemList.Where(x => x.Id % 3 == 0).Select(x => x.Id).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int ZLinq_ListClass_Where_Max()
	{
		return itemList.AsValueEnumerable().Where(x => x.Id % 3 == 0).Select(x => x.Id).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int Linq_ArrayClass_Where_Max()
	{
		return itemArray.Where(x => x.Id % 3 == 0).Select(x => x.Id).DefaultIfEmpty(0).Max();
	}

	[Benchmark]
	public int ZLinq_ArrayClass_Where_Max()
	{
		return itemArray.AsValueEnumerable().Where(x => x.Id % 3 == 0).Select(x => x.Id).DefaultIfEmpty(0).Max();
	}

	#endregion Where_Max

	#region Where_SelectMany_FirstOrDefault

	[Benchmark]
	public int Linq_NestedIntLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedIntLists.Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => x > N / 2);
	}

	[Benchmark]
	public int ZLinq_NestedIntLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedIntLists.AsValueEnumerable().Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => x > N / 2);
	}

	[Benchmark]
	public int Linq_NestedIntArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedIntArrays.Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => x > N / 2);
	}

	[Benchmark]
	public int ZLinq_NestedIntArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedIntArrays.AsValueEnumerable().Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => x > N / 2);
	}

	[Benchmark]
	public string? Linq_NestedStringLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedStringLists.Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => int.Parse(x.Split('_')[1]) > N / 2);
	}

	[Benchmark]
	public string? ZLinq_NestedStringLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedStringLists.AsValueEnumerable().Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => int.Parse(x.Split('_')[1]) > N / 2);
	}

	[Benchmark]
	public string? Linq_NestedStringArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedStringArrays.Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => int.Parse(x.Split('_')[1]) > N / 2);
	}

	[Benchmark]
	public string? ZLinq_NestedStringArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedStringArrays.AsValueEnumerable().Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => int.Parse(x.Split('_')[1]) > N / 2);
	}

	[Benchmark]
	public SampleItem? Linq_NestedClassLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedItemLists.Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => x.Id > N / 2);
	}

	[Benchmark]
	public SampleItem? ZLinq_NestedClassLists_Where_SelectMany_FirstOrDefault()
	{
		return nestedItemLists.AsValueEnumerable().Where(x => x.Count > 0).SelectMany(x => x).FirstOrDefault(x => x.Id > N / 2);
	}

	[Benchmark]
	public SampleItem? Linq_NestedClassArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedItemArrays.Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => x.Id > N / 2);
	}

	[Benchmark]
	public SampleItem? ZLinq_NestedClassArrays_Where_SelectMany_FirstOrDefault()
	{
		return nestedItemArrays.AsValueEnumerable().Where(x => x.Length > 0).SelectMany(x => x).FirstOrDefault(x => x.Id > N / 2);
	}

	#endregion Where_SelectMany_FirstOrDefault

	#region Any_All

	[Benchmark]
	public bool Linq_ListInt_Any_All()
	{
		return intList.Any(x => x % 2 == 0) && intList.Where(x => x % 2 == 0).All(x => x >= 0);
	}

	[Benchmark]
	public bool ZLinq_ListInt_Any_All()
	{
		return intList.AsValueEnumerable().Any(x => x % 2 == 0) && intList.AsValueEnumerable().Where(x => x % 2 == 0).All(x => x >= 0);
	}

	[Benchmark]
	public bool Linq_ArrayInt_Any_All()
	{
		return intArray.Any(x => x % 2 == 0) && intArray.Where(x => x % 2 == 0).All(x => x >= 0);
	}

	[Benchmark]
	public bool ZLinq_ArrayInt_Any_All()
	{
		return intArray.AsValueEnumerable().Any(x => x % 2 == 0) && intArray.AsValueEnumerable().Where(x => x % 2 == 0).All(x => x >= 0);
	}

	[Benchmark]
	public bool Linq_ListString_Any_All()
	{
		return stringList.Any(x => x.Length > 0) && stringList.Where(x => x.Length > 0).All(x => x.StartsWith("Item"));
	}

	[Benchmark]
	public bool ZLinq_ListString_Any_All()
	{
		return stringList.AsValueEnumerable().Any(x => x.Length > 0) && stringList.AsValueEnumerable().Where(x => x.Length > 0).All(x => x.StartsWith("Item"));
	}

	[Benchmark]
	public bool Linq_ArrayString_Any_All()
	{
		return stringArray.Any(x => x.Length > 0) && stringArray.Where(x => x.Length > 0).All(x => x.StartsWith("Item"));
	}

	[Benchmark]
	public bool ZLinq_ArrayString_Any_All()
	{
		return stringArray.AsValueEnumerable().Any(x => x.Length > 0) && stringArray.AsValueEnumerable().Where(x => x.Length > 0).All(x => x.StartsWith("Item"));
	}

	[Benchmark]
	public bool Linq_ListClass_Any_All()
	{
		return itemList.Any(x => x.Id % 2 == 0) && itemList.Where(x => x.Id % 2 == 0).All(x => x.Name.StartsWith("Item"));
	}

	[Benchmark]
	public bool ZLinq_ListClass_Any_All()
	{
		return itemList.AsValueEnumerable().Any(x => x.Id % 2 == 0) && itemList.AsValueEnumerable().Where(x => x.Id % 2 == 0).All(x => x.Name.StartsWith("Item"));
	}

	[Benchmark]
	public bool Linq_ArrayClass_Any_All()
	{
		return itemArray.Any(x => x.Id % 2 == 0) && itemArray.Where(x => x.Id % 2 == 0).All(x => x.Name.StartsWith("Item"));
	}

	[Benchmark]
	public bool ZLinq_ArrayClass_Any_All()
	{
		return itemArray.AsValueEnumerable().Any(x => x.Id % 2 == 0) && itemArray.AsValueEnumerable().Where(x => x.Id % 2 == 0).All(x => x.Name.StartsWith("Item"));
	}

	#endregion Any_All

	#region Skip_First

	[Benchmark]
	public int Linq_ListInt_Skip_First()
	{
		return intList.Skip(intList.Count / 2).First();
	}

	[Benchmark]
	public int ZLinq_ListInt_Skip_First()
	{
		return intList.AsValueEnumerable().Skip(intList.Count / 2).First();
	}

	[Benchmark]
	public int Linq_ArrayInt_Skip_First()
	{
		return intArray.Skip(intArray.Length / 2).First();
	}

	[Benchmark]
	public int ZLinq_ArrayInt_Skip_First()
	{
		return intArray.AsValueEnumerable().Skip(intArray.Length / 2).First();
	}

	[Benchmark]
	public string Linq_ListString_Skip_First()
	{
		return stringList.Skip(stringList.Count / 2).First();
	}

	[Benchmark]
	public string ZLinq_ListString_Skip_First()
	{
		return stringList.AsValueEnumerable().Skip(stringList.Count / 2).First();
	}

	[Benchmark]
	public string Linq_ArrayString_Skip_First()
	{
		return stringArray.Skip(stringArray.Length / 2).First();
	}

	[Benchmark]
	public string ZLinq_ArrayString_Skip_First()
	{
		return stringArray.AsValueEnumerable().Skip(stringArray.Length / 2).First();
	}

	[Benchmark]
	public SampleItem Linq_ListClass_Skip_First()
	{
		return itemList.Skip(itemList.Count / 2).First();
	}

	[Benchmark]
	public SampleItem ZLinq_ListClass_Skip_First()
	{
		return itemList.AsValueEnumerable().Skip(itemList.Count / 2).First();
	}

	[Benchmark]
	public SampleItem Linq_ArrayClass_Skip_First()
	{
		return itemArray.Skip(itemArray.Length / 2).First();
	}

	[Benchmark]
	public SampleItem ZLinq_ArrayClass_Skip_First()
	{
		return itemArray.AsValueEnumerable().Skip(itemArray.Length / 2).First();
	}

	#endregion Skip_First

	#region Intersect_Count

	[Benchmark]
	public int Linq_ListInt_Intersect_Count()
	{
		return intList.Intersect(intArray).Count();
	}

	[Benchmark]
	public int ZLinq_ListInt_Intersect_Count()
	{
		return intList.AsValueEnumerable().Intersect(intArray).Count();
	}

	[Benchmark]
	public int Linq_ListString_Intersect_Count()
	{
		return stringList.Intersect(stringArray).Count();
	}

	[Benchmark]
	public int ZLinq_ListString_Intersect_Count()
	{
		return stringList.AsValueEnumerable().Intersect(stringArray).Count();
	}

	[Benchmark]
	public int Linq_ListClass_Intersect_Count()
	{
		return itemList.Intersect(itemArray).Count();
	}

	[Benchmark]
	public int ZLinq_ListClass_Intersect_Count()
	{
		return itemList.AsValueEnumerable().Intersect(itemArray).Count();
	}

	#endregion Intersect_Count
}
