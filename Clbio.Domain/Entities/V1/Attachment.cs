using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1
{
    public class Attachment : EntityBase
    {
        public string FileName { get; set; } = null!;
        public string Url { get; set; } = null!;
        public long SizeBytes { get; set; }
        public Guid TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
    }

}
