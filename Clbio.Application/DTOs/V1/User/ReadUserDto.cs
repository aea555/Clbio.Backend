using Clbio.Application.DTOs.V1.Base;

namespace Clbio.Application.DTOs.V1.User
{
    public class ReadUserDto : ResponseDtoBase
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = default!;
        public string DisplayName { get; set; } = default!;
        public string? AvatarUrl { get; set; }
    }
}
