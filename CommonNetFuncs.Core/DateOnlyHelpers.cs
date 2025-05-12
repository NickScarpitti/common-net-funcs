namespace CommonNetFuncs.Core;

/// <summary>
/// Helpers for dealing with DateOnlys
/// </summary>
public static class DateOnlyHelpers
{
    /// <summary>
    /// Get the number of business days found within a date range (inclusive)
    /// </summary>
    /// <param name="startDate">First date of range to get business days for</param>
    /// <param name="endDate">Last date of range to get business days for</param>
    /// <param name="exceptionDates">Days that will not be counted as a business day such as holidays</param>
    /// <returns>The number of business days between the start and end date</returns>
    public static int GetBusinessDays(DateOnly? startDate, DateOnly? endDate, List<DateOnly>? exceptionDates = null)
    {
        if ((startDate == null) || (endDate == null))
        {
            return 0;
        }

        DateOnly sDate = (DateOnly)startDate;
        DateOnly eDate = (DateOnly)endDate;

        decimal calcBusinessDays = 1 + ((((eDate.DayNumber - sDate.DayNumber) * 5m) - ((sDate.DayOfWeek - eDate.DayOfWeek) * 2m)) / 7m);

        if (eDate.DayOfWeek == DayOfWeek.Saturday)
        {
            calcBusinessDays--;
        }

        if (sDate.DayOfWeek == DayOfWeek.Sunday)
        {
            calcBusinessDays--;
        }

        if (exceptionDates != null)
        {
            int exceptionDays = exceptionDates.Count(x => (x >= sDate) && (x <= eDate) && (x.DayOfWeek != DayOfWeek.Saturday) && (x.DayOfWeek != DayOfWeek.Sunday));
            calcBusinessDays -= exceptionDays;
        }

        return (int)calcBusinessDays;
    }

    /// <summary>
    /// Get the date of the day requested given the week provided via the date parameter
    /// </summary>
    /// <param name="date">Date to search for the day of the week for</param>
    /// <param name="dayOfWeek">The day of the indicated week to return the date for</param>
    /// <returns>The date of the day of the week indicated by dayOfWeek</returns>
    public static DateOnly GetDayOfWeek(this in DateOnly date, DayOfWeek dayOfWeek = DayOfWeek.Monday)
    {
        int diff = (7 + (date.DayOfWeek - dayOfWeek)) % 7;
        return date.AddDays((-1) * diff);
    }

    /// <summary>
    /// Gets the first and last day of the month provided
    /// </summary>
    /// <returns>Tuple containing the first and last date of the specified month</returns>
    public static (DateOnly firstDay, DateOnly lastDay) GetMonthBoundaries(int month, int year)
    { return (GetFirstDayOfMonth(month, year), GetLastDayOfMonth(month, year)); }

    /// <summary>
    /// Gets the first and last day of the month provided
    /// </summary>
    /// <returns>Tuple containing the first and last date of the specified month</returns>
    public static (DateOnly firstDay, DateOnly lastDay) GetMonthBoundaries(this DateOnly date)
    { return GetMonthBoundaries(date.Month, date.Year); }

    /// <summary>
    /// Gets the first day of the month provided
    /// </summary>
    /// <returns>DateOnly for the first day of the specified month</returns>
    public static DateOnly GetFirstDayOfMonth(int month, int year) { return new(year, month, 1); }

    /// <summary>
    /// Gets the first day of the month provided
    /// </summary>
    /// <returns>DateOnly for the first day of the specified month</returns>
    public static DateOnly GetFirstDayOfMonth(this DateOnly date) { return GetFirstDayOfMonth(date.Month, date.Year); }

    /// <summary>
    /// Gets the lest day of the month provided
    /// </summary>
    /// <returns>DateOnly for the lest day of the specified month</returns>
    public static DateOnly GetLastDayOfMonth(int month, int year)
    { return new(year, month, DateTime.DaysInMonth(year, month)); }

    /// <summary>
    /// Gets the lest day of the month provided
    /// </summary>
    /// <returns>DateOnly for the lest day of the specified month</returns>
    public static DateOnly GetLastDayOfMonth(this DateOnly date) { return GetLastDayOfMonth(date.Month, date.Year); }

    /// <summary>
    /// Returns whether or not the provided double value is a valid OADate
    /// </summary>
    /// <param name="oaDate">Double to check as OADate</param>
    /// <returns>True if double passed in represents a valid OA Date</returns>
    public static bool IsValidOaDate(this double? oaDate)
    { return (oaDate != null) && (oaDate >= 657435.0) && (oaDate <= 2958465.99999999); }

    public static DateOnly GetToday() { return DateOnly.FromDateTime(DateTime.Today); }
}
