using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreTypeChecksBenchmarks
{
	private Type stringType = null!;
	private Type intType = null!;
	private Type dateTimeType = null!;
	private Type listType = null!;
	private Type arrayType = null!;
	private Type dictionaryType = null!;
	private Type delegateType = null!;

	[GlobalSetup]
	public void Setup()
	{
		stringType = typeof(string);
		intType = typeof(int);
		dateTimeType = typeof(DateTime);
		listType = typeof(List<int>);
		arrayType = typeof(int[]);
		dictionaryType = typeof(Dictionary<int, string>);
		delegateType = typeof(Action);
	}

	[Benchmark]
	public bool IsSimpleType_String()
	{
		return stringType.IsSimpleType();
	}

	[Benchmark]
	public bool IsSimpleType_Int()
	{
		return intType.IsSimpleType();
	}

	[Benchmark]
	public bool IsSimpleType_DateTime()
	{
		return dateTimeType.IsSimpleType();
	}

	[Benchmark]
	public bool IsNumericType_Int()
	{
		return intType.IsNumericType();
	}

	[Benchmark]
	public bool IsNumericType_String()
	{
		return stringType.IsNumericType();
	}

	[Benchmark]
	public bool IsEnumerable_List()
	{
		return listType.IsEnumerable();
	}

	[Benchmark]
	public bool IsEnumerable_Array()
	{
		return arrayType.IsEnumerable();
	}

	[Benchmark]
	public bool IsDictionary()
	{
		return dictionaryType.IsDictionary();
	}

	[Benchmark]
	public bool IsDelegate()
	{
		return delegateType.IsDelegate();
	}

	[Benchmark]
	public bool IsReadOnlyCollectionType_List()
	{
		return listType.IsReadOnlyCollectionType();
	}
}
