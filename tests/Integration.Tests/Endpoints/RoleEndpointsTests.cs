using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.Roles;
using Domain.Permissions;
using Domain.Roles;
using Integration.Tests.Authentication;
using Integration.Tests.Fixtures;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Endpoints;

public class RoleEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;
    private JsonSerializerOptions JsonOptions => _factory.JsonOptions;

    public RoleEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task GetRoles_WhenUnauthenticated_ReturnsUnauthorized()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetRoles_WhenAdmin_ReturnsRolesList()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/roles");

        // Assert
        response.EnsureSuccessStatusCode();
        GetRolesResponse? body = await response.Content.ReadFromJsonAsync<GetRolesResponse>(
            JsonOptions
        );
        Assert.NotNull(body);
        Assert.NotEmpty(body.Roles); // Admin role is seeded
    }

    [Fact]
    public async Task GetRoles_WhenNonAdmin_ReturnsForbidden()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetDevUserAuthTokenAsync(_client));

        // Act
        HttpResponseMessage response = await _client.GetAsync("/roles");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateRole_WhenAdmin_ReturnsCreated()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("Editor")
        );

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreatedResponse? body = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotEqual(Guid.Empty, body!.Id);
    }

    [Fact]
    public async Task CreateRole_WhenNonAdmin_ReturnsForbidden()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetDevUserAuthTokenAsync(_client));

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("Editor")
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task GetRole_WhenAdmin_ReturnsRoleWithPermissions()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("Viewer")
        );
        CreatedResponse? created =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/roles/{created!.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
        GetRoleResponse? body = await response.Content.ReadFromJsonAsync<GetRoleResponse>(
            JsonOptions
        );
        Assert.NotNull(body);
        Assert.Equal("Viewer", body.Name);
        Assert.Empty(body.Permissions);
    }

    [Fact]
    public async Task GetRole_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/roles/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_WhenAdmin_ReturnsNoContent()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("ToDelete")
        );
        CreatedResponse? created =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"/roles/{created!.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task DeleteRole_AdminRole_ReturnsConflict()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage listResponse = await _client.GetAsync("/roles");
        GetRolesResponse? list = await listResponse.Content.ReadFromJsonAsync<GetRolesResponse>(
            JsonOptions
        );
        RoleListItem adminRole = list!.Roles.First(r => r.Name == "Admin");

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"/roles/{adminRole.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task AssignRoleToUser_WhenAdmin_ReturnsNoContent()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("Assignable")
        );
        CreatedResponse? role = await roleResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        HttpResponseMessage userResponse = await _client.PostAsJsonAsync(
            "/users",
            new
            {
                email = $"assign{Guid.NewGuid()}@test.com",
                displayName = "Test",
                password = "Test@123",
            }
        );
        CreatedResponse? user = await userResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/roles/{role!.Id}/users/{user!.Id}",
            new { }
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AssignRoleToUser_WhenRoleDoesNotExist_ReturnsNotFound()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage userResponse = await _client.PostAsJsonAsync(
            "/users",
            new
            {
                email = $"assign2{Guid.NewGuid()}@test.com",
                displayName = "Test",
                password = "Test@123",
            }
        );
        CreatedResponse? user = await userResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/roles/{Guid.NewGuid()}/users/{user!.Id}",
            new { }
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task AddPermission_ResourceSpecific_ReturnsNoContent()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("PermRole")
        );
        CreatedResponse? role = await roleResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var body = new
        {
            action = "read",
            resourceType = "contentType",
            resourceId = Guid.NewGuid(),
            scope = "allow",
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/roles/{role!.Id}/permissions",
            body
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddPermission_TypeLevel_ReturnsNoContent()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("TypeLevelRole")
        );
        CreatedResponse? role = await roleResponse.Content.ReadFromJsonAsync<CreatedResponse>();

        var body = new
        {
            action = "list",
            resourceType = "contentItem",
            resourceId = (Guid?)null,
            scope = "allow",
        };

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            $"/roles/{role!.Id}/permissions",
            body
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task AddPermission_ThenGetRole_ShowsPermission()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("WithPerm")
        );
        CreatedResponse? role = await roleResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var resourceId = Guid.NewGuid();

        await _client.PostAsJsonAsync(
            $"/roles/{role!.Id}/permissions",
            new
            {
                action = "read",
                resourceType = "contentType",
                resourceId,
                scope = "allow",
            }
        );

        // Act
        HttpResponseMessage getResponse = await _client.GetAsync($"/roles/{role.Id}");
        GetRoleResponse? detail = await getResponse.Content.ReadFromJsonAsync<GetRoleResponse>(
            JsonOptions
        );

        // Assert
        Assert.Single(detail!.Permissions);
        Assert.Equal(CmsAction.Read, detail.Permissions[0].Action);
        Assert.Equal(resourceId, detail.Permissions[0].ResourceId);
    }

    [Fact]
    public async Task RemovePermission_WhenPermissionExists_ReturnsNoContent()
    {
        // Arrange
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage roleResponse = await _client.PostAsJsonAsync(
            "/roles",
            new CreateRoleCommand("RemovePerm")
        );
        CreatedResponse? role = await roleResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        var resourceId = Guid.NewGuid();

        var permBody = new
        {
            action = "read",
            resourceType = "contentType",
            resourceId,
            scope = "allow",
        };

        await _client.PostAsJsonAsync($"/roles/{role!.Id}/permissions", permBody);

        // Act
        HttpResponseMessage response = await _client.SendAsync(
            new HttpRequestMessage(HttpMethod.Delete, $"/roles/{role.Id}/permissions")
            {
                Content = JsonContent.Create(permBody),
            }
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);

        GetRoleResponse? detail = await (
            await _client.GetAsync($"/roles/{role.Id}")
        ).Content.ReadFromJsonAsync<GetRoleResponse>(JsonOptions);
        Assert.Empty(detail!.Permissions);
    }
}
