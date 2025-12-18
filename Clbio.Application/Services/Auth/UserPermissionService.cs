using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Extensions;
using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Domain.Extensions;
using Clbio.Shared.Results;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Clbio.Application.Services.Auth
{
    public sealed class UserPermissionService(IUnitOfWork uow, ICacheService cache, ICacheVersionService versionService, ILogger<UserPermissionService>? logger = null) : IUserPermissionService
    {
        private readonly IRepository<User> _userRepo = uow.Repository<User>();
        private readonly ICacheService _cache = cache;
        private readonly ICacheVersionService _versionService = versionService;
        private readonly IRepository<WorkspaceMember> _workspaceMemberRepo = uow.Repository<WorkspaceMember>();
        private readonly IRepository<RolePermissionEntity> _rolePermissionRepo = uow.Repository<RolePermissionEntity>();
        private readonly IRepository<Workspace> _workspaceRepo = uow.Repository<Workspace>();

        private readonly ILogger<UserPermissionService>? _logger = logger;

        private static readonly HashSet<Permission> AllowedActionsOnArchivedWorkspace =
        [
            Permission.ArchiveWorkspace, 
            Permission.DeleteWorkspace,  
            Permission.RemoveMember,
            Permission.ViewWorkspace,
            Permission.ViewColumn,
            Permission.ViewBoard,
            Permission.ViewTask,
            Permission.ViewComment,
            Permission.ViewAttachment,
            Permission.ViewMember,
            Permission.ViewAuditLog,
            Permission.ViewRole,
        ];

        public async Task<Result<bool>> HasPermissionAsync(
            Guid userId,
            Permission permission,
            Guid? workspaceId = null,
            CancellationToken ct = default)
        {
            try
            {
                var user = await _cache.GetOrSetAsync(
                    key: CacheKeys.User(userId),
                    factory: async () => await _userRepo.GetByIdAsync(userId, false, ct),
                    expiration: TimeSpan.FromMinutes(10));

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

                var wsVersion = await _versionService.GetWorkspaceVersionAsync(workspaceId.Value);

                // Validate workspace exists
                var workspace = await _cache.GetOrSetAsync(
                    key: CacheKeys.Workspace(workspaceId.Value, wsVersion),
                    factory: async () => await _workspaceRepo.GetByIdAsync(workspaceId.Value, false, ct),
                    expiration: TimeSpan.FromMinutes(10));

                if (workspace is null)
                    return Result<bool>.Fail("Workspace not found.");

                if (workspace.Status == WorkspaceStatus.Archived)
                {
                    // (Read-Only Mode)
                    bool isViewPermission = permission.ToString().StartsWith("View");

                    if (!isViewPermission && !AllowedActionsOnArchivedWorkspace.Contains(permission))
                    {
                        return Result<bool>.Fail("This action cannot be performed on an archived workspace.");
                    }
                }

                var membershipVersion =
                    await _versionService.GetMembershipVersionAsync(userId, workspaceId.Value);

                // Check membership 
                var membership = await _cache.GetOrSetAsync(
                    key: CacheKeys.Membership(userId, workspaceId.Value, membershipVersion),
                    factory: async () =>
                    {
                        return await _workspaceMemberRepo.Query()
                            .Where(wm => wm.UserId == userId && wm.WorkspaceId == workspaceId)
                            .FirstOrDefaultAsync(ct);
                    },
                    expiration: TimeSpan.FromMinutes(10));

                if (membership is null)
                    return Result<bool>.Ok(false); // user not in workspace

                var userWorkspaceRole = membership.Role;

                // role version
                var roleVersion = await _versionService.GetWorkspaceRoleVersionAsync(userWorkspaceRole);

                // Cache permissions for workspace role
                var rolePermissions = await _cache.GetOrSetAsync(
                    key: CacheKeys.RolePermissions(userWorkspaceRole, roleVersion),
                    factory: async () =>
                    {
                        return await _rolePermissionRepo.Query()
                            .Where(rp => rp.Role.WorkspaceRole == userWorkspaceRole)
                            .Select(rp => rp.Permission.Type)
                            .ToListAsync(ct);
                    },
                    expiration: TimeSpan.FromMinutes(15));

                bool has = rolePermissions.Contains(permission);

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
