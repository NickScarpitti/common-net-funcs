using System;
using System.Collections.Generic;
using BenchmarkDotNet.Attributes;
using CommonNetFuncs.Core;

namespace BenchmarkSuite;

#pragma warning disable S6562 // Provide the "DateTimeKind" when creating this object.
[MemoryDiagnoser]
[RankColumn]
public class CoreDateTimeHelpersBenchmarks
{
	private DateTime _startDate;
	private DateTime _endDate;
	private List<DateTime> _holidays = null!;

	[GlobalSetup]
	public void Setup()
	{
		_startDate = new DateTime(2025, 1, 1);
		_endDate = new DateTime(2025, 12, 31);
		_holidays = new List<DateTime>
			{
				new(2025, 1, 1),
				new(2025, 7, 4),
				new(2025, 12, 25)
			};
	}

	[Benchmark]
	public int GetBusinessDays()
	{
		return DateTimeHelpers.GetBusinessDays(_startDate, _endDate);
	}

	[Benchmark]
	public int GetBusinessDays_WithHolidays()
	{
		return DateTimeHelpers.GetBusinessDays(_startDate, _endDate, _holidays);
	}

	[Benchmark]
	public DateTime GetDayOfWeek()
	{
		return _startDate.GetDayOfWeek(DayOfWeek.Monday);
	}

	[Benchmark]
	public (DateTime firstDay, DateTime lastDay) GetMonthBoundaries()
	{
		return _startDate.GetMonthBoundaries();
	}

	[Benchmark]
	public DateTime GetFirstDayOfMonth()
	{
		return _startDate.GetFirstDayOfMonth();
	}

	[Benchmark]
	public DateTime GetLastDayOfMonth()
	{
		return _startDate.GetLastDayOfMonth();
	}

	[Benchmark]
	public bool IsValidOaDate()
	{
		double oaDate = 44927.0; // Jan 1, 2023
		return oaDate.IsValidOaDate();
	}
}
#pragma warning restore S6562 // Provide the "DateTimeKind" when creating this object.
