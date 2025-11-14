using Clbio.Domain.Entities.V1.Base;
using System.ComponentModel.DataAnnotations;

namespace Clbio.Domain.Entities.V1.Auth
{
    public class RefreshToken : EntityBase
    {
        public Guid UserId { get; set; }
        public User User { get; set; } = null!;

        [MaxLength(100)]
        public string TokenHash { get; set; } = null!;

        public DateTime ExpiresUtc { get; set; }
        public DateTime CreatedUtc { get; set; } = DateTime.UtcNow;

        public DateTime? RevokedUtc { get; set; }
        [MaxLength(100)]
        public string? ReplacedByTokenHash { get; set; }

        [MaxLength(512)]
        public string? UserAgent { get; set; }

        [MaxLength(45)]
        public string? IpAddress { get; set; }

        public bool IsActive => RevokedUtc is null && DateTime.UtcNow < ExpiresUtc;
    }

}
