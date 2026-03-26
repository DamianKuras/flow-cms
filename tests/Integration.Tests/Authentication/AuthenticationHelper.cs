using System.Net.Http.Headers;
using System.Net.Http.Json;
using Application.Auth;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Authentication;

public static class AuthenticationHelper
{
    public static async Task<string> GetAdminAuthTokenAsync(HttpClient client)
    {
        var loginRequest = new { email = TestConstants.AdminEmail, password = TestConstants.AdminPassword };

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/sign-in", loginRequest);
        response.EnsureSuccessStatusCode();

        SignInResponse? result = await response.Content.ReadFromJsonAsync<SignInResponse>();
        return result!.AccessToken;
    }

    public static async Task<string> GetDevUserAuthTokenAsync(HttpClient client, int userIndex = 1)
    {
        var loginRequest = new
        {
            email = string.Format(TestConstants.DevUserEmailPattern, userIndex),
            password = TestConstants.DevUserPassword,
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/auth/sign-in", loginRequest);
        response.EnsureSuccessStatusCode();

        SignInResponse? result = await response.Content.ReadFromJsonAsync<SignInResponse>();
        return result!.AccessToken;
    }

    public static void AddAuthToken(this HttpClient client, string token) =>
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
}
