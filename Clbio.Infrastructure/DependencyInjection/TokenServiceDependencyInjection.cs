using Clbio.Abstractions.Interfaces.Auth;
using Clbio.Application.Services.Auth;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Clbio.Infrastructure.DependencyInjection
{
    public static class TokenServiceDependencyInjection
    {
        public static IServiceCollection AddTokenService(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddScoped<ITokenService, TokenService>();
            return services;
        }
    }
}
