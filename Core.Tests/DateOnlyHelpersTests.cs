using System.Globalization;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class DateOnlyHelpersTests
{
	private readonly CultureInfo formatProvider = new("en-US");

	[Theory]
	[InlineData("2024-05-06", "2024-05-10", null, 5)] // Mon-Fri, no holidays
	[InlineData("2024-05-06", "2024-05-12", null, 5)] // Mon-Sun, no holidays
	[InlineData("2024-05-11", "2024-05-12", null, 0)] // Sat-Sun, no holidays
	[InlineData("2024-05-06", "2024-05-06", null, 1)] // Single day, Mon
	[InlineData("2024-05-11", "2024-05-11", null, 0)] // Single day, Sat
	[InlineData("2024-05-12", "2024-05-12", null, 0)] // Single day, Sun
	public void GetBusinessDays_BasicCases(string start, string end, object? _, int expected)
	{
		// Arrange
		DateOnly startDate = DateOnly.Parse(start, formatProvider);
		DateOnly endDate = DateOnly.Parse(end, formatProvider);

		// Act & Assert
		DateOnlyHelpers.GetBusinessDays(startDate, endDate).ShouldBe(expected);
	}

	[Fact]
	public void GetBusinessDays_ReturnsZero_WhenStartOrEndIsNull()
	{
		// Arrange
		DateOnly date = DateOnly.FromDateTime(DateTime.Today);

		// Act & Assert
		DateOnlyHelpers.GetBusinessDays(null, date).ShouldBe(0);
		DateOnlyHelpers.GetBusinessDays(date, null).ShouldBe(0);
		DateOnlyHelpers.GetBusinessDays(null, null).ShouldBe(0);
	}

	[Fact]
	public void GetBusinessDays_ExcludesExceptionDates()
	{
		// Arrange
		DateOnly start = new(2024, 5, 6); // Monday
		DateOnly end = new(2024, 5, 10); // Friday
		List<DateOnly> holidays = new() { new(2024, 5, 8), new(2024, 5, 9) }; // Wed, Thu

		// Act & Assert
		DateOnlyHelpers.GetBusinessDays(start, end, holidays).ShouldBe(3);
	}

	[Fact]
	public void GetBusinessDays_ExcludesExceptionDates_OnlyWeekdays()
	{
		// Arrange
		DateOnly start = new(2024, 5, 6); // Monday
		DateOnly end = new(2024, 5, 12); // Sunday
		List<DateOnly> holidays = new() { new(2024, 5, 11), new(2024, 5, 8) }; // Sat, Wed

		// Act & Assert
		DateOnlyHelpers.GetBusinessDays(start, end, holidays).ShouldBe(4); // Only Wed should be excluded
	}

	[Theory]
	[InlineData("2024-05-08", DayOfWeek.Monday, "2024-05-06")] // Wed, get Monday
	[InlineData("2024-05-08", DayOfWeek.Wednesday, "2024-05-08")] // Wed, get Wed
	[InlineData("2024-05-12", DayOfWeek.Sunday, "2024-05-12")] // Sun, get Sun
	[InlineData("2024-05-12", DayOfWeek.Monday, "2024-05-06")] // Sun, get Monday
	public void GetDayOfWeek_ReturnsCorrectDate(string dateStr, DayOfWeek dow, string expectedStr)
	{
		// Arrange
		DateOnly date = DateOnly.Parse(dateStr, formatProvider);
		DateOnly expected = DateOnly.Parse(expectedStr, formatProvider);

		// Act & Assert
		date.GetDayOfWeek(dow).ShouldBe(expected);
	}

	[Theory]
	[InlineData(2, 2024, "2024-02-01", "2024-02-29")] // Leap year
	[InlineData(2, 2023, "2023-02-01", "2023-02-28")] // Non-leap year
	[InlineData(1, 2024, "2024-01-01", "2024-01-31")]
	[InlineData(12, 2024, "2024-12-01", "2024-12-31")]
	public void GetMonthBoundaries_ByMonthYear_ReturnsCorrectBoundaries(int month, int year, string first, string last)
	{
		// Arrange & Act
		(DateOnly firstDay, DateOnly lastDay) = DateOnlyHelpers.GetMonthBoundaries(month, year);

		// Assert
		firstDay.ShouldBe(DateOnly.Parse(first, formatProvider));
		lastDay.ShouldBe(DateOnly.Parse(last, formatProvider));
	}

	[Fact]
	public void GetMonthBoundaries_ByDate_ReturnsCorrectBoundaries()
	{
		// Arrange
		DateOnly date = new(2024, 2, 15);

		// Act
		(DateOnly firstDay, DateOnly lastDay) = date.GetMonthBoundaries();

		// Assert
		firstDay.ShouldBe(new DateOnly(2024, 2, 1));
		lastDay.ShouldBe(new DateOnly(2024, 2, 29));
	}

	[Theory]
	[InlineData(2, 2024, "2024-02-01")]
	[InlineData(12, 2024, "2024-12-01")]
	public void GetFirstDayOfMonth_ByMonthYear_ReturnsFirstDay(int month, int year, string expected)
	{
		// Act & Assert
		DateOnlyHelpers.GetFirstDayOfMonth(month, year).ShouldBe(DateOnly.Parse(expected, formatProvider));
	}

	[Fact]
	public void GetFirstDayOfMonth_ByDate_ReturnsFirstDay()
	{
		// Arrange
		DateOnly date = new(2024, 5, 15);

		// Act & Assert
		date.GetFirstDayOfMonth().ShouldBe(new DateOnly(2024, 5, 1));
	}

	[Theory]
	[InlineData(2, 2024, "2024-02-29")]
	[InlineData(2, 2023, "2023-02-28")]
	[InlineData(4, 2024, "2024-04-30")]
	public void GetLastDayOfMonth_ByMonthYear_ReturnsLastDay(int month, int year, string expected)
	{
		// Act & Assert
		DateOnlyHelpers.GetLastDayOfMonth(month, year).ShouldBe(DateOnly.Parse(expected, formatProvider));
	}

	[Fact]
	public void GetLastDayOfMonth_ByDate_ReturnsLastDay()
	{
		// Arrange
		DateOnly date = new(2024, 5, 15);

		// Act & Assert
		date.GetLastDayOfMonth().ShouldBe(new DateOnly(2024, 5, 31));
	}

	[Fact]
	public void GetToday_ReturnsTodaysDate()
	{
		// Arrange
		DateOnly expected = DateOnly.FromDateTime(DateTime.Today);

		// Act & Assert
		DateOnlyHelpers.GetToday().ShouldBe(expected);
	}

	[Fact]
	public void GetToday_UsingUtc_ReturnsTodaysDate()
	{
		// Arrange
		DateOnly expected = DateOnly.FromDateTime(DateTime.UtcNow);

		// Act & Assert
		DateOnlyHelpers.GetToday(true).ShouldBe(expected);
	}
}
