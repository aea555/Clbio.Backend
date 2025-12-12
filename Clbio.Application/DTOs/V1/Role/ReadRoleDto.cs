namespace Clbio.Application.DTOs.V1.Role
{
    public class ReadRoleDto
    {
        public Guid Id { get; set; }
        public string DisplayName { get; set; } = null!;
        public string? Description { get; set; }
        public int? WorkspaceRoleValue { get; set; }
    }
}