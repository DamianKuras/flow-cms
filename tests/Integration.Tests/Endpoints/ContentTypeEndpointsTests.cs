using System.Net;
using System.Net.Http.Json;
using Api.Responses;
using Application.ContentTypes;
using Application.Fields;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Integration.Tests.Authentication;
using Integration.Tests.Builders;
using Integration.Tests.Fixtures;
using Integration.Tests.Helpers;

namespace Integration.Tests.Endpoints;

public class ContentTypeEndpointsTests
    : IClassFixture<IntegrationTestWebApplicationFactory>,
        IAsyncLifetime
{
    private const string REQUEST_URI = "/content-types";
    private readonly HttpClient _client;
    private readonly IntegrationTestWebApplicationFactory _factory;

    public ContentTypeEndpointsTests(IntegrationTestWebApplicationFactory factory)
    {
        _factory = factory;
        _client = factory.CreateClient();
    }

    public async Task InitializeAsync() => await _factory.ResetDatabaseAsync();

    public Task DisposeAsync() => Task.CompletedTask;

    #region GetContentTypes

    [Fact]
    public async Task GetContentTypes_ReturnsEmptyList_WhenNoContentTypesExist()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        PagedResponse<PagedContentType>? result = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        /// Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Data);
        Assert.Empty(result.Data);
    }

    [Fact]
    public async Task GetContentTypes_ReturnsListOfContentTypes_WhenContentTypesExist()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        await ContentTypeBuilder.Create(_client).TextField("Title").BuildAsync("BlogPost");

        // Act
        PagedResponse<PagedContentType>? result = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        // Assert
        Assert.NotNull(result);
        Assert.Single(result.Data);
        PagedContentType blogPostContentType = result.Data[0];
        Assert.Equal("BlogPost", blogPostContentType.Name);
        Assert.Equal("DRAFT", blogPostContentType.Status);
        Assert.Equal(1, blogPostContentType.Version);
    }

    #endregion

    #region CreateContentType

    [Fact]
    public async Task CreateContentType_ReturnsCreated_WithValidCommandAndAdminUser()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        CreateContentTypeCommand command = TestDataHelper.CreateValidContentTypeCommand("Article");

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

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
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        CreateContentTypeCommand command = TestDataHelper.CreateInvalidContentTypeCommand();

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentType_IncrementsVersion_WhenCreatingSameNameTwice()
    {
        // Arrange and Act
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto first = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("Product");

        ContentTypeDto second = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("Product");

        // Assert
        Assert.Equal(1, first.Version);
        Assert.Equal(2, second.Version);
    }

    [Fact]
    public async Task CreateContentType_ReturnsBadRequest_WithUnknownValidationRule()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
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
                        new CreateValidationRuleDto("UnknownRule", null),
                    }
                ),
            }
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    #endregion

    #region GetContentTypeById
    [Fact]
    public async Task GetContentTypeById_ReturnsContentType_WhenExists()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .TextField("Description")
            .BuildAsync("Page");

        // Act
        HttpResponseMessage response = await _client.GetAsync($"{REQUEST_URI}/{contentType.Id}");

        // Assert
        response.EnsureSuccessStatusCode();
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
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        var nonExistentId = Guid.NewGuid();

        // Act
        HttpResponseMessage response = await _client.GetAsync($"{REQUEST_URI}/{nonExistentId}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task GetContentTypeById_ReturnsFieldsWithValidationRules()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField(
                "Title",
                f =>
                    f.Required()
                        .WithValidationRule(
                            MaximumLengthValidationRule.TYPE_NAME,
                            new Dictionary<string, object> { ["max-length"] = 100 }
                        )
            )
            .BuildAsync("FormType");

        FieldDto titleField = contentType.Fields.First(f => f.Name == "Title");

        Assert.True(titleField.IsRequired);
        Assert.Single(titleField.ValidationRules);
        Assert.Equal(MaximumLengthValidationRule.TYPE_NAME, titleField.ValidationRules[0].Type);
    }

    [Fact]
    public async Task GetContentTypeById_ReturnsFieldsWithTransformationRules()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        var truncateRule = new TruncateByLengthTransformationRule(10);
        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField(
                "Title",
                f =>
                    f.WithTransformationRule(
                        TruncateByLengthTransformationRule.TYPE_NAME,
                        truncateRule.Parameters
                    )
            )
            .BuildAsync("TransformView");

        // Act
        FieldDto titleField = contentType.Fields.First(f => f.Name == "Title");

        // Assert
        Assert.NotNull(titleField.TransformationRules);
        Assert.Single(titleField.TransformationRules);
        Assert.Equal(
            TruncateByLengthTransformationRule.TYPE_NAME,
            titleField.TransformationRules[0].Type
        );
    }

    #endregion

    #region CreateContentType

    [Fact]
    public async Task CreateContentType_ReturnsForbidden_WhenAuthenticatedUserIsNotAdmin()
    {
        // Arrange
        string token = await AuthenticationHelper.GetDevUserAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        CreateContentTypeCommand command = TestDataHelper.CreateValidContentTypeCommand(
            "UnauthorizedType"
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentType_ReturnsBadRequest_WithUnknownTransformationRule()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        var command = new CreateContentTypeCommand(
            "TransformTest",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    Name: "Title",
                    Type: FieldTypes.Text,
                    IsRequired: true,
                    ValidationRules: null,
                    TransformationRules: new List<CreateTransformationRuleDto>
                    {
                        new CreateTransformationRuleDto("UnknownTransformer", null),
                    }
                ),
            }
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

        // Assert
        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task CreateContentType_Succeeds_WithValidTransformationRule()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        var command = new CreateContentTypeCommand(
            "TransformOk",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    Name: "Title",
                    Type: FieldTypes.Text,
                    IsRequired: true,
                    ValidationRules: null,
                    TransformationRules: new List<CreateTransformationRuleDto>
                    {
                        new CreateTransformationRuleDto("LowercaseTransformationRule", null),
                    }
                ),
            }
        );

        // Act
        HttpResponseMessage response = await _client.PostAsJsonAsync(REQUEST_URI, command);

        // Assert
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    #endregion

    #region  SoftDelete

    [Fact]
    public async Task SoftDeleteContentType_ReturnsNoContent_WhenExists()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("TempType");

        // Act
        HttpResponseMessage deleteResponse = await _client.DeleteAsync(
            $"{REQUEST_URI}/{contentType.Id}"
        );

        // Assert

        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage getResponse = await _client.GetAsync($"{REQUEST_URI}/{contentType.Id}");

        Assert.Equal(HttpStatusCode.NotFound, getResponse.StatusCode);
    }

    [Fact]
    public async Task SoftDeleteContentType_ReturnsNotFound_WhenDoesNotExist()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{REQUEST_URI}/{Guid.NewGuid()}");

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task DeleteContentType_ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange (admin creates content type)
        string adminToken = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(adminToken);

        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("ProtectedDelete");

        // Switch to non-admin
        string userToken = await AuthenticationHelper.GetDevUserAuthTokenAsync(_client);
        _client.AddAuthToken(userToken);

        // Act
        HttpResponseMessage response = await _client.DeleteAsync($"{REQUEST_URI}/{contentType.Id}");

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion


    #region Workflow
    [Fact]
    public async Task CompleteWorkflow_CreateGetListDelete_WorksCorrectly()
    {
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto contentType = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("WorkflowTest");

        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);
        Assert.NotNull(list);
        Assert.NotNull(list.Data);
        Assert.Contains(list.Data, ct => ct.Id == contentType.Id);

        HttpResponseMessage deleteResponse = await _client.DeleteAsync(
            $"{REQUEST_URI}/{contentType.Id}"
        );
        Assert.Equal(HttpStatusCode.NoContent, deleteResponse.StatusCode);

        HttpResponseMessage verifyResponse = await _client.GetAsync(
            $"{REQUEST_URI}/{contentType.Id}"
        );
        Assert.Equal(HttpStatusCode.NotFound, verifyResponse.StatusCode);
    }

    #endregion

    #region PublishContentType

    [Fact]
    public async Task PublishContentType_ReturnsNoContent_WhenDraftExists()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto draft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("BlogPost");

        // Act
        HttpResponseMessage response = await _client.PostAsync(
            $"{REQUEST_URI}/{draft.Name}/publish",
            null
        );

        // Assert
        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task PublishContentType_CreatesPublishedVersion_WithVersionOne()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        ContentTypeDto draft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("Article");

        // Act
        await _client.PostAsync($"{REQUEST_URI}/{draft.Name}/publish", null);

        // Assert
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        Assert.NotNull(list.Data);

        PagedContentType? published = list.Data.FirstOrDefault(ct =>
            ct.Name == draft.Name && ct.Status == "PUBLISHED"
        );

        Assert.NotNull(published);
        Assert.Equal(1, published.Version);
        Assert.Equal("PUBLISHED", published.Status);
    }

    [Fact]
    public async Task PublishContentType_IncrementsVersion_WhenPublishingMultipleTimes()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string contentTypeName = "Product";

        // Create and publish first version
        await ContentTypeBuilder.Create(_client).TextField("Title").BuildAsync(contentTypeName);
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Create and publish second version
        await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .TextField("Description")
            .BuildAsync(contentTypeName);
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Assert
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        Assert.NotNull(list.Data);

        PagedContentType? latestPublished = list
            .Data.Where(ct => ct.Name == contentTypeName && ct.Status == "PUBLISHED")
            .OrderByDescending(ct => ct.Version)
            .FirstOrDefault();

        Assert.NotNull(latestPublished);
        Assert.Equal(2, latestPublished.Version);
    }

    [Fact]
    public async Task PublishContentType_ArchivesPreviousPublication_WhenPublishingNewVersion()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string contentTypeName = "Event";

        // Create and publish first version
        ContentTypeDto firstDraft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync(contentTypeName);
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Create and publish second version
        await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .TextField("Location")
            .BuildAsync(contentTypeName);
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Assert - previous publication should be soft deleted (not in list)
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        Assert.NotNull(list.Data);

        var publishedVersions = list
            .Data.Where(ct => ct.Name == contentTypeName && ct.Status == "PUBLISHED")
            .ToList();

        // Should only have one published version (the latest)
        Assert.Single(publishedVersions);
        Assert.Equal(2, publishedVersions[0].Version);
    }

    [Fact]
    public async Task PublishContentType_ReturnsNotFound_WhenContentTypeDoesNotExist()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string nonExistentName = "NonExistentType";

        // Act
        HttpResponseMessage response = await _client.PostAsync(
            $"{REQUEST_URI}/{nonExistentName}/publish",
            null
        );

        // Assert
        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task PublishContentType_PreservesFieldsFromDraft()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string contentTypeName = "DetailedType";

        ContentTypeDto draft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title", f => f.Required())
            .TextField("Description")
            .BuildAsync(contentTypeName);

        // Act
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Assert - get the published version
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        PagedContentType? published = list.Data.FirstOrDefault(ct =>
            ct.Name == contentTypeName && ct.Status == "PUBLISHED"
        );

        Assert.NotNull(published);

        // Get full details to verify fields
        ContentTypeDto? publishedDetails = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"{REQUEST_URI}/{published.Id}"
        );

        Assert.NotNull(publishedDetails);
        Assert.Equal(2, publishedDetails.Fields.Count);
        Assert.Contains(publishedDetails.Fields, f => f.Name == "Title" && f.IsRequired);
        Assert.Contains(publishedDetails.Fields, f => f.Name == "Description");
    }

    [Fact]
    public async Task PublishContentType_PreservesTransformationRules()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        var truncateRule = new TruncateByLengthTransformationRule(10);

        string typeName = "TransformPublish";

        await ContentTypeBuilder
            .Create(_client)
            .TextField(
                "Title",
                f =>
                    f.WithTransformationRule(
                        TruncateByLengthTransformationRule.TYPE_NAME,
                        truncateRule.Parameters
                    )
            )
            .BuildAsync(typeName);

        // Act
        await _client.PostAsync($"{REQUEST_URI}/{typeName}/publish", null);

        // Assert
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        PagedContentType published = list!.Data.First(ct =>
            ct.Name == typeName && ct.Status == "PUBLISHED"
        );

        ContentTypeDto publishedDetails = await _client.GetFromJsonAsync<ContentTypeDto>(
            $"{REQUEST_URI}/{published.Id}"
        )!;

        FieldDto titleField = publishedDetails.Fields.First(f => f.Name == "Title");

        Assert.Single(titleField.TransformationRules);
        Assert.Equal(
            TruncateByLengthTransformationRule.TYPE_NAME,
            titleField.TransformationRules[0].Type
        );
    }

    [Fact]
    public async Task PublishContentType_CreatesNewId_ForPublishedVersion()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string contentTypeName = "UniqueIdTest";

        ContentTypeDto draft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync(contentTypeName);

        // Act
        await _client.PostAsync($"{REQUEST_URI}/{contentTypeName}/publish", null);

        // Assert
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        PagedContentType? published = list.Data.FirstOrDefault(ct =>
            ct.Name == contentTypeName && ct.Status == "PUBLISHED"
        );

        Assert.NotNull(published);
        Assert.NotEqual(draft.Id, published.Id);
    }

    [Fact]
    public async Task PublishContentType_WorksInCompletePublishingWorkflow()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);
        string contentTypeName = "CompleteWorkflow";

        // Act & Assert - Create initial draft
        ContentTypeDto draft1 = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync(contentTypeName);
        Assert.Equal("DRAFT", draft1.Status);
        Assert.Equal(1, draft1.Version);

        // Publish first version
        HttpResponseMessage publish1 = await _client.PostAsync(
            $"{REQUEST_URI}/{contentTypeName}/publish",
            null
        );
        Assert.Equal(HttpStatusCode.NoContent, publish1.StatusCode);

        // Verify published version exists
        PagedResponse<PagedContentType>? list1 = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);
        Assert.NotNull(list1);
        Assert.Contains(
            list1.Data,
            ct => ct.Name == contentTypeName && ct.Status == "PUBLISHED" && ct.Version == 1
        );

        // Create new draft
        ContentTypeDto draft2 = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .TextField("Content")
            .BuildAsync(contentTypeName);
        Assert.Equal("DRAFT", draft2.Status);
        Assert.Equal(2, draft2.Version);

        // Publish second version
        HttpResponseMessage publish2 = await _client.PostAsync(
            $"{REQUEST_URI}/{contentTypeName}/publish",
            null
        );
        Assert.Equal(HttpStatusCode.NoContent, publish2.StatusCode);

        // Verify only latest published version is visible
        PagedResponse<PagedContentType>? list2 = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);
        Assert.NotNull(list2);

        var allPublished = list2
            .Data.Where(ct => ct.Name == contentTypeName && ct.Status == "PUBLISHED")
            .ToList();

        Assert.Single(allPublished);
        Assert.Equal(2, allPublished[0].Version);
    }

    [Fact]
    public async Task PublishContentType_HandlesMultipleContentTypes_Independently()
    {
        // Arrange
        string token = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(token);

        await ContentTypeBuilder.Create(_client).TextField("Title").BuildAsync("TypeA");

        await ContentTypeBuilder.Create(_client).TextField("Name").BuildAsync("TypeB");

        // Act
        await _client.PostAsync($"{REQUEST_URI}/TypeA/publish", null);
        await _client.PostAsync($"{REQUEST_URI}/TypeB/publish", null);

        // Assert
        PagedResponse<PagedContentType>? list = await _client.GetFromJsonAsync<
            PagedResponse<PagedContentType>
        >(REQUEST_URI);

        Assert.NotNull(list);
        Assert.Contains(
            list.Data,
            ct => ct.Name == "TypeA" && ct.Status == "PUBLISHED" && ct.Version == 1
        );
        Assert.Contains(
            list.Data,
            ct => ct.Name == "TypeB" && ct.Status == "PUBLISHED" && ct.Version == 1
        );
    }

    [Fact]
    public async Task PublishContentType_ReturnsForbidden_WhenUserIsNotAdmin()
    {
        // Arrange (admin creates draft)
        string adminToken = await AuthenticationHelper.GetAdminAuthTokenAsync(_client);
        _client.AddAuthToken(adminToken);

        ContentTypeDto draft = await ContentTypeBuilder
            .Create(_client)
            .TextField("Title")
            .BuildAsync("ProtectedPublish");

        // Switch to non-admin
        string userToken = await AuthenticationHelper.GetDevUserAuthTokenAsync(_client);
        _client.AddAuthToken(userToken);

        // Act
        HttpResponseMessage response = await _client.PostAsync(
            $"{REQUEST_URI}/{draft.Name}/publish",
            null
        );

        // Assert
        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    #endregion

    // Helper class to deserialize the response
    private class CreatedResponse
    {
        public Guid Id { get; set; }
    }
}
