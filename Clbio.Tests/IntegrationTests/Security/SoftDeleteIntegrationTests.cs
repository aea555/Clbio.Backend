using Clbio.Domain.Entities.V1;
using Clbio.Tests.IntegrationTests.Base;
using Microsoft.EntityFrameworkCore;

namespace Clbio.Tests.IntegrationTests.Security
{
    public class SoftDeleteIntegrationTests : IntegrationTestBase
    {
        [Fact]
        public async Task SoftDelete_Works()
        {
            var user = new User { Email = "test@x.com", DisplayName = "Test", PasswordHash = "SomeHash" };
            Context.Users.Add(user);
            await Context.SaveChangesAsync();

            Context.Users.Remove(user);
            await Context.SaveChangesAsync();

            var exists = await Context.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == user.Id);
            Assert.True(exists);
            Assert.True(user.IsDeleted);
        }
    }
}
