using Clbio.Abstractions.Interfaces.Services;
using Clbio.Domain.Enums;
using Clbio.Domain.Extensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clbio.API.Extensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public class RequirePermissionAttribute(Permission permission, string? workspaceIdRouteParam = null) : Attribute, IAsyncAuthorizationFilter
    {
        public Permission Permission { get; } = permission;
        public string? WorkspaceIdRouteParam { get; } = workspaceIdRouteParam;

        private static readonly string[] DefaultWorkspaceParams =
            { "workspaceId", "wsId", "id" };

        public virtual async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            var http = context.HttpContext;

            //-------------------------------------------------------------
            // 1. Extract user ID from JWT
            //-------------------------------------------------------------
            var userIdStr = http.User.FindFirst("sub")?.Value
                         ?? http.User.FindFirst("userId")?.Value;

            if (userIdStr == null || !Guid.TryParse(userIdStr, out Guid userId))
            {
                context.Result = new UnauthorizedResult();
                return;
            }

            //-------------------------------------------------------------
            // 2. Extract workspaceId (if needed)
            //-------------------------------------------------------------
            Guid? workspaceId = null;

            if (PermissionMetadata.Scopes[Permission] == PermissionScope.Workspace)
            {
                // Route param takes priority if supplied
                if (WorkspaceIdRouteParam != null &&
                    context.RouteData.Values.TryGetValue(WorkspaceIdRouteParam, out var raw1) &&
                    Guid.TryParse(raw1?.ToString(), out Guid parsed1))
                {
                    workspaceId = parsed1;
                }
                else
                {
                    // Try default param names: workspaceId, wsId, id
                    foreach (var param in DefaultWorkspaceParams)
                    {
                        if (context.RouteData.Values.TryGetValue(param, out var raw) &&
                            Guid.TryParse(raw?.ToString(), out Guid parsed))
                        {
                            workspaceId = parsed;
                            break;
                        }

                        // Try query string
                        if (context.HttpContext.Request.Query.TryGetValue(param, out var rawq) &&
                            Guid.TryParse(rawq.ToString(), out Guid parsedq))
                        {
                            workspaceId = parsedq;
                            break;
                        }
                    }
                }

                if (workspaceId == null)
                {
                    context.Result = new BadRequestObjectResult(
                        ApiResponse.Fail("WorkspaceId is required for this action.", "WORKSPACE_ID_MISSING"));
                    return;
                }
            }

            //-------------------------------------------------------------
            // 3. Permission check
            //-------------------------------------------------------------
            var permService = http.RequestServices.GetRequiredService<IUserPermissionService>();

            var result = await permService.HasPermissionAsync(userId, Permission, workspaceId);

            if (!result.Success)
            {
                // Permission service error → 500 but generic error
                context.Result = new StatusCodeResult(500);
                return;
            }

            if (!result.Value)
            {
                // User authenticated but no permission
                context.Result = new ForbidResult();
                return;
            }
        }
    }
}
