using Clbio.Domain.Entities.V1.Base;
using Clbio.Domain.Enums;

namespace Clbio.Domain.Entities.V1
{
    public class PermissionEntity : EntityBase
    {
        public Permission Type { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
    }
}
