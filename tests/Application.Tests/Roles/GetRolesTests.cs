using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class GetRolesTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly GetRolesQueryHandler _handler;

    public GetRolesTests() =>
        _handler = new GetRolesQueryHandler(
            _mockRoleRepo.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<GetRolesQueryHandler>>()
        );

    [Fact]
    public async Task Handle_WhenAdmin_ReturnsAllRoles()
    {
        // Arrange
        var roles = new List<RoleListItem>
        {
            new(Guid.NewGuid(), "Admin"),
            new(Guid.NewGuid(), "Editor"),
        };
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo.Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(roles);

        // Act
        Result<GetRolesResponse> result = await _handler.Handle(
            new GetRolesQuery(),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(2, result.Value!.Roles.Count);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        // Act
        Result<GetRolesResponse> result = await _handler.Handle(
            new GetRolesQuery(),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRoleRepo.Verify(r => r.GetAllAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNoRolesExist_ReturnsEmptyList()
    {
        // Arrange
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetAllAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(new List<RoleListItem>());

        // Act
        Result<GetRolesResponse> result = await _handler.Handle(
            new GetRolesQuery(),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Roles);
    }
}
