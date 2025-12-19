using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.Column;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId:guid}/boards/{boardId:guid}/columns")]
    public class ColumnController(IColumnAppService service) : ControllerBase
    {
        private readonly IColumnAppService _service = service;

        // ---------------------------------------------------------------------
        // GET: /api/workspaces/{wsId}/boards/{boardId}/columns
        // requires: ViewColumn
        // ---------------------------------------------------------------------
        [HttpGet]
        [RequirePermission(Permission.ViewColumn, "workspaceId")]
        public async Task<IActionResult> GetAll(Guid workspaceId, Guid boardId, CancellationToken ct)
        {
            var result = await _service.GetAllAsync(boardId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // POST: /api/workspaces/{wsId}/boards/{boardId}/columns
        // requires: CreateColumn
        // ---------------------------------------------------------------------
        [HttpPost]
        [RequirePermission(Permission.CreateColumn, "workspaceId")]
        public async Task<IActionResult> Create(Guid workspaceId, Guid boardId, [FromBody] CreateColumnDto dto, CancellationToken ct)
        {
            dto.BoardId = boardId;

            var result = await _service.CreateAsync(workspaceId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // ---------------------------------------------------------------------
        // PUT: /api/workspaces/{wsId}/boards/{boardId}/columns/{columnId}
        // requires: UpdateColumn
        // ---------------------------------------------------------------------
        [HttpPut("{columnId:guid}")]
        [RequirePermission(Permission.UpdateColumn, "workspaceId")]
        public async Task<IActionResult> Update(Guid workspaceId, Guid boardId, Guid columnId, [FromBody] UpdateColumnDto dto, CancellationToken ct)
        {
            var result = await _service.UpdateAsync(columnId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Column updated."));
        }

        // ---------------------------------------------------------------------
        // DELETE: /api/workspaces/{wsId}/boards/{boardId}/columns/{columnId}
        // requires: DeleteColumn
        // ---------------------------------------------------------------------
        [HttpDelete("{columnId:guid}")]
        [RequirePermission(Permission.DeleteColumn, "workspaceId")]
        public async Task<IActionResult> Delete(Guid workspaceId, Guid boardId, Guid columnId, CancellationToken ct)
        {
            var result = await _service.DeleteAsync(workspaceId, boardId, columnId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Column deleted."));
        }

        // ---------------------------------------------------------------------
        // POST: /api/workspaces/{wsId}/boards/{boardId}/columns/reorder
        // requires: ReorderColumn
        // ---------------------------------------------------------------------
        [HttpPost("reorder")]
        [RequirePermission(Permission.ReorderColumn, "workspaceId")]
        public async Task<IActionResult> Reorder(Guid workspaceId, Guid boardId, [FromBody] List<Guid> columnOrder, CancellationToken ct)
        {
            var result = await _service.ReorderAsync(boardId, columnOrder, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Columns reordered."));
        }
    }
}