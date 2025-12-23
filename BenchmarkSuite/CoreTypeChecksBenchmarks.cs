using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreTypeChecksBenchmarks
{
	private Type _stringType = null!;
	private Type _intType = null!;
	private Type _dateTimeType = null!;
	private Type _listType = null!;
	private Type _arrayType = null!;
	private Type _dictionaryType = null!;
	private Type _delegateType = null!;

	[GlobalSetup]
	public void Setup()
	{
		_stringType = typeof(string);
		_intType = typeof(int);
		_dateTimeType = typeof(DateTime);
		_listType = typeof(List<int>);
		_arrayType = typeof(int[]);
		_dictionaryType = typeof(Dictionary<int, string>);
		_delegateType = typeof(Action);
	}

	[Benchmark]
	public bool IsSimpleType_String()
	{
		return _stringType.IsSimpleType();
	}

	[Benchmark]
	public bool IsSimpleType_Int()
	{
		return _intType.IsSimpleType();
	}

	[Benchmark]
	public bool IsSimpleType_DateTime()
	{
		return _dateTimeType.IsSimpleType();
	}

	[Benchmark]
	public bool IsNumericType_Int()
	{
		return _intType.IsNumericType();
	}

	[Benchmark]
	public bool IsNumericType_String()
	{
		return _stringType.IsNumericType();
	}

	[Benchmark]
	public bool IsEnumerable_List()
	{
		return _listType.IsEnumerable();
	}

	[Benchmark]
	public bool IsEnumerable_Array()
	{
		return _arrayType.IsEnumerable();
	}

	[Benchmark]
	public bool IsDictionary()
	{
		return _dictionaryType.IsDictionary();
	}

	[Benchmark]
	public bool IsDelegate()
	{
		return _delegateType.IsDelegate();
	}

	[Benchmark]
	public bool IsReadOnlyCollectionType_List()
	{
		return _listType.IsReadOnlyCollectionType();
	}
}
