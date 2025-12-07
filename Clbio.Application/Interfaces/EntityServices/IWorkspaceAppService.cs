using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IWorkspaceAppService
    {
        Task<Result<ReadWorkspaceDto?>> GetByIdAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Result<IEnumerable<ReadWorkspaceDto>>> GetAllForUserAsync(Guid userId, CancellationToken ct = default);
        Task<Result<ReadWorkspaceDto>> CreateAsync(Guid ownerId, CreateWorkspaceDto dto, CancellationToken ct = default);
        Task<Result> UpdateAsync(Guid workspaceId, UpdateWorkspaceDto dto, CancellationToken ct = default);
        Task<Result> ArchiveAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid workspaceId, CancellationToken ct = default);
    }
}
