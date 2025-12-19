using Clbio.Application.DTOs.V1.WorkspaceInvitation;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IWorkspaceInvitationAppService
    {
        Task<Result<ReadWorkspaceInvitationDto>> SendInvitationAsync(
            Guid workspaceId, CreateWorkspaceInvitationDto dto, Guid actorId, CancellationToken ct = default);
        
        Task<Result<PagedResult<ReadWorkspaceInvitationDto>>> GetMyInvitationsPagedAsync(
            Guid userId, 
            int page = 1, 
            int pageSize = 10, 
            CancellationToken ct = default);
        
        Task<Result> RespondAsync(Guid invitationId, Guid userId, bool accept, CancellationToken ct = default);
    }
}
