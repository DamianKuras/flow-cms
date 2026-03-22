using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class AssignRoleToUserTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IUserRepository> _mockUserRepo = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly AssignRoleToUserCommandHandler _handler;

    public AssignRoleToUserTests() =>
        _handler = new AssignRoleToUserCommandHandler(
            _mockRoleRepo.Object,
            _mockUserRepo.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<AssignRoleToUserCommandHandler>>()
        );

    [Fact]
    public async Task Handle_WhenAdmin_AssignsRoleAndReturnsUserId()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));
        _mockUserRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new User(userId, "user@test.com", "User"));

        // Act
        Result<Guid> result = await _handler.Handle(
            new AssignRoleToUserCommand(roleId, userId),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(userId, result.Value);
        _mockRoleRepo.Verify(
            r => r.AssignToUserAsync(roleId, userId, It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        // Act
        Result<Guid> result = await _handler.Handle(
            new AssignRoleToUserCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRoleRepo.Verify(
            r =>
                r.AssignToUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleListItem?)null);

        // Act
        Result<Guid> result = await _handler.Handle(
            new AssignRoleToUserCommand(roleId, Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRoleRepo.Verify(
            r =>
                r.AssignToUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));
        _mockUserRepo
            .Setup(r => r.GetByIdAsync(userId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((User?)null);

        // Act
        Result<Guid> result = await _handler.Handle(
            new AssignRoleToUserCommand(roleId, userId),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRoleRepo.Verify(
            r =>
                r.AssignToUserAsync(
                    It.IsAny<Guid>(),
                    It.IsAny<Guid>(),
                    It.IsAny<CancellationToken>()
                ),
            Times.Never
        );
    }
}
