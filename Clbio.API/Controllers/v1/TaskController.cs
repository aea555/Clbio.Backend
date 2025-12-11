using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.TaskItem;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/tasks")]
    public class TaskController(ITaskAppService service) : ControllerBase
    {
        private readonly ITaskAppService _service = service;

        [HttpGet]
        [RequirePermission(Permission.ViewTask, "workspaceId")]
        public async Task<IActionResult> GetAll(Guid workspaceId, [FromQuery] Guid boardId, CancellationToken ct)
        {
            if (boardId == Guid.Empty)
                return BadRequest(ApiResponse.Fail("boardId query parameter is required."));

            var result = await _service.GetByBoardAsync(workspaceId, boardId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        [HttpPost]
        [RequirePermission(Permission.CreateTask, "workspaceId")]
        public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateTaskItemDto dto, CancellationToken ct)
        {
            if (dto.ColumnId == Guid.Empty) return BadRequest(ApiResponse.Fail("ColumnId is required."));

            var result = await _service.CreateAsync(workspaceId, dto, ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPut("{taskId:guid}")]
        [RequirePermission(Permission.UpdateTask, "workspaceId")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid taskId, [FromBody] UpdateTaskItemDto dto, CancellationToken ct)
        {
            if (dto.Id != taskId) return BadRequest(ApiResponse.Fail("ID mismatch."));

            var result = await _service.UpdateAsync(workspaceId, dto, ct);
            return result.Success ? Ok(ApiResponse.Ok("Updated")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPut("{taskId:guid}/move")]
        [RequirePermission(Permission.MoveTask, "workspaceId")]
        public async Task<IActionResult> Move(Guid workspaceId, Guid taskId, [FromBody] MoveTaskDto dto, CancellationToken ct)
        {
            var result = await _service.MoveTaskAsync(workspaceId, taskId, dto.TargetColumnId, dto.NewPosition, ct);
            return result.Success ? Ok(ApiResponse.Ok("Moved")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpDelete("{taskId:guid}")]
        [RequirePermission(Permission.DeleteTask, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid taskId, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, taskId, ct);
            return result.Success ? Ok(ApiResponse.Ok("Deleted")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPut("{taskId:guid}/assign")]
        [RequirePermission(Permission.AssignTask, "workspaceId")]
        public async Task<IActionResult> Assign(Guid workspaceId, Guid taskId, [FromBody] Guid? assigneeId, CancellationToken ct)
        {
            var actorId = User.GetUserId();
            var result = await _service.AssignUserAsync(workspaceId, taskId, assigneeId, actorId, ct);
            return result.Success ? NoContent() : BadRequest(ApiResponse.Fail(result.Error));
        }
    }
}