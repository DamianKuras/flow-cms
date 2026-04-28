using Domain.Permissions;
using Infrastructure.Data;
using Infrastructure.Persistence.Permissions;
using Infrastructure.Services;
using Integration.Tests.Fixtures;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace Integration.Tests.Services;

public sealed class RolePermissionServiceTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly IntegrationTestWebApplicationFactory _factory;

    public RolePermissionServiceTests(IntegrationTestWebApplicationFactory factory) => _factory = factory;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task AddPermissionToRoleAsync_ShouldAddPermissionCorrectly()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var role = new global::Infrastructure.Users.AppRole { Id = roleId, Name = roleId.ToString(), NormalizedName = roleId.ToString() };
        dbContext.Set<global::Infrastructure.Users.AppRole>().Add(role);
        await dbContext.SaveChangesAsync();
        var resourceId = Guid.NewGuid();
        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Create,
            new ContentItemResource(resourceId),
            PermissionScope.Allow
        );

        // Act
        await service.AddPermissionToRoleAsync(roleId, rule);

        // Assert
        RolePermissionEntity? entity = await dbContext.RolePermissions.SingleOrDefaultAsync(rp => rp.RoleId == roleId);
        Assert.NotNull(entity);
        Assert.Equal(CmsAction.Create, entity.Action);
        Assert.Equal(global::Infrastructure.Persistence.Permissions.ResourceType.ContentItem, entity.ResourceType);
        Assert.Equal(resourceId.ToString(), entity.ResourceId);
        Assert.Equal(PermissionScope.Allow, entity.Scope);
    }

    [Fact]
    public async Task RemovePermissionFromRoleAsync_ShouldRemovePermission_WhenItExists()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var role = new global::Infrastructure.Users.AppRole { Id = roleId, Name = roleId.ToString(), NormalizedName = roleId.ToString() };
        dbContext.Set<global::Infrastructure.Users.AppRole>().Add(role);
        await dbContext.SaveChangesAsync();
        const string resourceId = "blog-posts";

        var entity = new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Action = CmsAction.Delete,
            ResourceType = global::Infrastructure.Persistence.Permissions.ResourceType.ContentType,
            ResourceId = resourceId,
            Scope = PermissionScope.Allow
        };
        dbContext.RolePermissions.Add(entity);
        await dbContext.SaveChangesAsync();

        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Delete,
            new ContentTypeResource(resourceId),
            PermissionScope.Allow
        );

        // Act
        await service.RemovePermissionFromRoleAsync(roleId, rule);

        // Assert
        bool exists = await dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == roleId);
        Assert.False(exists);
    }

    [Fact]
    public async Task RemovePermissionFromRoleAsync_ShouldDoNothing_WhenPermissionDoesNotExist()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var role = new global::Infrastructure.Users.AppRole { Id = roleId, Name = roleId.ToString(), NormalizedName = roleId.ToString() };
        dbContext.Set<global::Infrastructure.Users.AppRole>().Add(role);
        await dbContext.SaveChangesAsync();
        var resourceId = Guid.NewGuid();
        
        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Update,
            new FieldResource(resourceId),
            PermissionScope.Deny
        );

        // Act
        await service.RemovePermissionFromRoleAsync(roleId, rule);

        // Assert
        int count = await dbContext.RolePermissions.CountAsync();
        Assert.Equal(0, count);
    }

    [Fact]
    public async Task AddPermissionToRoleAsync_ThrowsNotSupportedException_ForUnknownResource()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var unknownResource = new UnknownResource();
        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Read,
            unknownResource,
            PermissionScope.Allow
        );

        // Act & Assert
        await Assert.ThrowsAsync<NotSupportedException>(() => service.AddPermissionToRoleAsync(roleId, rule));
    }

    [Fact]
    public async Task AddPermissionToRoleAsync_TypeLevelPermission_StoresNullResourceId()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var role = new global::Infrastructure.Users.AppRole { Id = roleId, Name = roleId.ToString(), NormalizedName = roleId.ToString() };
        dbContext.Set<global::Infrastructure.Users.AppRole>().Add(role);
        await dbContext.SaveChangesAsync();

        var rule = PermissionRule.ForResourceType(
            ActorType.User,
            CmsAction.List,
            Domain.Permissions.ResourceType.ContentType,
            PermissionScope.Allow
        );

        // Act
        await service.AddPermissionToRoleAsync(roleId, rule);

        // Assert
        RolePermissionEntity? entity = await dbContext.RolePermissions.SingleOrDefaultAsync(rp => rp.RoleId == roleId);
        Assert.NotNull(entity);
        Assert.Equal(CmsAction.List, entity.Action);
        Assert.Equal(global::Infrastructure.Persistence.Permissions.ResourceType.ContentType, entity.ResourceType);
        Assert.Null(entity.ResourceId);
        Assert.Equal(PermissionScope.Allow, entity.Scope);
    }

    [Fact]
    public async Task RemovePermissionFromRoleAsync_TypeLevelPermission_RemovesCorrectEntity()
    {
        // Arrange
        using IServiceScope scope = _factory.Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        var service = new RolePermissionService(dbContext);

        var roleId = Guid.NewGuid();
        var role = new global::Infrastructure.Users.AppRole { Id = roleId, Name = roleId.ToString(), NormalizedName = roleId.ToString() };
        dbContext.Set<global::Infrastructure.Users.AppRole>().Add(role);
        await dbContext.SaveChangesAsync();

        dbContext.RolePermissions.Add(new RolePermissionEntity
        {
            Id = Guid.NewGuid(),
            RoleId = roleId,
            Action = CmsAction.List,
            ResourceType = global::Infrastructure.Persistence.Permissions.ResourceType.ContentType,
            ResourceId = null,
            Scope = PermissionScope.Allow,
        });
        await dbContext.SaveChangesAsync();

        var rule = PermissionRule.ForResourceType(
            ActorType.User,
            CmsAction.List,
            Domain.Permissions.ResourceType.ContentType,
            PermissionScope.Allow
        );

        // Act
        await service.RemovePermissionFromRoleAsync(roleId, rule);

        // Assert
        Assert.False(await dbContext.RolePermissions.AnyAsync(rp => rp.RoleId == roleId));
    }

    private record UnknownResource : Resource
    {
        public override Domain.Permissions.ResourceType Type => throw new NotImplementedException();
    }
}
