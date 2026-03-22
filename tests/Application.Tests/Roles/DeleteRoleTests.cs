using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class DeleteRoleTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly DeleteRoleCommandHandler _handler;

    public DeleteRoleTests() =>
        _handler = new DeleteRoleCommandHandler(
            _mockRoleRepo.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<DeleteRoleCommandHandler>>()
        );

    [Fact]
    public async Task Handle_WhenAdmin_DeletesRoleAndReturnsId()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));

        // Act
        Result<Guid> result = await _handler.Handle(
            new DeleteRoleCommand(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleId, result.Value);
        _mockRoleRepo.Verify(r => r.DeleteAsync(roleId, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        // Act
        Result<Guid> result = await _handler.Handle(
            new DeleteRoleCommand(Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRoleRepo.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
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
            new DeleteRoleCommand(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRoleRepo.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenDeletingAdminRole_ReturnsConflict()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Admin"));

        // Act
        Result<Guid> result = await _handler.Handle(
            new DeleteRoleCommand(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Conflict, result.Error?.Type);
        _mockRoleRepo.Verify(
            r => r.DeleteAsync(It.IsAny<Guid>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }
}
