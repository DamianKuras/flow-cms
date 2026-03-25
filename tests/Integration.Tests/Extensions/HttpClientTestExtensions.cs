using System.Net.Http.Json;
using Integration.Tests.Infrastructure;

namespace Integration.Tests;

public static class HttpClientTestExtensions
{
    public static async Task<T> GetJsonAsync<T>(this HttpClient client, string url)
    {
        HttpResponseMessage response = await client.GetAsync(url);
        response.EnsureSuccessStatusCode();
        return await response.ReadJsonAsync<T>();
    }

    public static async Task<Guid> PostAndReadIdAsync<T>(this HttpClient client, string url, T body)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(url, body);
        response.EnsureSuccessStatusCode();
        return await response.ReadCreatedIdAsync();
    }
}
