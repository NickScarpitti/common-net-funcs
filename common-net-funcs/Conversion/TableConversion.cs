using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace Common_Net_Funcs.Conversion;
public class DataTableConversion
{
    /// <summary>
    /// Convert datatable to equivalent list of specified class
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToList<T>(DataTable table) where T : class, new()
    {
        List<Tuple<DataColumn, PropertyInfo>> map = new List<Tuple<DataColumn, PropertyInfo>>();

        foreach (PropertyInfo propertyInfo in typeof(T).GetProperties())
        {
            if (table.Columns.Contains(propertyInfo.Name))
            {
                map.Add(new Tuple<DataColumn, PropertyInfo>(table.Columns[propertyInfo.Name]!, propertyInfo));
            }
        }

        List<T?> list = new List<T?>(table.Rows.Count);
        foreach (DataRow row in table.Rows)
        {
            if (row == null)
            {
                list.Add(null);
                continue;
            }
            T item = new T();
            foreach (Tuple<DataColumn, PropertyInfo> pair in map)
            {
                object? value = row[pair.Value1!];

                //Handle issue where DB returns Int16 for boolean values
                if ((value.GetType() == typeof(short) || value.GetType() == typeof(short?)) && 
                    (pair.Value2!.PropertyType == typeof(bool) || pair.Value2!.PropertyType == typeof(bool?)))
                {
                    pair.Value2!.SetValue(item, value is not DBNull ? Convert.ToBoolean(value) : null);
                }
                else
                {
                    pair.Value2!.SetValue(item, value is not DBNull ? value : null);
                }
            }
            list.Add(item);
        }
        return list;
    }

    /// <summary>
    /// Convert datatable to equivalent list of specified class using a Parallel.Foreach loop to get data from each row
    /// </summary>
    /// <typeparam name="T">Class to use in table conversion</typeparam>
    /// <param name="table">Table to convert to list</param>
    /// <param name="maxDegreeOfParallelism">Parallelism parameter to be used in Parallel.Foreach loop</param>
    /// <returns>List containing table values as the specified class</returns>
    public static List<T?> ConvertDataTableToListParallel<T>(DataTable table, int maxDegreeOfParallelism = -1) where T : class, new()
    {
        ConcurrentBag<Tuple<DataColumn, PropertyInfo>> map = new ConcurrentBag<Tuple<DataColumn, PropertyInfo>>();

        Parallel.ForEach(typeof(T).GetProperties(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, propertyInfo =>
        {
            if (table.Columns.Contains(propertyInfo.Name))
            {
                map.Add(new Tuple<DataColumn, PropertyInfo>(table.Columns[propertyInfo.Name]!, propertyInfo));
            }
        });

        ConcurrentBag<T?> bag = new ConcurrentBag<T?>();
        Parallel.ForEach(table.AsEnumerable(), new() { MaxDegreeOfParallelism = maxDegreeOfParallelism }, row =>
        {
            T? item = new T();
            if (row != null) 
            { 
                foreach (Tuple<DataColumn, PropertyInfo> pair in map)
                {
                    object? value = row[pair.Value1!];

                    //Handle issue where DB returns Int16 for boolean values
                    if ((value.GetType() == typeof(short) || value.GetType() == typeof(short?)) &&
                        (pair.Value2!.PropertyType == typeof(bool) || pair.Value2!.PropertyType == typeof(bool?)))
                    {
                        pair.Value2!.SetValue(item, value is not DBNull ? Convert.ToBoolean(value) : null);
                    }
                    else
                    {
                        pair.Value2!.SetValue(item, value is not DBNull ? value : null);
                    }
                }
            }
            else
            {
                item = null;
            }
            bag.Add(item);
        });
        return bag.ToList();
    }
}

sealed class Tuple<T1, T2>
{
    public Tuple() { }
    public Tuple(T1 value1, T2 value2) { Value1 = value1; Value2 = value2; }
    public T1? Value1 { get; set; }
    public T2? Value2 { get; set; }
}
