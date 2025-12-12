using Clbio.Abstractions.Interfaces.Services;
using Clbio.API.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/presence")]
    [Authorize]
    public class PresenceController(IPresenceService service) : ControllerBase
    {
        private readonly IPresenceService _service = service;

        // 1. HEARTBEAT
        // Frontend: setInterval(() => post('/api/presence/heartbeat'), 30000);
        [HttpPost("heartbeat")]
        public async Task<IActionResult> Heartbeat()
        {
            var userId = User.GetUserId();
            await _service.HeartbeatAsync(userId);
            return Ok();
        }

        // 2. CHECK STATUS
        // Body: [ "guid1", "guid2", ... ]
        [HttpPost("check")]
        public async Task<IActionResult> CheckOnlineUsers([FromBody] List<Guid> userIds)
        {
            var onlineUsers = await _service.GetOnlineUsersAsync(userIds);
            return Ok(ApiResponse.Ok(onlineUsers));
        }
    }
}