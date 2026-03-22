using Application.Interfaces;
using Application.Roles;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Permissions;
using Domain.Roles;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.Roles;

public class GetRoleTests
{
    private readonly Mock<IRoleRepository> _mockRoleRepo = new();
    private readonly Mock<IPermissionProvider> _mockPermissionProvider = new();
    private readonly Mock<IContentTypeRepository> _mockContentTypeRepo = new();
    private readonly Mock<IContentItemRepository> _mockContentItemRepo = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly GetRoleQueryHandler _handler;

    public GetRoleTests() =>
        _handler = new GetRoleQueryHandler(
            _mockRoleRepo.Object,
            _mockPermissionProvider.Object,
            _mockContentTypeRepo.Object,
            _mockContentItemRepo.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<GetRoleQueryHandler>>()
        );

    [Fact]
    public async Task Handle_WhenAdmin_ReturnsRoleWithPermissions()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        var resourceId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Editor"));
        _mockPermissionProvider
            .Setup(p =>
                p.GetPermissionsAsync(
                    It.Is<IReadOnlyCollection<Guid>>(ids => ids.Contains(roleId)),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(
                new List<PermissionRule>
                {
                    PermissionRule.ForResource(
                        ActorType.User,
                        CmsAction.Read,
                        new ContentTypeResource(resourceId)
                    ),
                    PermissionRule.ForResourceType(
                        ActorType.User,
                        CmsAction.List,
                        ResourceType.ContentItem
                    ),
                }
            );

        // Act
        Result<GetRoleResponse> result = await _handler.Handle(
            new GetRoleQuery(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(roleId, result.Value!.Id);
        Assert.Equal("Editor", result.Value.Name);
        Assert.Equal(2, result.Value.Permissions.Count);
    }

    [Fact]
    public async Task Handle_WhenNotAdmin_ReturnsForbidden()
    {
        // Arrange
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(false);

        // Act
        Result<GetRoleResponse> result = await _handler.Handle(
            new GetRoleQuery(Guid.NewGuid()),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
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
        Result<GetRoleResponse> result = await _handler.Handle(
            new GetRoleQuery(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task Handle_WhenRoleHasNoPermissions_ReturnsEmptyPermissionsList()
    {
        // Arrange
        var roleId = Guid.NewGuid();
        _mockUserContext.Setup(c => c.IsInRoleAsync("Admin")).ReturnsAsync(true);
        _mockRoleRepo
            .Setup(r => r.GetByIdAsync(roleId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new RoleListItem(roleId, "Empty Role"));
        _mockPermissionProvider
            .Setup(p =>
                p.GetPermissionsAsync(
                    It.IsAny<IReadOnlyCollection<Guid>>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(new List<PermissionRule>());

        // Act
        Result<GetRoleResponse> result = await _handler.Handle(
            new GetRoleQuery(roleId),
            CancellationToken.None
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Empty(result.Value!.Permissions);
    }
}
