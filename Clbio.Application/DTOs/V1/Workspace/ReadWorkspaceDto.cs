using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Workspace
{
    public class ReadWorkspaceDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid OwnerId { get; set; }
        public string OwnerDisplayName { get; set; } = null!;

        // Optional summary info
        public int MemberCount { get; set; }
        public int BoardCount { get; set; }
    }
}
