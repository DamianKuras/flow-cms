using System.Net.Http.Json;
using Application.ContentItems;
using Application.ContentTypes;
using Integration.Tests.Builders;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Api;

public static class ContentItemApi
{
    public static async Task<Guid> Create(
        HttpClient client,
        ContentTypeDto type,
        Action<ContentItemValuesBuilder> values
    )
    {
        var builder = new ContentItemValuesBuilder(type);
        values(builder);

        var cmd = new CreateContentItemCommand("Item", type.Id, builder.Build());

        HttpResponseMessage response = await client.PostAsJsonAsync("/content-items", cmd);
        return await response.ReadCreatedIdAsync();
    }

    public static async Task<ContentItemDto> Get(HttpClient client, Guid id) =>
        await client.GetAsync($"/content-items/{id}").Result.ReadJsonAsync<ContentItemDto>();

    public static async Task<Guid> Publish(HttpClient client, Guid draftId)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/content-items/{draftId}/publish",
            new { }
        );
        response.EnsureSuccessStatusCode();
        return (await response.ReadJsonAsync<PublishedResponse>()).Id;
    }

    private record PublishedResponse(Guid Id);
}
