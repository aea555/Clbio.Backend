using Clbio.Abstractions.Interfaces.Cache; // Cache servisi için
using Clbio.Domain.Enums; 
using Clbio.Infrastructure.Data;
using Clbio.Infrastructure.Extensions;
using Clbio.Shared.Results;

namespace Clbio.API.Extensions
{
    public static class SeederConfiguration
    {
        public static async Task<Result> AddRolePermissionSeederAsync(this IHost app)
        {
            try
            {
                using var scope = app.Services.CreateScope();
                var services = scope.ServiceProvider;

                var db = services.GetRequiredService<AppDbContext>();
                var invalidator = services.GetRequiredService<ICacheInvalidationService>();

                await RolePermissionSeeder.SeedAsync(db);

                foreach (var role in Enum.GetValues<WorkspaceRole>())
                {
                    await invalidator.InvalidateWorkspaceRole(role);
                }

                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
    }
}