using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace CommonNetFuncs.EFCore;
public interface IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    #region Read

    Task<T?> GetByKey(bool full, object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null);
    Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T?> GetByKey(bool full, object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null);
    Task<T?> GetByKeyFull(object[] primaryKey, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<List<T>?> GetAll(bool full, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<List<T>?> GetAll(TimeSpan? queryTimeout = null, bool trackEntities = false);
    IQueryable<T> GetQueryAll(TimeSpan? queryTimeout = null, bool trackEntities = false);
    Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    IQueryable<T> GetQueryAllFull(TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<List<T2>?> GetAll<T2>(bool full, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    IQueryable<T2> GetQueryAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    IQueryable<T2> GetQueryAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false,
        bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<List<T>?> GetWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    IQueryable<T> GetQueryWithFilter(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    IQueryable<T> GetQueryWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false,
        bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<List<T2>?> GetWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    IQueryable<T2> GetQueryWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false);
    Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    IQueryable<T2> GetQueryWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null,
        bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<List<T>?> GetNavigationWithFilter<T2>(bool full, Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    Task<List<T>?> GetNavigationWithFilter<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    Task<List<T>?> GetNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    IQueryable<T> GetQueryNavigationWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null,
        bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;

    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0,
        int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
        TimeSpan? queryTimeout = null, bool trackEntities = false) where T2 : class;
    Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0,
        TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    IQueryable<T2> GetQueryPagingWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString, TimeSpan? queryTimeout = null,
        bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;

    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression,
        int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression, int skip = 0,
        int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false) where T2 : class;
    Task<GenericPagingModel<T2>> GetWithPagingFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression,
        int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;
    IQueryable<T2> GetQueryPagingWithFilterFull<T2, TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, Expression<Func<T, TKey>> ascendingOrderEpression,
        TimeSpan? queryTimeout = null, bool? splitQueryOverride = null, bool handlingCircularRefException = false, bool trackEntities = false, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true) where T2 : class;

    Task<T?> GetOneWithFilter(bool full, Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null, int maxNavigationDepth = 100,
        List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T2?> GetOneWithFilter<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T?> GetMaxByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T?> GetMinByOrder<TKey>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T2?> GetMax<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T2?> GetMaxFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<T2?> GetMin<T2>(bool full, Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false,
        bool? splitQueryOverride = null, int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);
    Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null, bool trackEntities = true);
    Task<T2?> GetMinFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> minExpression, TimeSpan? queryTimeout = null, bool trackEntities = false, bool? splitQueryOverride = null,
        int maxNavigationDepth = 100, List<Type>? navPropAttributesToIgnore = null, bool useCaching = true);

    Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null);

    #endregion Read

    #region Write

    Task Create(T model, bool removeNavigationProps = false);
    Task CreateMany(IEnumerable<T> model, bool removeNavigationProps = false);
    void DeleteByObject(T model, bool removeNavigationProps = false);
    Task<bool> DeleteByKey(object id);
    bool DeleteMany(IEnumerable<T> models, bool removeNavigationProps = false);
    Task<bool> DeleteManyByKeys(IEnumerable<object> keys);
    void Update(T models, bool removeNavigationProps = false);
    bool UpdateMany(List<T> models, bool removeNavigationProps = false);
    Task<bool> SaveChanges();

    #endregion Write
}
