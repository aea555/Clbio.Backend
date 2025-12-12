using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Comment;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    // Base route
    [Route("api/workspaces/{workspaceId:guid}")]
    public class CommentController(ICommentAppService service) : ControllerBase
    {
        private readonly ICommentAppService _service = service;

        // ---------------------------------------------------------------------
        // GET: /api/workspaces/{wsId}/tasks/{taskId}/comments
        // requires: ViewComment
        // ---------------------------------------------------------------------
        [HttpGet("tasks/{taskId:guid}/comments")]
        [RequirePermission(Permission.ViewComment, "workspaceId")]
        public async Task<IActionResult> GetTaskComments(Guid workspaceId, Guid taskId, CancellationToken ct)
        {
            var result = await _service.GetAllAsync(workspaceId, taskId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // POST: /api/workspaces/{wsId}/tasks/{taskId}/comments
        // requires: CreateComment
        // ---------------------------------------------------------------------
        [HttpPost("tasks/{taskId:guid}/comments")]
        [RequirePermission(Permission.CreateComment, "workspaceId")]
        public async Task<IActionResult> Create(Guid workspaceId, Guid taskId, [FromBody] CreateCommentDto dto, CancellationToken ct)
        {
            // force URL task id
            dto.TaskId = taskId;

            // get user id from token
            dto.AuthorId = User.GetUserId();

            var result = await _service.CreateAsync(workspaceId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // DELETE: /api/workspaces/{wsId}/comments/{commentId}
        // Requires: CreateComment (if user can create they can delete)
        [HttpDelete("comments/{commentId:guid}")]
        [RequirePermission(Permission.CreateComment, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid commentId, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.DeleteAsync(workspaceId, commentId, userId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Comment deleted."));
        }
    }
}