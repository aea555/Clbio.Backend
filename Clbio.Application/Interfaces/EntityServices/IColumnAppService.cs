using Clbio.Application.DTOs.V1.Column;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IColumnAppService
    {
        Task<Result<List<ReadColumnDto>>> GetAllAsync(Guid boardId, CancellationToken ct = default);
        Task<Result<ReadColumnDto>> CreateAsync(Guid workspaceId, CreateColumnDto dto, CancellationToken ct = default);
        Task<Result> UpdateAsync(Guid id, UpdateColumnDto dto, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid id, CancellationToken ct = default);
        Task<Result> ReorderAsync(Guid boardId, List<Guid> columnOrder, CancellationToken ct = default);
    }
}
