using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace OrgDirectory.Web.Data.Repositories;

public class Repository<T> : IRepository<T> where T : class
{
    private readonly DbSet<T> _dbSet;
    protected readonly DbContext _context;

    public Repository(DbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    public async Task<T?> GetByIdAsync(object id) => await _dbSet.FindAsync(id);

    public async Task<List<T>> GetAllAsync(Expression<Func<T, bool>>? predicate = null, Func<IQueryable<T>, IOrderedQueryable<T>>? orderBy = null, string includeProps = "")
    {
        IQueryable<T> query = _dbSet;
        if (predicate != null)
            query = query.Where(predicate);

        foreach (var includeProp in includeProps.Split(new[] {','}, StringSplitOptions.RemoveEmptyEntries))
        {
            query = query.Include(includeProp.Trim());
        }

        if (orderBy != null)
            query = orderBy(query);

        return await query.AsNoTracking().ToListAsync();
    }

    public async Task AddAsync(T entity) => await _dbSet.AddAsync(entity);

    public void Update(T entity) => _dbSet.Update(entity);

    public void Remove(T entity) => _dbSet.Remove(entity);
}
