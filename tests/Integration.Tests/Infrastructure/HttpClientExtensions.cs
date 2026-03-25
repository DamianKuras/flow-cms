using System.Net.Http.Json;

namespace Integration.Tests.Infrastructure;

public static class HttpClientExtensions
{
    public static async Task<T> ReadJsonAsync<T>(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<T>())!;
    }

    public static async Task<Guid> ReadCreatedIdAsync(this HttpResponseMessage response)
    {
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<CreatedResponse>())!.Id;
    }
}

public record CreatedResponse(Guid Id);
