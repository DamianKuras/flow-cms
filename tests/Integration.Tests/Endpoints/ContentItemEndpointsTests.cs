using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Application.ContentItems;
using Application.ContentTypes;
using Application.Fields;
using Integration.Tests.Api;
using Integration.Tests.Authentication;
using Integration.Tests.Builders;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;

namespace Integration.Tests.Endpoints;

public class ContentItemEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    private const string REQUEST_URI_CONTENT_TYPES = "/content-types";
    private const string REQUEST_URI_CONTENT_ITEMS = "/content-items";

    public ContentItemEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public Task DisposeAsync() => Task.CompletedTask;

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    [Fact]
    public async Task CreateContentItem_WithoutAuth_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            new { title = "Test" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContentItems_WithoutAuth_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync(REQUEST_URI_CONTENT_ITEMS);
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task GetContentItemById_WithoutAuth_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.GetAsync(
            $"{REQUEST_URI_CONTENT_ITEMS}/{Guid.NewGuid()}"
        );
        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task UpdateContentItem_WithoutAuth_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.PatchAsJsonAsync(
            $"{REQUEST_URI_CONTENT_ITEMS}/{Guid.NewGuid()}",
            new { title = "Updated" }
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task DeleteContentItem_WithoutAuth_ReturnsUnauthorized()
    {
        HttpResponseMessage response = await _client.DeleteAsync(
            $"{REQUEST_URI_CONTENT_ITEMS}/{Guid.NewGuid()}"
        );

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentItem_ReturnsCreated_WithValidCommand()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        // Arrange
        Guid contentTypeId = await CreateContentType(
            TestDataHelper.CreateValidContentTypeCommand("Article")
        );
        ContentTypeDto contentTypeDto = await GetContentType(contentTypeId);
        FieldDto? titleField = contentTypeDto.Fields.FirstOrDefault(f => f.Name == "Title");
        FieldDto? bodyField = contentTypeDto.Fields.FirstOrDefault(f => f.Name == "Body");
        Assert.NotNull(titleField);
        Assert.NotNull(bodyField);
        var values = new Dictionary<Guid, object?>
        {
            [titleField.Id] = "First Title",
            [bodyField.Id] = "Article body content here",
        };
        var create_content_item_command = new CreateContentItemCommand(
            "First Article",
            contentTypeId,
            values
        );
        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            create_content_item_command
        );
        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreatedResponse? result = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task Create_WithNumericField_StoresCorrectly()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .NumberField("ViewCount", f => f.Required())
            .BuildAsync();

        Guid itemId = await ContentItemApi.Create(_client, type, v => v.Set("ViewCount", 42));

        ContentItemDto item = await ContentItemApi.Get(_client, itemId);
        ;
        Assert.Equal(42, item.Values["ViewCount"].Value.GetInt32());
    }

    [Fact]
    public async Task CreateContentItem_WithMissingRequiredField_ReturnsBadRequest()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title", f => f.Required())
            .BuildAsync();

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            new CreateContentItemCommand("Invalid Item", type.Id, new Dictionary<Guid, object?>())
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentItem_WithInvalidFieldType_ReturnsBadRequest()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .NumberField("Views", f => f.Required())
            .BuildAsync();

        var values = new Dictionary<Guid, object?> { [type.Fields.Single().Id] = "not-a-number" };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            new CreateContentItemCommand("Invalid", type.Id, values)
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentItem_WithUnknownFieldId_ReturnsConflict()
    {
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync();

        var values = new Dictionary<Guid, object?> { [Guid.NewGuid()] = "Unknown field" };

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            new CreateContentItemCommand("Test", type.Id, values)
        );

        Assert.True(response.StatusCode is HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateContentItem_WithNonExistentContentType_ReturnsNotFound()
    {
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_ITEMS,
            new CreateContentItemCommand("Test", Guid.NewGuid(), new Dictionary<Guid, object?>())
        );

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentItem_WithMultipleFieldTypes_StoresAllCorrectly()
    {
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title", f => f.Required())
            .NumberField("ViewCount")
            .TextField("Description")
            .BuildAsync();

        FieldDto titleField = type.Fields.First(f => f.Name == "Title");
        FieldDto viewCountField = type.Fields.First(f => f.Name == "ViewCount");
        FieldDto descriptionField = type.Fields.First(f => f.Name == "Description");

        var values = new Dictionary<Guid, object?>
        {
            [titleField.Id] = "Test Article",
            [viewCountField.Id] = 100,
            [descriptionField.Id] = "A test description",
        };

        Guid itemId = await ContentItemApi.Create(
            _client,
            type,
            v =>
            {
                v.Set("Title", "Test Article");
                v.Set("ViewCount", 100);
                v.Set("Description", "A test description");
            }
        );

        ContentItemDto item = await ContentItemApi.Get(_client, itemId);
        Assert.Equal("Test Article", item.Values["Title"].Value.GetString());
        Assert.Equal(100, item.Values["ViewCount"].Value.GetInt32());
        Assert.Equal("A test description", item.Values["Description"].Value.GetString());
    }

    [Fact]
    public async Task CreateContentItem_WithEmptyValues_CreatesSuccessfully()
    {
        _client.AddAuthToken(await AuthenticationHelper.GetAdminAuthTokenAsync(_client));

        ContentTypeDto type = await ContentTypeBuilder
            .Create(_client)
            .TextField("OptionalField")
            .BuildAsync();

        Guid itemId = await ContentItemApi.Create(_client, type, v => { });

        ContentItemDto item = await ContentItemApi.Get(_client, itemId);
        Assert.NotNull(item);
        Assert.NotEqual(Guid.Empty, item.Id);
    }

    private class CreatedResponse
    {
        public Guid Id { get; set; }
    }

    private record ContentTypeWithGuid(ContentTypeDto ContentTypeDto, Guid Id);

    private async Task<Guid> CreateContentType(CreateContentTypeCommand command)
    {
        HttpResponseMessage response = await _client.PostAsJsonAsync(
            REQUEST_URI_CONTENT_TYPES,
            command
        );
        CreatedResponse? createdResponse =
            await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(createdResponse);
        return createdResponse.Id;
    }

    private async Task<ContentTypeDto> GetContentType(Guid id)
    {
        HttpResponseMessage contentTypeResponse = await _client.GetAsync(
            $"{REQUEST_URI_CONTENT_TYPES}/{id}"
        );
        ContentTypeDto? contentTypeDto =
            await contentTypeResponse.Content.ReadFromJsonAsync<ContentTypeDto>();
        Assert.NotNull(contentTypeDto);
        return contentTypeDto;
    }
}
