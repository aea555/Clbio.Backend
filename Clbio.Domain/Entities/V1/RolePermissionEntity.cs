using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class RolePermissionEntity : EntityBase
    {
        public Guid RoleId { get; set; }
        public RoleEntity Role { get; set; } = null!;

        public Guid PermissionId { get; set; }
        public PermissionEntity Permission { get; set; } = null!;
    }
}
