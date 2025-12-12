using System.Net;
using System.Net.Http.Json;
using Application.ContentTypes;
using Application.Fields;
using Domain.Fields;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;

namespace Integration.Tests.Endpoints;

public class ContentTypeEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    public ContentTypeEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    [Fact]
    public async Task GetContentTypes_ReturnsEmptyList_WhenNoContentTypesExist()
    {
        // Act
        HttpResponseMessage response = await _client.GetAsync("/content-type");

        // Assert
        response.EnsureSuccessStatusCode();
        List<ContentTypeListDto>? contentTypes = await response.Content.ReadFromJsonAsync<
            List<ContentTypeListDto>
        >();
        Assert.NotNull(contentTypes);
        Assert.Empty(contentTypes);
    }

    [Fact]
    public async Task GetContentTypes_ReturnsListOfContentTypes_WhenContentTypesExist()
    {
        // Arrange
        CreateContentTypeCommand createCommand = TestDataHelper.CreateValidContentTypeCommand(
            "BlogPost"
        );
        await _client.PostAsJsonAsync("/content-type", createCommand);

        // Act
        HttpResponseMessage response = await _client.GetAsync("/content-type");

        // Assert
        response.EnsureSuccessStatusCode();
        List<ContentTypeListDto>? contentTypes = await response.Content.ReadFromJsonAsync<
            List<ContentTypeListDto>
        >();
        Assert.NotNull(contentTypes);
        Assert.Single(contentTypes);
        Assert.Equal("BlogPost", contentTypes[0].Name);
        Assert.Equal("DRAFT", contentTypes[0].Status);
        Assert.Equal(1, contentTypes[0].Version);
    }

    [Fact]
    public async Task CreateContentType_ReturnsCreated_WithValidCommand()
    {
        // Arrange
        CreateContentTypeCommand command = TestDataHelper.CreateValidContentTypeCommand("Article");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/content-type", command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        CreatedResponse? result = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        Assert.NotNull(result);
        Assert.NotEqual(Guid.Empty, result.Id);
        Assert.NotNull(response.Headers.Location);
    }

    [Fact]
    public async Task CreateContentType_ReturnsBadRequest_WithInvalidCommand()
    {
        // Arrange
        CreateContentTypeCommand command = TestDataHelper.CreateInvalidContentTypeCommand();

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/content-type", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentType_IncrementsVersion_WhenCreatingSameNameTwice()
    {
        // Arrange
        CreateContentTypeCommand command1 = TestDataHelper.CreateValidContentTypeCommand("Product");
        CreateContentTypeCommand command2 = TestDataHelper.CreateValidContentTypeCommand("Product");

        // Act
        HttpResponseMessage response1 = await _client.PostAsJsonAsync("/content-type", command1);
        CreatedResponse? result1 = await response1.Content.ReadFromJsonAsync<CreatedResponse>();

        HttpResponseMessage response2 = await _client.PostAsJsonAsync("/content-type", command2);
        CreatedResponse? result2 = await response2.Content.ReadFromJsonAsync<CreatedResponse>();

        // Assert
        Assert.Equal(HttpStatusCode.Created, response1.StatusCode);
        Assert.Equal(HttpStatusCode.Created, response2.StatusCode);

        ContentTypeDto? contentType1 = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"/content-type/{result1!.Id}"
        );
        ContentTypeDto? contentType2 = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"/content-type/{result2!.Id}"
        );

        Assert.Equal(1, contentType1!.Version);
        Assert.Equal(2, contentType2!.Version);
    }

    [Fact]
    public async Task CreateContentType_ReturnsBadRequest_WithUnknownValidationRule()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "TestType",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    Name: "TestField",
                    Type: FieldTypes.Text,
                    IsRequired: true,
                    ValidationRules: new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(Type: "UnknownRule", Parameters: null),
                    }
                ),
            }
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync("/content-type", command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task GetContentTypeById_ReturnsContentType_WhenExists()
    {
        // Arrange
        CreateContentTypeCommand createCommand = TestDataHelper.CreateValidContentTypeCommand(
            "Page"
        );
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/content-type",
            createCommand
        );
        CreatedResponse? createResult =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        Guid contentTypeId = createResult!.Id;

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/content-type/{contentTypeId}");

        // Assert
        response.EnsureSuccessStatusCode();
        ContentTypeDto? contentType = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        Assert.NotNull(contentType);
        Assert.Equal("DRAFT", contentType.Status);
        Assert.Equal(2, contentType.Fields.Count);
        Assert.Contains(contentType.Fields, f => f.Name == "Title");
        Assert.Contains(contentType.Fields, f => f.Name == "Description");
    }

    [Fact]
    public async Task GetContentTypeById_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/content-type/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContentTypeById_ReturnsFieldsWithValidationRules()
    {
        // Arrange
        CreateContentTypeCommand createCommand = TestDataHelper.CreateValidContentTypeCommand(
            "FormType"
        );
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/content-type",
            createCommand
        );
        CreatedResponse? createResult =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        Guid contentTypeId = createResult!.Id;

        // Act
        HttpResponseMessage response = await _client.GetAsync($"/content-type/{contentTypeId}");

        // Assert
        response.EnsureSuccessStatusCode();
        ContentTypeDto? contentType = await response.Content.ReadFromJsonAsync<ContentTypeDto>();
        Assert.NotNull(contentType);

        Application.ContentTypes.FieldDto titleField = contentType.Fields.First(f =>
            f.Name == "Title"
        );
        Assert.True(titleField.IsRequired);
        Assert.NotNull(titleField.ValidationRules);
        Assert.Single(titleField.ValidationRules);
        Assert.Equal("MaximumLengthValidationRule", titleField.ValidationRules[0].Type);
    }

    [Fact]
    public async Task DeleteContentType_ReturnsNoContent_WhenExists()
    {
        // Arrange
        CreateContentTypeCommand createCommand = TestDataHelper.CreateValidContentTypeCommand(
            "TempType"
        );
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/content-type",
            createCommand
        );
        CreatedResponse? createResult =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        Guid contentTypeId = createResult!.Id;

        // Act
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(
            $"/content-type/{contentTypeId}"
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // Verify it's actually deleted
        HttpResponseMessage getResponse = await _client.GetAsync($"/content-type/{contentTypeId}");
        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task DeleteContentType_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"/content-type/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task CompleteWorkflow_CreateGetListDelete_WorksCorrectly()
    {
        // 1. Create a content type
        CreateContentTypeCommand createCommand = TestDataHelper.CreateValidContentTypeCommand(
            "WorkflowTest"
        );
        HttpResponseMessage createResponse = await _client.PostAsJsonAsync(
            "/content-type",
            createCommand
        );
        Assert.Equal(HttpStatusCode.Created, createResponse.StatusCode);
        CreatedResponse? createResult =
            await createResponse.Content.ReadFromJsonAsync<CreatedResponse>();
        Guid contentTypeId = createResult!.Id;

        // 2. Get the created content type by ID
        HttpResponseMessage getResponse = await _client.GetAsync($"/content-type/{contentTypeId}");
        getResponse.EnsureSuccessStatusCode();
        ContentTypeDto? contentType = await getResponse.Content.ReadFromJsonAsync<ContentTypeDto>();
        Assert.NotNull(contentType);
        Assert.Equal("DRAFT", contentType.Status);

        // 3. List all content types
        HttpResponseMessage listResponse = await _client.GetAsync("/content-type");
        List<ContentTypeListDto>? contentTypes = await listResponse.Content.ReadFromJsonAsync<
            List<ContentTypeListDto>
        >();
        Assert.Contains(contentTypes!, ct => ct.Id == contentTypeId);

        // 4. Delete the content type
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(
            $"/content-type/{contentTypeId}"
        );
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        // 5. Verify deletion
        HttpResponseMessage verifyResponse = await _client.GetAsync(
            $"/content-type/{contentTypeId}"
        );
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    // Helper class to deserialize the response
    private class CreatedResponse
    {
        public Guid Id { get; set; }
    }
}
