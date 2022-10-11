using System.Data;
using System.Reflection;

namespace CommonNetCoreFuncs.Tools;

/// <summary>
/// Methods for making asynchronous programming easier
/// </summary>
public static class AsyncHelpers
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Task to update obj property asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="UT"></typeparam>
    /// <param name="obj">Object to update</param>
    /// <param name="propertyName">Name of property to update within obj object</param>
    /// <param name="task">Async task to run that returns the value to assign to the property indicated</param>
    /// <returns></returns>
    public static async Task ObjectUpdate<T, UT>(T? obj, string propertyName, Task<UT> task)
    {
        try
        {
            PropertyInfo[] props = typeof(T).GetProperties();
            if (props != null && props.Any())
            {
                PropertyInfo? prop = props.Where(x => x.Name.StrEq(propertyName)).FirstOrDefault();
                if (prop != null)
                {
                    UT value = await task;
                    prop.SetValue(obj, value);
                }
                else
                {
                    throw new Exception("Invalid property name for object update");
                }
            }
            else
            {
                throw new Exception("Unable to get properties of object to update");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ObjectUpdate Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="UT"></typeparam>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    /// <returns></returns>
    public static async Task ObjectFill<T, UT>(T? obj, Task<UT> task)
    {
        try
        {
            if (obj != null)
            {
                UT resultObject = await task;
                if (resultObject != null)
                {
                    resultObject.CopyPropertiesTo(obj);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ObjectFill Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <typeparam name="UT"></typeparam>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    /// <returns></returns>
    public static async Task ObjectFill<T>(List<T>? obj, Task<IEnumerable<T>?> task)
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
            logger.Error(ex, "ObjectFill Error");
        }
    }

    /// <summary>
    /// Task to fill dt variable asynchronously
    /// </summary>
    /// <param name="dt">DataTable to insert data into</param>
    /// <param name="task">Async task that returns a DataTable object to insert into dt</param>
    /// <returns></returns>
    public static async Task ObjectFill(DataTable dt, Task<DataTable> task)
    {
        try
        {
            using DataTable resultTable = await task;
            if (resultTable != null)
            {
                DataTableReader reader = resultTable.CreateDataReader();
                dt.Load(reader);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ObjectFill Error");
        }
    }

    /// <summary>
    /// Task to fill ms variable asynchronously
    /// </summary>
    /// <param name="ms">MemoryStream to insert data into</param>
    /// <param name="task">Async task that returns a MemoryStream object to insert into ms</param>
    /// <returns></returns>
    public static async Task ObjectFill(MemoryStream ms, Task<MemoryStream> task)
    {
        try
        {
            using MemoryStream resultObject = await task;
            if (resultObject != null)
            {
                resultObject.WriteTo(ms);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "ObjectFill Error");
        }
    }
}
