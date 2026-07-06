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

/// <summary>36-column test class that matches the user's real-world export scenario.</summary>
public class ExportTestRow
{
	public int Id { get; set; }
	public string? FirstName { get; set; }
	public string? LastName { get; set; }
	public string? Email { get; set; }
	public string? Phone { get; set; }
	public string? Status { get; set; }
	public string? Category { get; set; }
	public string? SubCategory { get; set; }
	public decimal Amount { get; set; }
	public decimal Price { get; set; }
	public decimal Cost { get; set; }
	public decimal Total { get; set; }
	public int Quantity { get; set; }
	public int Count { get; set; }
	public bool IsActive { get; set; }
	public bool IsEnabled { get; set; }
	public DateTime CreatedDate { get; set; }
	public DateTime UpdatedDate { get; set; }
	public DateTime ProcessedDate { get; set; }
	public string? Field01 { get; set; }
	public string? Field02 { get; set; }
	public string? Field03 { get; set; }
	public string? Field04 { get; set; }
	public string? Field05 { get; set; }
	public string? Field06 { get; set; }
	public string? Field07 { get; set; }
	public string? Field08 { get; set; }
	public string? Field09 { get; set; }
	public string? Field10 { get; set; }
	public string? Field11 { get; set; }
	public string? Field12 { get; set; }
	public string? Field13 { get; set; }
	public string? Field14 { get; set; }
	public string? Field15 { get; set; }
	public string? Field16 { get; set; }
	public string? Field17 { get; set; }
}
