using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}")]
    [Authorize] 
    public class AttachmentController(IAttachmentAppService service) : ControllerBase
    {
        private readonly IAttachmentAppService _service = service;

        // ---------------------------------------------------------------------
        // GET ALL ATTACHMENTS FOR A TASK
        // ---------------------------------------------------------------------
        [HttpGet("tasks/{taskId:guid}/attachments")]
        [RequirePermission(Permission.ViewAttachment, "workspaceId")]
        public async Task<IActionResult> GetAll(
            [FromRoute] Guid workspaceId,
            [FromRoute] Guid taskId,
            CancellationToken ct)
        {
            var result = await _service.GetAllAsync(workspaceId, taskId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // UPLOAD (CREATE) ATTACHMENT
        // ---------------------------------------------------------------------
        [HttpPost("tasks/{taskId:guid}/attachments")]
        [RequirePermission(Permission.CreateAttachment, "workspaceId")]
        public async Task<IActionResult> Create(
            [FromRoute] Guid workspaceId,
            [FromRoute] Guid taskId,
            [FromForm] CreateAttachmentDto dto, 
            CancellationToken ct)
        {
            if (dto.Files.Count > 5)
            {
                return BadRequest(ApiResponse.Fail("You can upload a maximum of 5 files at once."));
            }

            var userId = User.GetUserId();

            var result = await _service.CreateRangeAsync(workspaceId, taskId, dto, userId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error));

            return StatusCode(201, ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // DELETE ATTACHMENT
        // ---------------------------------------------------------------------
        [HttpDelete("attachments/{attachmentId:guid}")]
        [RequirePermission(Permission.CreateAttachment, "workspaceId")] 
        public async Task<IActionResult> Delete(
            [FromRoute] Guid workspaceId,
            [FromRoute] Guid attachmentId,
            CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, attachmentId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error));

            return Ok(ApiResponse.Ok("Attachment deleted successfully."));
        }
    }
}