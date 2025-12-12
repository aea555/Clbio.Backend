using Clbio.API.Extensions;
using Clbio.Application.DTOs.V1.User;
using Clbio.Application.Interfaces.EntityServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/users")]
    public class UserController(IUserAppService service) : ControllerBase
    {
        private readonly IUserAppService _service = service;

        // GET api/users/me
        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetMe(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.GetAsync(userId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            if (result.Value == null)
                return NotFound(ApiResponse.Fail("User not found."));

            return Ok(ApiResponse.Ok(result.Value));
        }

        // PUT api/users/me
        [HttpPut("me")]
        [Authorize]
        public async Task<IActionResult> UpdateMe([FromBody] UpdateUserDto dto, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.UpdateAsync(userId, dto, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok("Profile updated."));
        }
    }
}