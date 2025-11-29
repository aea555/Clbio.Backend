using Clbio.Domain.Entities.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities.V1.Auth
{
    public class EmailVerificationToken : EntityBase
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(100)]
        public string TokenHash { get; set; } = null!;
        public DateTime ExpiresUtc { get; set; }
        public bool Used { get; set; } = false;
    }
}
