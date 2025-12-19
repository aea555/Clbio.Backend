using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Base;
using Clbio.Application.DTOs.V1.WorkspaceInvitation;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api")]
    [Authorize] 
    public class WorkspaceInvitationController(IWorkspaceInvitationAppService service) : ControllerBase
    {
        private readonly IWorkspaceInvitationAppService _service = service;

        // ---------------------------------------------------------------------
        // 1. SEND INVITATION
        // ---------------------------------------------------------------------
        [HttpPost("workspaces/{workspaceId:guid}/invitations")]
        [RequirePermission(Permission.AddMember, "workspaceId")] 
        public async Task<IActionResult> SendInvitation(
            [FromRoute] Guid workspaceId,
            [FromBody] CreateWorkspaceInvitationDto dto,
            CancellationToken ct)
        {
            var actorId = User.GetUserId();

            var result = await _service.SendInvitationAsync(workspaceId, dto, actorId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            return StatusCode(201, ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // 2. GET MY INVITATIONS (PAGED)
        // ---------------------------------------------------------------------
        [HttpGet("invitations/my")]
        public async Task<IActionResult> GetMyInvitations(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            CancellationToken ct = default)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 50) pageSize = 50;

            var userId = User.GetUserId();

            var result = await _service.GetMyInvitationsPagedAsync(userId, page, pageSize, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            var pagedData = result.Value; 

            var resp = new ReadWorkspaceInvitationPagedReturnDto
            {
                Items = pagedData.Items,
                Meta = new PagedMetaDto
                {
                    TotalCount = pagedData.TotalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(pagedData.TotalCount / (double)pageSize)
                }
            };

            return Ok(ApiResponse.Ok(resp));
        }

        // ---------------------------------------------------------------------
        // 3. RESPOND (ACCEPT / DECLINE)
        // ---------------------------------------------------------------------
        [HttpPost("invitations/{id:guid}/respond")]
        public async Task<IActionResult> Respond(
            [FromRoute] Guid id,
            [FromQuery] bool accept, // 
            CancellationToken ct)
        {
            var userId = User.GetUserId();

            var result = await _service.RespondAsync(id, userId, accept, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));
                
            var message = accept ? "Invitation accepted." : "Invitation declined.";
            return Ok(ApiResponse.Ok(message));
        }
    }
}