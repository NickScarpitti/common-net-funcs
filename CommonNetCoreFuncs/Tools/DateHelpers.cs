namespace CommonNetCoreFuncs.Tools
{
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
        /// <returns></returns>
        public static int GetBusinessDays(DateTime? startDate, DateTime? endDate, List<DateTime>? exceptionDates = null)
        {
            
            if (startDate == null || endDate == null)
            {
                return 0;
            }

            DateTime sDate = (DateTime)startDate;
            DateTime eDate = (DateTime)endDate;

            double calcBusinessDays = 1 + ((eDate - sDate).TotalDays * 5 - (sDate.DayOfWeek - eDate.DayOfWeek) * 2) / 7;

            if (eDate.DayOfWeek == DayOfWeek.Saturday) calcBusinessDays--;
            if (sDate.DayOfWeek == DayOfWeek.Sunday) calcBusinessDays--;

            if (exceptionDates != null)
            {
                int exceptionDays = exceptionDates.Where(x => x >= sDate && x <= eDate && x.DayOfWeek != DayOfWeek.Saturday && x.DayOfWeek != DayOfWeek.Sunday).Count();
                calcBusinessDays -= exceptionDays;
            }

            return (int)calcBusinessDays;
        }

        /// <summary>
        /// Get the date of the day requested given the week provided via the dateTime parameter
        /// </summary>
        /// <param name="dateTime">Date to search for the day of the week for</param>
        /// <param name="dayOfWeek">The day of the indicated week to return the date for</param>
        /// <returns></returns>
        public static DateTime GetDayOfWeek(this DateTime dateTime, DayOfWeek dayOfWeek = DayOfWeek.Monday)
        {
            int diff = (7 + (dateTime.DayOfWeek - dayOfWeek)) % 7;
            return dateTime.AddDays(-1 * diff).Date;
        }
    }
}
