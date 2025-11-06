using Clbio.Abstractions.Interfaces;
using Clbio.Abstractions.Interfaces.Repositories;
using Clbio.Abstractions.Interfaces.Services;
using Clbio.Application.Services;
using Clbio.Domain.Entities;
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
                .Setup(r => r.GetByIdAsync(workspaceId, It.IsAny<CancellationToken>()))
                .ReturnsAsync(new Workspace { Id = workspaceId });

            mockBoardRepo
                .Setup(r => r.FindAsync(It.IsAny<Expression<Func<Board, bool>>>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(boards);

            var service = new WorkspaceService(mockUow.Object, mockBoardService.Object);

            // Act
            await service.DeleteAsync(workspaceId, CancellationToken.None);

            // Assert
            mockBoardService.Verify(b => b.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            mockWorkspaceRepo.Verify(r => r.DeleteAsync(workspaceId, It.IsAny<CancellationToken>()), Times.Once);
            mockUow.Verify(u => u.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
