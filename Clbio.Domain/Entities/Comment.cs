using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Comment : EntityBase
    {
        [Required, MaxLength(500)]
        public string Body { get; set; } = null!;
        public Guid TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;
        public Guid AuthorId { get; set; }
        public User Author { get; set; } = null!;
    }
}
