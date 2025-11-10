using Clbio.Application.DTOs.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Application.DTOs.V1.ActivityLog
{
    public class CreateActivityLogDto : RequestDtoBase
    {
        [Required]
        public Guid WorkspaceId { get; set; }

        [Required]
        public Guid ActorId { get; set; }

        [Required]
        [StringLength(100)]
        public string ActionType { get; set; } = null!;

        [Required]
        [StringLength(100)]
        public string EntityType { get; set; } = null!;

        [Required]
        public Guid EntityId { get; set; }

        [Required]
        public string Metadata { get; set; } = null!;

        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
