using System.Collections.Concurrent;
using System.Linq.Dynamic.Core;
using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using static Common_Net_Funcs.Tools.DataValidation;
using static Common_Net_Funcs.Tools.DebugHelpers;
using static Common_Net_Funcs.Tools.ObjectHelpers;

namespace Common_Net_Funcs.EFCore;

//TODO:: Investigate if circularReferencingEntities can be removed due to EF8 Lay Loading for no-tracking queries

/// <summary>
/// Common EF Core interactions with a database. Must be using dependency injection for this class to work.
/// </summary>
/// <typeparam name="T">Entity class to be used with these methods.</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against.</typeparam>
public class BaseDbContextActions<T, UT>(IServiceProvider serviceProvider) : IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    static readonly ConcurrentDictionary<Type, bool> circularReferencingEntities = new();

    private static readonly NLog.Logger logger = NLog.LogManager.GetCurrentClassLogger();

    public IServiceProvider serviceProvider = serviceProvider;

    #region Read

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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>Record of type T corresponding to the primary key passed in.</returns>
    public async Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
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
                if (splitQueryOverride == null)
                {
                    model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().IncludeNavigationProperties(context).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().IncludeNavigationProperties(context).GetObjectByPartial(model);
                }
                else if (splitQueryOverride == true)
                {
                    model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).GetObjectByPartial(model);
                }
                else
                {
                    model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).AsNoTracking().GetObjectByPartial(model) :
                        context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).GetObjectByPartial(model);
                }
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
                        if (splitQueryOverride == null)
                        {
                            model = context.Set<T>().IncludeNavigationProperties(context).GetObjectByPartial(model);
                        }
                        else if (splitQueryOverride == true)
                        {
                            model = context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).GetObjectByPartial(model);
                        }
                        else
                        {
                            model = context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).GetObjectByPartial(model);
                        }
                    }
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        //Microsoft.EntityFrameworkCore.Query.NavigationBaseIncludeIgnored

        return model;
    }

    /// <summary>
    /// Gets all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async Task<List<T>?> GetAll(TimeSpan? queryTimeout = null)
    {
        IQueryable<T> query = GetQueryAll(queryTimeout);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table.
    /// Same as running a SELECT * query.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public IQueryable<T> GetQueryAll(TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return context.Set<T>().AsNoTracking();
    }

    /// <summary>
    /// Gets all records from the corresponding table and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null)
    {
        IQueryable<T2> query = GetQueryAll(selectExpression, queryTimeout);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public IQueryable<T2> GetQueryAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return context.Set<T>().AsNoTracking().Select(selectExpression);
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public async Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        IQueryable<T> query = GetQueryAllFull(queryTimeout, splitQueryOverride);
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
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <returns>All records from the table corresponding to class T.</returns>
    public IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false)
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
                null => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context),
                true => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                   context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).AsNoTracking() :
                   context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context),
                _ => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context)
            };
        }
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table and transforms them into the type T2.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2.</returns>
    public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        IQueryable<T2> query = GetQueryAllFull(selectExpression, queryTimeout, splitQueryOverride);
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
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
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
    /// <returns>All records from the table corresponding to class T2.</returns>
    public IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false)
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
                null => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context).Select(selectExpression),
                true => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Select(selectExpression),
                _ => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Select(selectExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Select(selectExpression)
            };
        }
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null)
    {
        IQueryable<T> query = GetQueryWithFilter(whereExpression, queryTimeout);
        List<T>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression and transforms them into the type T2.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T> GetQueryWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return context.Set<T>().Where(whereExpression).AsNoTracking();
    }

    /// <summary>
    /// Gets all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null)
    {
        IQueryable<T2> query = GetQueryWithFilter(whereExpression, selectExpression, queryTimeout);
        List<T2>? model = null;
        try
        {
            model = await query.ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> query.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T2> GetQueryWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        return context.Set<T>().Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct();
    }

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core string representation of an order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the records with navigation properties specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core string representation of an order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null) where T2 : class
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        GenericPagingModel<T2> model = new();
        try
        {
            IQueryable<T2> qModel = context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression);
            var results = await qModel.Select(x => new { Entities = x, TotalCount = qModel.Count() })
                .Skip(skip).Take(pageSize > 0 ? pageSize : int.MaxValue).ToListAsync();

            model.TotalRecords = results.FirstOrDefault()?.TotalCount ?? await qModel.CountAsync();
            model.Entities = results.ConvertAll(x => x.Entities);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets the records specified by the skip and take parameters from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT <SpecificFields> WHERE <condition> query with Limit/Offset or Fetch/Offset parameters.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="orderByString">EF Core string representation of an order by statement to keep results consistent.</param>
    /// <param name="skip">How many records to skip before the ones that should be returned.</param>
    /// <param name="pageSize">How many records to take after the skipped records.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The records specified by the skip and take parameters from the table corresponding to class T that also satisfy the conditions of linq query expression, which are converted to T2.</returns>
    public async Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null) where T2 : class
    {
        IQueryable<T2> qModel;
        GenericPagingModel<T2> model = new();
        try
        {
            qModel = GetQueryPagingWithFilterFull(whereExpression, selectExpression, ascendingOrderEpression, queryTimeout, splitQueryOverride);

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

                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    public IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression,
        Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false)
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
                null => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().Select(selectExpression) :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).Select(selectExpression)
            };
        }
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, queryTimeout, splitQueryOverride);
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
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets query to get all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <param name="handlingCircularRefException">If handling InvalidOperationException where .AsNoTracking() can't be used</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression.</returns>
    public IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false)
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
                null => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression),
                true => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression),
                _ => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression)
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression)
            };
        }
    }

    /// <summary>
    /// Gets all records with navigation properties from the corresponding table that satisfy the conditions of the linq query expression, and then transforms them into the T2 class using the select expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        IQueryable<T2> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride);
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
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
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
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression and have been transformed in to the T2 class with the select expression.</returns>
    public IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false)
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
                null => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).Distinct() :
                    context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct()
            };
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
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public async Task<List<T>?> GetWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null) where T2 : class
    {
        IQueryable<T> query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride);
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
                    query = GetQueryWithFilterFull(whereExpression, selectExpression, queryTimeout, splitQueryOverride, true);
                    model = await query.ToListAsync();
                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T2), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
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
    /// <returns>All records from the table corresponding to class T2 that also satisfy the conditions of linq query expression and have been transformed in to the T class with the select expression.</returns>
    public IQueryable<T> GetQueryWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false) where T2 : class
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
                null => !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                true => !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => !circularReferencingEntities.TryGetValue(typeof(T2), out _) ?
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking() :
                    context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
        else
        {
            return splitQueryOverride switch
            {
                null => context.Set<T2>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                true => context.Set<T2>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct(),
                _ => context.Set<T2>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).Distinct()
            };
        }
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression.
    /// Same as running a SELECT * WHERE <condition> LIMIT 1 or SELECT TOP 1 * WHERE <condition> LIMIT 1 query.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().FirstOrDefaultAsync(whereExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record from the corresponding table that satisfy the conditions of the linq query expression and transforms it into the T2 class with a select expression.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="selectExpression">Linq expression to transform the returned records to the desired output.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression that has been transformed into the T2 class with the select expression.</returns>
    public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Gets first record with navigation properties from the corresponding table that satisfy the conditions of the linq query expression.
    /// Navigation properties using newtonsoft.Json [JsonIgnore] attributes will not be included.
    /// </summary>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with its navigation properties that also satisfies the conditions of the linq query expression.</returns>
    public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>();
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            if (splitQueryOverride == null)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
            }
            else if (splitQueryOverride == true)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
            }
            else
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).AsNoTracking().FirstOrDefaultAsync(whereExpression) :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    if (splitQueryOverride == null)
                    {
                        model = await context.Set<T>().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
                    }
                    else if (splitQueryOverride == true)
                    {
                        model = await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
                    }
                    else
                    {
                        model = await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).FirstOrDefaultAsync(whereExpression);
                    }

                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>First record from the table corresponding to class T with its navigation properties that also satisfies the conditions of the linq query expression and has been transformed into the T2 class.</returns>
    public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            if (splitQueryOverride == null)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
            }
            else if (splitQueryOverride == true)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
            }
            else
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).AsNoTracking().Select(selectExpression).FirstOrDefaultAsync() :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    if (splitQueryOverride == null)
                    {
                        model = await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
                    }
                    else if (splitQueryOverride == true)
                    {
                        model = await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
                    }
                    else
                    {
                        model = await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
                    }

                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a descending order expression to return the record containing the maximum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the descendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="descendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression</returns>
    public async Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the maximum value according to the ascending order expression with it's navigation properties</returns>
    public async Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            if (splitQueryOverride == null)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
            }
            else if (splitQueryOverride == true)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                   await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                   await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
            }
            else
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                   await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                   await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    if (splitQueryOverride == null)
                    {
                        model = await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
                    }
                    else if (splitQueryOverride == true)
                    {
                        model = await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
                    }
                    else
                    {
                        model = await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderByDescending(descendingOrderEpression).FirstOrDefaultAsync();
                    }

                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a max expression to return the record containing the maximum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="maxExpression">A linq expression used in the .Max() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The maximum value specified by the min expression</returns>
    public async Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).MaxAsync(maxExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a ascending order expression to return the record containing the minimum value according to that order.
    /// </summary>
    /// <typeparam name="TKey">Type being used to order records with in the ascendingOrderEpression</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="ascendingOrderEpression">A linq expression used to order the query results with before taking the top result</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression</returns>
    public async Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
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
    /// <param name="splitQueryOverride">Override for the default query splitting behavior of the database context. Value of true will split queries, false will prevent splitting.</param>
    /// <returns>The record that contains the minimum value according to the ascending order expression with it's navigation properties</returns>
    public async Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null)
        {
            context.Database.SetCommandTimeout((TimeSpan)queryTimeout);
        }

        T? model = null;
        try
        {
            if (splitQueryOverride == null)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
            }
            else if (splitQueryOverride == true)
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
            }
            else
            {
                model = !circularReferencingEntities.TryGetValue(typeof(T), out _) ?
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).AsNoTracking().FirstOrDefaultAsync() :
                    await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
            }
        }
        catch (InvalidOperationException ioEx)
        {
            if (ioEx.HResult == -2146233079)
            {
                try
                {
                    if (splitQueryOverride == null)
                    {
                        model = await context.Set<T>().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
                    }
                    else if (splitQueryOverride == true)
                    {
                        model = await context.Set<T>().AsSplitQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
                    }
                    else
                    {
                        model = await context.Set<T>().AsSingleQuery().IncludeNavigationProperties(context).Where(whereExpression).OrderBy(ascendingOrderEpression).FirstOrDefaultAsync();
                    }

                    logger.Warn($"Adding {typeof(T).Name} to circularReferencingEntities");
                    circularReferencingEntities.AddDictionaryItem(typeof(T), true);
                }
                catch (Exception ex2)
                {
                    logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error1");
                    logger.Error(ex2, $"{ex2.GetLocationOfEexception()} Error2");
                }
            }
            else
            {
                logger.Error(ioEx, $"{ioEx.GetLocationOfEexception()} Error");
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

    /// <summary>
    /// Uses a min expression to return the record containing the minimum value specified.
    /// </summary>
    /// <typeparam name="T2">Class type to return, specified by the selectExpression parameter</typeparam>
    /// <param name="whereExpression">A linq expression used to filter query results.</param>
    /// <param name="minExpression">A linq expression used in the .Min() function</param>
    /// <param name="queryTimeout">Override the database default for query timeout.</param>
    /// <returns>The minimum value specified by the min expression</returns>
    public async Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        if (queryTimeout != null) { context.Database.SetCommandTimeout((TimeSpan)queryTimeout); }

        T2? model = default;
        try
        {
            model = await context.Set<T>().Where(whereExpression).MinAsync(minExpression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return model;
    }

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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
        return count;
    }

    #endregion Read

    #region Write

    /// <summary>
    /// Creates a new record in the table corresponding to type T
    /// </summary>
    /// <param name="model">Record of type T to be added to the table</param>
    public async Task Create(T model)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            await context.Set<T>().AddAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Creates new records in the table corresponding to type T
    /// </summary>
    /// <param name="model">Records of type T to be added to the table</param>
    public async Task CreateMany(IEnumerable<T> model)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            await context.Set<T>().AddRangeAsync(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type T matching the object of type T passed in
    /// </summary>
    /// <param name="model">Record of type T to delete</param>
    public void DeleteByObject(T model)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;

        try
        {
            context.Set<T>().Remove(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Delete record in the table corresponding to type T matching the primary key passed in
    /// </summary>
    /// <param name="id">Key of the record of type T to delete</param>
    public async Task DeleteByKey(object id)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
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
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Delete records in the table corresponding to type T matching the enumerable objects of type T passed in
    /// </summary>
    /// <param name="model">Records of type T to delete</param>
    public void DeleteMany(IEnumerable<T> model)
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        try
        {
            context.Set<T>().RemoveRange(model);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
        }
    }

    /// <summary>
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="model">The modified entity</param>
    public void Update(T model) //Send in modified object
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        context.Entry(model).State = EntityState.Modified;
    }

    /// <summary>
    /// Mark an entity as modified in order to be able to persist changes to the database upon calling context.SaveChanges()
    /// </summary>
    /// <param name="models">The modified entity</param>
    public void UpdateMany(List<T> models) //Send in modified objects
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        context.UpdateRange(models);
    }

    /// <summary>
    /// Persist any tracked changes to the database
    /// </summary>
    /// <returns>Boolean indicating success</returns>
    public async Task<bool> SaveChanges()
    {
        using DbContext context = serviceProvider.GetRequiredService<UT>()!;
        var result = false;
        try
        {
            result = await context.SaveChangesAsync() > 0;
        }
        catch (DbUpdateException duex)
        {
            logger.Error(duex, $"{duex.GetLocationOfEexception()} DBUpdate Error");
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{ex.GetLocationOfEexception()} Error");
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
