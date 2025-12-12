using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Attachment;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}")]
    public class AttachmentController(IAttachmentAppService service) : ControllerBase
    {
        private readonly IAttachmentAppService _service = service;

        [HttpGet("tasks/{taskId:guid}/attachments")]
        [RequirePermission(Permission.ViewAttachment, "workspaceId")]
        public async Task<IActionResult> GetAll(Guid workspaceId, Guid taskId, CancellationToken ct)
        {
            var result = await _service.GetAllAsync(workspaceId, taskId, ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPost("tasks/{taskId:guid}/attachments")]
        [RequirePermission(Permission.CreateAttachment, "workspaceId")]
        public async Task<IActionResult> Create(Guid workspaceId, Guid taskId, [FromBody] CreateAttachmentDto dto, CancellationToken ct)
        {
            dto.TaskId = taskId;
            dto.UploadedById = User.GetUserId();

            var result = await _service.CreateAsync(workspaceId, dto, ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpDelete("attachments/{attachmentId:guid}")]
        [RequirePermission(Permission.CreateAttachment, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid attachmentId, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, attachmentId, ct);
            return result.Success ? Ok(ApiResponse.Ok("Deleted")) : BadRequest(ApiResponse.Fail(result.Error));
        }
    }
}