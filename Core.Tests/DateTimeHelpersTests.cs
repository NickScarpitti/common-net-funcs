using System.Globalization;
using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class DateTimeHelpersTests
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
		DateTime startDate = DateTime.Parse(start, formatProvider);
		DateTime endDate = DateTime.Parse(end, formatProvider);

		// Act & Assert
		DateTimeHelpers.GetBusinessDays(startDate, endDate).ShouldBe(expected);
	}

	[Fact]
	public void GetBusinessDays_ReturnsZero_WhenStartOrEndIsNull()
	{
		// Arrange
		DateTime date = DateTime.Today;

		// Act & Assert
		DateTimeHelpers.GetBusinessDays(null, date).ShouldBe(0);
		DateTimeHelpers.GetBusinessDays(date, null).ShouldBe(0);
		DateTimeHelpers.GetBusinessDays(null, null).ShouldBe(0);
	}

	[Fact]
	public void GetBusinessDays_ExcludesExceptionDates()
	{
		// Arrange
		DateTime start = new(2024, 5, 6, 0, 0, 0, DateTimeKind.Unspecified); // Monday
		DateTime end = new(2024, 5, 10, 0, 0, 0, DateTimeKind.Unspecified); // Friday
		List<DateTime> holidays = new() { new(2024, 5, 8, 0, 0, 0, DateTimeKind.Unspecified), new(2024, 5, 9, 0, 0, 0, DateTimeKind.Unspecified) }; // Wed, Thu

		// Act & Assert
		DateTimeHelpers.GetBusinessDays(start, end, holidays).ShouldBe(3);
	}

	[Fact]
	public void GetBusinessDays_ExcludesExceptionDates_OnlyWeekdays()
	{
		// Arrange
		DateTime start = new(2024, 5, 6, 0, 0, 0, DateTimeKind.Unspecified); // Monday
		DateTime end = new(2024, 5, 12, 0, 0, 0, DateTimeKind.Unspecified); // Sunday
		List<DateTime> holidays = new() { new(2024, 5, 11, 0, 0, 0, DateTimeKind.Unspecified), new(2024, 5, 8, 0, 0, 0, DateTimeKind.Unspecified) }; // Sat, Wed

		// Act & Assert
		DateTimeHelpers.GetBusinessDays(start, end, holidays).ShouldBe(4); // Only Wed should be excluded
	}

	[Theory]
	[InlineData("2024-05-08", DayOfWeek.Monday, "2024-05-06")] // Wed, get Monday
	[InlineData("2024-05-08", DayOfWeek.Wednesday, "2024-05-08")] // Wed, get Wed
	[InlineData("2024-05-12", DayOfWeek.Sunday, "2024-05-12")] // Sun, get Sun
	[InlineData("2024-05-12", DayOfWeek.Monday, "2024-05-06")] // Sun, get Monday
	public void GetDayOfWeek_ReturnsCorrectDate(string dateStr, DayOfWeek dow, string expectedStr)
	{
		// Arrange
		DateTime date = DateTime.Parse(dateStr, formatProvider);
		DateTime expected = DateTime.Parse(expectedStr, formatProvider);

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
		(DateTime firstDay, DateTime lastDay) = DateTimeHelpers.GetMonthBoundaries(month, year);

		// Assert
		firstDay.ShouldBe(DateTime.Parse(first, formatProvider));
		lastDay.ShouldBe(DateTime.Parse(last, formatProvider));
	}

	[Fact]
	public void GetMonthBoundaries_ByDate_ReturnsCorrectBoundaries()
	{
		// Arrange
		DateTime date = new(2024, 2, 15, 0, 0, 0, DateTimeKind.Unspecified);

		// Act
		(DateTime firstDay, DateTime lastDay) = date.GetMonthBoundaries();

		// Assert
		firstDay.ShouldBe(new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Unspecified));
		lastDay.ShouldBe(new DateTime(2024, 2, 29, 0, 0, 0, DateTimeKind.Unspecified));
	}

	[Theory]
	[InlineData(2, 2024, "2024-02-01")]
	[InlineData(12, 2024, "2024-12-01")]
	public void GetFirstDayOfMonth_ByMonthYear_ReturnsFirstDay(int month, int year, string expected)
	{
		// Act & Assert
		DateTimeHelpers.GetFirstDayOfMonth(month, year).ShouldBe(DateTime.Parse(expected, formatProvider));
	}

	[Fact]
	public void GetFirstDayOfMonth_ByDate_ReturnsFirstDay()
	{
		// Arrange
		DateTime date = new(2024, 5, 15, 0, 0, 0, DateTimeKind.Unspecified);

		// Act & Assert
		date.GetFirstDayOfMonth().ShouldBe(new DateTime(2024, 5, 1, 0, 0, 0, DateTimeKind.Unspecified));
	}

	[Theory]
	[InlineData(2, 2024, "2024-02-29")]
	[InlineData(2, 2023, "2023-02-28")]
	[InlineData(4, 2024, "2024-04-30")]
	public void GetLastDayOfMonth_ByMonthYear_ReturnsLastDay(int month, int year, string expected)
	{
		// Act & Assert
		DateTimeHelpers.GetLastDayOfMonth(month, year).ShouldBe(DateTime.Parse(expected, formatProvider));
	}

	[Fact]
	public void GetLastDayOfMonth_ByDate_ReturnsLastDay()
	{
		// Arrange
		DateTime date = new(2024, 5, 15, 0, 0, 0, DateTimeKind.Unspecified);

		// Act & Assert
		date.GetLastDayOfMonth().ShouldBe(new DateTime(2024, 5, 31, 0, 0, 0, DateTimeKind.Unspecified));
	}


	[Fact]
	public void IsValidOaDate_NullableDouble_ReturnsFalseIfNull()
	{
		// Arrange
		double? value = null;

		// Act & Assert
		value.IsValidOaDate().ShouldBeFalse();
	}

	[Theory]
	[InlineData(657435.0, true)]
	[InlineData(2958465.99999999, true)]
	[InlineData(657434.999, false)]
	[InlineData(2958466.0, false)]
	public void IsValidOaDate_NullableDouble_ReturnsExpected(double value, bool expected)
	{
		// Arrange
		_ = value;

		// Act & Assert
		value.IsValidOaDate().ShouldBe(expected);
	}

	[Theory]
	[InlineData(657435.0, true)]
	[InlineData(2958465.99999999, true)]
	[InlineData(657434.999, false)]
	[InlineData(2958466.0, false)]
	public void IsValidOaDate_Double_ReturnsExpected(double value, bool expected)
	{
		// Act & Assert
		value.IsValidOaDate().ShouldBe(expected);
	}
}
