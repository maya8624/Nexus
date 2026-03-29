using Microsoft.EntityFrameworkCore;
using Nexus.Application.Interfaces.Repository;
using Nexus.Infrastructure.Persistence;
using System.Linq.Expressions;

namespace Nexus.Infrastructure.Repositories
{
    public class RepositoryBase<T> : IRepositoryBase<T> where T : class
    {
        private readonly AppDbContext _context;
        private readonly DbSet<T> _dbSet;

        public RepositoryBase(AppDbContext context)
        {
            _context = context;
            _dbSet = context.Set<T>();
        }

        public async Task Create(T entity, CancellationToken ct)
            => await _dbSet.AddAsync(entity, ct);

        public async Task CreateRange(IEnumerable<T> entities, CancellationToken ct)
            => await _dbSet.AddRangeAsync(entities, ct);
        public void Delete(T entity)
            => _dbSet.Remove(entity);

        public async Task<IList<T>> GetAll(CancellationToken ct)
            => await _dbSet.ToListAsync(ct);

        public async Task<IEnumerable<T>> GetByCondition(Expression<Func<T, bool>> expression, CancellationToken ct)
            => await _dbSet.Where(expression).ToListAsync(ct);

        public async Task<T?> Find(int id, CancellationToken ct)
            => await _dbSet.FindAsync(id, ct);
         
        public async Task<bool> IsAny(Expression<Func<T, bool>> expression, CancellationToken ct)
            => await _dbSet.AnyAsync(expression, ct);
           
        public void Update(T entity)
            => _dbSet.Update(entity);
    }
}
