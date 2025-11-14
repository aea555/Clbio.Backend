using Clbio.Domain.Entities.V1.Base;

namespace Clbio.Domain.Entities.V1.Auth
{
    public class LoginAttempt : EntityBase
    {
        public string Email { get; set; } = null!;
        public bool Succeeded { get; set; }
        public string? IpAddress { get; set; }
    }

}
