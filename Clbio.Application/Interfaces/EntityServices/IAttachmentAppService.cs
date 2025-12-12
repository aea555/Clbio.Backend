using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IAttachmentAppService
    {
        Task<Result<List<ReadAttachmentDto>>> GetAllAsync(Guid workspaceId, Guid taskId, CancellationToken ct = default);
        Task<Result<ReadAttachmentDto>> CreateAsync(Guid workspaceId, CreateAttachmentDto dto, CancellationToken ct = default);
        Task<Result> DeleteAsync(Guid workspaceId, Guid attachmentId, CancellationToken ct = default);
    }
}