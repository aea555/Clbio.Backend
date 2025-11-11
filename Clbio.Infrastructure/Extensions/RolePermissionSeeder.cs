using Clbio.Domain.Entities.V1;
using Clbio.Domain.Enums;
using Clbio.Domain.Extensions;
using Clbio.Infrastructure.Data;
using Clbio.Shared.Extensions.YourProjectNamespace.Common.Extensions;
using Microsoft.EntityFrameworkCore;
using System.Collections.ObjectModel;

namespace Clbio.Infrastructure.Extensions
{
    public static class RolePermissionSeeder
    {
        public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
        {
            // --- Seed permissions ---
            foreach (var perm in Enum.GetValues<Permission>())
            {
                if (!await db.Permissions.AnyAsync(p => p.Type == perm, ct))
                {
                    db.Permissions.Add(new PermissionEntity
                    {
                        Type = perm,
                        DisplayName = perm.GetDescription(),
                        Description = $"Auto-seeded from Permission enum ({perm})."
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            // --- seed roles (global & workspace) ---
            foreach (var globalRole in Enum.GetValues<GlobalRole>())
            {
                if (!await db.Roles.AnyAsync(r => r.GlobalRole == globalRole, ct))
                {
                    db.Roles.Add(new RoleEntity
                    {
                        GlobalRole = globalRole,
                        DisplayName = globalRole.ToString(),
                        Description = $"Auto-seeded global role ({globalRole})."
                    });
                }
            }

            foreach (var wsRole in Enum.GetValues<WorkspaceRole>())
            {
                if (!await db.Roles.AnyAsync(r => r.WorkspaceRole == wsRole, ct))
                {
                    db.Roles.Add(new RoleEntity
                    {
                        WorkspaceRole = wsRole,
                        DisplayName = wsRole.ToString(),
                        Description = $"Auto-seeded workspace role ({wsRole})."
                    });
                }
            }

            await db.SaveChangesAsync(ct);

            // --- map RolePermissions ---
            var allRoles = await db.Roles.ToListAsync(ct);
            var allPerms = await db.Permissions.ToListAsync(ct);

            foreach (var role in allRoles)
            {
                ReadOnlyCollection<Permission> mappedPermissions = role switch
                {
                    { GlobalRole: GlobalRole.Admin } => RolePermissionMap.GetGlobalPermissions(GlobalRole.Admin),
                    { WorkspaceRole: WorkspaceRole.Owner } => RolePermissionMap.GetWorkspacePermissions(WorkspaceRole.Owner),
                    { WorkspaceRole: WorkspaceRole.PrivilegedMember } => RolePermissionMap.GetWorkspacePermissions(WorkspaceRole.PrivilegedMember),
                    { WorkspaceRole: WorkspaceRole.Member } => RolePermissionMap.GetWorkspacePermissions(WorkspaceRole.Member),
                    _ => Array.AsReadOnly(Array.Empty<Permission>())
                };

                foreach (var perm in mappedPermissions)
                {
                    var permissionEntity = allPerms.First(p => p.Type == perm);

                    bool exists = await db.RolePermissions.AnyAsync(
                        rp => rp.RoleId == role.Id && rp.PermissionId == permissionEntity.Id, ct);

                    if (!exists)
                    {
                        db.RolePermissions.Add(new RolePermissionEntity
                        {
                            RoleId = role.Id,
                            PermissionId = permissionEntity.Id
                        });
                    }
                }
            }

            await db.SaveChangesAsync(ct);
        }
    }

}
