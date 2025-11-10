using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.Column
{
    public class ReadColumnDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = null!;
        public int Position { get; set; }
        public Guid BoardId { get; set; }

        //optional
        public int TaskCount { get; set; }
    }
}
