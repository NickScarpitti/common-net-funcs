using System.Linq.Expressions;
using System.Linq.Dynamic.Core;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using System.Collections.Concurrent;
using Common_Net_Funcs.Tools;
using static Common_Net_Funcs.Tools.ObjectHelpers;

namespace Common_Net_Funcs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using DI for this method to work
/// </summary>
/// <typeparam name="T">Entity class to be used with these methods</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against</typeparam>
public class BaseDbContextActions<T, UT> : IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public IServiceProvider serviceProvider;

    public BaseDbContextActions(IServiceProvider serviceProvider)
    {
        this.serviceProvider = serviceProvider;
    }

    #region Read

    /// <summary>
    /// Get individual record by the primary key.
    /// If using a compound primary key, use an object of the same class to be returned with the primary key fields populated.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned</param>
    /// <returns>Record of type T corresponding to the primary key passed in</returns>
    public async Task<T?> GetByKey(object primaryKey)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await context.Set<T>().FindAsync(primaryKey);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Get individual record by the primary key with all navigation properties.
    /// If using a compound primary key, use an object of the same class to be returned with the primary key fields populated.
    /// Navigation properties using Newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="primaryKey">Primary key of the record to be returned</param>
    /// <returns>Record of type T corresponding to the primary key passed in</returns>
    public async Task<T?> GetByKeyFull(object primaryKey)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await GetByKey(primaryKey);
            if (model != null)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context, typeof(T)).AsNoTracking().GetObjectByPartial(model) :
                    context.Set<T>().IncludeNavigationProperties(context, typeof(T)).GetObjectByPartial(model);
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
                        model = context.Set<T>().IncludeNavigationProperties(context, typeof(T)).GetObjectByPartial(model);
                    }
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        //Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored

        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table.
    /// Same as running a SELECT * query
    /// </summary>
    /// <returns>All records from the table corresponding to class T</returns>
    public async Task<List<T>?> GetAll()
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = await context.Set<T>().AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = await context.Set<T>().AsNoTracking().Select(selectExpression).ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table.
    /// Navigation properties using Newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <returns>All records from the table corresponding to class T</returns>
    public async Task<List<T>?> GetAllFull()
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ? model =
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).AsNoTracking().ToListAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).AsNoTracking().Select(selectExpression).ToListAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Select(selectExpression).ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Select(selectExpression).ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query
    /// </summary>
    /// <param name="whereExpression">Linq expression to filter the records to be returned</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression</returns>
    public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        GenericPagingModel<T2> model = new();

        try
        {
            IQueryable<T2> qModel = context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression);
            var results = await qModel.OrderBy(orderByString ?? string.Empty).Select(x => new { Entities = x, TotalCount = qModel.Count() })
                .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using Newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">Linq expression to filter the records to be returned</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression</returns>
    public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).AsNoTracking().ToListAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct().ToListAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).Distinct().ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).Distinct().ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T>?> GetWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T2>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking().ToListAsync() :
                await context.Set<T2>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).Distinct().ToListAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T2>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).Distinct().ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> LIMIT 1 or SELECT TOP 1 * WHERE <condition> LIMIT 1 query.
    /// </summary>
    /// <param name="whereExpression">Linq expression to filter the record to be returned</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression</returns>
    public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await context.Set<T>().FirstOrDefaultAsync(whereExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using Newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">Linq expression to filter the record to be returned</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression</returns>
    public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).FirstOrDefaultAsync(whereExpression);
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).FirstOrDefaultAsync(whereExpression);
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = default;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).MaxAsync(maxExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error1");
                    logger.Error(ex2, $"{MethodBase.GetCurrentMethod()?.Name} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{MethodBase.GetCurrentMethod()?.Name} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).MinAsync(maxExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<int> GetCount(Expression<Func<T, bool>> whereExpression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        int count = 0;
        try
        {
            count = await context.Set<T>().Where(whereExpression).AsNoTracking().CountAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return count;
    }

    #endregion

    #region Write

    /// <summary>
    /// Creates a new record in the table corresponding to type T
    /// </summary>
    /// <param name="model">Record of type T to be added to the table</param>
    public async Task Create(T model)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        try
        {
            await context.Set<T>().AddAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
    }

    /// <summary>
    /// Creates new records in the table corresponding to type T
    /// </summary>
    /// <param name="model">Records of type T to be added to the table</param>
    public async Task CreateMany(IEnumerable<T> model)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        try
        {
            await context.Set<T>().AddRangeAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type T matching the object of type T passed in
    /// </summary>
    /// <param name="model">Record of type T to delete</param>
    public void DeleteByObject(T model)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;

        try
        {
            context.Set<T>().Remove(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type T matching the primary key passed in
    /// </summary>
    /// <param name="id">Key of the record of type T to delete</param>
    public async Task DeleteByKey(object id)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        var table = context.Set<T>();

        try
        {
            var deleteItem = await table.FindAsync(id);
            if (deleteItem != null)
            {
                table.Remove(deleteItem);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
    }

    /// <summary>
    /// Delete records in the table corresponding to type T matching the enumerable objects of type T passed in
    /// </summary>
    /// <param name="model">Records of type T to delete</param>
    public void DeleteMany(IEnumerable<T> model)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        try
        {
            context.Set<T>().RemoveRange(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
    }

    /// <summary>
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="model">The modified entity</param>
    public void Update(T model) //Send in modified object
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        context.Entry(model).State = EntityState.Modified;
    }

    /// <summary>
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="models">The modified entity</param>
    public void UpdateMany(List<T> models) //Send in modified objects
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        context.UpdateRange(models);
    }

    /// <summary>
    /// Persist any tracked changes to the database
    /// </summary>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> SaveChanges()
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        var result = false;
        try
        {
            result = await context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateException duex)
        {
            logger.Error(duex, $"{MethodBase.GetCurrentMethod()?.Name} DBUpdate Error");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return result;
    }

    #endregion
}

public class GenericPagingModel<T> where T : class
{
    public GenericPagingModel()
    {
        Entities = new();
    }

    public List<T> Entities { get; set; }
    public int TotalRecords { get; set; }
}
