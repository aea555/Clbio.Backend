using Clbio.Domain.Enums;

namespace Clbio.API.Extensions.Attributes
{
    [AttributeUsage(AttributeTargets.Method | AttributeTargets.Class)]
    public sealed class RequireGlobalPermissionAttribute(Permission permission) : RequirePermissionAttribute(permission, workspaceIdRouteParam: null)
    {
    }
}
