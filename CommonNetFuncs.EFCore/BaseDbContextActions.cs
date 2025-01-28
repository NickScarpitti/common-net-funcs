﻿using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using System.Text.Json;
using System.Text.Json.Serialization;
using CommonNetFuncs.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Z.EntityFramework.Plus;
using static CommonNetFuncs.Core.Collections;
using static CommonNetFuncs.Core.ExceptionLocation;

namespace CommonNetFuncs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="T">Entity class to be used with these methods.</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against.</typeparam>
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
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: Used only when running "full" query. If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public Task<T?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetByKey(primaryKey, queryTimeout) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Get individual record by the single field primary key.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().FindAsync(primaryKey);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Get individual record by the primary key with all navigation properties.
    /// If using a compound primary key, use an object of the same class to be returned with the primary key fields populated.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await GetByKey(primaryKey);
            if (model != null)
            {
                model = splitQueryOverride switch
                {
                    null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                    true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                    _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                };
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await GetByKey(primaryKey);
                    if (model != null)
                    {
                        model = splitQueryOverride switch
                        {
                            null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                            true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                            _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
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
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: Used only when running "full" query. If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public Task<T?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetByKey(primaryKey, queryTimeout) : GetByKeyFull(primaryKey, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Get individual record by a compound primary key.
    /// The values in the primaryKey array need to be ordered in the same order they are declared in AppDbContext
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().FindAsync(primaryKey);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Get individual record by the primary key with all navigation properties.
    /// If using a compound primary key, use an object of the same class to be returned with the primary key fields populated.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await GetByKey(primaryKey);
            if (model != null)
            {
                model = splitQueryOverride switch
                {
                    null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                    true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                    _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                };
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await GetByKey(primaryKey);
                    if (model != null)
                    {
                        model = splitQueryOverride switch
                        {
                            null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                            true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
                            _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).GetObjectByPartial(model),
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

    #region GetAll NoSelect

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public Task<List<T>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetAll(queryTimeout, trackEntities) : GetAllFull(queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public IAsyncEnumerable<T>? GetAllStreaming(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetAllStreaming(queryTimeout, trackEntities) : GetAllFullStreaming(queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async Task<List<T>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        IQueryable<T> query = GetQueryAll(queryTimeout, trackEntities);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
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
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async IAsyncEnumerable<T>? GetAllStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false)
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
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
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T> query = GetQueryAllFull(queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryAllFull(queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
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
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async IAsyncEnumerable<T>? GetAllFullStreaming(TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T> query = GetQueryAllFull(queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
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
                    query = GetQueryAllFull(queryTimeout, splitQueryOverride, true);
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore)
            };
        }
    }

    #endregion

    #region GetAll Select

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public Task<List<T2>?> GetAll<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetAll(selectExpression, queryTimeout, trackEntities) : GetAllFull(selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table with or without navigation properties.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public IAsyncEnumerable<T2>? GetAllStreaming<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetAllStreaming(selectExpression, queryTimeout, trackEntities) : GetAllFullStreaming(selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        IQueryable<T2> query = GetQueryAll(selectExpression, queryTimeout, trackEntities);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async IAsyncEnumerable<T2>? GetAllStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
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
    /// Gets all records with navigation properties from the corresponding table and transforms them into the type T2.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryAllFull(selectExpression, queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
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
    /// Gets all records with navigation properties from the corresponding table and transforms them into the type T2.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async IAsyncEnumerable<T2>? GetAllFullStreaming<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
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
                    query = GetQueryAllFull(selectExpression, queryTimeout, splitQueryOverride, true);
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table and transforms them into the type T2.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null,
        bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetWithFilter No Select

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public Task<List<T>?> GetWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetWithFilter(whereExpression, queryTimeout, trackEntities) :
            GetWithFilterFull(whereExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="full">If true, will run "full" query that includes navigation properties</param>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public IAsyncEnumerable<T>? GetWithFilterStreaming(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetWithFilterStreaming(whereExpression, queryTimeout, trackEntities) :
            GetWithFilterFullStreaming(whereExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null,bool trackEntities = false)
    {
        IQueryable<T> query = GetQueryWithFilter(whereExpression, queryTimeout, trackEntities);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T>? GetWithFilterStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null,bool trackEntities = false)
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
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
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryWithFilterFull(whereExpression, queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
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
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T>? GetWithFilterFullStreaming(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
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
                    query = GetQueryWithFilterFull(whereExpression, queryTimeout, splitQueryOverride, true);
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null,
        bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression)
            };
        }
    }

    #endregion

    #region GetWithFilter Select

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the T2 class using the select expression
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public Task<List<T2>?> GetWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities) :
            GetWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, and then transforms them into the T2 class using the select expression
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities) :
            GetWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
    {
        IQueryable<T2> query = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async IAsyncEnumerable<T2>? GetWithFilterStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false)
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
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
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T2> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T2>? model = null;

        try
        {
            model = await query.ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
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
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public async IAsyncEnumerable<T2>? GetWithFilterFullStreaming<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        IQueryable<T2> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
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
                    query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
    }

    ///// <summary>
    ///// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    ///// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    ///// </summary>
    ///// <param name="whereExpression">A linq expression used to filter query results.</param>
    ///// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    ///// <param name="queryTimeout">Override the database default for query timeout.</param>
    ///// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    ///// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    ///// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    //public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T>> selectExpression, TimeSpan? queryTimeout = null,
    //    bool trackEntities = false, bool? splitQueryOverride = null)
    //{
    //    IQueryable<T> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities);
    //    List<T>? model = null;

    //    try
    //    {
    //        model = await query.ToListAsync();
    //    }
    //    catch (InvalidOperationException ioEx)
    //    {
    //        if (ioEx.HResult == -2146233079)
    //        {
    //            try
    //            {
    //                query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
    //                model = await query.ToListAsync();
    //                logger.Warn("{msg}", $"Adding {typeof(T).Name} to circularReferencingEntities");
    //                circularReferencingEntities.TryAdd(typeof(T), true);
    //            }
    //            catch (Exception ex2)
    //            {
    //                logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error1");
    //                logger.Error(ex2, "{msg}", $"{ex2.GetLocationOfException()} Error2");
    //            }
    //        }
    //        else
    //        {
    //            logger.Error(ioEx, "{msg}", $"{ioEx.GetLocationOfException()} Error");
    //        }
    //    }
    //    catch (Exception ex)
    //    {
    //        logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
    //    }
    //    return model;
    //}

    ///// <summary>
    ///// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    ///// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    ///// </summary>
    ///// <param name="whereExpression">A linq expression used to filter query results.</param>
    ///// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    ///// <param name="queryTimeout">Override the database default for query timeout.</param>
    ///// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    ///// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    ///// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    ///// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    //public IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T>> selectExpression, TimeSpan? queryTimeout = null,
    //    bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false)
    //{
    //    using DbContext context = serviceProvider.GetRequiredService<UT>()!;
    //    if (queryTimeout != null)
    //    {
    //        context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
    //    }

    //    if (!handlingCircularRefException)
    //    {
    //        return splitQueryOverride switch
    //        {
    //            null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
    //                context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
    //                context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
    //            true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
    //                context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
    //                context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
    //            _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
    //                context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
    //                context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
    //        };
    //    }
    //    else
    //    {
    //        return splitQueryOverride switch
    //        {
    //            null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
    //            true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
    //            _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
    //        };
    //    }
    //}

    #endregion

    #region GetNavigationWithFilter

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type T with or without its navigation properties using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public Task<List<T>?> GetNavigationWithFilter<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        return !full ? GetNavigationWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities) :
            GetNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type T with or without its navigation properties using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        return !full ? GetNavigationWithFilterStreaming(whereExpression, selectExpression, queryTimeout, trackEntities) :
            GetNavigationWithFilterFullStreaming(whereExpression, selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type T using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public async Task<List<T>?> GetNavigationWithFilter<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
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
    /// Gets the navigation property of a different class and outputs a class of type T using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public async IAsyncEnumerable<T>? GetNavigationWithFilterStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
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
                    query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets the navigation property of a different class and outputs a class of type T using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public async Task<List<T>?> GetNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        List<T>? model = null;
        try
        {
            //model = await query.ToListAsync();
            using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            model = splitQueryOverride switch
            {
                //Need to add in navigation properties of the output type since they are not kept in the original query
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                    await query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                    await query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                    await query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync()
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
                //model = await query.ToListAsync();
                logger.Warn("{msg}", $"Adding {typeof(T2).Name} to circularReferencingEntities");
                circularReferencingEntities.TryAdd(typeof(T2), true);
                try
                {
                    await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
                    model = splitQueryOverride switch
                    {
                        //Need to add in navigation properties of the output type since they are not kept in the original query
                        null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            await query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                            await query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                        true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            await query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                            await query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                        _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            await query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().ToListAsync() :
                            await query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync()
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
                            model = splitQueryOverride switch
                            {
                                null => await query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                                true => await query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync(),
                                _ => await query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().ToListAsync()
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
    /// Gets the navigation property of a different class and outputs a class of type T using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public async IAsyncEnumerable<T>? GetNavigationWithFilterFullStreaming<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T> query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
        IAsyncEnumerable<T>? enumeratedReader = null;
        try
        {
            //model = await query.ToListAsync();
            using DbContext context = serviceProvider.GetRequiredService<UT>()!;
            enumeratedReader = splitQueryOverride switch
            {
                //Need to add in navigation properties of the output type since they are not kept in the original query
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                    query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable()
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                query = GetQueryNavigationWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
                //model = await query.ToListAsync();
                logger.Warn("{msg}", $"Adding {typeof(T2).Name} to circularReferencingEntities");
                circularReferencingEntities.TryAdd(typeof(T2), true);
                try
                {
                    await using DbContext context = serviceProvider.GetRequiredService<UT>()!;
                    enumeratedReader = splitQueryOverride switch
                    {
                        //Need to add in navigation properties of the output type since they are not kept in the original query
                        null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                            query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                        true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                            query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                        _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                            query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsNoTracking().AsAsyncEnumerable() :
                            query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable()
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
                            enumeratedReader = splitQueryOverride switch
                            {
                                null => query.IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                                true => query.AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable(),
                                _ => query.AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Distinct().AsAsyncEnumerable()
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
                yield return enumerator;
            }
        }
    }

    /// <summary>
    /// Gets query to get the navigation property of a different class and outputs a class of type T using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class to return navigation property from.</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public IQueryable<T> GetQueryNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null,
        bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T2>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
    }

    #endregion

    #region GetWithPagingFilter String Order

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        return !full ? GetWithPagingFilter(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities) :
            GetWithPagingFilterFull(whereExpression, selectExpression, orderByString, skip, pageSize, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        GenericPagingModel<T2> model = new();
        try
        {
            IQueryable<T2> qModel = !trackEntities ? context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression) : context.Set<T>().Where(whereExpression).Select(selectExpression);

            var results = await qModel.OrderBy(orderByString ?? string.Empty).Select(x => new { Entities = x, TotalCount = qModel.Count() })
                .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T2> qModel;
        GenericPagingModel<T2> model = new();
        try
        {
            qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);

            var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, orderByString, queryTimeout, splitQueryOverride, true);
                    var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

                    model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public IQueryable<T2> GetQueryPagingWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString,
        TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2: class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(orderByString ?? string.Empty).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetWithPagingFilter TKey Order

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        return !full ? GetWithPagingFilter(whereExpression, selectExpression, ascendingOrderEpression, skip, pageSize, queryTimeout, trackEntities) :
            GetWithPagingFilterFull(whereExpression, selectExpression, ascendingOrderEpression, skip, pageSize, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
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
                .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
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
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        IQueryable<T2> qModel;
        GenericPagingModel<T2> model = new();
        try
        {
            qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderEpression, queryTimeout, splitQueryOverride, false, trackEntities, maxNavigationDepth, navPropAttributesToIgnore, useCaching);

            var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderEpression, queryTimeout, splitQueryOverride, true);
                    var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() }).Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

                    model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="ascendingOrderEpression">EF Core expression for order by statement to keep results consistent.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The query to get the records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression,
        TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        if (!handlingCircularRefException)
        {
            return splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
    }

    #endregion

    #region GetOneWithFilter No Select

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with or without navigation properties that also satisfies the conditions of the linq query expression.</returns>
    public Task<T?> GetOneWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null,
    bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetOneWithFilter(whereExpression, queryTimeout, trackEntities) :
            GetOneWithFilterFull(whereExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> LIMIT 1 or SELECT TOP 1 * WHERE <condition> LIMIT 1 query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().FirstOrDefaultAsync(whereExpression) : await context.Set<T>().AsNoTracking().FirstOrDefaultAsync(whereExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>();
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).FirstOrDefaultAsync(whereExpression),
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
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression with or without navigation properties, then transforms it into the T2 class with the select expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with or without navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the T2 class.</returns>
    public Task<T2?> GetOneWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetOneWithFilter(whereExpression, selectExpression, queryTimeout, trackEntities) :
            GetOneWithFilterFull(whereExpression, selectExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the T2 class with a select expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression that has been transformed into the T2 class with the select expression.</returns>
    public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync() :
                await context.Set<T>().AsNoTracking().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the T2 class with the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the T2 class.</returns>
    public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync(),
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
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression with or without navigation properties</returns>
    public Task<T?> GetMaxByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetMaxByOrder(whereExpression, descendingOrderEpression, queryTimeout, trackEntities) :
            GetMaxByOrderFull(whereExpression, descendingOrderEpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Uses a descending order expression to return the record containing the maximum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression</returns>
    public async Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync() :
                await context.Set<T>().AsNoTracking().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a descending order expression to return the record and its navigation properties containing the maximum value according to that order.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression with it's navigation properties</returns>
    public async Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync(),
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
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression with or without navigation properties</returns>
    public Task<T?> GetMinByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetMinByOrder(whereExpression, ascendingOrderEpression, queryTimeout, trackEntities) :
            GetMinByOrderFull(whereExpression, ascendingOrderEpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Uses a ascending order expression to return the record containing the minimum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression</returns>
    public async Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync() :
                await context.Set<T>().AsNoTracking().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a ascending order expression to return the record and its navigation properties containing the minimum value according to that order.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression with it's navigation properties</returns>
    public async Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync(),
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The maximum object specified by the max expression with or without navigation properties</returns>
    public Task<T2?> GetMax<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetMax(whereExpression, maxExpression, queryTimeout, trackEntities) :
            GetMaxFull(whereExpression, maxExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Uses a max expression to return the record containing the maximum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The maximum value specified by the max expression</returns>
    public async Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).MaxAsync(maxExpression) : await context.Set<T>().AsNoTracking().Where(whereExpression).MaxAsync(maxExpression);
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The maximum object specified by the min expression</returns>
    public async Task<T2?> GetMaxFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression) :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MaxAsync(maxExpression) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MaxAsync(maxExpression),
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function</param>
    /// <param name="queryTimeout">Optional: Override the database default for query timeout.</param>
    /// <param name="trackEntities">Optional: If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Optional: Used only when running "full" query. Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The minimum object specified by the min expression with or without navigation properties</returns>
    public Task<T2?> GetMin<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        return !full ? GetMin(whereExpression, minExpression, queryTimeout, trackEntities) :
            GetMinFull(whereExpression, minExpression, queryTimeout, trackEntities, splitQueryOverride, maxNavigationDepth, navPropAttributesToIgnore, useCaching);
    }

    /// <summary>
    /// Uses a min expression to return the record containing the minimum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <returns>The minimum value specified by the min expression</returns>
    public async Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null) { context.Database.SetCommandTimeout((TimeSpan)queryTimeout); }

        T2? model = default;
        try
        {
            model = trackEntities ? await context.Set<T>().Where(whereExpression).MinAsync(minExpression) : await context.Set<T>().AsNoTracking().Where(whereExpression).MinAsync(minExpression);
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
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="trackEntities">If true, entities will be tracked in memory. Default is false for "Full" queries, and queries that return more than one entity.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The minimum object specified by the min expression</returns>
    public async Task<T2?> GetMinFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null,
        bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = splitQueryOverride switch
            {
                null => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MinAsync(minExpression) :
                    await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
                true => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MinAsync(minExpression) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
                _ => !trackEntities && !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).AsNoTracking().MinAsync(minExpression) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
            };
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = splitQueryOverride switch
                    {
                        null => await context.Set<T>().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
                        true => await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
                        _ => await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context, maxNavigationDepth, navPropAttributesToIgnore).Where(whereExpression).MinAsync(minExpression),
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
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The number of records that satisfy the where expression.</returns>
    public async Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        int count = 0;
        try
        {
            count = await context.Set<T>().Where(whereExpression).AsNoTracking().CountAsync();
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
    /// Creates a new record in the table corresponding to type T
    /// </summary>
    /// <param name="model">Record of type T to be added to the table</param>
    public async Task Create(T model, bool removeNavigationProps = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (removeNavigationProps)
        {
            model.RemoveNavigationProperties(context);
        }

        try
        {
            await context.Set<T>().AddAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
        }
    }

    /// <summary>
    /// Creates new records in the table corresponding to type T
    /// </summary>
    /// <param name="model">Records of type T to be added to the table</param>
    public async Task CreateMany(IEnumerable<T> model, bool removeNavigationProps = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (removeNavigationProps)
        {
            model.SetValue(x => x.RemoveNavigationProperties(context));
        }

        try
        {
            //await context.Set<T>().BulkInsertAsync(model); //Doesn't give updated identity values. EF Core Extensions (Paid)
            await context.Set<T>().AddRangeAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(model, defaultJsonSerializerOptions)}");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type T matching the object of type T passed in
    /// </summary>
    /// <param name="model">Record of type T to delete</param>
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
    /// Delete record in the table corresponding to type T matching the primary key passed in
    /// </summary>
    /// <param name="key">Key of the record of type T to delete</param>
    public async Task<bool> DeleteByKey(object key)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        DbSet<T> table = context.Set<T>();
        bool success = false;
        try
        {
            T? deleteItem = await table.FindAsync(key);
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
    /// Delete records in the table corresponding to type T matching the enumerable objects of type T passed in
    /// </summary>
    /// <param name="models">Records of type T to delete</param>
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
    /// Delete records in the table corresponding to type T matching the enumerable objects of type T passed in
    /// </summary>
    /// <param name="models">Records of type T to delete</param>
    public async Task<bool> DeleteManyTracked(IEnumerable<T> models, bool removeNavigationProps = false)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            if (removeNavigationProps)
            {
                models.SetValue(x => x.RemoveNavigationProperties(context));
            }
            await context.Set<T>().DeleteRangeByKeyAsync(models); //EF Core +, Does not require separate save
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tModel: {JsonSerializer.Serialize(models, defaultJsonSerializerOptions)}");
        }
        return false;
    }

    /// <summary>
    /// Delete records in the table corresponding to type T matching the enumerable objects of type T passed in
    /// </summary>
    /// <param name="keys">Keys of type T to delete</param>
    public async Task<bool> DeleteManyByKeys(IEnumerable<object> keys) //Does not work with Postgres
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            await context.Set<T>().DeleteRangeByKeyAsync(keys); //EF Core +, Does not require separate save
            return true;
        }
        catch (Exception ex)
        {
            logger.Error(ex, "{msg}", $"{ex.GetLocationOfException()} Error\n\tKeys: {JsonSerializer.Serialize(keys)}");
        }
        return false;
    }

    /// <summary>
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="model">The modified entity</param>
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
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="models">The modified entity</param>
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
    /// Persist any tracked changes to the database
    /// </summary>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> SaveChanges()
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        bool result = false;
        try
        {
            result = await context.SaveChangesAsync() > 0;
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

public class GenericPagingModel<T> where T : class
{
    public GenericPagingModel()
    {
        Entities = [];
    }

    public List<T> Entities { get; set; }
    public int TotalRecords { get; set; }
}
