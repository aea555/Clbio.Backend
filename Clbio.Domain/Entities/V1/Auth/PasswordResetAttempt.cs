using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1.Auth
{
    public class PasswordResetAttempt : EntityBase
    {
        public string Email { get; set; } = null!;
        public DateTime AttemptedAtUtc { get; set; } = DateTime.UtcNow;
        public bool Succeeded { get; set; }
        public string? IpAddress { get; set; }
    }

}
