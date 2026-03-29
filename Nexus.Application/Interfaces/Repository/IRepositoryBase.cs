using System.Linq.Expressions;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IRepositoryBase<T> where T : class
    {
        Task Create(T entity, CancellationToken ct);
        Task CreateRange(IEnumerable<T> entities, CancellationToken ct);
        Task<IList<T>> GetAll(CancellationToken ct);
        Task<T?> Find(int id, CancellationToken ct);
        Task<IEnumerable<T>> GetByCondition(Expression<Func<T, bool>> expression, CancellationToken ct);
        Task<bool> IsAny(Expression<Func<T, bool>> expression, CancellationToken ct);
        void Update(T entity);
        void Delete(T entity);
    }
}
