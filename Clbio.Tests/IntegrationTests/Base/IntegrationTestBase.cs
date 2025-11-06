using Clbio.Infrastructure.Data;

namespace Clbio.Tests.IntegrationTests.Base
{
    public abstract class IntegrationTestBase : IDisposable
    {
        protected readonly AppDbContext Context;

        protected IntegrationTestBase()
        {
            Context = TestDbContextFactory.CreateContext();
        }

        public void Dispose()
        {
            TestDbContextFactory.DestroyContext(Context);
        }
    }

}
