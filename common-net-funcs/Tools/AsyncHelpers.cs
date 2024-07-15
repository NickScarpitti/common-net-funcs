using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using MemoryPack;
using MessagePack;

namespace Common_Net_Funcs.Tools;

[MemoryPackable]
[MessagePackObject(true)]
public partial class AsyncIntString
{
    public AsyncIntString()
    {
        AsyncString = string.Empty;
    }
    public int AsyncInt { get; set; }
    public string AsyncString { get; set; }
}

/// <summary>
/// Methods for making asynchronous programming easier
/// </summary>
public static class AsyncHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Task to update obj property asynchronously
    /// </summary>
    /// <param name="obj">Object to update</param>
    /// <param name="propertyName">Name of property to update within obj object</param>
    /// <param name="task">Async task to run that returns the value to assign to the property indicated</param>
    public static async Task ObjectUpdate<T, UT>(this T? obj, string propertyName, Task<UT> task)
    {
        try
        {
            PropertyInfo[] props = typeof(T).GetProperties();
            if (props?.AnyFast() == true)
            {
                PropertyInfo? prop = Array.Find(props, x => x.Name.StrEq(propertyName));
                if (prop != null)
                {
                    UT value = await task;
                    prop.SetValue(obj, value);
                }
                else
                {
                    throw new("Invalid property name for object update");
                }
            }
            else
            {
                throw new("Unable to get properties of object to update");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this T? obj, Task<T?> task)
    {
        try
        {
            if (obj != null)
            {
                T? resultObject = await task;
                resultObject?.CopyPropertiesTo(obj);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this List<T>? obj, Task<List<T>?> task)
    {
        try
        {
            if (obj != null)
            {
                List<T>? resultObject = await task;
                if (resultObject != null)
                {
                    obj.AddRange(resultObject);
                    resultObject = null;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Task<List<T>?> task)
    {
        try
        {
            if (obj != null)
            {
                List<T>? resultObject = await task;
                if (resultObject != null)
                {
                    obj.AddRangeParallel(resultObject);
                    resultObject = null;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Task<ConcurrentBag<T>?> task)
    {
        try
        {
            if (obj != null)
            {
                ConcurrentBag<T>? resultObject = await task;
                if (resultObject != null)
                {
                    obj.AddRangeParallel(resultObject);
                    resultObject = null;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this List<T>? obj, Task<IEnumerable<T>?> task)
    {
        try
        {
            if (obj != null)
            {
                IEnumerable<T>? resultObject = await task;
                if (resultObject != null)
                {
                    obj.AddRange(resultObject);
                    resultObject = null;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill dt variable asynchronously
    /// </summary>
    /// <param name="dt">DataTable to insert data into</param>
    /// <param name="task">Async task that returns a DataTable object to insert into dt</param>
    public static async Task ObjectFill(this DataTable dt, Task<DataTable> task)
    {
        try
        {
            using DataTable resultTable = await task;
            if (resultTable != null)
            {
                await using DataTableReader reader = resultTable.CreateDataReader();
                dt.Load(reader);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Task to fill ms variable asynchronously
    /// </summary>
    /// <param name="ms">MemoryStream to insert data into</param>
    /// <param name="task">Async task that returns a MemoryStream object to insert into ms</param>
    public static async Task ObjectFill(this MemoryStream ms, Task<MemoryStream> task)
    {
        try
        {
            await using MemoryStream resultObject = await task;
            resultObject?.WriteTo(ms);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfEexception()} Error");
        }
    }
}
