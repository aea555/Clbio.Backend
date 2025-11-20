using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Domain.Extensions;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class UserPermissionService(IUnitOfWork uow, ILogger<UserPermissionService>? logger = null) : IUserPermissionService
    {
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly IRepository<WorkspaceMember> _workspaceMemberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<RolePermissionEntity> _rolePermissionRepo = uow.Repository<RolePermissionEntity>();
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();

        private readonly ILogger<UserPermissionService>? _logger = logger;

        public async Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            Permission permission,
            Guid? workspaceId = null,
            CancellationToken ct = default)
        {
            try
            {
                var user = await _userRepo.GetByIdAsync(userId, false, ct);
                if (user is null)
                    return Result<bool>.Fail("User not found.");

                var scope = PermissionMetadata.Scopes[permission];

                // Global admin gets everything
                if (user.GlobalRole == GlobalRole.Admin)
                    return Result<bool>.Ok(true);

                // Global-scoped permission for non-admin is denied
                if (scope == PermissionScope.Global)
                    return Result<bool>.Ok(false);

                // User-scoped permissions allowed for all authenticated users
                if (scope == PermissionScope.User)
                    return Result<bool>.Ok(true);

                // Workspace-scoped permissions require a workspaceId
                if (workspaceId == null)
                    return Result<bool>.Ok(false);

                // Validate workspace exists
                var workspace = await _workspaceRepo.GetByIdAsync(workspaceId.Value, false, ct);
                if (workspace is null)
                    return Result<bool>.Fail("Workspace not found.");

                // Check membership 
                var membership = await _workspaceMemberRepo.Query()
                    .Where(wm => wm.UserId == userId && wm.WorkspaceId == workspaceId)
                    .FirstOrDefaultAsync(ct);

                if (membership is null)
                    return Result<bool>.Ok(false); // user not in workspace

                var userWorkspaceRole = membership.Role;

                // Load permissions assigned to that workspace role
                bool has = await _rolePermissionRepo.Query()
                    .Where(rp =>
                        rp.Permission.Type == permission &&
                        rp.Role.WorkspaceRole == userWorkspaceRole)
                    .AnyAsync(ct);

                return Result<bool>.Ok(has);
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "HasPermissionAsync failed with exception");
                return Result<bool>.Fail("HasPermission failed.");
            }
        }
    }
}
