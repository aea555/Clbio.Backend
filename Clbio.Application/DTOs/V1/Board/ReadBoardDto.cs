using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Board
{
    public class ReadBoardDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public Guid WorkspaceId { get; set; }

        // optional summary
        public int ColumnCount { get; set; }
    }
}
