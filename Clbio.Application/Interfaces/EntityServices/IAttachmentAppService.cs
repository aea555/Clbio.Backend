using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IAttachmentAppService
    {
        Task<Result<List<ReadAttachmentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);
        Task<Result<List<ReadAttachmentDto>>> CreateRangeAsync(
        Guid workspaceId, 
        Guid taskId,
        CreateAttachmentDto dto, 
        Guid userId, 
        CancellationToken ct = default);
        Task<Result<ReadTaskItemDto>> DeleteAsync(Guid workspaceId, Guid attachmentId, CancellationToken ct = default);
    }
}