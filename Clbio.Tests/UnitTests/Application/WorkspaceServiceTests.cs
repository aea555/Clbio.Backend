using AutoMapper;
using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Mappings.V1;
using Clbio.Application.Services;
using Clbio.Domain.Entities.V1;
using Clbio.Tests.Utils.Fakes;
using Microsoft.Extensions.Logging;
using Moq;
using System.Linq.Expressions;

namespace Clbio.Tests.UnitTests.Application
{
    public class WorkspaceServiceTests
    {
        [Fact]
        public async Task DeleteAsync_WhenWorkspaceExists_DeletesBoardsAndWorkspace()
        {
            // Arrange
            var workspaceId = Guid.NewGuid();
            var boards = new List<Board>
            {
                new() { Id = Guid.NewGuid(), WorkspaceId = workspaceId },
                new() { Id = Guid.NewGuid(), WorkspaceId = workspaceId }
            };

            var mockWorkspaceRepo = new Mock<IRepository<Workspace>>();
            var mockBoardRepo = new Mock<IRepository<Board>>();
            var mockBoardService = new Mock<IBoardService>();
            var mockUow = new Mock<IUnitOfWork>();

            mockUow
                .Setup(u => u.Repository<Workspace>())
                .Returns(mockWorkspaceRepo.Object);

            mockUow
                .Setup(u => u.Repository<Board>())
                .Returns(mockBoardRepo.Object);

            mockWorkspaceRepo
                .Setup(r => r.GetByIdAsync(workspaceId, false, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Workspace { Id = workspaceId });

            mockBoardRepo
                .Setup(r => r.FindAsync(
                    It.IsAny<Expression<Func<Board, bool>>>(),
                    It.IsAny<bool>(),                 // match tracked param
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(boards);

            var fakeCache = new FakeCaching();
            var fakeInvalidator = new FakeCacheInvalidationService();
            var fakeVersions = new FakeCacheVersionService();
            var loggerFactory = LoggerFactory.Create(builder => { });

            var mapperConfig = new MapperConfiguration(cfg =>
            {
                cfg.AddProfile(new WorkspaceMappings());
            }, loggerFactory);

            var fakeMapper = mapperConfig.CreateMapper();


            var service = new WorkspaceService(
                mockUow.Object,
                fakeCache,
                fakeInvalidator,
                fakeVersions,
                mockBoardService.Object,
                fakeMapper,
                null
            );

            // Act
            await service.DeleteAsync(workspaceId, CancellationToken.None);

            // Assert
            mockBoardService.Verify(b => b.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            mockWorkspaceRepo.Verify(r => r.DeleteAsync(workspaceId, It.IsAny<CancellationToken>()), Times.Once);
            mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
