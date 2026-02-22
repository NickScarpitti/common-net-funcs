using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

[MemoryDiagnoser]
[RankColumn]
public class CoreDateTimeHelpersBenchmarks
{
	private DateTime startDate;
	private DateTime endDate;
	private List<DateTime> holidays = null!;

	[GlobalSetup]
	public void Setup()
	{
		startDate = new DateTime(2025, 1, 1);
		endDate = new DateTime(2025, 12, 31);
		holidays = new List<DateTime>
			{
				new(2025, 1, 1),
				new(2025, 7, 4),
				new(2025, 12, 25)
			};
	}

	[Benchmark]
	public int GetBusinessDays()
	{
		return DateTimeHelpers.GetBusinessDays(startDate, endDate);
	}

	[Benchmark]
	public int GetBusinessDays_WithHolidays()
	{
		return DateTimeHelpers.GetBusinessDays(startDate, endDate, holidays);
	}

	[Benchmark]
	public DateTime GetDayOfWeek()
	{
		return startDate.GetDayOfWeek(DayOfWeek.Monday);
	}

	[Benchmark]
	public (DateTime firstDay, DateTime lastDay) GetMonthBoundaries()
	{
		return startDate.GetMonthBoundaries();
	}

	[Benchmark]
	public DateTime GetFirstDayOfMonth()
	{
		return startDate.GetFirstDayOfMonth();
	}

	[Benchmark]
	public DateTime GetLastDayOfMonth()
	{
		return startDate.GetLastDayOfMonth();
	}

	[Benchmark]
	public static bool IsValidOaDate()
	{
		const double oaDate = 44927.0; // Jan 1, 2023
		return oaDate.IsValidOaDate();
	}
}
