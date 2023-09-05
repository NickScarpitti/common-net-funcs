using System.Linq.Expressions;
using System.Reflection;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.Extensions.DependencyInjection;
using static Common_Net_Funcs.Tools.ObjectHelpers;

namespace Common_Net_Funcs.EFCore;

/// <summary>
/// Common EF Core interactions with a database. Must be using DI for this method to work
/// </summary>
/// <typeparam name="T">Entity class to be used with these methods</typeparam>
/// <typeparam name="UT">DB Context for the database you with to run these actions against</typeparam>
public class BaseDbContextActions<T, UT> : IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
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
                IQueryable<T> query = context.Set<T>().IncludeNavigationProperties(context, typeof(T));
                model = GetObjectByPartial(query, model);
            }
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
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

    public async Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = await context.Set<T>().Select(selectExpression).AsNoTracking().ToListAsync();
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
            model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = await context.Set<T>().IncludeNavigationProperties(context, typeof(T)).Select(selectExpression).AsNoTracking().ToListAsync();
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
    /// <param name="expression">Linq expression to filter the records to be returned</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression</returns>
    public async Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            model = await context.Set<T>().Where(expression).AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            model = await context.Set<T>().Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking().ToListAsync();
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
    /// <param name="expression">Linq expression to filter the records to be returned</param>
    /// <returns>All records from the table corresponding to class T that also satisfy the conditions of linq query expression</returns>
    public async Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T>? model = null;
        try
        {
            IQueryable<T> query = context.Set<T>().IncludeNavigationProperties(context, typeof(T));
            model = await query.Where(expression).AsNoTracking().ToListAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        List<T2>? model = null;
        try
        {
            IQueryable<T> query = context.Set<T>().IncludeNavigationProperties(context, typeof(T));
            model = await query.Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking().ToListAsync();
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
            IQueryable<T2> query = context.Set<T2>().IncludeNavigationProperties(context, typeof(T));
            model = await query.Where(whereExpression).Select(selectExpression).Distinct().AsNoTracking().ToListAsync();
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
    /// <param name="expression">Linq expression to filter the record to be returned</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression</returns>
    public async Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            model = await context.Set<T>().FirstOrDefaultAsync(expression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = null;
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
    /// <param name="expression">Linq expression to filter the record to be returned</param>
    /// <returns>First record from the table corresponding to class T that also satisfy the conditions of the linq query expression</returns>
    public async Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> expression)
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T? model = null;
        try
        {
            IQueryable<T> query = context.Set<T>().IncludeNavigationProperties(context, typeof(T));
            model = await query.FirstOrDefaultAsync(expression);
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
    }

    public async Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class
    {
        using DbContext context = serviceProvider.GetService<UT>()!;
        T2? model = null;
        try
        {
            IQueryable<T> query = context.Set<T>().IncludeNavigationProperties(context, typeof(T));
            model = await query.Where(whereExpression).Select(selectExpression).FirstOrDefaultAsync();
        }
        catch (Exception ex)
        {
            logger.Error(ex, $"{MethodBase.GetCurrentMethod()?.Name} Error");
        }
        return model;
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
