using System.Net.Http.Json;
using Application.ContentTypes;
using Domain;
using Domain.Fields;
using Integration.Tests.Infrastructure;

namespace Integration.Tests.Builders;

public sealed class ContentTypeBuilder
{
    private readonly HttpClient _client;
    private readonly List<CreateFieldDto> _fields = [];

    private ContentTypeBuilder(HttpClient client) => _client = client;

    public static ContentTypeBuilder Create(HttpClient client) => new(client);

    public ContentTypeBuilder TextField(string name, Action<FieldBuilder>? configure = null)
    {
        var builder = new FieldBuilder(name, FieldTypes.Text);
        configure?.Invoke(builder);
        _fields.Add(builder.Build());
        return this;
    }

    public ContentTypeBuilder NumberField(string name, Action<FieldBuilder>? configure = null)
    {
        var builder = new FieldBuilder(name, FieldTypes.Numeric);
        configure?.Invoke(builder);
        _fields.Add(builder.Build());
        return this;
    }

    public async Task<ContentTypeDto> BuildAsync(string name = "TestType")
    {
        var cmd = new CreateContentTypeCommand(name, _fields);

        HttpResponseMessage response = await _client.PostAsJsonAsync("/content-types", cmd);
        response.EnsureSuccessStatusCode();

        Guid id = await response.ReadCreatedIdAsync();

        ContentTypeDto? data = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"/content-types/{id}"
        );

        Assert.NotNull(data);

        return data;
    }

    public async Task<ContentTypeDto> BuildAndPublishAsync(
        string name = "TestType",
        MigrationMode migrationMode = MigrationMode.Lazy
    )
    {
        ContentTypeDto draft = await BuildAsync(name);

        HttpResponseMessage publishResponse = await _client.PostAsJsonAsync(
            $"/content-types/{name}/publish",
            new { MigrationMode = migrationMode.ToString() }
        );
        publishResponse.EnsureSuccessStatusCode();

        // Reload to get the published version.
        ContentTypeDto? published = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"/content-types/{draft.Id}"
        );
        Assert.NotNull(published);
        return published;
    }
}
