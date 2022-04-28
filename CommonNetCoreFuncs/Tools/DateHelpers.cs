using System;

namespace CommonNetCoreFuncs.Tools
{
    public static class DateHelpers
    {
        public static double GetBusinessDays(DateTime? startDate, DateTime? endDate)
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

            return calcBusinessDays;
        }
    }
}
