using System.Linq.Expressions;
using Microsoft.EntityFrameworkCore;

namespace Common_Net_Funcs.EFCore;
public interface IBaseDbContextActions<T, UT> where T : class where UT : DbContext
{
    #region Read
    Task<T?> GetByKey(object primaryKey);
    Task<T?> GetByKeyFull(object primaryKey);
    Task<List<T>?> GetAll();
    Task<List<T>?> GetWithFilter(Expression<Func<T, bool>> expression);
    Task<List<T>?> GetWithFilterFull(Expression<Func<T, bool>> expression);
    Task<T?> GetOneWithFilter(Expression<Func<T, bool>> expression);
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
