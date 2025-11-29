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
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await RolePermissionSeeder.SeedAsync(db);
                return Result.Ok();
            }
            catch (Exception ex)
            {
                return Result.Fail(ex.Message);
            }
        }
    }
}
