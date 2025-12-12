using Clbio.API.Extensions;
using Clbio.Application.Interfaces.EntityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/roles")]
    public class RoleController(IRoleAppService service) : ControllerBase
    {
        private readonly IRoleAppService _service = service;

        [HttpGet("workspace-roles")]
        [Authorize]
        public async Task<IActionResult> GetWorkspaceRoles(CancellationToken ct)
        {
            var result = await _service.GetWorkspaceRolesAsync(ct);
            return result.Success ? Ok(ApiResponse.Ok(result.Value)) : BadRequest(ApiResponse.Fail(result.Error));
        }
    }
}