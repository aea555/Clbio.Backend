using Clbio.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Clbio.API.Extensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = true)]
    public sealed class RequireWorkspacePermissionAttribute(Permission permission) : RequirePermissionAttribute(permission)
    {
        private static readonly string[] DefaultParamNames =
            { "workspaceId", "wsId", "id" };


        public override async Task OnAuthorizationAsync(AuthorizationFilterContext context)
        {
            // Try to auto-detect workspaceId from route
            foreach (var param in DefaultParamNames)
            {
                if (context.RouteData.Values.TryGetValue(param, out var raw) &&
                    Guid.TryParse(raw?.ToString(), out Guid wsId))
                {
                    SetWorkspaceId(wsId);
                    await base.OnAuthorizationAsync(context);
                    return;
                }
            }

            // Try query string fallback
            foreach (var param in DefaultParamNames)
            {
                if (context.HttpContext.Request.Query.TryGetValue(param, out var raw) &&
                    Guid.TryParse(raw.ToString(), out Guid wsId))
                {
                    SetWorkspaceId(wsId);
                    await base.OnAuthorizationAsync(context);
                    return;
                }
            }

            context.Result = new ObjectResult("WorkspaceId not provided")
            {
                StatusCode = StatusCodes.Status400BadRequest
            };
        }
    }
}
