namespace Clbio.Application.Extensions
{
    public static class CacheChannels
    {
        public const string WorkspaceInvalidated = "cache:workspace:invalidated";
        public const string WorkspaceRoleInvalidated = "cache:role:invalidated";
        public const string UserInvalidated = "cache:user:invalidated";
        public const string MembershipInvalidated = "cache:membership:invalidated";
        public const string GlobalRoleInvalidated = "cache:globalrole:invalidated";
        public const string InvitationInvalidated = "cache:invitations:invalidated";

        // optional
        public const string FullReset = "cache:fullreset";
    }
}
