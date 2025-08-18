﻿using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
using static CommonNetFuncs.Core.Collections;
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
/// <typeparam name="T">Entity <see langword="class"/> to be used with these methods.</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against.</typeparam>
/// <param name="serviceProvider"><see cref="IServiceProvider"/> for dependency injection.</param>
public class BaseDbContextActions<T, UT>(IServiceProvider serviceProvider) : IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();
    private static readonly JsonSerializerOptions defaultJsonSerializerOptions = new() { ReferenceHandler = ReferenceHandler.IgnoreCycles };
    static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

    public IServiceProvider serviceProvider = serviceProvider;

    #region Read

    #region GetByKey Object

    /// <summary>
    /// Get individual record by the single field primary key, with or without navigation properties
    /// </summary>
    /// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public Task<T?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetByKey(primaryKey, queryTimeout, cancellationToken) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Get individual record by the single field primary key.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().FindAsync(new object?[] { primaryKey }, cancellationToken: cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
            if (model != null)
            {
                model = fullQueryOptions.SplitQueryOverride switch
                {
                    null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                    true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                    _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
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
                            null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                            true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                            _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                        };
                    }
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        //Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored

        return model;
    }

    #endregion

    #region GetByKey Object[]

    /// <summary>
    /// Get individual record by the single field primary key with or without navigation properties
    /// </summary>
    /// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: Used only when running "Full" query. If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public Task<T?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
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
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().FindAsync(primaryKey, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    /// <returns>Record of type <typeparamref name="T"/> corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await GetByKey(primaryKey, queryTimeout, cancellationToken).ConfigureAwait(false);
            if (model != null)
            {
                model = fullQueryOptions.SplitQueryOverride switch
                {
                    null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                    true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                    _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().GetObjectByPartial(model, cancellationToken: cancellationToken) :
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
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
                            null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                            true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                            _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).GetObjectByPartial(model, cancellationToken: cancellationToken),
                        };
                    }
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        return model;
    }

    #endregion

    #region GetAll NoSelect

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public Task<List<T>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetAll(queryTimeout, trackEntities, cancellationToken) : GetAllFull(queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public IAsyncEnumerable<T>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public async Task<List<T>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryAll(queryTimeout, trackEntities);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public async IAsyncEnumerable<T>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryAll(queryTimeout, trackEntities);
        IAsyncEnumerable<T>? enumeratedReader = null;
        try
        {
            enumeratedReader = query.AsAsyncEnumerable();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public IQueryable<T> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return !trackEntities ? context.Set<T>().AsNoTracking() : context.Set<T>();
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public async Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public async IAsyncEnumerable<T>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryAllFull(queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T>? enumeratedReader = null;
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
                    query = GetQueryAllFull(queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    enumeratedReader = query.AsAsyncEnumerable();
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions)
            };
        }
    }

    #endregion

    #region GetAll Select

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <param name="full">If <see langword="true"/>, will run "full" query that includes navigation properties.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public Task<List<T2>?> GetAll<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetAll(selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetAllFull(selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/>.</returns>
    public IAsyncEnumerable<T2>? GetAllStreaming<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetAllStreaming(selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetAllFullStreaming(selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets all records from the corresponding table and transforms them into the type <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryAll(selectExpression, queryTimeout, trackEntities);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table and transforms them <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public async IAsyncEnumerable<T2>? GetAllStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryAll(selectExpression, queryTimeout, trackEntities);
        IAsyncEnumerable<T2>? enumeratedReader = null;
        try
        {
            enumeratedReader = query.AsAsyncEnumerable();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T2 enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table and transforms them <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public IQueryable<T2> GetQueryAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return !trackEntities ? context.Set<T>().AsNoTracking().Select(selectExpression) : context.Set<T>().Select(selectExpression);
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryAllFull(selectExpression, queryTimeout, true);
                    model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public async IAsyncEnumerable<T2>? GetAllFullStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T2>? enumeratedReader = null;
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
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T2 enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table and transforms them <typeparamref name="T2"/>.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/>.</returns>
    public IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression)
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetWithFilter No Select

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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public Task<List<T>?> GetWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetWithFilter(whereExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetWithFilterFull(whereExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public IAsyncEnumerable<T>? GetWithFilterStreaming(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetWithFilterStreaming(whereExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetWithFilterFullStreaming(whereExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryWithFilter(whereExpression, queryTimeout, trackEntities);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T>? GetWithFilterStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryWithFilter(whereExpression, queryTimeout, trackEntities);
        IAsyncEnumerable<T>? enumeratedReader = null;
        try
        {
            enumeratedReader = query.AsAsyncEnumerable();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them <typeparamref name="T2"/>.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T> GetQueryWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking() : context.Set<T>().Where(whereExpression);
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T>? GetWithFilterFullStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T>? enumeratedReader = null;
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
                    query = GetQueryWithFilterFull(whereExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    enumeratedReader = query.AsAsyncEnumerable();
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
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
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool handlingCircularRefException = false,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression)
            };
        }
    }

    #endregion

    #region GetWithFilter Select

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="T2"/> class using the select expression
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
    public Task<List<T2>?> GetWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the <typeparamref name="T2"/> class using the select expression
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
    public IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE [condition] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE [condition] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities);
        IAsyncEnumerable<T2>? enumeratedReader = null;
        try
        {
            enumeratedReader = query.AsAsyncEnumerable();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T2 enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE [condition] query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T2> GetQueryWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
            context.Set<T>().Where(whereExpression).Select(selectExpression).Distinct();
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
    public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        List<T2>? model = null;

        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
    public async IAsyncEnumerable<T2>? GetWithFilterFullStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        IQueryable<T2> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T2>? enumeratedReader = null;

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
                    query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    enumeratedReader = query.AsAsyncEnumerable();
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T2 enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the <typeparamref name="T2"/> class using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T2"/> class with the select expression.</returns>
    public IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null)
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
    }

    #endregion

    #region GetNavigationWithFilter

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> with or without its navigation properties using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> class with the select expression.</returns>
    public Task<List<T>?> GetNavigationWithFilter<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        return !full ? GetNavigationWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> with or without its navigation properties using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        return !full ? GetNavigationWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetNavigationWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public async Task<List<T>?> GetNavigationWithFilter<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    model = await query.ToListAsync(cancellationToken).ConfigureAwait(false);
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T2), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public async IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T>? enumeratedReader = null;
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
                    query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    enumeratedReader = query.AsAsyncEnumerable();
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T2), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public async Task<List<T>?> GetNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        fullQueryOptions ??= new FullQueryOptions();
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        List<T>? model = null;
        try
        {
            //model = await query.ToListAsync();
            await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            model = fullQueryOptions.SplitQueryOverride switch
            {
                //Need to add in navigation properties of the output type since they are not kept in the original query
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
                    await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
                    await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
                    await query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false)
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                //model = await query.ToListAsync();
                logger.Warn("{msg}", $"Adding {typeof(T2).Name} to circularReferencingEntities");
                circularReferencingEntities.TryAdd(typeof(T2), true);
                try
                {
                    await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
                    model = fullQueryOptions.SplitQueryOverride switch
                    {
                        //Need to add in navigation properties of the output type since they are not kept in the original query
                        null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
                            await query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
                        true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().ToListAsync(cancellationToken).ConfigureAwait(false) :
                            await query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().ToListAsync(cancellationToken).ConfigureAwait(false),
                        _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
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
                            logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                            circularReferencingEntities.TryAdd(typeof(T), true);
                            await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public async IAsyncEnumerable<T>? GetNavigationWithFilterFullStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, [EnumeratorCancellation] CancellationToken cancellationToken = default) where T2 : class
    {
        fullQueryOptions ??= new FullQueryOptions();
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, false, trackEntities, fullQueryOptions);
        IAsyncEnumerable<T>? enumeratedReader = null;
        try
        {
            //model = await query.ToListAsync();
            await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            enumeratedReader = fullQueryOptions.SplitQueryOverride switch
            {
                //Need to add in navigation properties of the output type since they are not kept in the original query
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable()
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                //model = await query.ToListAsync();
                logger.Warn("{msg}", $"Adding {typeof(T2).Name} to circularReferencingEntities");
                circularReferencingEntities.TryAdd(typeof(T2), true);
                try
                {
                    await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
                    enumeratedReader = fullQueryOptions.SplitQueryOverride switch
                    {
                        //Need to add in navigation properties of the output type since they are not kept in the original query
                        null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
                            query.IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
                        true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsNoTracking().AsAsyncEnumerable() :
                            query.AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Distinct().AsAsyncEnumerable(),
                        _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
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
                            logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                            circularReferencingEntities.TryAdd(typeof(T), true);
                            await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }

        if (enumeratedReader != null)
        {
            await foreach (T enumerator in enumeratedReader)
            {
                if (!cancellationToken.IsCancellationRequested)
                {
                    yield return enumerator;
                }
                else
                {
                    yield break;
                }
            }
        }
    }

    /// <summary>
    /// Gets query to get the navigation property of a different class and outputs a class of type <typeparamref name="T"/> using the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>All records from the table corresponding to class <typeparamref name="T2"/> that also satisfy the conditions of linq query expression and have been transformed in to the <typeparamref name="T"/> with the select expression.</returns>
    public IQueryable<T> GetQueryNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T2>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
    }

    #endregion

    #region GetWithPagingFilter String Order

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0,
        int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        return !full ? GetWithPagingFilter(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetWithPagingFilterFull(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where T2 : class
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        GenericPagingModel<T2> model = new();
        try
        {
            IQueryable<T2> qModel = !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression) : context.Set<T>().Where(whereExpression).Select(selectExpression);

            //var results = await qModel.OrderBy(orderByString ?? string.Empty).Select(x => new { Entities = x, TotalCount = qModel.Count() })
            //    .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

            //model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
            //model.Entities = results.ConvertAll(x => x.Entities);

            model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false); //results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
            model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false); //results.ConvertAll(x => x.Entities);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0,
        int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        IQueryable<T2> qModel;
        GenericPagingModel<T2> model = new();
        try
        {
            qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, false, trackEntities, fullQueryOptions);

            //var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

            model.TotalRecords = await qModel.CountAsync(cancellationToken).ConfigureAwait(false); //results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
            model.Entities = await qModel.Skip(skip).Take(pageSize).ToListAsync(cancellationToken).ConfigureAwait(false);//results.ConvertAll(x => x.Entities);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

                    model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
                    model.Entities = results.ConvertAll(x => x.Entities);

                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public IQueryable<T2> GetQueryPagingWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString,
        TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetWithPagingFilter TKey Order

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default) where T2 : class
    {
        return !full ? GetWithPagingFilter(whereExpression, selectExpression, ascendingOrderEpression, skip, pageSize, queryTimeout, trackEntities, cancellationToken) :
            GetWithPagingFilterFull(whereExpression, selectExpression, ascendingOrderEpression, skip, pageSize, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, CancellationToken cancellationToken = default) where T2 : class
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        GenericPagingModel<T2> model = new();
        try
        {
            IQueryable<T2> qModel = !trackEntities ? context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression);

            var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() })
                .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT [SpecificFields] WHERE [condition] query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">Optional: How many records to skip before the ones that should be returned. Default is 0.</param>
    /// <param name="pageSize">Optional: How many records to take after the skipped records. Default is 0 (same as int.MaxValue)</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null,
        CancellationToken cancellationToken = default) where T2 : class
    {
        IQueryable<T2> qModel;
        GenericPagingModel<T2> model = new();
        try
        {
            qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderEpression, queryTimeout, false, trackEntities, fullQueryOptions);

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
                    qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderEpression, queryTimeout, true, fullQueryOptions: fullQueryOptions);
                    var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync(cancellationToken).ConfigureAwait(false);

                    model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync(cancellationToken).ConfigureAwait(false);
                    model.Entities = results.ConvertAll(x => x.Entities);

                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="handlingCircularRefException">Optional: If handling InvalidOperationException where .AsNoTracking() can't be used set to true. Default is <see langword="false"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of linq query expression, which are converted to <typeparamref name="T2"/>.</returns>
    public IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression,
        TimeSpan? queryTimeout = null, bool handlingCircularRefException = false, bool trackEntities = false, FullQueryOptions? fullQueryOptions = null) where T2 : class
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
        else
        {
            return fullQueryOptions.SplitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetOneWithFilter No Select

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> with or without navigation properties that also satisfies the conditions of the linq query expression.</returns>
    public Task<T?> GetOneWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
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
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
                await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        using DbContext context = serviceProvider.GetRequiredService<UT>();
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).AsNoTracking().FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).FirstOrDefaultAsync(whereExpression, cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    #endregion

    #region GetOneWithFilter Select

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, then transforms it into the <typeparamref name="T2"/> class with the select expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> with or without navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="T2"/> class.</returns>
    public Task<T2?> GetOneWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetOneWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetOneWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="T2"/> class with a select expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> that also satisfy the conditions of the linq query expression that has been transformed into the <typeparamref name="T2"/> class with the select expression.</returns>
    public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                await context.Set<T>().AsNoTracking().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the <typeparamref name="T2"/> class with the select expression.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>First record from the table corresponding to class <typeparamref name="T"/> with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the <typeparamref name="T2"/> class.</returns>
    public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    #endregion

    #region GetMaxByOrder

    /// <summary>
    /// Uses a descending order expression to return the record containing the maximum value according to that order with or without navigation properties.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression with or without navigation properties.</returns>
    public Task<T?> GetMaxByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetMaxByOrder(whereExpression, descendingOrderEpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetMaxByOrderFull(whereExpression, descendingOrderEpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Uses a descending order expression to return the record containing the maximum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression.</returns>
    public async Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                await context.Set<T>().AsNoTracking().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a descending order expression to return the record and its navigation properties containing the maximum value according to that order.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression with it's navigation properties</returns>
    public async Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    #endregion

    #region GetMinByOrder

    /// <summary>
    /// Uses a ascending order expression to return the record containing the minimum value according to that order with or without navigation properties.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression with or without navigation properties.</returns>
    public Task<T?> GetMinByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetMinByOrder(whereExpression, ascendingOrderEpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetMinByOrderFull(whereExpression, ascendingOrderEpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Uses a ascending order expression to return the record containing the minimum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression.</returns>
    public async Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                await context.Set<T>().AsNoTracking().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a ascending order expression to return the record and its navigation properties containing the minimum value according to that order.
    /// Navigation properties using System.Text.Json.Serialization <see cref="JsonIgnoreAttribute"/> will not be included.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression with it's navigation properties.</returns>
    public async Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    #endregion

    #region GetMax

    /// <summary>
    /// Uses a max expression to return the record containing the maximum object specified with or without navigation properties.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The maximum object specified by the max expression with or without navigation properties.</returns>
    public Task<T2?> GetMax<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetMax(whereExpression, maxExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetMaxFull(whereExpression, maxExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Uses a max expression to return the record containing the maximum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The maximum value specified by the max expression.</returns>
    public async Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) : await context.Set<T>().AsNoTracking().Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a max expression to return the record containing the maximum object specified and its navigation properties.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The maximum object specified by the min expression.</returns>
    public async Task<T2?> GetMaxFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MaxAsync(maxExpression, cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    #endregion

    #region GetMin

    /// <summary>
    /// Uses a min expression to return the record containing the minimum object specified with or without navigation properties.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="fullQueryOptions">Optional: Used only when running "Full" query. Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The minimum object specified by the min expression with or without navigation properties</returns>
    public Task<T2?> GetMin<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        return !full ? GetMin(whereExpression, minExpression, queryTimeout, trackEntities, cancellationToken: cancellationToken) :
            GetMinFull(whereExpression, minExpression, queryTimeout, trackEntities, fullQueryOptions, cancellationToken);
    }

    /// <summary>
    /// Uses a min expression to return the record containing the minimum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The minimum value specified by the min expression.</returns>
    public async Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
                await context.Set<T>().AsNoTracking().Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a min expression to return the record containing the minimum object specified and its navigation properties.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout. Default is <see langword="null"/>.</param>
    /// <param name="trackEntities">Optional: If <see langword="true"/>, entities will be tracked in memory. Default is <see langword="false"/> for "Full" queries, and queries that return more than one entity. Default is <see langword="false"/>.</param>
    /// <param name="fullQueryOptions">Optional: Configures how the query is run and how the navigation properties are retrieved.</param>
    /// <param name="cancellationToken">Optional: Cancellation token for this operation.</param>
    /// <returns>The minimum object specified by the min expression.</returns>
    public async Task<T2?> GetMinFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        FullQueryOptions? fullQueryOptions = null, CancellationToken cancellationToken = default)
    {
        fullQueryOptions ??= new FullQueryOptions();
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = fullQueryOptions.SplitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).AsNoTracking().MinAsync(minExpression, cancellationToken).ConfigureAwait(false) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
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
                        null => await context.Set<T>().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, fullQueryOptions).Where(whereExpression).MinAsync(minExpression, cancellationToken).ConfigureAwait(false),
                    };
                    logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.TryAdd(typeof(T), true);
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
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
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
    public async Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, CancellationToken cancellationToken = default)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        int count = 0;
        try
        {
            count = await context.Set<T>().Where(whereExpression).AsNoTracking().CountAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return count;
    }

    #endregion Read

    #region Write

    /// <summary>
    /// Creates a new record in the table corresponding to type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="model">Record of type <typeparamref name="T"/> to be added to the table.</param>
    /// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
    public async Task Create(T model, bool removeNavigationProps = false)
    {
        if (model == null)
        {
            throw new ArgumentNullException(nameof(model), "Model cannot be null");
        }

        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (removeNavigationProps)
        {
            model.RemoveNavigationProperties(context);
        }

        try
        {
            await context.Set<T>().AddAsync(model).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
        }
    }

    /// <summary>
    /// Creates new records in the table corresponding to type <typeparamref name="T"/>.
    /// </summary>
    /// <param name="model">Records of type <typeparamref name="T"/> to be added to the table.</param>
    /// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
    public async Task CreateMany(IEnumerable<T> model, bool removeNavigationProps = false)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (removeNavigationProps)
        {
            model.SetValue(x => x.RemoveNavigationProperties(context));
        }

        try
        {
            //await context.Set<T>().BulkInsertAsync(model); //Doesn't give updated identity values. EF Core Extensions (Paid)
            await context.Set<T>().AddRangeAsync(model).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type <typeparamref name="T"/> matching the object of type <typeparamref name="T"/> passed in.
    /// </summary>
    /// <param name="model">Record of type <typeparamref name="T"/> to delete.</param>
    /// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
    public void DeleteByObject(T model, bool removeNavigationProps = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;

        try
        {
            if (removeNavigationProps)
            {
                model.RemoveNavigationProperties(context);
            }
            context.Set<T>().Remove(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type <typeparamref name="T"/> matching the primary key passed in.
    /// </summary>
    /// <param name="key">Key of the record of type <typeparamref name="T"/> to delete.</param>
    public async Task<bool> DeleteByKey(object key)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        DbSet<T> table = context.Set<T>();
        bool success = false;
        try
        {
            T? deleteItem = await table.FindAsync(key).ConfigureAwait(false);
            if (deleteItem != null)
            {
                table.Remove(deleteItem);
                success = true;
            }
            //changes = await table.DeleteByKeyAsync(key); //EF Core +, Does not require save changes, Does not work with PostgreSQL
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tKey: {JsonSerializer.Serialize(key)}");
        }
        return success;
    }

    /// <summary>
    /// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
    /// </summary>
    /// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
    /// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
    public bool DeleteMany(IEnumerable<T> models, bool removeNavigationProps = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            if (removeNavigationProps)
            {
                models.SetValue(x => x.RemoveNavigationProperties(context));
            }
            context.Set<T>().RemoveRange(models); //Requires separate save
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(models, defaultJsonSerializerOptions)}");
        }
        return false;
    }

    /// <summary>
    /// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
    /// </summary>
    /// <param name="models">Records of type <typeparamref name="T"/> to delete.</param>
    /// <param name="removeNavigationProps">Optional: If true, all navigation properties / related entities will be removed from the main entity. Default is false.</param>
    public async Task<bool> DeleteManyTracked(IEnumerable<T> models, bool removeNavigationProps = false)
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            if (removeNavigationProps)
            {
                models.SetValue(x => x.RemoveNavigationProperties(context));
            }
            await context.Set<T>().DeleteRangeByKeyAsync(models).ConfigureAwait(false); //EF Core +, Does not require separate save
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(models, defaultJsonSerializerOptions)}");
        }
        return false;
    }

    /// <summary>
    /// Delete records in the table corresponding to type <typeparamref name="T"/> matching the enumerable objects of type <typeparamref name="T"/> passed in.
    /// </summary>
    /// <param name="keys">Keys of type <typeparamref name="T"/> to delete.</param>
    public async Task<bool> DeleteManyByKeys(IEnumerable<object> keys) //Does not work with PostgreSQL, not testable
    {
        await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            await context.Set<T>().DeleteRangeByKeyAsync(keys).ConfigureAwait(false); //EF Core +, Does not require separate save
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
    public void Update(T model, bool removeNavigationProps = false) //Send in modified object
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
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
    public bool UpdateMany(List<T> models, bool removeNavigationProps = false) //Send in modified objects
    {
        bool result = true;
        try
        {
            using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            if (removeNavigationProps)
            {
                models.SetValue(x => x.RemoveNavigationProperties(context));
            }
            //await context.BulkUpdateAsync(models); EF Core Extensions (Paid)
            context.UpdateRange(models);
        }
        catch (DbUpdateException duex)
        {
            result = false;
            logger.Error(duex, "{msg}", $"{duex.GetLocationOfException()} DBUpdate Error\n\tModels: {JsonSerializer.Serialize(models)}");
        }
        catch (Exception ex)
        {
            result = false;
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModels: {JsonSerializer.Serialize(models)}");
        }
        return result;
    }

    /// <summary>
    /// Persist any tracked changes to the database.
    /// </summary>
    /// <returns>Boolean indicating success.</returns>
    public async Task<bool> SaveChanges()
    {
        bool result = false;
        try
        {
            await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            result = await context.SaveChangesAsync().ConfigureAwait(false) > 0;
        }
        catch (DbUpdateException duex)
        {
            logger.Error(duex, "{msg}", $"{duex.GetLocationOfException()} DBUpdate Error");
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return result;
    }

    #endregion Write
}

public sealed class GenericPagingModel<T> where T : class
{
    public GenericPagingModel()
    {
        Entities = [];
    }

    public List<T> Entities { get; set; }

    public int TotalRecords { get; set; }
}
