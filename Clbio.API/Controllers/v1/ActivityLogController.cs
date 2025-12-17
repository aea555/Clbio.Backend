using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.DTOs.V1.ActivityLog;
using Clbio.Application.DTOs.V1.Base;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.V1
{
    [ApiController]
    [Route("api/workspaces/{workspaceId}/activity-logs")]
    [Authorize]
    public class ActivityLogController(IActivityLogAppService service) : ControllerBase
    {
        private readonly IActivityLogAppService _service = service;

        /// <summary>
        /// Retrieves paginated activity logs for a specific workspace.
        /// </summary>
        /// <param name="workspaceId">The workspace ID.</param>
        /// <param name="page">Page number (default 1).</param>
        /// <param name="pageSize">Items per page (default 20, max 100).</param>
        /// <returns>Paginated list of activity logs.</returns>
        [HttpGet]
        [RequirePermission(Permission.ViewWorkspace, "workspaceId")] 
        [ProducesResponseType(200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        public async Task<IActionResult> GetWorkspaceActivityLogs(
            Guid workspaceId, 
            [FromQuery] int page = 1, 
            [FromQuery] int pageSize = 20)
        {
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // Hard limit

            var result = await _service.GetPagedAsync(workspaceId, page, pageSize);

            if (!result.Success)
            {
                return BadRequest(ApiResponse.Fail(result.Error));
            }

            var (items, totalCount) = result.Value;

            var resp = new ReadActivityLogPagedReturnDto
            {
                Items = items,
                Meta = new PagedMetaDto
                {
                    TotalCount = totalCount,
                    Page = page,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
                }
            };

            return Ok(ApiResponse.Ok(resp));
        }
    }
}