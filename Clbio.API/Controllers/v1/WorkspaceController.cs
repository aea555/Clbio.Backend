using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Workspace;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces")]
    public class WorkspaceController(IWorkspaceAppService service) : ControllerBase
    {
        private readonly IWorkspaceAppService _service = service;

        // ───────────────────────────────────────────────────────────────
        // GET /api/workspaces/{workspaceId}
        // Requires: ViewWorkspace
        // ───────────────────────────────────────────────────────────────
        [HttpGet("{workspaceId:guid}")]
        [RequirePermission(Permission.ViewWorkspace, "workspaceId")]
        public async Task<IActionResult> GetById(Guid workspaceId, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(workspaceId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            if (result.Value == null)
                return NotFound(ApiResponse.Fail("Workspace not found."));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ───────────────────────────────────────────────────────────────
        // GET /api/workspaces (list my workspaces)
        // Requires: authenticated only
        // ───────────────────────────────────────────────────────────────
        [HttpGet]
        [Authorize]
        public async Task<IActionResult> GetMine(CancellationToken ct)
        {
            var userId = User.GetUserId();

            var result = await _service.GetAllForUserAsync(userId, ct);
            return Ok(ApiResponse.Ok(result.Value));
        }

        // ───────────────────────────────────────────────────────────────
        // POST /api/workspaces
        // Requires: authenticated only
        // ───────────────────────────────────────────────────────────────
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateWorkspaceDto dto, CancellationToken ct)
        {
            // OwnerId should ALWAYS be the authenticated user — not from body
            var ownerId = User.GetUserId();

            var result = await _service.CreateAsync(ownerId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            return CreatedAtAction(nameof(GetById), new { workspaceId = result.Value!.Id }, ApiResponse.Ok(result.Value));
        }

        // ───────────────────────────────────────────────────────────────
        // PUT /api/workspaces/{workspaceId}
        // Requires: ManageWorkspace
        // ───────────────────────────────────────────────────────────────
        [HttpPut("{workspaceId:guid}")]
        [RequirePermission(Permission.ManageWorkspace, "workspaceId")]
        public async Task<IActionResult> Update(Guid workspaceId, [FromBody] UpdateWorkspaceDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateAsync(workspaceId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            return Ok(ApiResponse.Ok("Workspace updated."));
        }

        // ───────────────────────────────────────────────────────────────
        // POST /api/workspaces/{workspaceId}/archive
        // Requires: ArchiveWorkspace
        // ───────────────────────────────────────────────────────────────
        [HttpPost("{workspaceId:guid}/archive")]
        [RequirePermission(Permission.ArchiveWorkspace, "workspaceId")]
        public async Task<IActionResult> Archive(Guid workspaceId, CancellationToken ct)
        {
            var result = await _service.ArchiveAsync(workspaceId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            return Ok(ApiResponse.Ok("Workspace archived."));
        }

        // ───────────────────────────────────────────────────────────────
        // DELETE /api/workspaces/{workspaceId}
        // Requires: DeleteWorkspace
        // ───────────────────────────────────────────────────────────────
        [HttpDelete("{workspaceId:guid}")]
        [RequirePermission(Permission.DeleteWorkspace, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error, result.Code));

            return Ok(ApiResponse.Ok("Workspace deleted."));
        }
    }
}
