using Clbio.Application.DTOs.V1.Comment;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface ICommentAppService
    {
        // List comments of task
        Task<Result<List<ReadCommentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);
        // Add comment
        Task<Result<ReadCommentDto>> CreateAsync(Guid workspaceId, CreateCommentDto dto, CancellationToken ct = default);
        // Delete comment
        Task<Result> DeleteAsync(Guid workspaceId, Guid commentId, Guid userId, CancellationToken ct = default);
    }
}