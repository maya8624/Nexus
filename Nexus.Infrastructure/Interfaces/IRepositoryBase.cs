using System.Linq.Expressions;

namespace Nexus.Infrastructure.Interfaces
{
    public interface IRepositoryBase<T> where T : class
    {
        Task Create(T entity);
        Task CreateRange(IEnumerable<T> entities);
        Task<IList<T>> GetAll();
        Task<T> Find(int id);
        Task<IEnumerable<T>> GetByCondition(Expression<Func<T, bool>> expression);
        void Update(T entity);
        void Delete(T entity);
    }
}
