using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Interfaces;
using Clbio.Application.Services;
using Clbio.Application.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Application.DependencyInjection
{
    public static class ServiceInjection
    {
        public static IServiceCollection AddServices(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IWorkspaceService, WorkspaceService>();
            services.AddScoped<IBoardService, BoardService>();
            services.AddScoped<IColumnService, ColumnService>();
            services.AddScoped<ITaskService, TaskService>();
            services.AddScoped<IUserService, UserService>();
            services.AddScoped<IWorkspaceMemberService, WorkspaceMemberService>();
            services.AddScoped<ICommentService, CommentService>();
            services.AddScoped<INotificationService, NotificationService>();
            services.AddScoped<IAttachmentService, AttachmentService>();
            services.AddScoped<IActivityLogService, ActivityLogService>();
            services.AddScoped<IAuthService, AuthService>();

            return services;
        }
    }
}
