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
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
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
