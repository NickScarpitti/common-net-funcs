using System.Reflection;

namespace Common_Net_Funcs.Compare;

public class GenericCompare<T> : IEqualityComparer<T>
{
    /// <summary>
    /// Compare two class objects for value equality
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <returns>True if both objects contain identical values for all properties</returns>
    public bool Equals(T? obj1, T? obj2)
    {
        // They're both null.
        if (obj1 == null && obj2 == null)
        {
            return true;
        }
        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null)
        {
            return false;
        }
        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType())
        {
            return false;
        }

        foreach (PropertyInfo prop in obj1.GetType().GetProperties())
        {
            var aPropValue = prop.GetValue(obj1) ?? string.Empty;
            var bPropValue = prop.GetValue(obj2) ?? string.Empty;
            if (aPropValue.ToString() != bPropValue.ToString())
            {
                return false;
            }
        }
        return true;
    }

    /// <summary>
    /// Compare two class objects for value equality
    /// </summary>
    /// <param name="obj1"></param>
    /// <param name="obj2"></param>
    /// <param name="exemptProps">Names of properties to not include in the matching check</param>
    /// <returns>True if both objects contain identical values for all properties except for the ones identified by exemptProps</returns>
    public bool Equals(T? obj1, T? obj2, IEnumerable<string> exemptProps)
    {
        // They're both null.
        if (obj1 == null && obj2 == null)
        {
            return true;
        }
        // One is null, so they can't be the same.
        if (obj1 == null || obj2 == null)
        {
            return false;
        }
        // How can they be the same if they're different types?
        if (obj1.GetType() != obj1.GetType())
        {
            return false;
        }

        foreach (PropertyInfo prop in obj1.GetType().GetProperties())
        {
            if (!exemptProps.Contains(prop.Name))
            {
                var aPropValue = prop.GetValue(obj1) ?? string.Empty;
                var bPropValue = prop.GetValue(obj2) ?? string.Empty;
                if (aPropValue.ToString() != bPropValue.ToString())
                {
                    return false;
                }
            }
        }
        return true;
    }

    /// <summary>
    /// Get hash of an object
    /// </summary>
    /// <param name="obj"></param>
    /// <returns>Hash string of object</returns>
    public int GetHashCode(T obj)
    {
        PropertyInfo[]? props = obj?.GetType().GetProperties();
        string? allProps = null;
        if (props != null)
        {
            //Order by here makes this consistent
            foreach (PropertyInfo prop in props.OrderBy(x => x.Name))
            {
                var propValue = prop.GetValue(obj) ?? string.Empty;
                allProps += propValue;
            }
        }
        return allProps?.GetHashCode() ?? 0;
    }
}
