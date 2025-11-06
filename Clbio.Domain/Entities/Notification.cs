using Clbio.Domain.Entities.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities
{
    public class Notification : EntityBase
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;
        [Required, MaxLength(500)]
        public string Message { get; set; } = null!;
        public bool IsRead { get; set; } = false;
    }
}
