using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Comment
{
    public class ReadCommentDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Body { get; set; } = null!;
        public Guid TaskId { get; set; }
        public Guid AuthorId { get; set; }
        public string AuthorDisplayName { get; set; } = null!;
        public string? AuthorAvatarUrl { get; set; }
    }
}
