using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Common_Net_Funcs.EFCore;
public interface IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    #region Read
    Task<T?> GetByKey(object primaryKey);
    Task<T?> GetByKeyFull(object primaryKey);
    Task<List<T>?> GetAll();
    Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression);
    Task<List<T>?> GetAllFull();
    Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression);
    Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression);
    Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression);
    Task<GenericPagingModel<T2>> GetWithPagingFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression, string? orderByString = null, int skip = 0, int pageSize = 0) where T2 : class;
    Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression);
    Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression);
    Task<List<T>?> GetWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression) where T2 : class;
    Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression);
    Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression);
    Task<T?> GetOneWithFilterFull(Expression<Func<T, bool>> whereExpression);
    Task<T2?> GetOneWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression);
    Task<T?> GetMaxByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression);
    Task<T?> GetMaxByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> descendingOrderEpression);
    Task<T2?> GetMax<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression);
    Task<T?> GetMinByOrder<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression);
    Task<T?> GetMinByOrderFull<TKey>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, TKey>> ascendingOrderEpression);
    Task<T2?> GetMin<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> maxExpression);
    Task<int> GetCount(Expression<Func<T, bool>> whereExpression);
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
