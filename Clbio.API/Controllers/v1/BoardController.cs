using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Board;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/boards")]
    public class BoardController : ControllerBase
    {
        private readonly IBoardAppService _service;

        public BoardController(IBoardAppService service)
        {
            _service = service;
        }

        // -------------------------------------------------------------
        // GET: /api/workspaces/{workspaceId}/boards
        // Requires ViewBoard
        // -------------------------------------------------------------
        [HttpGet]
        [RequirePermission(Permission.ViewBoard, "workspaceId")]
        public async Task<IActionResult> GetBoards(Guid workspaceId, CancellationToken ct)
        {
            var result = await _service.GetAllAsync(workspaceId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // -------------------------------------------------------------
        // GET: /api/workspaces/{workspaceId}/boards/{boardId}
        // Requires ViewBoard
        // -------------------------------------------------------------
        [HttpGet("{boardId:guid}")]
        [RequirePermission(Permission.ViewBoard, "workspaceId")]
        public async Task<IActionResult> Get(Guid workspaceId, Guid boardId, CancellationToken ct)
        {
            var result = await _service.GetByIdAsync(workspaceId, boardId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            if (result.Value == null)
                return NotFound(ApiResponse.Fail("Board not found."));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // -------------------------------------------------------------
        // POST: /api/workspaces/{workspaceId}/boards
        // Requires CreateBoard
        // -------------------------------------------------------------
        [HttpPost]
        [RequirePermission(Permission.CreateBoard, "workspaceId")]
        public async Task<IActionResult> Create(Guid workspaceId, [FromBody] CreateBoardDto dto, CancellationToken ct)
        {
            dto.WorkspaceId = workspaceId;

            var result = await _service.CreateAsync(dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // -------------------------------------------------------------
        // PUT: /api/workspaces/{workspaceId}/boards/{boardId}
        // Requires UpdateBoard
        // -------------------------------------------------------------
        [HttpPut("{boardId:guid}")]
        [RequirePermission(Permission.UpdateBoard, "workspaceId")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid boardId, [FromBody] UpdateBoardDto dto, CancellationToken ct)
        {
            // ID in body is meaningless—ensure route ID wins
            if (dto.Id != boardId)
                return BadRequest(ApiResponse.Fail("Board ID mismatch."));

            var result = await _service.UpdateAsync(workspaceId, boardId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Board updated."));
        }

        // -------------------------------------------------------------
        // DELETE: /api/workspaces/{workspaceId}/boards/{boardId}
        // Requires DeleteBoard
        // -------------------------------------------------------------
        [HttpDelete("{boardId:guid}")]
        [RequirePermission(Permission.DeleteBoard, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid boardId, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, boardId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Board deleted."));
        }

        // -------------------------------------------------------------
        // POST: /api/workspaces/{workspaceId}/boards/reorder
        // Requires ReorderBoard
        // -------------------------------------------------------------
        [HttpPost("reorder")]
        [RequirePermission(Permission.ReorderBoard, "workspaceId")]
        public async Task<IActionResult> Reorder(Guid workspaceId, [FromBody] List<Guid> boardOrder, CancellationToken ct)
        {
            var result = await _service.ReorderAsync(workspaceId, boardOrder, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Boards reordered."));
        }
    }
}
