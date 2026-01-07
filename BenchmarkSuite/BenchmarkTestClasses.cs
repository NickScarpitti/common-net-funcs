using System;
using System.Collections.Generic;

namespace BenchmarkSuite;


// Test classes for benchmarks
public class SimpleClass
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Value { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedDate { get; set; }
}

public class SimpleClassDto
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public double Value { get; set; }
	public bool IsActive { get; set; }
	public DateTime CreatedDate { get; set; }
}

public class ComplexClass
{
	public int Id { get; set; }
	public string? Name { get; set; }
	public string? Title { get; set; }
	public string? Description { get; set; }
	public int Count { get; set; }
	public decimal Price { get; set; }
	public bool IsEnabled { get; set; }
	public List<int>? Numbers { get; set; }
	public List<string>? Tags { get; set; }
	public Dictionary<string, string>? Metadata { get; set; }
	public SimpleClass? Nested { get; set; }
}

public class NestedClass
{
	public int Level { get; set; }
	public int Id { get; set; }
	public string? Name { get; set; }
	public SimpleClass? Child { get; set; }
	public NestedClass? Child2 { get; set; }
	public List<SimpleClass>? Children { get; set; }
}
