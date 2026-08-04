using System.Linq.Expressions;

namespace Nexus.Application.Interfaces.Repository
{
    public interface IRepositoryBase<T> where T : class
    {
        Task Create(T entity, CancellationToken ct);
        Task CreateRange(IEnumerable<T> entities, CancellationToken ct);
        Task<IList<T>> GetAll(CancellationToken ct);
        /// <summary>
        /// Looks up an entity by its primary key, checking the change tracker before the database.
        /// </summary>
        /// <remarks>
        /// Generic over the key so the single-key entities in this repo all share one implementation —
        /// <c>int</c>, <c>Guid</c> (<c>FileUpload</c>) and <c>string</c> (<c>Fingerprint</c>,
        /// <c>IngestCursor</c>). Not for composite keys.
        /// </remarks>
        Task<T?> Find<TKey>(TKey key, CancellationToken ct) where TKey : notnull;
        Task<IEnumerable<T>> GetByCondition(Expression<Func<T, bool>> expression, CancellationToken ct);
        Task<bool> IsAny(Expression<Func<T, bool>> expression, CancellationToken ct);
        void Update(T entity);
        void Delete(T entity);
    }
}
