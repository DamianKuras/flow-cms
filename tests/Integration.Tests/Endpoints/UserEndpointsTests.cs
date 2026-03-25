using System.Net;
using System.Net.Http.Json;
using Application.Auth;
using Integration.Tests.Authentication;
using Integration.Tests.Fixtures;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Endpoints;

public class UserEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public UserEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Register_NewUser_Succeeds()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        var registerRequest = new CreateUserCommand(
            Email: $"test{Guid.NewGuid()}@test.com",
            DisplayName: "Test",
            Password: "Test@123"
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/users", registerRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        Assert.NotNull(response);
        Assert.NotNull(response.Content);

        CreatedResponse? createdResponse =
            await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(createdResponse);
        Assert.NotEqual(Guid.Empty, createdResponse.Id);
    }

    [Fact]
    public async Task Register_DuplicateEmail_ReturnsConflict()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        var registerRequest = new CreateUserCommand(
            Email: "admin@admin.com",
            DisplayName: "Admin",
            Password: "Test@123"
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/users", registerRequest);

        // Assert
        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenUnauthorized_ReturnsUnauthorized()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/users");

        // Assert
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetUsers_WhenAuthorizedAdmin_ReturnsUsers()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/users?pageNumber=1&pageSize=10");

        // Assert
        response.EnsureSuccessStatusCode();
        string content = await response.Content.ReadAsStringAsync();
        Assert.Contains("pagedList", content);
        Assert.Contains("totalCount", content);
    }
}
