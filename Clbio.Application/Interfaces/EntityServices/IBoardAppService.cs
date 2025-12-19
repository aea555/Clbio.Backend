using Clbio.Application.DTOs.V1.Board;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IBoardAppService
    {
        Task<Result<List<ReadBoardDto>>> GetAllAsync(Guid workspaceId, CancellationToken ct = default);
        Task<Result<ReadBoardDto?>> GetByIdAsync(Guid workspaceId, Guid boardId, CancellationToken ct = default);
        Task<Result<ReadBoardDto>> CreateAsync(Guid actorId, CreateBoardDto dto, CancellationToken ct = default);
        Task<Result> UpdateAsync(Guid workspaceId, Guid boardId, UpdateBoardDto dto, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid actorId, Guid workspaceId, Guid boardId, CancellationToken ct = default);
        Task<Result> ReorderAsync(Guid workspaceId, List<Guid> boardOrder, CancellationToken ct = default);
        Task<Result<List<ReadBoardDto>>> SearchAsync(Guid workspaceId, string? searchTerm, int maxResults = 10, CancellationToken ct = default);
    }
}
