using Clbio.Application.Services.Auth;
using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clbio.API.Extensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute(
        Permission permission,
        string? workspaceIdRouteParam = null) : Attribute, IAsyncAuthorizationFilter
    {
        public Permission Permission { get; } = permission;
        public string? WorkspaceIdRouteParam { get; } = workspaceIdRouteParam;
        protected Guid? WorkspaceId { get; private set; }

        protected void SetWorkspaceId(Guid? id)
        {
            WorkspaceId = id;
        }

        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            // 1) Get UserId from claims
            var userIdStr = http.User.FindFirst("sub")?.Value
                         ?? http.User.FindFirst("userId")?.Value;

            if (userIdStr == null || !Guid.TryParse(userIdStr, out Guid userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            // 2) Determine workspaceId if needed
            Guid? workspaceId = null;

            if (WorkspaceIdRouteParam != null)
            {
                if (context.RouteData.Values.TryGetValue(WorkspaceIdRouteParam, out var wsRaw) &&
                    Guid.TryParse(wsRaw?.ToString(), out Guid wsIdParsed))
                {
                    workspaceId = wsIdParsed;
                }
            }

            // 3) Resolve permission service
            var permService = http.RequestServices.GetRequiredService<UserPermissionService>();

            var result = await permService.HasPermissionAsync(userId, Permission, workspaceId);

            if (!result.Success)
            {
                // Some internal error occurred in HasPermission
                context.Result = new ObjectResult(result.Error) { StatusCode = 500 };
                return;
            }

            if (!result.Value)
            {
                context.Result = new ForbidResult();
                return;
            }
        }
    }

}
