using CommonNetFuncs.Core;

namespace Core.Tests;

public sealed class DateTimeHelpersTests
{
    [Theory]
    [InlineData("2024-05-06", "2024-05-10", null, 5)] // Mon-Fri, no holidays
    [InlineData("2024-05-06", "2024-05-12", null, 5)] // Mon-Sun, no holidays
    [InlineData("2024-05-11", "2024-05-12", null, 0)] // Sat-Sun, no holidays
    [InlineData("2024-05-06", "2024-05-06", null, 1)] // Single day, Mon
    [InlineData("2024-05-11", "2024-05-11", null, 0)] // Single day, Sat
    [InlineData("2024-05-12", "2024-05-12", null, 0)] // Single day, Sun
    public void GetBusinessDays_BasicCases(string start, string end, object? _, int expected)
    {
        DateTime startDate = DateTime.Parse(start);
        DateTime endDate = DateTime.Parse(end);
        DateTimeHelpers.GetBusinessDays(startDate, endDate).ShouldBe(expected);
    }

    [Fact]
    public void GetBusinessDays_ReturnsZero_WhenStartOrEndIsNull()
    {
        DateTime date = DateTime.Today;
        DateTimeHelpers.GetBusinessDays(null, date).ShouldBe(0);
        DateTimeHelpers.GetBusinessDays(date, null).ShouldBe(0);
        DateTimeHelpers.GetBusinessDays(null, null).ShouldBe(0);
    }

    [Fact]
    public void GetBusinessDays_ExcludesExceptionDates()
    {
        DateTime start = new(2024, 5, 6); // Monday
        DateTime end = new(2024, 5, 10); // Friday
        List<DateTime> holidays = new() { new(2024, 5, 8), new(2024, 5, 9) }; // Wed, Thu
        DateTimeHelpers.GetBusinessDays(start, end, holidays).ShouldBe(3);
    }

    [Fact]
    public void GetBusinessDays_ExcludesExceptionDates_OnlyWeekdays()
    {
        DateTime start = new(2024, 5, 6); // Monday
        DateTime end = new(2024, 5, 12); // Sunday
        List<DateTime> holidays = new() { new(2024, 5, 11), new(2024, 5, 8) }; // Sat, Wed
        // Only Wed should be excluded
        DateTimeHelpers.GetBusinessDays(start, end, holidays).ShouldBe(4);
    }

    [Theory]
    [InlineData("2024-05-08", DayOfWeek.Monday, "2024-05-06")] // Wed, get Monday
    [InlineData("2024-05-08", DayOfWeek.Wednesday, "2024-05-08")] // Wed, get Wed
    [InlineData("2024-05-12", DayOfWeek.Sunday, "2024-05-12")] // Sun, get Sun
    [InlineData("2024-05-12", DayOfWeek.Monday, "2024-05-06")] // Sun, get Monday
    public void GetDayOfWeek_ReturnsCorrectDate(string dateStr, DayOfWeek dow, string expectedStr)
    {
        DateTime date = DateTime.Parse(dateStr);
        DateTime expected = DateTime.Parse(expectedStr);
        date.GetDayOfWeek(dow).ShouldBe(expected);
    }

    [Theory]
    [InlineData(2, 2024, "2024-02-01", "2024-02-29")] // Leap year
    [InlineData(2, 2023, "2023-02-01", "2023-02-28")] // Non-leap year
    [InlineData(1, 2024, "2024-01-01", "2024-01-31")]
    [InlineData(12, 2024, "2024-12-01", "2024-12-31")]
    public void GetMonthBoundaries_ByMonthYear_ReturnsCorrectBoundaries(int month, int year, string first, string last)
    {
        (DateTime firstDay, DateTime lastDay) = DateTimeHelpers.GetMonthBoundaries(month, year);
        firstDay.ShouldBe(DateTime.Parse(first));
        lastDay.ShouldBe(DateTime.Parse(last));
    }

    [Fact]
    public void GetMonthBoundaries_ByDate_ReturnsCorrectBoundaries()
    {
        DateTime date = new(2024, 2, 15);
        (DateTime firstDay, DateTime lastDay) = date.GetMonthBoundaries();
        firstDay.ShouldBe(new DateTime(2024, 2, 1));
        lastDay.ShouldBe(new DateTime(2024, 2, 29));
    }

    [Theory]
    [InlineData(2, 2024, "2024-02-01")]
    [InlineData(12, 2024, "2024-12-01")]
    public void GetFirstDayOfMonth_ByMonthYear_ReturnsFirstDay(int month, int year, string expected)
    {
        DateTimeHelpers.GetFirstDayOfMonth(month, year).ShouldBe(DateTime.Parse(expected));
    }

    [Fact]
    public void GetFirstDayOfMonth_ByDate_ReturnsFirstDay()
    {
        DateTime date = new(2024, 5, 15);
        date.GetFirstDayOfMonth().ShouldBe(new DateTime(2024, 5, 1));
    }

    [Theory]
    [InlineData(2, 2024, "2024-02-29")]
    [InlineData(2, 2023, "2023-02-28")]
    [InlineData(4, 2024, "2024-04-30")]
    public void GetLastDayOfMonth_ByMonthYear_ReturnsLastDay(int month, int year, string expected)
    {
        DateTimeHelpers.GetLastDayOfMonth(month, year).ShouldBe(DateTime.Parse(expected));
    }

    [Fact]
    public void GetLastDayOfMonth_ByDate_ReturnsLastDay()
    {
        DateTime date = new(2024, 5, 15);
        date.GetLastDayOfMonth().ShouldBe(new DateTime(2024, 5, 31));
    }

    [Fact]
    public void IsValidOaDate_NullableDouble_ReturnsFalseIfNull()
    {
        double? value = null;
        value.IsValidOaDate().ShouldBeFalse();
    }

    [Theory]
    [InlineData(657435.0, true)]
    [InlineData(2958465.99999999, true)]
    [InlineData(657434.999, false)]
    [InlineData(2958466.0, false)]
    public void IsValidOaDate_NullableDouble_ReturnsExpected(double value, bool expected)
    {
        _ = value;
        value.IsValidOaDate().ShouldBe(expected);
    }

    [Theory]
    [InlineData(657435.0, true)]
    [InlineData(2958465.99999999, true)]
    [InlineData(657434.999, false)]
    [InlineData(2958466.0, false)]
    public void IsValidOaDate_Double_ReturnsExpected(double value, bool expected)
    {
        value.IsValidOaDate().ShouldBe(expected);
    }
}
