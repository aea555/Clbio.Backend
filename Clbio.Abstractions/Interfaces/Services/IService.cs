using Clbio.Domain.Entities.Base;

namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IService<T> where T : EntityBase
    {
        Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
        Task<(IEnumerable<T> Items, int TotalCount)> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<T> CreateAsync(T entity, CancellationToken ct = default);
        Task UpdateAsync(T entity, CancellationToken ct = default);
        Task DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
