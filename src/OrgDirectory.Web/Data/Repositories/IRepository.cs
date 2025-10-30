using System.Linq.Expressions;

namespace OrgDirectory.Web.Data.Repositories;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(object id);
    Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProps = "");
    Task AddAsync(T entity);
    void Update(T entity);
    void Remove(T entity);
}
