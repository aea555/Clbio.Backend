using Clbio.Abstractions.Interfaces.Services;
using Clbio.Infrastructure.Email;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class EmailSenderDependencyInjection
    {
        public static IServiceCollection AddEmailSender(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<IEmailSender, MailerSendEmailSender>();
            return services;
        }
    }
}
