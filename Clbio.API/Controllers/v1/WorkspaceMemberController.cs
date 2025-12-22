using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.WorkspaceMember;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/members")]
    [Authorize]
    public class WorkspaceMemberController(IWorkspaceMemberAppService service) : ControllerBase
    {
        private readonly IWorkspaceMemberAppService _service = service;

        [HttpGet]
        [RequirePermission(Permission.ViewMember, "workspaceId")]
        public async Task<IActionResult> GetAll(Guid workspaceId, CancellationToken ct)
        {
            var result = await _service.GetByWorkspaceAsync(workspaceId, ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpGet("me")]
        [RequirePermission(Permission.ViewMember, "workspaceId")]
        public async Task<IActionResult> GetMyMembership(Guid workspaceId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.GetByUserIdAsync(workspaceId, userId, ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPost]
        [RequirePermission(Permission.AddMember, "workspaceId")]
        public async Task<IActionResult> Add(Guid workspaceId, [FromBody] CreateWorkspaceMemberDto dto, CancellationToken ct)
        {
            throw new NotSupportedException();
        }

        [HttpPut("{userId:guid}")]
        [RequirePermission(Permission.UpdateRole, "workspaceId")]
        public async Task<IActionResult> UpdateRole(Guid workspaceId, Guid userId, [FromBody] UpdateWorkspaceMemberDto dto, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            var result = await _service.UpdateRoleAsync(workspaceId, userId, dto.Role, actorId, ct);

            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        // Kick
        [HttpDelete("{userId:guid}")]
        [RequirePermission(Permission.RemoveMember, "workspaceId")]
        public async Task<IActionResult> Remove(Guid workspaceId, Guid userId, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            var result = await _service.RemoveMemberAsync(workspaceId, userId, actorId, ct);

            return result.Success ? Ok(ApiResponse.Ok("Member removed.")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        // Leave
        [HttpDelete("me")]
        [RequirePermission(Permission.ViewWorkspace, "workspaceId")]
        public async Task<IActionResult> Leave(Guid workspaceId, CancellationToken ct)
        {
            var myId = User.GetUserId();
            var result = await _service.LeaveWorkspaceAsync(workspaceId, myId, ct);

            return result.Success ? Ok(ApiResponse.Ok("You left the workspace.")) : BadRequest(ApiResponse.Fail(result.Error));
        }
    }
}