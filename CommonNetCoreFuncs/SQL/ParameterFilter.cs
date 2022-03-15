using System;

namespace CommonNetCoreFuncs.SQL
{
    public static class ParameterFilter
    {
        public static bool IsClean(this string parameter)
        {
            return string.IsNullOrWhiteSpace(parameter) || (!parameter.Contains(';') && !parameter.Contains('\'') && !parameter.Contains('[') && !parameter.Contains(']') && !parameter.Contains('"') && !parameter.Contains('`') &&
                !parameter.Contains("select ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("from ", StringComparison.InvariantCultureIgnoreCase) &&
                !parameter.Contains("where ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("having ", StringComparison.InvariantCultureIgnoreCase) &&
                !parameter.Contains("select ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("group by ", StringComparison.InvariantCultureIgnoreCase) &&
                !parameter.Contains("order by ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("join ", StringComparison.InvariantCultureIgnoreCase) &&
                !parameter.Contains("union ", StringComparison.InvariantCultureIgnoreCase) && !parameter.Contains("max(", StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
