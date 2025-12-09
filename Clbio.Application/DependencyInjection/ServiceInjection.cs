using Clbio.Abstractions.Interfaces.Cache;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Interfaces;
using Clbio.Application.Interfaces.EntityServices;
using Clbio.Application.Services;
using Clbio.Application.Services.Auth;
using Clbio.Application.Services.Auth.External;
using Clbio.Application.Services.Cache;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Application.DependencyInjection
{
    public static class ServiceInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IWorkspaceAppService, WorkspaceService>();
            services.AddScoped<IBoardAppService, BoardService>();
            services.AddScoped<IColumnAppService, ColumnService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IUserAppService, UserService>();
            services.AddScoped<IRoleAppService, RoleService>();
            services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
            services.AddScoped<ICommentAppService, CommentService>();
            services.AddScoped<INotificationAppService, NotificationService>();
            services.AddScoped<IAttachmentAppService, AttachmentService>();
            services.AddScoped<IActivityLogAppService, ActivityLogService>();
            services.AddScoped<IAuthService, AuthService>();
            services.AddScoped<IAuthThrottlingService, AuthThrottlingService>();
            services.AddScoped<IEmailVerificationService, EmailVerificationService>();
            services.AddScoped<IPasswordResetService, PasswordResetService>();
            services.AddScoped<ITokenFactoryService, TokenFactoryService>();
            services.AddScoped<IGoogleAuthService, GoogleAuthService>();
            services.AddScoped<IUserPermissionService, UserPermissionService>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<ICacheInvalidationService, CacheInvalidationService>();
            services.AddScoped<ICacheVersionService, RedisCacheVersionService>();
            services.AddHostedService<RedisCacheInvalidationSubscriber>();

            return services;
        }
    }
}
