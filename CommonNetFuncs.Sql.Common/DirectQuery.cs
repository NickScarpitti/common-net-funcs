using System.Data;
using System.Data.Common;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using CommonNetFuncs.Core;
using FastExpressionCompiler;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.Sql.Common;

/// <summary>
/// Interact with databases by using direct queries
/// </summary>
public static class DirectQuery
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

	/// <summary>
	/// Returns a DataTable using the SQL and data connection passed to the function
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns><see cref="DataTable"/> containing the results of the database command</returns>
	public static Task<DataTable> GetDataTable(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
	{
		return GetDataTableInternal(conn, cmd, commandTimeoutSeconds, maxRetry, cancellationToken);
	}

	/// <summary>
	/// Reads data using into a DataTable object using the provided database connection and command
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns><see cref="DataTable"/> containing the results of the database command</returns>
	internal static async Task<DataTable> GetDataTableInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
	{
		DataTable dt = new();
		for (int i = 0; i < maxRetry; i++)
		{
			try
			{
				cmd.CommandTimeout = commandTimeoutSeconds;
				await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
				await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);
				dt.Load(reader);
				break;
			}
			catch (DbException ex)
			{
				logger.Error(ex, "DB Error @ {ErrorLocation}", ex.GetLocationOfException());
				dt.Clear();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
				dt.Clear();
			}
			finally
			{
				await conn.CloseAsync().ConfigureAwait(false);
			}
		}
		return dt;
	}

	/// <summary>
	/// Returns a DataTable using the SQL and data connection passed to the function
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns><see cref="DataTable"/> containing the results of the database command</returns>
	public static DataTable GetDataTableSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
	{
		return GetDataTableInternalSynchronous(conn, cmd, commandTimeoutSeconds, maxRetry);
	}

	/// <summary>
	/// Reads data using into a DataTable object using the provided database connection and command
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns><see cref="DataTable"/> containing the results of the database command</returns>
	internal static DataTable GetDataTableInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
	{
		DataTable dt = new();
		for (int i = 0; i < maxRetry; i++)
		{
			try
			{
				cmd.CommandTimeout = commandTimeoutSeconds;
				conn.Open();
				using DbDataReader reader = cmd.ExecuteReader();
				dt.Load(reader);
				break;
			}
			catch (DbException ex)
			{
				logger.Error(ex, "DB Exception @ {ErrorLocation}", ex.GetLocationOfException());
				dt.Clear();
			}
			catch (Exception ex)
			{
				logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
				dt.Clear();
			}
			finally
			{
				conn.Close();
			}
		}
		return dt;
	}

	/// <summary>
	/// Execute an update query asynchronously
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
	public static Task<UpdateResult> RunUpdateQuery(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
	{
		return RunUpdateQueryInternal(conn, cmd, commandTimeoutSeconds, maxRetry, cancellationToken);
	}

	/// <summary>
	/// Execute an update query asynchronously
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
	internal static async Task<UpdateResult> RunUpdateQueryInternal(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3, CancellationToken cancellationToken = default)
	{
		for (int i = 0; i < maxRetry; i++)
		{
			try
			{
				UpdateResult updateResult = new();
				cmd.CommandTimeout = commandTimeoutSeconds;
				await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
				updateResult.RecordsChanged = await cmd.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
				updateResult.Success = true;
				return updateResult;
			}
			catch (DbException ex)
			{
				logger.Error(ex, "DB Error @{ErrorLocation}", ex.GetLocationOfException());
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error executing update query @ {ErrorLocation}", ex.GetLocationOfException());
			}
			finally
			{
				await conn.CloseAsync().ConfigureAwait(false);
			}
		}
		return new();
	}

	/// <summary>
	/// Execute an update query synchronously
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
	public static UpdateResult RunUpdateQuerySynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
	{
		return RunUpdateQueryInternalSynchronous(conn, cmd, commandTimeoutSeconds, maxRetry);
	}

	/// <summary>
	/// Execute an update query synchronously
	/// </summary>
	/// <param name="conn">Database connection to use</param>
	/// <param name="cmd">Command to use with parameters</param>
	/// <param name="commandTimeoutSeconds">Query execution timeout length in seconds</param>
	/// <param name="maxRetry">Number of times to re-try executing the command on failure</param>
	/// <returns>UpdateResult containing the number of records altered and whether the query executed successfully</returns>
	internal static UpdateResult RunUpdateQueryInternalSynchronous(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3)
	{
		for (int i = 0; i < maxRetry; i++)
		{
			try
			{
				UpdateResult updateResult = new();
				cmd.CommandTimeout = commandTimeoutSeconds;
				conn.Open();
				updateResult.RecordsChanged = cmd.ExecuteNonQuery();
				updateResult.Success = true;
				return updateResult;
			}
			catch (DbException ex)
			{
				logger.Error(ex, "DB Error @{ErrorLocation}", ex.GetLocationOfException());
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error executing update query @ {ErrorLocation}", ex.GetLocationOfException());
			}
			finally
			{
				conn.Close();
			}
		}
		return new();
	}

	// Cache for compiled mapping delegates

	#region Caching

	private static readonly CacheManager<Type, Delegate> MappingCache = new();

	public static ICacheManagerApi<Type, Delegate> CacheManager => MappingCache;

	/// <summary>
	/// Clears LimitedMappingCache cache and sets the size to the specified value.
	/// </summary>
	private static Delegate GetOrAddPropertiesFromMappingCache<T>(Type key) where T : class, new()
	{
		bool isLimitedCache = CacheManager.IsUsingLimitedCache();
		if (isLimitedCache ? CacheManager.GetLimitedCache().TryGetValue(key, out Delegate? function) :
						CacheManager.GetCache().TryGetValue(key, out function))
		{
			return function!;
		}

		function = CreateMapperDelegate<T>(key);
		if (isLimitedCache)
		{
			CacheManager.TryAddLimitedCache(key, function);
		}
		else
		{
			CacheManager.TryAddCache(key, function);
		}
		return function;
	}

	#endregion

	// Creates a mapping delegate for a type and caches it
	private static Func<IDataReader, T> GetOrCreateMapperDelegate<T>(bool useCache) where T : class, new()
	{
		Type type = typeof(T);

		return useCache ? (Func<IDataReader, T>)GetOrAddPropertiesFromMappingCache<T>(type) :
						CreateMapperDelegate<T>(type);
	}

	private static Func<IDataReader, T> CreateMapperDelegate<T>(Type type) where T : class, new()
	{
		// Parameter expressions
		ParameterExpression readerParam = Expression.Parameter(typeof(IDataReader), "reader");
		MethodInfo getValueMethod = typeof(IDataRecord).GetMethod("GetValue")!;
		MethodInfo getOrdinalMethod = typeof(IDataRecord).GetMethod("GetOrdinal")!;
		MethodInfo isDBNullMethod = typeof(IDataRecord).GetMethod("IsDBNull")!;

		// Create new instance expression
		NewExpression instanceExp = Expression.New(type);

		// Get all settable properties
		IEnumerable<PropertyInfo> properties = type.GetProperties(BindingFlags.Public | BindingFlags.Instance).Where(x => x.CanWrite);

		// Create property assignments
		List<MemberBinding> assignments = [];

		foreach (PropertyInfo prop in properties)
		{
			// Create GetOrdinal call
			MethodCallExpression getOrdinalCall = Expression.Call(readerParam, getOrdinalMethod, Expression.Constant(prop.Name));

			// Create IsDBNull check
			MethodCallExpression isDbNullCall = Expression.Call(readerParam, isDBNullMethod, getOrdinalCall);

			// Get value from reader
			MethodCallExpression getValue = Expression.Call(readerParam, getValueMethod, getOrdinalCall);

			// Convert value to property type if needed
			//UnaryExpression convertedValue = Expression.Convert(getValue, prop.PropertyType);
			Expression convertedValue;
			if (prop.PropertyType == typeof(int))
			{
				// Convert to Int32 from Int64
				convertedValue = Expression.Convert(Expression.Call(typeof(Convert).GetMethod(nameof(Convert.ToInt32), new[] { typeof(object) })!, getValue), typeof(int));
			}
			else
			{
				convertedValue = Expression.Convert(getValue, prop.PropertyType);
			}

			// Create conditional expression: if IsDBNull then default else converted value
			ConditionalExpression assignValue = Expression.Condition(isDbNullCall, Expression.Default(prop.PropertyType), convertedValue);

			// Create property binding
			assignments.Add(Expression.Bind(prop, assignValue));
		}

		// Create member init expression
		MemberInitExpression memberInit = Expression.MemberInit(instanceExp, assignments);

		// Create and compile lambda
		Expression<Func<IDataReader, T>> lambda = Expression.Lambda<Func<IDataReader, T>>(memberInit, readerParam);
		return lambda.CompileFast();
	}

	/// <summary>
	/// Get a stream of data from the database using the provided connection and command
	/// </summary>
	/// <param name="conn">Database connection to execute the command on</param>
	/// <param name="cmd">Database command to execute</param>
	/// <param name="commandTimeoutSeconds">Optional: Command execution timeout in seconds</param>
	/// <param name="cancellationToken">Optional: Token to monitor for cancellation requests</param>
	/// <returns></returns>
	public static IEnumerable<T> GetDataStreamSynchronous<T>(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, bool useCache = true, CancellationToken cancellationToken = default) where T : class, new()
	{
		DbDataReader? reader = null;
		try
		{
			cmd.CommandTimeout = commandTimeoutSeconds;
			conn.Open();
			reader = cmd.ExecuteReader();
			Func<IDataReader, T> mapper = GetOrCreateMapperDelegate<T>(useCache);

			foreach (T item in EnumerateReaderSynchronous(reader, mapper, cancellationToken))
			{
				yield return item;
			}
		}
		finally
		{
			reader?.Dispose();
			conn.Close();
		}
	}

	public static async IAsyncEnumerable<T> GetDataStreamAsync<T>(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, bool useCache = true, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
	{
		try
		{
			cmd.CommandTimeout = commandTimeoutSeconds;
			await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
			await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

			Func<IDataReader, T> mapper = GetOrCreateMapperDelegate<T>(useCache);

			IAsyncEnumerator<T> enumeratedReader = EnumerateReader(reader, mapper, cancellationToken).GetAsyncEnumerator(cancellationToken);
			while (await enumeratedReader.MoveNextAsync().ConfigureAwait(false))
			{
				if (!cancellationToken.IsCancellationRequested)
				{
					yield return enumeratedReader.Current;
				}
				else
				{
					yield break; // Exit if cancellation is requested
				}
			}
		}
		finally
		{
			await conn.CloseAsync().ConfigureAwait(false);
		}
	}

	public static async Task<IEnumerable<T>> GetDataDirectAsync<T>(DbConnection conn, DbCommand cmd, int commandTimeoutSeconds = 30, int maxRetry = 3, bool useCache = true, CancellationToken cancellationToken = default) where T : class, new()
	{
		List<T> values = [];
		for (int i = 0; i < maxRetry; i++)
		{
			try
			{
				cmd.CommandTimeout = commandTimeoutSeconds;
				await conn.OpenAsync(cancellationToken).ConfigureAwait(false);
				await using DbDataReader reader = await cmd.ExecuteReaderAsync(cancellationToken).ConfigureAwait(false);

				Func<IDataReader, T> mapper = GetOrCreateMapperDelegate<T>(useCache);
				while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
				{
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
					try
					{
						values.Add(mapper(reader));
					}
					catch (Exception ex)
					{
						logger.Error(ex, "Error mapping data: @ {ErrorLocation}", ex.GetLocationOfException());
						throw;
					}
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
				}
				break;
			}
			catch (Exception ex)
			{
				logger.Error(ex, "There was an error");
			}
			finally
			{
				await conn.CloseAsync().ConfigureAwait(false);
			}
		}
		return values;
	}

	private static async IAsyncEnumerable<T> EnumerateReader<T>(DbDataReader reader, Func<IDataReader, T> mapper, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T : class, new()
	{
		while (await reader.ReadAsync(cancellationToken).ConfigureAwait(false))
		{
			T result;
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
			try
			{
				result = mapper(reader);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error mapping data @ {ErrorLocation}", ex.GetLocationOfException());
				throw;
			}
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both
			yield return result;
		}
	}

	private static IEnumerable<T> EnumerateReaderSynchronous<T>(DbDataReader reader, Func<IDataReader, T> mapper, CancellationToken cancellationToken = default) where T : class, new()
	{
		while (reader.Read())
		{
			T result;
#pragma warning disable S2139 // Exceptions should be either logged or rethrown but not both
			try
			{
				result = mapper(reader);
			}
			catch (Exception ex)
			{
				logger.Error(ex, "Error mapping data @ {ErrorLocation}", ex.GetLocationOfException());
				throw;
			}
#pragma warning restore S2139 // Exceptions should be either logged or rethrown but not both

			if (!cancellationToken.IsCancellationRequested)
			{
				yield return result;
			}
			else
			{
				yield break; // Exit if cancellation is requested
			}
		}
	}
}
