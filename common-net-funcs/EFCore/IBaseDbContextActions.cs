using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Common_Net_Funcs.EFCore;
public interface IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    #region Read
    Task<T?> GetByKey(object primaryKey, TimeSpan? queryTimeout = null);
    Task<T?> GetByKey(object[] primaryKey, TimeSpan? queryTimeout = null);
    Task<T?> GetByKeyFull(object primaryKey, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<List<T>?> GetAll(TimeSpan? queryTimeout = null);
    Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null);
    Task<List<T>?> GetAllFull(TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null);
    Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null);
    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0, TimeSpan? queryTimeout = null) where T2 : class;
    Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<List<T>?> GetWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null) where T2 : class;
    Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression, TimeSpan? queryTimeout = null);
    Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null);
    Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null);
    Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null);
    Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null);
    Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression, TimeSpan? queryTimeout = null, bool? splitQueryOverride = null);
    Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression, TimeSpan? queryTimeout = null);
    Task<int> GetCount(Expression<Func<T, bool>> whereExpression, TimeSpan? queryTimeout = null);
    #endregion

    #region Write
    Task Create(T model);
    Task CreateMany(IEnumerable<T> model);
    void DeleteByObject(T model);
    Task DeleteByKey(object id);
    void DeleteMany(IEnumerable<T> model);
    void Update(T model);
    void UpdateMany(List<T> models);
    Task<bool> SaveChanges();
    #endregion
}
