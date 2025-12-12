using Clbio.Application.DTOs.V1.Role;
using Clbio.Shared.Results;

namespace Clbio.Application.Interfaces.EntityServices
{
    public interface IRoleAppService
    {
        Task<Result<List<ReadRoleDto>>> GetWorkspaceRolesAsync(CancellationToken ct = default);
    }
}