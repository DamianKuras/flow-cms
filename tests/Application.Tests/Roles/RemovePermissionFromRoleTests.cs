using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.Permissions;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class RemovePermissionFromRoleTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IRolePermissionService> _mockPermissionService = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly RemovePermissionFromRoleCommandHandler _handler;

    public RemovePermissionFromRoleTests() =>
        _handler = new RemovePermissionFromRoleCommandHandler(
            _mockRoleRepo.Object,
            _mockPermissionService.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<RemovePermissionFromRoleCommandHandler>>()
        );

    [Fact]
    public async Task Handle_ResourceSpecificPermission_RemovesAndReturnsRoleId()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        const string resourceId = "blog-posts";
        var command = new RemovePermissionFromRoleCommand(
            roleId,
            CmsAction.Read,
            ResourceType.ContentType,
            resourceId,
            PermissionScope.Allow
        );
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleId, result.Value);
        _mockPermissionService.Verify(
            s =>
                s.RemovePermissionFromRoleAsync(
                    roleId,
                    It.Is<PermissionRule>(r =>
                        r
                        == PermissionRule.ForResource(
                            ActorType.User,
                            CmsAction.Read,
                            new ContentTypeResource(resourceId),
                            PermissionScope.Allow
                        )
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_TypeLevelPermission_RemovesRuleWithNullResource()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new RemovePermissionFromRoleCommand(
            roleId,
            CmsAction.List,
            ResourceType.ContentItem,
            null,
            PermissionScope.Allow
        );
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockPermissionService.Verify(
            s =>
                s.RemovePermissionFromRoleAsync(
                    roleId,
                    It.Is<PermissionRule>(r =>
                        r.Resource == null
                        && r.ResourceType == ResourceType.ContentItem
                        && r.Action == CmsAction.List
                    )
                ),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        // Arrange
        var command = new RemovePermissionFromRoleCommand(
            Guid.NewGuid(),
            CmsAction.Read,
            ResourceType.ContentType,
            null,
            PermissionScope.Allow
        );
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockPermissionService.Verify(
            s => s.RemovePermissionFromRoleAsync(It.IsAny<Guid>(), It.IsAny<PermissionRule>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenRoleNotFound_ReturnsNotFound()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var command = new RemovePermissionFromRoleCommand(
            roleId,
            CmsAction.Read,
            ResourceType.ContentType,
            null,
            PermissionScope.Allow
        );
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((RoleListItem?)null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
    }
}
