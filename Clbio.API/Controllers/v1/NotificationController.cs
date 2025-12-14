using Clbio.API.Extensions;
using Clbio.API.Extensions.Attributes;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace Clbio.API.Controllers.v1
{
    [ApiController]
    [Route("api/notifications")]
    public class NotificationController(INotificationAppService service) : ControllerBase
    {
        private readonly INotificationAppService _service = service;

        [HttpGet("unread-count")]
        [RequirePermission(Permission.ViewNotification)]
        public async Task<IActionResult> GetUnreadCount(CancellationToken ct)
        {
            var userId = User.GetUserId();

            var result = await _service.GetUnreadCountAsync(userId, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            return Ok(ApiResponse.Ok(new { Count = result.Value }));
        }

        [HttpGet]
        [RequirePermission(Permission.ViewNotification)]
        public async Task<IActionResult> GetMy(
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] bool unreadOnly = false,
            CancellationToken ct = default)
        {
            // simple validation
            if (page < 1) page = 1;
            if (pageSize < 1) pageSize = 20;
            if (pageSize > 100) pageSize = 100; // prevent overfetching

            var userId = User.GetUserId();

            var result = await _service.GetMyNotificationsPagedAsync(userId, page, pageSize, unreadOnly, ct);

            if (!result.Success)
                return BadRequest(ApiResponse.Fail(result.Error!, result.Code));

            // returning data:
            // {
            //    "success": true,
            //    "data": {
            //        "items": [ ... ],
            //        "total": 150,
            //        "page": 1,
            //        "pageSize": 20,
            //        "unreadOnly": false
            //    }
            // }
            return Ok(ApiResponse.Ok(new
            {
                items = result.Value.Items,
                total = result.Value.TotalCount,
                page,
                pageSize,
                unreadOnly
            }));
        }

        [HttpPatch("{id:guid}/read")]
        [RequirePermission(Permission.MarkNotificationAsRead)]
        public async Task<IActionResult> MarkRead(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.MarkAsReadAsync(userId, id, ct);
            return result.Success ? Ok(ApiResponse.Ok("Marked as read")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpPatch("read-all")]
        [RequirePermission(Permission.MarkNotificationAsRead)]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.MarkAllAsReadAsync(userId, ct);
            return result.Success ? Ok(ApiResponse.Ok("All marked as read")) : BadRequest(ApiResponse.Fail(result.Error));
        }

        [HttpDelete("{id:guid}")]
        [RequirePermission(Permission.ViewNotification)]
        public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
        {
            var userId = User.GetUserId();
            var result = await _service.DeleteAsync(userId, id, ct);
            return result.Success ? Ok(ApiResponse.Ok("Deleted")) : BadRequest(ApiResponse.Fail(result.Error));
        }
    }
}