using Clbio.Domain.Entities.V1.Base;
using System.Linq.Expressions;

namespace Clbio.Abstractions.Interfaces.Repositories
{
    public interface IRepository<T> where T : EntityBase
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        public IQueryable<T> Query();
        Task<T?> GetByIdAsync(Guid id, bool tracked = false, CancellationToken ct = default);
        Task<IEnumerable<T>> FindAsync(Expression<Func<T, bool>> predicate, bool tracked = false, CancellationToken ct = default);
        Task<T> AddAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
