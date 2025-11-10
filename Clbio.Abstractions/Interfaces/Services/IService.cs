using Clbio.Domain.Entities.V1.Base;
using Clbio.Shared.Results;

namespace Clbio.Abstractions.Interfaces.Services
{
    public interface IService<T> where T : EntityBase
    {
        Task<Result<IEnumerable<T>>> GetAllAsync(CancellationToken ct = default);
        Task<Result<(IEnumerable<T> Items, int TotalCount)>> GetPagedAsync(int page, int pageSize, CancellationToken ct = default);
        Task<Result<T?>> GetByIdAsync(Guid id, CancellationToken ct = default);
        Task<Result<T>> CreateAsync(T entity, CancellationToken ct = default);
        Task<Result> UpdateAsync(T entity, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
    }
}
