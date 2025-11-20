using Clbio.Domain.Entities.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Domain.Entities.V1
{
    public class RoleEntity : EntityBase
    {
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public GlobalRole? GlobalRole { get; set; }
        public WorkspaceRole? WorkspaceRole { get; set; }
        public ICollection<RolePermissionEntity> RolePermissions { get; set; } = [];

        //helpers
        public bool IsGlobalRole => GlobalRole == Enums.GlobalRole.Admin;
        public bool IsWorkspaceRole => WorkspaceRole != null;
    }
}
