using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System.Runtime.CompilerServices;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

public partial class BaseDbContextActions<TEntity, TContext> : IBaseDbContextActions<TEntity, TContext> where TEntity : class where TContext : DbContext
{
	private static IQueryable<TEntity> ApplyGlobalFilters(IQueryable<TEntity> query, GlobalFilterOptions? globalFilterOptions)
	{
		if (globalFilterOptions?.DisableAllFilters == true || (globalFilterOptions?.FilterNamesToDisable.AnyFast() ?? false))
		{
			return (globalFilterOptions.FilterNamesToDisable?.Length > 0)
					? query.IgnoreQueryFilters(globalFilterOptions.FilterNamesToDisable)
					: query.IgnoreQueryFilters();
		}
		return query;
	}

	private static IQueryable<TEntity> ApplyTrackingAndFilters(DbContext context, bool trackEntities, GlobalFilterOptions? globalFilterOptions)
	{
		IQueryable<TEntity> query = !trackEntities ? context.Set<TEntity>().AsNoTracking() : context.Set<TEntity>();
		return ApplyGlobalFilters(query, globalFilterOptions);
	}

	private static IQueryable<TEntity> BuildFullQuery(DbContext context, FullQueryOptions fullQueryOptions, bool handlingCircularRefException, bool trackEntities)
	{
		DbSet<TEntity> baseQuery = context.Set<TEntity>();

		IQueryable<TEntity> query = fullQueryOptions.SplitQueryOverride switch
		{
			null => baseQuery.IncludeNavigationProperties(context, fullQueryOptions),
			true => baseQuery.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
			_ => baseQuery.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
		};

		if (!handlingCircularRefException && !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _))
		{
			query = query.AsNoTracking();
		}

		return query;
	}

	private static IQueryable<TOutput> BuildFullQuery<TOutput>(DbContext context, FullQueryOptions fullQueryOptions, bool handlingCircularRefException, bool trackEntities) where TOutput : class
	{
		DbSet<TOutput> baseQuery = context.Set<TOutput>();

		IQueryable<TOutput> query = fullQueryOptions.SplitQueryOverride switch
		{
			null => baseQuery.IncludeNavigationProperties(context, fullQueryOptions),
			true => baseQuery.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
			_ => baseQuery.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
		};

		if (!handlingCircularRefException && !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _))
		{
			query = query.AsNoTracking();
		}

		return query;
	}

	private static IQueryable<TEntity> BuildFullQuery(IQueryable<TEntity> baseQuery, DbContext context, FullQueryOptions fullQueryOptions,
		bool handlingCircularRefException, bool trackEntities)
	{
		IQueryable<TEntity> query = fullQueryOptions.SplitQueryOverride switch
		{
			null => baseQuery.IncludeNavigationProperties(context, fullQueryOptions),
			true => baseQuery.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
			_ => baseQuery.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
		};

		if (!handlingCircularRefException && !trackEntities && !circularReferencingEntities.TryGetValue(typeof(TEntity), out _))
		{
			query = query.AsNoTracking();
		}

		return query;
	}

	private DbContext InitializeContext(TimeSpan? queryTimeout = null)
	{
		DbContext context = ServiceProvider.GetRequiredService<TContext>()!;
		if (queryTimeout != null)
		{
			context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
		}
		return context;
	}

	private static async Task<GenericPagingModel<TOutput>> BuildPagingResult<TOutput>(IQueryable<TOutput> query, int skip, int pageSize, CancellationToken cancellationToken) where TOutput : class
	{
		return new GenericPagingModel<TOutput>
		{
			TotalRecords = await query.CountAsync(cancellationToken).ConfigureAwait(false),
			Entities = await query.Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false)
		};
	}

	private static async IAsyncEnumerable<T> ExecuteStreaming<T>(Func<IQueryable<T>> queryBuilder, [EnumeratorCancellation] CancellationToken cancellationToken)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = queryBuilder().AsAsyncEnumerable();
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	private static async Task<TResult?> ExecuteWithCircularRefHandling<TResult>(Func<bool, CancellationToken, Task<TResult?>> operation, CancellationToken cancellationToken = default)
	{
		try
		{
			return await operation(false, cancellationToken).ConfigureAwait(false);
		}
		catch (InvalidOperationException ioEx) when (ioEx.HResult == -2146233079)
		{
			try
			{
				TResult? result = await operation(true, cancellationToken).ConfigureAwait(false);
				logger.Warn(ioEx, AddCircularRefTemplate, typeof(TEntity).Name);
				circularReferencingEntities.TryAdd(typeof(TEntity), true);
				return result;
			}
			catch (Exception ex2)
			{
				logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
				logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
			}
		}
		catch (InvalidOperationException ioEx)
		{
			logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		return default;
	}

	// Synchronous version for streaming operations
	private static async IAsyncEnumerable<T>? ExecuteStreamingWithCircularRefHandling<T>(Func<bool, IQueryable<T>> queryBuilder, [EnumeratorCancellation] CancellationToken cancellationToken = default)
	{
		IAsyncEnumerable<T>? enumeratedReader = null;
		try
		{
			enumeratedReader = queryBuilder(false).AsAsyncEnumerable();
		}
		catch (InvalidOperationException ioEx) when (ioEx.HResult == -2146233079)
		{
			try
			{
				enumeratedReader = queryBuilder(true).AsAsyncEnumerable();
				logger.Warn(ioEx, AddCircularRefTemplate, typeof(TEntity).Name);
				circularReferencingEntities.TryAdd(typeof(TEntity), true);
			}
			catch (Exception ex2)
			{
				logger.Error(ioEx, Error1LocationTemplate, ioEx.GetLocationOfException());
				logger.Error(ex2, Error2LocationTemplate, ex2.GetLocationOfException());
			}
		}
		catch (InvalidOperationException ioEx)
		{
			logger.Error(ioEx, ErrorLocationTemplate, ioEx.GetLocationOfException());
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}

		if (enumeratedReader != null)
		{
			await foreach (T enumerator in enumeratedReader.WithCancellation(cancellationToken).ConfigureAwait(false))
			{
				yield return enumerator;
			}
		}
	}

	private static async Task<TResult?> ExecuteQueryWithErrorLogging<TResult>(Func<Task<TResult?>> operation)
	{
		try
		{
			return await operation().ConfigureAwait(false);
		}
		catch (Exception ex)
		{
			logger.Error(ex, ErrorLocationTemplate, ex.GetLocationOfException());
		}
		return default;
	}
}
