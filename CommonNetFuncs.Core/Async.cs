using System.Collections.Concurrent;
using System.Data;
using System.Reflection;

namespace CommonNetFuncs.Core;

public static class Async
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this T obj, Task<T> task)
    {
        try
        {
            if (obj != null)
            {
                T? resultObject = await task;
                if (!typeof(T).IsSimpleType())
                {
                    lock (obj)
                    {
                        resultObject?.CopyPropertiesTo(obj);
                    }
                }
                else
                {
                    obj = resultObject;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this IList<T?> obj, Task<T?> task)
    {
        try
        {
            T? resultObject = await task;
            lock (obj)
            {
                obj.Add(resultObject);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T?> obj, Task<T?> task)
    {
        try
        {
            T? resultObject = await task;
            lock (obj)
            {
                obj.Add(resultObject);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this HashSet<T?> obj, Task<T?> task)
    {
        try
        {
            T? resultObject = await task;
            lock (obj)
            {
                obj.Add(resultObject);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this T obj, Func<Task<T>> task, SemaphoreSlim? semaphore)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync().ConfigureAwait(false);
            }
            if (obj != null)
            {
                T? resultObject = await task().ConfigureAwait(false);
                if (!typeof(T).IsSimpleType())
                {
                    lock (obj)
                    {
                        resultObject?.CopyPropertiesTo(obj);
                    }
                }
                else
                {
                    obj = resultObject;
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T?> obj, Func<Task<T?>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            if (obj != null)
            {
                T? resultObject = await task().ConfigureAwait(false);
                obj.Add(resultObject);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this IList<T?> obj, Func<Task<T?>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            if (obj != null)
            {
                T? resultObject = await task().ConfigureAwait(false);
                obj.Add(resultObject);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this HashSet<T?> obj, Func<Task<T?>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            if (obj != null)
            {
                T? resultObject = await task().ConfigureAwait(false);
                lock (obj)
                {
                    obj.Add(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this List<T> obj, Task<List<T>?> task)
    {
        try
        {
            List<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Task<HashSet<T>?> task)
    {
        try
        {
            HashSet<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Async task that returns the value to insert into obj object</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Task<List<T>?> task)
    {
        try
        {
            List<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this List<T> obj, Func<Task<List<T>>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            List<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Func<Task<HashSet<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            HashSet<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject, cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill obj variable asynchronously
    /// </summary>
    /// <param name="obj">Object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Func<Task<List<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            List<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject, cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this List<T> obj, Task<IEnumerable<T>> task)
    {
        try
        {
            IEnumerable<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Task<IEnumerable<T>> task)
    {
        try
        {
            IEnumerable<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this List<T> obj, Func<Task<IEnumerable<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            IEnumerable<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Func<Task<IEnumerable<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            IEnumerable<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject, cancellationToken: cancellationToken);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Task<IEnumerable<T>?> task)
    {
        try
        {
            IEnumerable<T>? resultObject = await task;
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Func<Task<IEnumerable<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            IEnumerable<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject, cancellationToken: cancellationToken);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
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
            ConcurrentBag<T>? resultObject = await task;
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Async task that returns the list of values to insert into obj object</param>
    public static async Task ObjectFill<T>(this HashSet<T> obj, Task<ConcurrentBag<T>?> task)
    {
        try
        {
            ConcurrentBag<T>? resultObject = await task;
            if (resultObject != null)
            {
                lock (obj)
                {
                    obj.AddRange(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Func<Task<ConcurrentBag<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            ConcurrentBag<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject, cancellationToken: cancellationToken);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
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
            List<T>? resultObject = await task;
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill list obj variable asynchronously
    /// </summary>
    /// <param name="obj">List object to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill<T>(this ConcurrentBag<T>? obj, Func<Task<List<T>>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            List<T>? resultObject = await task().ConfigureAwait(false);
            if (resultObject != null)
            {
                if (obj != null)
                {
                    obj.AddRangeParallel(resultObject, cancellationToken: cancellationToken);
                }
                else
                {
                    obj = new(resultObject);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
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
                lock (dt)
                {
                    dt.Load(reader);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill dt variable asynchronously
    /// </summary>
    /// <param name="dt">DataTable to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run that returns a DataTable object to insert into dt</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill(this DataTable dt, Func<Task<DataTable>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            using DataTable resultTable = await task().ConfigureAwait(false);
            if (resultTable != null)
            {
                await using DataTableReader reader = resultTable.CreateDataReader();
                lock (dt)
                {
                    dt.Load(reader);
                }
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
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
            lock (ms)
            {
                resultObject?.WriteTo(ms);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to fill ms variable asynchronously
    /// </summary>
    /// <param name="ms">MemoryStream to insert data into</param>
    /// <param name="task">Function that creates and returns the task to run and returns a MemoryStream object to insert into ms</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectFill(this MemoryStream ms, Func<Task<MemoryStream>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }
            await using MemoryStream resultObject = await task().ConfigureAwait(false);
            lock (ms)
            {
                resultObject?.WriteTo(ms);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

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
            if (props.Length > 0)
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
    }

    /// <summary>
    /// Task to update obj property asynchronously
    /// </summary>
    /// <param name="obj">Object to update</param>
    /// <param name="propertyName">Name of property to update within obj object</param>
    /// <param name="task">Function that creates and returns the task to run</param>
    /// <param name="semaphore">Semaphore to limit number of concurrent operations</param>
    public static async Task ObjectUpdate<T, UT>(this T? obj, string propertyName, Func<Task<UT>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
    {
        try
        {
            if (semaphore != null)
            {
                await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
            }

            PropertyInfo[] props = typeof(T).GetProperties();
            if (props.Length > 0)
            {
                PropertyInfo? prop = Array.Find(props, x => x.Name.StrEq(propertyName));
                if (prop != null)
                {
                    // Only start the task after acquiring the semaphore
                    UT value = await task().ConfigureAwait(false);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        finally
        {
            semaphore?.Release();
        }
    }

    public static async Task<ConcurrentBag<T>> RunAll<T>(this IEnumerable<Func<Task<T>>> tasks, SemaphoreSlim? semaphore = null, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false)
    {
        semaphore ??= new(1);
        cancellationTokenSource ??= new();
        ConcurrentBag<T> results = [];
        CancellationToken token = cancellationTokenSource.Token;
        await Parallel.ForEachAsync(tasks, async (task, _) =>
        {
            try
            {
                if (token.IsCancellationRequested)
                {
                    return; // Exit if cancellation is requested
                }
                await semaphore.WaitAsync(token).ConfigureAwait(false);
                results.Add(await task().ConfigureAwait(false));
            }
            catch (Exception ex)
            {
                if (breakOnError)
                {
                    await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }
                logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            finally
            {
                semaphore.Release();
            }
        }).ConfigureAwait(false);
        return results;
    }

    public static async Task RunAll(this IEnumerable<Func<Task>> tasks, SemaphoreSlim? semaphore = null, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false)
    {
        semaphore ??= new(1, 1);
        cancellationTokenSource ??= new();
        CancellationToken token = cancellationTokenSource.Token;
        await Parallel.ForEachAsync(tasks, async (task, _) =>
        {
            try
            {
                await semaphore.WaitAsync(token).ConfigureAwait(false);
                await task().ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (breakOnError)
                {
                    await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
                }
                logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
            }
            finally
            {
                semaphore.Release();
            }
        }).ConfigureAwait(false);
    }

    /// <summary>
    /// <para>Run a task with a semaphore to limit the number of concurrent operations.</para>
    /// <para>Useful when running tasks with Parallel.ForEachAsync where you want operations to run in parallel but
    /// sequentially within the same loop</para>
    /// </summary>
    /// <param name="task">Task to run with semaphore</param>
    /// <param name="semaphore">Semaphore to limit concurrent processes</param>
    /// <param name="cancellationTokenSource">Optional: Cancellation token source for concurrent operations</param>
    /// <param name="breakOnError">Optional: If true, will cancel operations using the same CancellationTokenSource</param>
    public static async Task RunAsyncWithSemaphore(this Task task, SemaphoreSlim? semaphore = null, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false, string? errorText = null)
    {
        semaphore ??= new(1, 1);
        cancellationTokenSource ??= new();
        CancellationToken token = cancellationTokenSource.Token;
        try
        {
            await semaphore.WaitAsync(token).ConfigureAwait(false);
            await task;
        }
        catch (Exception ex)
        {
            if (breakOnError)
            {
                await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
            }
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error{(errorText.IsNullOrWhiteSpace() ? string.Empty : $"\n{errorText}")}");
        }
        finally
        {
            semaphore.Release();
        }
    }
}

public class AsyncIntString
{
    public int AsyncInt { get; set; }

    public decimal AsyncDecimal { get; set; }

    public float AsyncFloat { get; set; }

    public string AsyncString { get; set; } = string.Empty;
}

public sealed class ResultTaskGroup<T>(List<Task<T>>? tasks = null, SemaphoreSlim? semaphore = null)
{
    public List<Task<T>> Tasks { get; set; } = tasks ?? [];

    public SemaphoreSlim? Semaphore { get; set; } = semaphore;

    public async Task<T[]> RunTasks(CancellationToken? cancellationToken = null)
    {
        if (Tasks.Count == 0)
        {
            return [];
        }

        if (Semaphore == null)
        {
            return await Task.WhenAll(Tasks);
        }

        T[] results = new T[Tasks.Count];

        await Parallel.ForAsync(0, Tasks.Count, cancellationToken ?? new(), async (i, cancellationToken) =>
        {
            try
            {
                await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
                results[i] = await Tasks[i].ConfigureAwait(false);
            }
            finally
            {
                Semaphore.Release();
            }
        });

        return results;
    }
}

public sealed class TaskGroup(List<Task>? tasks = null, SemaphoreSlim? semaphore = null)
{
    public List<Task> Tasks { get; set; } = tasks ?? [];

    public SemaphoreSlim? Semaphore { get; set; } = semaphore;

    public async Task RunTasks()
    {
        if (Tasks.Count == 0)
        {
            return;
        }

        if (Semaphore == null)
        {
            await Task.WhenAll(Tasks);
            return;
        }

        await Parallel.ForEachAsync(Tasks, async (task, _) =>
        {
            try
            {
                await Semaphore.WaitAsync(_).ConfigureAwait(false);
                await task.ConfigureAwait(false);
            }
            finally
            {
                Semaphore.Release();
            }
        });
    }
}
