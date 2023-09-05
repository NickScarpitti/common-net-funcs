using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Common_Net_Funcs.EFCore;
public interface IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    #region Read
    Task<T?> GetByKey(object primaryKey);
    Task<T?> GetByKeyFull(object primaryKey);
    Task<List<T>?> GetAll();
    Task<List<T2>?> GetAll<T2>(Expression<Func<T, T2>> selectExpression) where T2 : class;
    Task<List<T>?> GetAllFull();
    Task<List<T2>?> GetAllFull<T2>(Expression<Func<T, T2>> selectExpression) where T2 : class;
    Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression);
    Task<List<T2>?> GetWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class;
    Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression);
    Task<List<T2>?> GetWithFilterFull<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class;
    Task<List<T>?> GetWithFilterFull<T2>(Expression<Func<T2, bool>> whereExpression, Expression<Func<T2, T>> selectExpression) where T2 : class;
    Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression);
    Task<T2?> GetOneWithFilter<T2>(Expression<Func<T, bool>> whereExpression, Expression<Func<T, T2>> selectExpression) where T2 : class;
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
