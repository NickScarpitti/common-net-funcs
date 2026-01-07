using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
public class CoreDateOnlyHelpersBenchmarks
{
	private readonly DateOnly _startDate = new(2024, 1, 1);
	private readonly DateOnly _endDate = new(2024, 12, 31);
	private readonly List<DateOnly> _holidays;
	private readonly List<DateOnly> _manyHolidays;

	public CoreDateOnlyHelpersBenchmarks()
	{
		// Create a list of 10 holidays throughout the year
		_holidays = new List<DateOnly>
		{
			new(2024, 1, 1),   // New Year
			new(2024, 2, 14),  // Valentine's
			new(2024, 5, 27),  // Memorial Day
			new(2024, 7, 4),   // Independence Day
			new(2024, 9, 2),   // Labor Day
			new(2024, 11, 28), // Thanksgiving
			new(2024, 12, 25), // Christmas
			new(2024, 12, 24), // Christmas Eve
			new(2024, 11, 11), // Veterans Day
			new(2024, 10, 14)  // Columbus Day
		};

		// Create a much larger list for stress testing
		_manyHolidays = new List<DateOnly>();
		for (int i = 1; i <= 365; i += 7)
		{
			_manyHolidays.Add(new DateOnly(2024, 1, 1).AddDays(i));
		}
	}

	[Benchmark(Description = "GetBusinessDays - no exceptions")]
	public int GetBusinessDays_NoExceptions()
	{
		return DateOnlyHelpers.GetBusinessDays(_startDate, _endDate, null);
	}

	[Benchmark(Description = "GetBusinessDays - 10 holidays")]
	public int GetBusinessDays_WithHolidays()
	{
		return DateOnlyHelpers.GetBusinessDays(_startDate, _endDate, _holidays);
	}

	[Benchmark(Description = "GetBusinessDays - many holidays")]
	public int GetBusinessDays_ManyHolidays()
	{
		return DateOnlyHelpers.GetBusinessDays(_startDate, _endDate, _manyHolidays);
	}

	[Benchmark(Description = "GetBusinessDays - short range")]
	public int GetBusinessDays_ShortRange()
	{
		return DateOnlyHelpers.GetBusinessDays(new DateOnly(2024, 1, 1), new DateOnly(2024, 1, 15), _holidays);
	}
}
