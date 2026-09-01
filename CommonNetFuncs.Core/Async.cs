using System.Collections.Concurrent;
using System.Data;
using System.Reflection;
using CommonNetFuncs.Core.Internal;
using static CommonNetFuncs.Core.ExceptionLocation;
using static CommonNetFuncs.Core.ReflectionCaches;

namespace CommonNetFuncs.Core;

public static class Async
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Task to fill <see cref="T"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">Object to insert data into.</param>
	/// <param name="task">Async task that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this T obj, Task<T> task) where T : class?
	{
		try
		{
			if (obj is not null)
			{
				T? resultObject = await task.ConfigureAwait(false);
				if (!typeof(T).IsSimpleType())
				{
					lock (obj)
					{
						resultObject?.CopyPropertiesTo(obj);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="T"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">Object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this T obj, Func<Task<T>> task) where T : class?
	{
		try
		{
			if (obj is not null)
			{
				T? resultObject = await task().ConfigureAwait(false);
				if (!typeof(T).IsSimpleType())
				{
					lock (obj)
					{
						resultObject?.CopyPropertiesTo(obj);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="T"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">Object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	public static async Task ObjectFill<T>(this T obj, Func<Task<T>> task, SemaphoreSlim? semaphore) where T : class
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync().ConfigureAwait(false);
			}
			if (obj is not null)
			{
				T? resultObject = await task().ConfigureAwait(false);
				if (!typeof(T).IsSimpleType())
				{
					lock (obj)
					{
						resultObject?.CopyPropertiesTo(obj);
					}
				}
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates an <see cref="IList{T}"/> from the awaited result of a Task, accepting either a nullable (Task<T?>) or non-nullable (Task<T>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be T or T? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Async task that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this IList<T?> obj, Task<TResult> task) where TResult : T?
	{
		try
		{
			TResult resultObject = await task.ConfigureAwait(false);
			lock (obj)
			{
				obj.Add(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates an <see cref="IList{T}"/> from the awaited result of a Task, accepting either a nullable (Task<T?>) or non-nullable (Task<T>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be T or T? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this IList<T?> obj, Func<Task<TResult>> task) where TResult : T?
	{
		try
		{
			TResult resultObject = await task().ConfigureAwait(false);
			lock (obj)
			{
				obj.Add(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates an <see cref="IList{T}"/> from the awaited result of a Task, accepting either a nullable (Task<T?>) or non-nullable (Task<T>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be T or T? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<T, TResult>(this IList<T?> obj, Func<Task<TResult>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default) where TResult : T?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			if (obj != null)
			{
				TResult resultObject = await task().ConfigureAwait(false);
				obj.Add(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Task to fill <see cref="HashSet{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Async task that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this HashSet<T?> obj, Task<T?> task)
	{
		try
		{
			T? resultObject = await task.ConfigureAwait(false);
			lock (obj)
			{
				obj.Add(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="HashSet{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this HashSet<T?> obj, Func<Task<T?>> task)
	{
		try
		{
			T? resultObject = await task().ConfigureAwait(false);
			lock (obj)
			{
				obj.Add(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="HashSet{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="List{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Async task that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this List<T> obj, Task<TResult> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task.ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="List{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Async task that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this List<T> obj, Func<Task<TResult>> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="List{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the list being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">List object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<T, TResult>(this List<T> obj, Func<Task<TResult>> task, SemaphoreSlim? semaphore, CancellationToken cancellationToken = default) where TResult : IEnumerable<T>?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			TResult resultObject = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="HashSet{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the set being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Async task that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this HashSet<T> obj, Task<TResult> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task.ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="HashSet{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the set being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Async task that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this HashSet<T> obj, Func<Task<TResult>> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="HashSet{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the set being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">HashSet object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<T, TResult>(this HashSet<T> obj, Func<Task<TResult>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default) where TResult : IEnumerable<T>?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			TResult resultObject = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Task to fill <see cref="ConcurrentBag{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Async task that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this ConcurrentBag<T?> obj, Task<T?> task)
	{
		try
		{
			T? resultObject = await task.ConfigureAwait(false);
			obj.Add(resultObject);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="ConcurrentBag{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	public static async Task ObjectFill<T>(this ConcurrentBag<T?> obj, Func<Task<T?>> task)
	{
		try
		{
			T? resultObject = await task().ConfigureAwait(false);
			obj.Add(resultObject);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to fill <see cref="ConcurrentBag{T}"/> obj variable asynchronously.
	/// </summary>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentBag{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the bag being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Async task that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this ConcurrentBag<T>? obj, Task<TResult> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task.ConfigureAwait(false);
			if (resultObject != null && obj != null)
			{
				obj.AddRangeParallel(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentBag{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the bag being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object.</param>
	public static async Task ObjectFill<T, TResult>(this ConcurrentBag<T>? obj, Func<Task<TResult>> task) where TResult : IEnumerable<T>?
	{
		try
		{
			TResult resultObject = await task().ConfigureAwait(false);
			if (resultObject != null && obj != null)
			{
				obj.AddRangeParallel(resultObject);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentBag{T}"/> from the awaited result of a Task, accepting either a nullable (Task<IEnumerable<T>?>) or non-nullable (Task<IEnumerable<T>>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="T">Element type of the bag being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be IEnumerable<T> or IEnumerable<T>? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentBag object to insert data into.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the list of values to insert into obj object.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<T, TResult>(this ConcurrentBag<T>? obj, Func<Task<TResult>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default) where TResult : IEnumerable<T>?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			TResult resultObject = await task().ConfigureAwait(false);
			if (resultObject != null && obj != null)
			{
				obj.AddRangeParallel(resultObject, cancellationToken: cancellationToken);
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentDictionary{TKey, TValue}"/> entry from the awaited result of a Task, accepting either a nullable (Task<TValue?>) or non-nullable (Task<TValue>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TKey">Key type of the dictionary being filled.</typeparam>
	/// <typeparam name="TValue">Value type of the dictionary being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be TValue or TValue? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentDictionary object to insert data into.</param>
	/// <param name="key">Key of the item to insert into the ConcurrentDictionary.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj with the provided key.</param>
	public static async Task ObjectFill<TKey, TValue, TResult>(this ConcurrentDictionary<TKey, TValue?>? obj, TKey key, Task<TResult> task) where TKey : notnull where TResult : TValue?
	{
		try
		{
			obj?[key] = await task.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentDictionary{TKey, TValue}"/> entry from the awaited result of a Task, accepting either a nullable (Task<TValue?>) or non-nullable (Task<TValue>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TKey">Key type of the dictionary being filled.</typeparam>
	/// <typeparam name="TValue">Value type of the dictionary being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be TValue or TValue? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentDictionary object to insert data into.</param>
	/// <param name="key">Key of the item to insert into the ConcurrentDictionary.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj with the provided key.</param>
	public static async Task ObjectFill<TKey, TValue, TResult>(this ConcurrentDictionary<TKey, TValue?>? obj, TKey key, Func<Task<TResult>> task) where TKey : notnull where TResult : TValue?
	{
		try
		{
			obj?[key] = await task().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="ConcurrentDictionary{TKey, TValue}"/> entry from the awaited result of a Task, accepting either a nullable (Task<TValue?>) or non-nullable (Task<TValue>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TKey">Key type of the dictionary being filled.</typeparam>
	/// <typeparam name="TValue">Value type of the dictionary being filled.</typeparam>
	/// <typeparam name="TResult">Inferred from the caller's task; must be TValue or TValue? — never specify this explicitly.</typeparam>
	/// <param name="obj">ConcurrentDictionary object to insert data into.</param>
	/// <param name="key">Key of the item to insert into the ConcurrentDictionary.</param>
	/// <param name="task">Function that creates and returns the task to run that returns the value to insert into obj with the provided key.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<TKey, TValue, TResult>(this ConcurrentDictionary<TKey, TValue?>? obj, TKey key, Func<Task<TResult>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default) where TKey : notnull where TResult : TValue?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			obj?[key] = await task().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="DataTable"/> from the awaited result of a Task, accepting either a nullable (Task<DataTable?>) or non-nullable (Task<DataTable>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be DataTable or DataTable? — never specify this explicitly.</typeparam>
	/// <param name="dt">DataTable to insert data into.</param>
	/// <param name="task">Async task that returns a <see cref="DataTable"/> object to insert into <paramref name="dt"/>.</param>
	public static async Task ObjectFill<TResult>(this DataTable dt, Task<TResult> task) where TResult : DataTable?
	{
		try
		{
			using TResult resultTable = await task.ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="DataTable"/> from the awaited result of a Task, accepting either a nullable (Task<DataTable?>) or non-nullable (Task<DataTable>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be DataTable or DataTable? — never specify this explicitly.</typeparam>
	/// <param name="dt">DataTable to insert data into.</param>
	/// <param name="task">Async task that returns a <see cref="DataTable"/> object to insert into <paramref name="dt"/>.</param>
	public static async Task ObjectFill<TResult>(this DataTable dt, Func<Task<TResult>> task) where TResult : DataTable?
	{
		try
		{
			using TResult resultTable = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="DataTable"/> from the awaited result of a Task, accepting either a nullable (Task<DataTable?>) or non-nullable (Task<DataTable>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be DataTable or DataTable? — never specify this explicitly.</typeparam>
	/// <param name="dt">DataTable to insert data into.</param>
	/// <param name="task"><see cref="Func{TResult}"/> that creates and returns the task to run that returns a DataTable object to insert into <paramref name="dt"/>.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<TResult>(this DataTable dt, Func<Task<TResult>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default) where TResult : DataTable?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			using TResult resultTable = await task().ConfigureAwait(false);
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
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Populates a <see cref="MemoryStream"/> from the awaited result of a Task, accepting either a nullable (Task<MemoryStream?>) or non-nullable (Task<MemoryStream>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be MemoryStream or MemoryStream? — never specify this explicitly.</typeparam>
	/// <param name="ms">MemoryStream to insert data into.</param>
	/// <param name="task">Async task that returns a <see cref="MemoryStream"/> object to insert into <paramref name="ms"/>.</param>
	public static async Task ObjectFill<TResult>(this MemoryStream ms, Task<TResult> task) where TResult : MemoryStream?
	{
		try
		{
			await using TResult resultObject = await task.ConfigureAwait(false);
#pragma warning disable S3998 // Threads should not lock on objects with weak identity
			lock (ms)
			{
				resultObject?.WriteTo(ms);
			}
#pragma warning restore S3998 // Threads should not lock on objects with weak identity
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="MemoryStream"/> from the awaited result of a Task, accepting either a nullable (Task<MemoryStream?>) or non-nullable (Task<MemoryStream>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be MemoryStream or MemoryStream? — never specify this explicitly.</typeparam>
	/// <param name="ms">MemoryStream to insert data into.</param>
	/// <param name="task">Async task that returns a <see cref="MemoryStream"/> object to insert into <paramref name="ms"/>.</param>
	public static async Task ObjectFill<TResult>(this MemoryStream ms, Func<Task<TResult>> task) where TResult : MemoryStream?
	{
		try
		{
			await using TResult resultObject = await task().ConfigureAwait(false);
#pragma warning disable S3998 // Threads should not lock on objects with weak identity
			lock (ms)
			{
				resultObject?.WriteTo(ms);
			}
#pragma warning restore S3998 // Threads should not lock on objects with weak identity
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Populates a <see cref="MemoryStream"/> from the awaited result of a Task, accepting either a nullable (Task<MemoryStream?>) or non-nullable (Task<MemoryStream>) task without triggering CS8620 at the call site.
	/// </summary>
	/// <typeparam name="TResult">Inferred from the caller's task; must be MemoryStream or MemoryStream? — never specify this explicitly.</typeparam>
	/// <param name="ms">MemoryStream to insert data into.</param>
	/// <param name="task"><see cref="Func{TResult}"/> that creates and returns the task to run and returns a <see cref="MemoryStream"/> object to insert into <paramref name="ms"/>.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectFill<TResult>(this MemoryStream ms, Func<Task<TResult>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default) where TResult : MemoryStream?
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}
			await using TResult resultObject = await task().ConfigureAwait(false);
#pragma warning disable S3998 // Threads should not lock on objects with weak identity
			lock (ms)
			{
				resultObject?.WriteTo(ms);
			}
#pragma warning restore S3998 // Threads should not lock on objects with weak identity
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Task to update an object <see langword="property"/> asynchronously.
	/// </summary>
	/// <param name="obj">Object to update.</param>
	/// <param name="propertyName">Name of property to update within <paramref name="obj"/>.</param>
	/// <param name="task">Async task to run that returns the value to assign to the property indicated.</param>
	public static async Task ObjectUpdate<TObj, TTask>(this TObj? obj, string propertyName, Task<TTask> task)
	{
		try
		{
			PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(TObj));
			if (props.Length > 0)
			{
				PropertyInfo? prop = Array.Find(props, x => x.Name.StrEq(propertyName));
				if (prop != null)
				{
					TTask value = await task.ConfigureAwait(false);
					prop.SetValue(obj, value);
				}
				else
				{
					throw new ArgumentException("Invalid property name for object update");
				}
			}
			else
			{
				throw new ArgumentException("Unable to get properties of object to update");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to update an object <see langword="property"/> asynchronously.
	/// </summary>
	/// <param name="obj">Object to update.</param>
	/// <param name="propertyName">Name of property to update within <paramref name="obj"/>.</param>
	/// <param name="task">Async task to run that returns the value to assign to the property indicated.</param>
	public static async Task ObjectUpdate<TObj, TTask>(this TObj? obj, string propertyName, Func<Task<TTask>> task)
	{
		try
		{
			PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(TObj));
			if (props.Length > 0)
			{
				PropertyInfo? prop = Array.Find(props, x => x.Name.StrEq(propertyName));
				if (prop != null)
				{
					TTask value = await task().ConfigureAwait(false);
					prop.SetValue(obj, value);
				}
				else
				{
					throw new ArgumentException("Invalid property name for object update");
				}
			}
			else
			{
				throw new ArgumentException("Unable to get properties of object to update");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
	}

	/// <summary>
	/// Task to update obj <see langword="property"/> asynchronously.
	/// </summary>
	/// <param name="obj">Object to update.</param>
	/// <param name="propertyName">Name of property to update within obj object.</param>
	/// <param name="task"><see cref="Func{TResult}"/> that creates and returns the task to run.</param>
	/// <param name="semaphore">Semaphore to limit number of concurrent operations.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public static async Task ObjectUpdate<TObj, TTask>(this TObj? obj, string propertyName, Func<Task<TTask>> task, SemaphoreSlim semaphore, CancellationToken cancellationToken = default)
	{
		try
		{
			if (semaphore != null)
			{
				await semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
			}

			PropertyInfo[] props = GetOrAddPropertiesFromReflectionCache(typeof(TObj));
			if (props.Length > 0)
			{
				PropertyInfo? prop = Array.Find(props, x => x.Name.StrEq(propertyName));
				if (prop != null)
				{
					// Only start the task after acquiring the semaphore
					TTask value = await task().ConfigureAwait(false);
					prop.SetValue(obj, value);
				}
				else
				{
					throw new ArgumentException("Invalid property name for object update");
				}
			}
			else
			{
				throw new ArgumentException("Unable to get properties of object to update");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		finally
		{
			semaphore?.Release();
		}
	}

	/// <summary>
	/// Run a group of tasks in parallel, with an optional semaphore to limit concurrency.
	/// </summary>
	/// <param name="tasks">Tasks to run</param>
	/// <param name="semaphore">Optional: Semaphore to limit concurrency.</param>
	/// <param name="cancellationTokenSource">Optional: Token source to cancel task with.</param>
	/// <param name="breakOnError">Optional: Triggers task cancellation if any task fails.</param>
	/// <returns>ConcurrentBag filled with task results.</returns>
	public static async Task<ConcurrentBag<T>> RunAll<T>(this IEnumerable<Func<Task<T>>> tasks, SemaphoreSlim? semaphore = null, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false)
	{
		cancellationTokenSource ??= new();
		ConcurrentBag<T> results = [];
		CancellationToken token = cancellationTokenSource.Token;
		await AsyncCompat.ForEachAsync(tasks, async (task, _) =>
		{
			bool semaphoreAcquired = false;
			try
			{
				if (token.IsCancellationRequested)
				{
					return; // Exit if cancellation is requested
				}

				if (semaphore != null)
				{
					await semaphore.WaitAsync(token).ConfigureAwait(false);
					semaphoreAcquired = true;
				}

				results.Add(await task().ConfigureAwait(false));
			}
			catch (Exception ex)
			{
				if (breakOnError)
				{
					await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
				}
				logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
			}
			finally
			{
				if (semaphoreAcquired)
				{
					semaphore!.Release();
				}
			}
		}).ConfigureAwait(false);
		return results;
	}

	/// <summary>
	/// Run a group of tasks in parallel, with an optional semaphore to limit concurrency.
	/// </summary>
	/// <param name="tasks">Tasks to run.</param>
	/// <param name="semaphore">Optional: Semaphore to limit concurrency.</param>
	/// <param name="cancellationTokenSource">Optional: Token source to cancel task with.</param>
	/// <param name="breakOnError">Optional: Triggers task cancellation if any task fails.</param>
	public static async Task RunAll(this IEnumerable<Func<Task>> tasks, SemaphoreSlim? semaphore = null, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false)
	{
		cancellationTokenSource ??= new();
		CancellationToken token = cancellationTokenSource.Token;
		await AsyncCompat.ForEachAsync(tasks, async (task, _) =>
		{
			bool semaphoreAcquired = false;
			try
			{
				if (token.IsCancellationRequested)
				{
					return; // Exit if cancellation is requested
				}

				if (semaphore != null)
				{
					await semaphore.WaitAsync(token).ConfigureAwait(false);
					semaphoreAcquired = true;
				}

				await task().ConfigureAwait(false);
			}
			catch (Exception ex)
			{
				if (breakOnError)
				{
					await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
				}
				logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
			}
			finally
			{
				if (semaphoreAcquired)
				{
					semaphore!.Release();
				}
			}
		}).ConfigureAwait(false);
	}

	/// <summary>
	/// <para>Run a task with a semaphore to limit the number of concurrent operations.</para>
	/// <remarks>Useful when running tasks with Parallel.ForEachAsync where you want operations to run in parallel but sequentially within the same loop</remarks>
	/// </summary>
	/// <param name="task">Task to run with semaphore.</param>
	/// <param name="semaphore">Semaphore to limit concurrent processes.</param>
	/// <param name="cancellationTokenSource">Optional: Cancellation token source for concurrent operations.</param>
	/// <param name="breakOnError">Optional: If <see langword="true"/>, will cancel operations using the same CancellationTokenSource.</param>
	public static async Task RunAsyncWithSemaphore(this Task task, SemaphoreSlim semaphore, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false, string? errorText = null)
	{
		cancellationTokenSource ??= new();
		CancellationToken token = cancellationTokenSource.Token;
		try
		{
			await semaphore.WaitAsync(token).ConfigureAwait(false);
			await task.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			if (breakOnError)
			{
				await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
			}
			logger.Error(ex, "{ExceptionLocation} Error{ErrorText}", ex.GetLocationOfException(), errorText.IsNullOrWhiteSpace() ? string.Empty : $"\n{errorText}");
		}
		finally
		{
			semaphore.Release();
		}
	}

	public static async Task<T?> RunAsyncWithSemaphore<T>(this Task<T?> task, SemaphoreSlim semaphore, CancellationTokenSource? cancellationTokenSource = null, bool breakOnError = false, string? errorText = null)
	{
		cancellationTokenSource ??= new();
		CancellationToken token = cancellationTokenSource.Token;
		try
		{
			await semaphore.WaitAsync(token).ConfigureAwait(false);
			return await task.ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			if (breakOnError)
			{
				await cancellationTokenSource.CancelAsync().ConfigureAwait(false);
			}
			logger.Error(ex, "{ExceptionLocation} Error{ErrorText}", ex.GetLocationOfException(), errorText.IsNullOrWhiteSpace() ? string.Empty : $"\n{errorText}");
		}
		finally
		{
			semaphore.Release();
		}
		return default;
	}
}

public class AsyncIntString
{
	public int AsyncInt { get; set; }

	public decimal AsyncDecimal { get; set; }

	public float AsyncFloat { get; set; }

	public string AsyncString { get; set; } = string.Empty;
}

/// <summary>
/// Used to run a group of tasks that return results in parallel, with an optional semaphore to limit concurrency.
/// </summary>
/// <typeparam name="T">Type of the result of the tasks.</typeparam>
/// <param name="tasks">Tasks to be run in parallel.</param>
/// <param name="semaphore">Optional: Semaphore used to limit concurrency.</param>
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
			cancellationToken?.ThrowIfCancellationRequested();
			foreach (Task task in Tasks.Where(x => x.Status == TaskStatus.Created))
			{
				cancellationToken?.ThrowIfCancellationRequested();
				task.Start();
			}

			return await Task.WhenAll(Tasks).ConfigureAwait(false);
		}

		T[] results = new T[Tasks.Count];
		await AsyncCompat.ForAsync(0, Tasks.Count, cancellationToken ?? new(), async (i, cancellationToken) =>
		{
			try
			{
				await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
				Task<T> task = Tasks[i];
				if (task.Status == TaskStatus.Created)
				{
					task.Start();
				}
				await Task.WhenAll(task).ConfigureAwait(false);
				results[i] = task.Result;
			}
			finally
			{
				Semaphore.Release();
			}
		}).ConfigureAwait(false);

		return results;
	}
}

/// <summary>
/// Used to run a group of tasks in parallel, with an optional semaphore to limit concurrency.
/// </summary>
/// <param name="tasks">Tasks to be run in parallel.</param>
/// <param name="semaphore">Optional: Semaphore used to limit concurrency.</param>
public sealed class TaskGroup(List<Task>? tasks = null, SemaphoreSlim? semaphore = null)
{
	public List<Task> Tasks { get; set; } = tasks ?? [];

	public SemaphoreSlim? Semaphore { get; set; } = semaphore;

	public async Task RunTasks(CancellationToken? cancellationToken = null)
	{
		if (Tasks.Count == 0)
		{
			return;
		}

		if (Semaphore == null)
		{
			cancellationToken?.ThrowIfCancellationRequested();
			foreach (Task task in Tasks.Where(x => x.Status == TaskStatus.Created))
			{
				cancellationToken?.ThrowIfCancellationRequested();
				task.Start();
			}
			await Task.WhenAll(Tasks).ConfigureAwait(false);
			Tasks.Clear();
			return;
		}

		await AsyncCompat.ForEachAsync(Tasks, cancellationToken ?? new(), async (task, cancellationToken) =>
		{
			try
			{
				await Semaphore.WaitAsync(cancellationToken).ConfigureAwait(false);
				if (task.Status == TaskStatus.Created)
				{
					task.Start();
				}
				await Task.WhenAll(task).ConfigureAwait(false);
			}
			finally
			{
				Semaphore.Release();
			}
		}).ConfigureAwait(false);
		Tasks.Clear();
	}
}
