using System.Net;
using System.Net.Http.Json;
using Application.Auth;
using Integration.Tests.Authentication;
using Integration.Tests.Fixtures;

namespace Integration.Tests.Endpoints;

public class AuthEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public AuthEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task SignIn_Returns_Access_And_Refresh_Tokens()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        var loginRequest = new { email = "admin@admin.com", password = "Admin@123" };

        // Act
        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/sign-in", loginRequest);

        // Assert
        response.EnsureSuccessStatusCode();
        SignInResponseDTO? result = await response.Content.ReadFromJsonAsync<SignInResponseDTO>();
        Assert.NotNull(result);
        Assert.False(string.IsNullOrWhiteSpace(result.AccessToken));
        Assert.False(string.IsNullOrWhiteSpace(result.RefreshToken));
    }

    [Fact]
    public async Task Access_Protected_Endpoint_With_Valid_AccessToken_Succeeds()
    {
        // Arrange
        HttpClient client = _factory.CreateClient();
        string token = await AuthenticationHelper.GetAuthTokenAsync(client);
        client.AddAuthToken(token);
        // Act
        HttpResponseMessage response = await client.GetAsync("/content-types");
        // Assert
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_Issues_New_Tokens()
    {
        HttpClient client = _factory.CreateClient();
        var login = new { email = "admin@admin.com", password = "Admin@123" };
        HttpResponseMessage signInResponse = await client.PostAsJsonAsync("/auth/sign-in", login);
        SignInResponseDTO? tokens =
            await signInResponse.Content.ReadFromJsonAsync<SignInResponseDTO>();
        Assert.NotNull(tokens);

        var refreshPayload = new SignInWithRefreshTokenCommand(tokens.RefreshToken);
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/auth/refresh-token",
            refreshPayload
        );
        refreshResponse.EnsureSuccessStatusCode();
        SignInResponseDTO? refreshed =
            await refreshResponse.Content.ReadFromJsonAsync<SignInResponseDTO>();

        Assert.NotNull(refreshed);

        Assert.False(string.IsNullOrWhiteSpace(refreshed.AccessToken));

        Assert.NotEqual(tokens.RefreshToken, refreshed.RefreshToken);

        Assert.False(string.IsNullOrWhiteSpace(refreshed.ExpiresIn));
    }

    [Fact]
    public async Task Old_Refresh_Token_Fails_After_Use()
    {
        HttpClient client = _factory.CreateClient();

        var login = new { email = "admin@admin.com", password = "Admin@123" };
        HttpResponseMessage signInResponse = await client.PostAsJsonAsync("/auth/sign-in", login);
        SignInResponseDTO? tokens =
            await signInResponse.Content.ReadFromJsonAsync<SignInResponseDTO>();

        Assert.NotNull(tokens);
        var refreshPayload = new SignInWithRefreshTokenCommand(tokens.RefreshToken);
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/auth/refresh-token",
            refreshPayload
        );
        refreshResponse.EnsureSuccessStatusCode();

        SignInResponseDTO? refreshed =
            await refreshResponse.Content.ReadFromJsonAsync<SignInResponseDTO>();

        var reusePayload = new SignInWithRefreshTokenCommand(tokens.RefreshToken);
        HttpResponseMessage reuseResponse = await client.PostAsJsonAsync(
            "/auth/refresh-token",
            reusePayload
        );

        Assert.Equal(HttpStatusCode.Unauthorized, reuseResponse.StatusCode);
    }

    [Fact]
    public async Task Logout_Revokes_RefreshToken()
    {
        HttpClient client = _factory.CreateClient();

        var login = new { email = "admin@admin.com", password = "Admin@123" };
        HttpResponseMessage signInResponse = await client.PostAsJsonAsync("/auth/sign-in", login);
        SignInResponseDTO? tokens =
            await signInResponse.Content.ReadFromJsonAsync<SignInResponseDTO>();
        Assert.NotNull(tokens);

        await client.PostAsJsonAsync("/auth/sing-out", new { tokens.RefreshToken });

        var refreshPayload = new SignInWithRefreshTokenCommand(tokens.RefreshToken);
        HttpResponseMessage refreshResponse = await client.PostAsJsonAsync(
            "/auth/refresh-token",
            refreshPayload
        );

        Assert.Equal(HttpStatusCode.Unauthorized, refreshResponse.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithInvalidCredentials_ReturnsUnauthorized()
    {
        var loginRequest = new { email = "wrong@email.com", password = "Wrong@123" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/auth/sign-in", loginRequest);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_Protected_Endpoint_WithoutToken_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();
        HttpResponseMessage response = await client.GetAsync("/content-types");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Access_Protected_Endpoint_WithInvalidToken_ReturnsUnauthorized()
    {
        HttpClient client = _factory.CreateClient();
        client.AddAuthToken("invalid.jwt.token");
        HttpResponseMessage response = await client.GetAsync("/content-types");
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task RefreshToken_WithInvalidToken_ReturnsUnauthorized()
    {
        var refreshPayload = new SignInWithRefreshTokenCommand("invalid-refresh-token");
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            "/auth/refresh-token",
            refreshPayload
        );
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithInvalidEmailFormat_ReturnsBadRequest()
    {
        var loginRequest = new { email = "not-an-email", password = "Password@123" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/auth/sign-in", loginRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task SignIn_WithMissingPassword_ReturnsBadRequest()
    {
        var loginRequest = new { email = "test@test.com", password = "" };
        HttpResponseMessage response = await _client.PostAsJsonAsync("/auth/sign-in", loginRequest);
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }
}
