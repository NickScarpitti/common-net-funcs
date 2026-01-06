using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Query;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Optional configurations for the <see cref="BaseDbContextActions"/> class
/// </summary>
public sealed class FullQueryOptions(bool? splitQueryOverride = null) : NavigationPropertiesOptions
{
	/// <summary>
	/// Optional: Override the database default split query behavior. Only used when running "Full" queries that include navigation properties.
	/// </summary>
	public bool? SplitQueryOverride { get; set; } = splitQueryOverride;
}

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="TEntity">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="TContext">DB Context for the database you with to run these actions against.</typeparam>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for dependency injection.</param>
public class BaseDbContextActions<TEntity, TContext>(IServiceProvider serviceProvider) : IBaseDbContextActions<TEntity, TContext> where TEntity : class where TContext : DbContext
{
	private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
	private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
	static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

	#region Read

	#region GetByKey

	/// <summary>
	/// Get individual record by the single field primary key, with or without navigation properties
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public Task<TEntity?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetByKey(primaryKey, queryTimeout, cancellationToken) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Get individual record by the single field primary key.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = await context.Set<TEntity>().FindAsync(new object?[] { primaryKey }, cancellationToken: cancellationToken).ConfigureAwait(true);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Get individual record by the single field primary key with or without navigation properties
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public Task<TEntity?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetByKey(primaryKey, queryTimeout, cancellationToken) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Get individual record by a compound primary key.
	/// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = await context.Set<TEntity>().FindAsync(primaryKey, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Get individual record by the primary key with all navigation properties.
	/// If using a compound primary key, use an object of the same <see langword="class"/> to be returned with the primary key fields populated.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				model = fullQueryOptions.SplitQueryOverride switch
				{
					null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
				};
			}
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
					if (model != null)
					{
						model = fullQueryOptions.SplitQueryOverride switch
						{
							null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
						};
					}
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		//Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored

		return model;
	}

	/// <summary>
	/// Get individual record by the primary key with all navigation properties.
	/// If using a compound primary key, use an object of the same <see langword="class"/> to be returned with the primary key fields populated.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="primaryKey">Primary key of the record to be returned.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>Record of type <typeparamref name="TEntity"/> corresponding to the primary key passed in.</returns>
	public async Task<TEntity?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
			if (model != null)
			{
				model = fullQueryOptions.SplitQueryOverride switch
				{
					null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
					_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
						context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(context, model, cancellationToken: cancellationToken) :
						context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
				};
			}
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
					if (model != null)
					{
						model = fullQueryOptions.SplitQueryOverride switch
						{
							null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
							_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(context, model, cancellationToken: cancellationToken),
						};
					}
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		return model;
	}

	#endregion

	#region GetAll

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public Task<List<TEntity>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAll(queryTimeout, trackEntities, cancellationToken) : GetAllFull(queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async Task<List<TEntity>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
	{
		List<TEntity>? model = null;
		try
		{
			model = await GetQueryAll(queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public Task<List<TOutput>?> GetAll<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAll(selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetAllFull(selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them into the type <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async Task<List<TOutput>?> GetAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
	{
		List<TOutput>? model = null;
		try
		{
			model = await GetQueryAll(selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TEntity>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(queryTimeout, trackEntities, cancellationToken) : GetAllFullStreaming(queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async IAsyncEnumerable<TEntity>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAll(queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records from the corresponding table with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(bool full, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetAllStreaming(selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetAllFullStreaming(selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async IAsyncEnumerable<TOutput>? GetAllStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TOutput>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAll(selectExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TOutput enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table.
	/// Same as running a SELECT * query.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IQueryable<TEntity> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !trackEntities ? context.Set<TEntity>().AsNoTracking() : context.Set<TEntity>();
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryAll<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !trackEntities ? context.Set<TEntity>().AsNoTracking().Select(selectExpression) : context.Set<TEntity>().Select(selectExpression);
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async Task<List<TEntity>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		List<TEntity>? model = null;
		try
		{
			model = await GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async Task<List<TOutput>?> GetAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		List<TOutput>? model = null;
		try
		{
			model = await GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryAllFull(selectExpression, queryTimeout, true).ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public async IAsyncEnumerable<TEntity>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public async IAsyncEnumerable<TOutput>? GetAllFullStreaming<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IQueryable<TOutput> query = GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
		IAsyncEnumerable<TOutput>? enumeratedReader = null;
		try
		{
			enumeratedReader = query.AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					query = GetQueryAllFull(selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					enumeratedReader = query.AsAsyncEnumerable();
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TOutput enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/>.</returns>
	public IQueryable<TEntity> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
			};
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table and transforms them <typeparamref name="TOutput"/>.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryAllFull<TOutput>(Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression)
			};
	}

	#endregion

	#region GetWithFilter

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public Task<List<TEntity>?> GetWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, queryTimeout, trackEntities, cancellationToken) :
			GetWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TEntity>?> GetWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
	{
		List<TEntity>? model = null;
		try
		{
			model = await GetQueryWithFilter(whereExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public Task<List<TOutput>?> GetWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TOutput>?> GetWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
	{
		List<TOutput>? model = null;
		try
		{
			model = await GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	public IServiceProvider ServiceProvider { get; set; } = serviceProvider;

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IAsyncEnumerable<TEntity>? GetWithFilterStreaming(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<TEntity>? GetWithFilterStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilter(whereExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<TOutput>? GetWithFilterStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TOutput>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities).AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TOutput enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="TOutput"/>.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TEntity> GetQueryWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !trackEntities ? context.Set<TEntity>().Where(whereExpression).AsNoTracking() : context.Set<TEntity>().Where(whereExpression);
	}

	/// <summary>
	/// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] query.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TOutput> GetQueryWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !trackEntities ? context.Set<TEntity>().Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
			context.Set<TEntity>().Where(whereExpression).Select(selectExpression).Distinct();
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async Task<List<TEntity>?> GetWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		List<TEntity>? model = null;
		try
		{
			model = await GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public async Task<List<TOutput>?> GetWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		List<TOutput>? model = null;
		try
		{
			model = await GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions)
				.ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public async IAsyncEnumerable<TEntity>? GetWithFilterFullStreaming(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public async IAsyncEnumerable<TOutput>? GetWithFilterFullStreaming<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<TOutput>? enumeratedReader = null;

		try
		{
			enumeratedReader = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TOutput enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression.</returns>
	public IQueryable<TEntity> GetQueryWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
			};
	}

	/// <summary>
	/// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="TOutput"/> class using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public IQueryable<TOutput> GetQueryWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			};
	}

	#endregion

	#region GetNavigationWithFilter

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> class with the select expression.</returns>
	public Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetNavigationWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async Task<List<TEntity>?> GetNavigationWithFilter<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		List<TEntity>? model = null;
		try
		{
			model = await GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = await GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions)
						.ToListAsync(cancellationToken).ConfigureAwait(false);
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TOutput), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> with or without its navigation properties using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(bool full, Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetNavigationWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetNavigationWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async IAsyncEnumerable<TEntity>? GetNavigationWithFilterStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TOutput : class
	{
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			enumeratedReader = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					enumeratedReader = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions).AsAsyncEnumerable();
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TOutput), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async Task<List<TEntity>?> GetNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		List<TEntity>? model = null;
		try
		{
			IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			model = fullQueryOptions.SplitQueryOverride switch
			{
				//Need to add in navigation properties of the output type since they are not kept in the original query
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
					await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
				logger.Warn("{msg}", $"Adding {typeof(TOutput).Name} to circularReferencingEntities");
				circularReferencingEntities.TryAdd(typeof(TOutput), true);
				try
				{
					await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
					model = fullQueryOptions.SplitQueryOverride switch
					{
						//Need to add in navigation properties of the output type since they are not kept in the original query
						null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
						true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
						_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
							await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
					};
				}
				catch (InvalidOperationException ioEx2) //Error could be caused by navigation properties of the output type, so need to try that as well
				{
					if (ioEx2.HResult == -2146233079)
					{
						try
						{
							logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
							circularReferencingEntities.TryAdd(typeof(TEntity), true);
							await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
							model = fullQueryOptions.SplitQueryOverride switch
							{
								null => await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
								true => await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
								_ => await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
							};
						}
						catch (Exception ex2)
						{
							logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
							logger.Error(ioEx2, "{msg}", $"{ioEx2.GetLocationOfException()} Error1");
							logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
						}
					}
					else
					{
						logger.Error(ioEx2, "{msg}", $"{ioEx.GetLocationOfException()} Error");
					}
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public async IAsyncEnumerable<TEntity>? GetNavigationWithFilterFullStreaming<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
				bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		IAsyncEnumerable<TEntity>? enumeratedReader = null;
		try
		{
			IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			enumeratedReader = fullQueryOptions.SplitQueryOverride switch
			{
				//Need to add in navigation properties of the output type since they are not kept in the original query
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
					query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				IQueryable<TEntity> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
				logger.Warn("{msg}", $"Adding {typeof(TOutput).Name} to circularReferencingEntities");
				circularReferencingEntities.TryAdd(typeof(TOutput), true);
				try
				{
					await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
					enumeratedReader = fullQueryOptions.SplitQueryOverride switch
					{
						//Need to add in navigation properties of the output type since they are not kept in the original query
						null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
						true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
						_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
							query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
							query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
					};
				}
				catch (InvalidOperationException ioEx2) //Error could be caused by navigation properties of the output type, so need to try that as well
				{
					if (ioEx2.HResult == -2146233079)
					{
						try
						{
							logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
							circularReferencingEntities.TryAdd(typeof(TEntity), true);
							await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
							enumeratedReader = fullQueryOptions.SplitQueryOverride switch
							{
								null => query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
								true => query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
								_ => query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
							};
						}
						catch (Exception ex2)
						{
							logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
							logger.Error(ioEx2, "{msg}", $"{ioEx2.GetLocationOfException()} Error1");
							logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
						}
					}
					else
					{
						logger.Error(ioEx2, "{msg}", $"{ioEx.GetLocationOfException()} Error");
					}
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (TEntity enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	/// <summary>
	/// Gets query to get the navigation property of a different class and outputs a class of type <typeparamref name="TEntity"/> using the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class to return navigation property from.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>All records from the table corresponding to class <typeparamref name="TOutput"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="TEntity"/> with the select expression.</returns>
	public IQueryable<TEntity> GetQueryNavigationWithFilterFull<TOutput>(Expression<Func<TOutput, bool>> whereExpression, Expression<Func<TOutput, TEntity>> selectExpression, TimeSpan? queryTimeout = null,
		bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TOutput), out _) ?
					context.Set<TOutput>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<TOutput>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TOutput), out _) ?
					context.Set<TOutput>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<TOutput>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TOutput), out _) ?
					context.Set<TOutput>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
					context.Set<TOutput>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TOutput>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				true => context.Set<TOutput>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
				_ => context.Set<TOutput>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
			};
	}

	#endregion

	#region GetWithPagingFilter

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0,
		int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where TOutput : class
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		GenericPagingModel<TOutput> model = new();
		try
		{
			IQueryable<TOutput> qModel = !trackEntities ? context.Set<TEntity>().Where(whereExpression).AsNoTracking().Select(selectExpression) : context.Set<TEntity>().Where(whereExpression).Select(selectExpression);
			model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		return !full ? GetWithPagingFilter(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, cancellationToken) :
			GetWithPagingFilterFull(whereExpression, selectExpression, ascendingOrderExpression, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilter<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where TOutput : class
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		GenericPagingModel<TOutput> model = new();
		try
		{
			IQueryable<TOutput> qModel = !trackEntities ? context.Set<TEntity>().Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().Select(selectExpression) :
				context.Set<TEntity>().Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression);

			var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() })
				.Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

			model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = results.ConvertAll(x => x.Entities);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString = null, int skip = 0,
		int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where TOutput : class
	{
		GenericPagingModel<TOutput> model = new();
		try
		{
			IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, false, trackEntities, fullQueryOptions);
			model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false); //results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);//results.ConvertAll(x => x.Entities);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

					model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
					model.Entities = results.ConvertAll(x => x.Entities);

					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
	/// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public async Task<GenericPagingModel<TOutput>> GetWithPagingFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression,
		Expression<Func<TEntity, TKey>> ascendingOrderExpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
		CancellationToken cancellationToken = default) where TOutput : class
	{
		GenericPagingModel<TOutput> model = new();
		try
		{
			IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderExpression, queryTimeout, false, trackEntities, fullQueryOptions);

			var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

			model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
			model.Entities = results.ConvertAll(x => x.Entities);
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					IQueryable<TOutput> qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
					var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

					model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
					model.Entities = results.ConvertAll(x => x.Entities);

					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, string? orderByString,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
			};
	}

	/// <summary>
	/// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="ascendingOrderExpression">EF Core expression for order by statement to keep results consistent.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="TOutput"/>.</returns>
	public IQueryable<TOutput> GetQueryPagingWithFilterFull<TOutput, TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression,
		TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where TOutput : class
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		return !handlingCircularRefException
			? fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().Select(selectExpression) :
					context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression)
			}
			: fullQueryOptions.SplitQueryOverride switch
			{
				null => context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression),
				true => context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression),
				_ => context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).Select(selectExpression)
			};
	}

	#endregion

	#region GetOneWithFilter

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with or without navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public Task<TEntity?> GetOneWithFilter(bool full, Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetOneWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
	/// Same as running a SELECT * WHERE [condition] LIMIT 1 or SELECT TOP 1 * WHERE [condition] LIMIT 1 query.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of the linq query expression.</returns>
	public async Task<TEntity?> GetOneWithFilter(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
				await context.Set<TEntity>().AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, then transforms it into the <typeparamref name="TOutput"/> class with the select expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with or without navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="TOutput"/> class.</returns>
	public Task<TOutput?> GetOneWithFilter<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetOneWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetOneWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="TOutput"/> class with a select expression.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> that also satisfy the conditions of the linq query expression that has been transformed into the <typeparamref name="TOutput"/> class with the select expression.</returns>
	public async Task<TOutput?> GetOneWithFilter<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
				await context.Set<TEntity>().AsNoTracking().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
	public async Task<TEntity?> GetOneWithFilterFull(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		using DbContext context = ServiceProvider.GetRequiredService<TContext>();
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="TOutput"/> class with the select expression.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>First record from the table corresponding to class <typeparamref name="TEntity"/> with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="TOutput"/> class.</returns>
	public async Task<TOutput?> GetOneWithFilterFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	#endregion

	#region GetMaxByOrder

	/// <summary>
	/// Uses a descending order expression to return the record containing the maximum value according to that order with or without navigation properties.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression with or without navigation properties.</returns>
	public Task<TEntity?> GetMaxByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMaxByOrder(whereExpression, descendingOrderExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetMaxByOrderFull(whereExpression, descendingOrderExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a descending order expression to return the record containing the maximum value according to that order.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression.</returns>
	public async Task<TEntity?> GetMaxByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
				await context.Set<TEntity>().AsNoTracking().Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Uses a descending order expression to return the record and its navigation properties containing the maximum value according to that order.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the descendingOrderExpression</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="descendingOrderExpression">A linq expression used to order the query results with before taking the top result</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the maximum value according to the ascending order expression with it's navigation properties</returns>
	public async Task<TEntity?> GetMaxByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> descendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	#endregion

	#region GetMinByOrder

	/// <summary>
	/// Uses a ascending order expression to return the record containing the minimum value according to that order with or without navigation properties.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression with or without navigation properties.</returns>
	public Task<TEntity?> GetMinByOrder<TKey>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMinByOrder(whereExpression, ascendingOrderExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetMinByOrderFull(whereExpression, ascendingOrderExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a ascending order expression to return the record containing the minimum value according to that order.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression.</returns>
	public async Task<TEntity?> GetMinByOrder<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
				await context.Set<TEntity>().AsNoTracking().Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Uses a ascending order expression to return the record and its navigation properties containing the minimum value according to that order.
	/// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
	/// </summary>
	/// <typeparam name="TKey">Type being used to order records with in the ascendingOrderExpression.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="ascendingOrderExpression">A linq expression used to order the query results with before taking the top result.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The record that contains the minimum value according to the ascending order expression with it's navigation properties.</returns>
	public async Task<TEntity?> GetMinByOrderFull<TKey>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TKey>> ascendingOrderExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TEntity? model = null;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	#endregion

	#region GetMax

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the max expression with or without navigation properties.</returns>
	public Task<TOutput?> GetMax<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMax(whereExpression, maxExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetMaxFull(whereExpression, maxExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum value specified.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum value specified by the max expression.</returns>
	public async Task<TOutput?> GetMax<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) : await context.Set<TEntity>().AsNoTracking().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Uses a max expression to return the record containing the maximum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="maxExpression">A linq expression used in the .Max() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The maximum object specified by the min expression.</returns>
	public async Task<TOutput?> GetMaxFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	#endregion

	#region GetMin

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified with or without navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
	/// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression with or without navigation properties</returns>
	public Task<TOutput?> GetMin<TOutput>(bool full, Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		return !full ? GetMin(whereExpression, minExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
			GetMinFull(whereExpression, minExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum value specified.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum value specified by the min expression.</returns>
	public async Task<TOutput?> GetMin<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = trackEntities ? await context.Set<TEntity>().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
				await context.Set<TEntity>().AsNoTracking().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	/// <summary>
	/// Uses a min expression to return the record containing the minimum object specified and its navigation properties.
	/// </summary>
	/// <typeparam name="TOutput">Class type to return, specified by the selectExpression parameter.</typeparam>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="minExpression">A linq expression used in the .Min() function.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
	/// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The minimum object specified by the min expression.</returns>
	public async Task<TOutput?> GetMinFull<TOutput>(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<TEntity, TOutput>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
		FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
	{
		fullQueryOptions ??= new FullQueryOptions();
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		TOutput? model = default;
		try
		{
			model = fullQueryOptions.SplitQueryOverride switch
			{
				null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
				true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
				_ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _) ?
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
					await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
			};
		}
		catch (InvalidOperationException ioEx)
		{
			if (ioEx.HResult == -2146233079)
			{
				try
				{
					model = fullQueryOptions.SplitQueryOverride switch
					{
						null => await context.Set<TEntity>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
						true => await context.Set<TEntity>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
						_ => await context.Set<TEntity>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
					};
					logger.Warn("{msg}", $"Adding {typeof(TEntity).Name} to circularReferencingEntities");
					circularReferencingEntities.TryAdd(typeof(TEntity), true);
				}
				catch (Exception ex2)
				{
					logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
					logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
				}
			}
			else
			{
				logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
			}
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return model;
	}

	#endregion

	/// <summary>
	/// Gets the number of records in the table represented by T that satisfy the where expression.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter query results.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records that satisfy the where expression.</returns>
	public async Task<int> GetCount(Expression<Func<TEntity, bool>> whereExpression, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}

		int count = 0;
		try
		{
			count = await context.Set<TEntity>().Where(whereExpression).AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return count;
	}

	#endregion Read

	#region Write

	/// <summary>
	/// Creates a new record in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task Create(TEntity model, bool removeNavigationProps = false)
	{
		if (model == null)
		{
			throw new ArgumentNullException(nameof(model), "Model cannot be null");
		}

		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (removeNavigationProps)
		{
			model.RemoveNavigationProperties(context);
		}

		try
		{
			await context.Set<TEntity>().AddAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
		}
	}

	/// <summary>
	/// Creates new records in the table corresponding to type <typeparamref name="TEntity"/>.
	/// </summary>
	/// <param name="model">Records of type <typeparamref name="TEntity"/> to be added to the table.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public async Task CreateMany(IEnumerable<TEntity> model, bool removeNavigationProps = false)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (removeNavigationProps)
		{
			model.SetValue(x => x.RemoveNavigationProperties(context));
		}

		try
		{
			//await context.Set<T>().BulkInsertAsync(model); //Doesn't give updated identity values. EF Core Extensions (Paid)
			await context.Set<TEntity>().AddRangeAsync(model).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the object of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="model">Record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public void DeleteByObject(TEntity model, bool removeNavigationProps = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;

		try
		{
			if (removeNavigationProps)
			{
				model.RemoveNavigationProperties(context);
			}
			context.Set<TEntity>().Remove(model);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
		}
	}

	/// <summary>
	/// Delete record in the table corresponding to type <typeparamref name="TEntity"/> matching the primary key passed in.
	/// </summary>
	/// <param name="key">Key of the record of type <typeparamref name="TEntity"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteByKey(object key)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		DbSet<TEntity> table = context.Set<TEntity>();
		try
		{
			TEntity? deleteItem = await table.FindAsync(key).ConfigureAwait(false);
			if (deleteItem != null)
			{
				table.Remove(deleteItem);
				return true;
			}
			//changes = await table.DeleteByKeyAsync(key); //EF Core +, Does not require save changes, Does not work with PostgreSQL
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tKey: {JsonSerializer.Serialize(key)}");
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public bool DeleteMany(IEnumerable<TEntity> models, bool removeNavigationProps = false)
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}
			context.Set<TEntity>().RemoveRange(models); //Requires separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(models, defaultJsonSerializerOptions)}");
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the where expression passed in.
	/// </summary>
	/// <param name="whereExpression">Expression to filter the records to delete.</param>
	/// <returns>The number of records deleted, or <see langword="null"/> if there was an error.</returns>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	public async Task<int?> DeleteMany(Expression<Func<TEntity, bool>> whereExpression, CancellationToken cancellationToken = default)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			return await context.Set<TEntity>().AsNoTracking().Where(whereExpression).ExecuteDeleteAsync(cancellationToken).ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tDelete Many Error");
		}
		return null;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="models">Records of type <typeparamref name="TEntity"/> to delete.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyTracked(IEnumerable<TEntity> models, bool removeNavigationProps = false)
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context));
			}
			await context.Set<TEntity>().DeleteRangeByKeyAsync(models).ConfigureAwait(false); //EF Core +, Does not require separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(models, defaultJsonSerializerOptions)}");
		}
		return false;
	}

	/// <summary>
	/// Delete records in the table corresponding to type <typeparamref name="TEntity"/> matching the enumerable objects of type <typeparamref name="TEntity"/> passed in.
	/// </summary>
	/// <param name="keys">Keys of type <typeparamref name="TEntity"/> to delete.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> DeleteManyByKeys(IEnumerable<object> keys) //Does not work with PostgreSQL, not testable
	{
		await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		try
		{
			await context.Set<TEntity>().DeleteRangeByKeyAsync(keys).ConfigureAwait(false); //EF Core +, Does not require separate save
			return true;
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tKeys: {JsonSerializer.Serialize(keys)}");
		}
		return false;
	}

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="model">The modified entity.</param>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	public void Update(TEntity model, bool removeNavigationProps = false) //Send in modified object
	{
		using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (removeNavigationProps)
		{
			model.RemoveNavigationProperties(context);
		}
		context.Entry(model).State = EntityState.Modified;
	}

	/// <summary>
	/// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges().
	/// </summary>
	/// <param name="models">The modified entity.</param>>
	/// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public bool UpdateMany(List<TEntity> models, bool removeNavigationProps = false, CancellationToken cancellationToken = default) //Send in modified objects
	{
		try
		{
			using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			if (removeNavigationProps)
			{
				models.SetValue(x => x.RemoveNavigationProperties(context), cancellationToken: cancellationToken);
			}
			//await context.BulkUpdateAsync(models); EF Core Extensions (Paid)
			context.UpdateRange(models);
			return true;
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{msg}", $"{duex.GetLocationOfException()} DBUpdate Error\n\tModels: {JsonSerializer.Serialize(models)}");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModels: {JsonSerializer.Serialize(models)}");
		}
		return false;
	}

	/// <summary>
	/// Executes an update operation on records matching the where expression without loading them into memory.
	/// Uses EF Core's ExecuteUpdate for efficient bulk updates.
	/// </summary>
	/// <param name="whereExpression">A linq expression used to filter records to update.</param>
	/// <param name="setPropertyCalls">Expression defining the properties to update using SetProperty calls.</param>
	/// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
	/// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
	/// <returns>The number of records affected by the update operation, or <see langword="null"/> if there was an error.</returns>
	public async Task<int?> UpdateMany(Expression<Func<TEntity, bool>> whereExpression, Expression<Func<SetPropertyCalls<TEntity>, SetPropertyCalls<TEntity>>> setPropertyCalls,
		TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
	{
		try
		{
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			if (queryTimeout != null)
			{
				context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
			}
			return await context.Set<TEntity>().AsNoTracking().Where(whereExpression).ExecuteUpdateAsync(setPropertyCalls, cancellationToken).ConfigureAwait(false);
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{msg}", $"{duex.GetLocationOfException()} DBUpdate Error");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return null;
	}

	/// <summary>
	/// Persist any tracked changes to the database.
	/// </summary>
	/// <returns><see langword="bool"/> indicating success.</returns>
	public async Task<bool> SaveChanges()
	{
		try
		{
			await using DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
			return await context.SaveChangesAsync().ConfigureAwait(false) > 0;
		}
		catch (DbUpdateException duex)
		{
			logger.Error(duex, "{msg}", $"{duex.GetLocationOfException()} DBUpdate Error");
		}
		catch (Exception ex)
		{
			logger.Error(ex, "{ErrorLocation} Error", ex.GetLocationOfException());
		}
		return false;
	}

	#endregion Write
}

public sealed class GenericPagingModel<T>// where T : class
{
	public GenericPagingModel()
	{
		Entities = [];
	}

	public List<T> Entities { get; set; }

	public int TotalRecords { get; set; }
}
