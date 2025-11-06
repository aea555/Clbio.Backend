using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Attachment : EntityBase
    {
        [Required, MaxLength(100)]
        public string FileName { get; set; } = null!;
        public string Url { get; set; } = null!;
        public long SizeBytes { get; set; }

        public Guid TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
    }

}
