namespace Common_Net_Funcs.Tools;

/// <summary>
/// Helpers for dealing with DateTimes
/// </summary>
public static class DateHelpers
{
    /// <summary>
    /// Get the number of business days found within a date range (inclusive)
    /// </summary>
    /// <param name="startDate">First date of range to get business days for</param>
    /// <param name="endDate">Last date of range to get business days for</param>
    /// <param name="exceptionDates">Days that will not be counted as a business day such as holidays</param>
    /// <returns>The number of business days between the start and end date</returns>
    public static int GetBusinessDays(DateTime? startDate, DateTime? endDate, List<DateTime>? exceptionDates = null)
    {
        if (startDate == null || endDate == null)
        {
            return 0;
        }

        DateTime sDate = (DateTime)startDate;
        DateTime eDate = (DateTime)endDate;

        decimal calcBusinessDays = 1 + ((((decimal)(eDate - sDate).TotalDays * 5m) - ((sDate.DayOfWeek - eDate.DayOfWeek) * 2m)) / 7m);

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
            int exceptionDays = exceptionDates.Count(x => x >= sDate && x <= eDate && x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday);
            calcBusinessDays -= exceptionDays;
        }

        return (int)calcBusinessDays;
    }

    /// <summary>
    /// Get the date of the day requested given the week provided via the dateTime parameter
    /// </summary>
    /// <param name="dateTime">Date to search for the day of the week for</param>
    /// <param name="dayOfWeek">The day of the indicated week to return the date for</param>
    /// <returns>The date of the day of the week indicated by dayOfWeek</returns>
    public static DateTime GetDayOfWeek(this in DateTime dateTime, DayOfWeek dayOfWeek = DayOfWeek.Monday)
    {
        int diff = (7 + (dateTime.DayOfWeek - dayOfWeek)) % 7;
        return dateTime.AddDays(-1 * diff).Date;
    }

    /// <summary>
    /// Gets the first and last day of the month provided
    /// </summary>
    /// <returns>Tuple containing the first and last date of the specified month</returns>
    public static (DateTime firstDay, DateTime lastDay) GetMonthBoundaries(int month, int year)
    {
        // Get the 1st day of the month (always day 1)
        DateTime firstDay = new(year, month, 1);

        // Calculate the last day of the month
        DateTime lastDay = firstDay.AddMonths(1).AddSeconds(-1);

        // Set the out parameters
        return (firstDay, lastDay);
    }

    /// <summary>
    /// Gets the first and last day of the month provided
    /// </summary>
    /// <returns>Tuple containing the first and last date of the specified month</returns>
    public static (DateTime firstDay, DateTime lastDay) GetMonthBoundaries(DateTime date)
    {
        return GetMonthBoundaries(date.Month, date.Year);
    }
}
