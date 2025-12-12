using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.ActivityLog
{
    public class ReadActivityLogDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public Guid WorkspaceId { get; set; }
        public Guid ActorId { get; set; }
        public string ActorDisplayName { get; set; } = null!;
        public string ActionType { get; set; } = null!;
        public string EntityType { get; set; } = null!;
        public Guid EntityId { get; set; }
        public string Metadata { get; set; } = null!;
        public string? IpAddress { get; set; }
        public string? UserAgent { get; set; }
    }
}
