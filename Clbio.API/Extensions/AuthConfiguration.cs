using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Clbio.API.Extensions
{
    public static class AuthConfiguration
    {
        public static IServiceCollection AddJwt(this IServiceCollection services)
        {
            var validIssuer = Environment.GetEnvironmentVariable("JWT_ISSUER");
            var validAudience = Environment.GetEnvironmentVariable("JWT_AUDIENCE");
            var secret = Environment.GetEnvironmentVariable("JWT_KEY");

            if (validIssuer is null || validAudience is null || secret is null)
                throw new Exception("Couldn't get JWT environment variables.");

            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {

                options.RequireHttpsMetadata = true;
                options.SaveToken = false;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateIssuerSigningKey = true,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.FromSeconds(30),

                    ValidIssuer = validIssuer,
                    ValidAudience = validAudience,
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(secret))
                };
            });

            services.AddAuthorization();

            return services;
        }
    }
}
