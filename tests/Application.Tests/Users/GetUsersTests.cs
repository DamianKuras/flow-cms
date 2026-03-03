using Application.Interfaces;
using Application.Users;
using Domain.Common;
using Domain.Permissions;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.Users;

public class GetUsersQueryHandlerTests
{
    private readonly Mock<IUserRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<GetUsersQueryHandler>> _mockLogger;
    private readonly GetUsersQueryHandler _handler;

    public GetUsersQueryHandlerTests()
    {
        _mockRepository = new Mock<IUserRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GetUsersQueryHandler>>();
        _handler = new GetUsersQueryHandler(_mockRepository.Object, _mockAuth.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReturnsUsersAndTotalCount()
    {
        // Arrange
        var query = new GetUsersQuery(new PaginationParameters(1, 10));
        var pagedUsers = new List<PagedUser>
        {
            new PagedUser(Guid.NewGuid(), "test1@example.com", "Test User 1", "Active", DateTime.UtcNow),
            new PagedUser(Guid.NewGuid(), "test2@example.com", "Test User 2", "Active", DateTime.UtcNow)
        };

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.List, ResourceType.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.CountAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        _mockRepository
            .Setup(r => r.Get(query.PaginationParameters, It.IsAny<CancellationToken>()))
            .ReturnsAsync(pagedUsers);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(5, result.Value.TotalCount);
        Assert.Equal(2, result.Value.PagedList.Count);
        _mockRepository.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Once);
        _mockRepository.Verify(r => r.Get(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var query = new GetUsersQuery(new PaginationParameters(1, 10));

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.List, ResourceType.User, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.CountAsync(It.IsAny<CancellationToken>()), Times.Never);
        _mockRepository.Verify(r => r.Get(It.IsAny<PaginationParameters>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
