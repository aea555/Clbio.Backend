using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface ITaskAppService
    {
        Task<Result<List<ReadTaskItemDto>>> GetByBoardAsync(Guid workspaceId, Guid boardId, CancellationToken ct = default);
        Task<Result<ReadTaskItemDto>> GetByIdAsync(Guid taskId, CancellationToken ct = default);
        Task<Result<ReadTaskItemDto>> CreateAsync(Guid actorId, Guid workspaceId, CreateTaskItemDto dto, CancellationToken ct = default);
        Task<Result> MoveTaskAsync(Guid actorId, Guid workspaceId, Guid taskId, Guid targetColumnId, int newPosition, CancellationToken ct = default);
        Task<Result> UpdateAsync(Guid workspaceId, UpdateTaskItemDto dto, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);
        Task<Result> AssignUserAsync(Guid workspaceId, Guid taskId, Guid? assigneeId, Guid actorId, CancellationToken ct = default);
    }
}
