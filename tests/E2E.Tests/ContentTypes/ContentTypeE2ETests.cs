using System.Net.Http.Json;
using System.Text.Json;
using Application.Auth;
using E2E.Tests.Config;
using E2E.Tests.Fixtures;
using E2E.Tests.Helpers;
using Microsoft.Playwright;

namespace E2E.Tests.ContentTypes;

public class ContentTypeE2ETests : E2ETestBase
{
    public ContentTypeE2ETests(E2ECollectionFixture fixture)
        : base(fixture) { }

    #region POST /content-types (create form)

    [Fact]
    public async Task CreateContentType_NavigatesToDetailPage_AfterSubmit()
    {
        await LoginAsAdminAsync();

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Blog Posts"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.AddField}");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']",
            "Title"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.Field.TypePlaceholder}");
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Page.WaitForURLAsync(
            url => url.Contains("/content-types/") && !url.Contains("/new"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.Contains("/content-types/", Page.Url);
        Assert.DoesNotContain("/new", Page.Url);
    }

    [Fact]
    public async Task CreateContentType_DetailPage_ShowsCreatedContentType()
    {
        await LoginAsAdminAsync();

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Blog Posts"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.AddField}");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']",
            "Title"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.Field.TypePlaceholder}");
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Page.WaitForURLAsync(
            url => url.Contains("/content-types/") && !url.Contains("/new"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        await Assertions
            .Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Blog Posts" }))
            .ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByText("DRAFT", new() { Exact = true })).ToBeVisibleAsync();

        await Assertions
            .Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Title" }))
            .ToBeVisibleAsync();

        await Assertions.Expect(Page.GetByText("Type: Text")).ToBeVisibleAsync();
    }

    [Fact]
    public async Task CreateContentType_WithNoFields_ShowsValidationError()
    {
        await LoginAsAdminAsync();

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Empty Type"
        );

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Assertions
            .Expect(Page.GetByText("At least one field is required."))
            .ToBeVisibleAsync();

        Assert.Contains("/content-types/new", Page.Url);
    }

    [Fact]
    public async Task CreateContentType_ShowsValidationError_WhenNameTooShort()
    {
        await LoginAsAdminAsync();

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync($"input[placeholder='{T.ContentType.Create.NamePlaceholder}']", "Hi");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Assertions
            .Expect(Page.GetByText("Name must be at least 5 characters."))
            .ToBeVisibleAsync();

        Assert.Contains("/content-types/new", Page.Url);
    }

    [Fact]
    public async Task CreateContentType_ClearsErrorAlert_OnResubmit()
    {
        await LoginAsAdminAsync();

        bool failNext = true;
        await Context.RouteAsync(
            "**/content-types",
            async route =>
            {
                if (route.Request.Method == "POST" && failNext)
                {
                    failNext = false;
                    await route.FulfillAsync(new RouteFulfillOptions { Status = 500 });
                }
                else
                {
                    await route.ContinueAsync();
                }
            }
        );

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Blog Posts"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.AddField}");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']",
            "Title"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.Field.TypePlaceholder}");
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='create-content-type']");
        await Assertions.Expect(Page.GetByText(T.ContentType.Create.ErrorRetry)).ToBeVisibleAsync();

        await Page.ClickAsync("[type=submit][form='create-content-type']");
        await Assertions.Expect(Page.GetByText(T.ContentType.Create.ErrorRetry)).ToBeHiddenAsync();

        await Page.WaitForURLAsync(
            url => url.Contains("/content-types/") && !url.Contains("/new"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );
    }

    [Fact]
    public async Task CreateContentType_RetriesWithFreshToken_After401()
    {
        await LoginAsAdminAsync();

        bool firstCall = true;
        await Context.RouteAsync(
            "**/content-types",
            async route =>
            {
                if (route.Request.Method == "POST" && firstCall)
                {
                    firstCall = false;
                    await route.FulfillAsync(new RouteFulfillOptions { Status = 401 });
                }
                else
                {
                    await route.ContinueAsync();
                }
            }
        );

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Blog Posts"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.AddField}");
        // Wait for the first field row to appear
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']"
        );

        // Fill the first field's name
        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']",
            "Title"
        );

        // Select the field type (required by form validation)
        await Page.ClickAsync($"text={T.ContentType.Create.Field.TypePlaceholder}");
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Page.WaitForURLAsync(
            url => url.Contains("/content-types/") && !url.Contains("/new"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.Contains("/content-types/", Page.Url);
        Assert.DoesNotContain("/new", Page.Url);
    }

    [Fact]
    public async Task CreateContentType_ShowsErrorAlert_WhenApiReturnsError()
    {
        await LoginAsAdminAsync();

        await Context.RouteAsync(
            "**/content-types",
            async route =>
            {
                if (route.Request.Method == "POST")
                {
                    await route.FulfillAsync(new RouteFulfillOptions { Status = 500 });
                }
                else
                {
                    await route.ContinueAsync();
                }
            }
        );

        await Page.GotoAsync("/content-types/new");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.NamePlaceholder}']",
            "Blog Posts"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.AddField}");
        await Page.WaitForSelectorAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']"
        );

        await Page.FillAsync(
            $"input[placeholder='{T.ContentType.Create.Field.NamePlaceholder}']",
            "Title"
        );

        await Page.ClickAsync($"text={T.ContentType.Create.Field.TypePlaceholder}");
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Assertions.Expect(Page.GetByText(T.ContentType.Create.ErrorRetry)).ToBeVisibleAsync();

        Assert.Contains("/content-types/new", Page.Url);
    }

    #endregion

    #region GET /content-types (list)

    [Fact]
    public async Task ContentTypeList_ShowsCreatedContentType()
    {
        await LoginAsAdminAsync();

        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        var command = new
        {
            name = "Articles",
            fields = new[]
            {
                new
                {
                    name = "Title",
                    type = "Text",
                    isRequired = true,
                    validationRules = Array.Empty<object>(),
                    transformationRules = Array.Empty<object>(),
                },
            },
        };
        HttpResponseMessage response = await client.PostAsJsonAsync("/content-types", command);
        response.EnsureSuccessStatusCode();

        // Now verify via UI
        await Page.GotoAsync("/content-types");
        await Page.WaitForSelectorAsync("text=Articles");

        // Use role=cell to avoid matching sidebar navigation links
        await Assertions
            .Expect(Page.GetByRole(AriaRole.Cell, new() { Name = "Articles" }))
            .ToBeVisibleAsync();
    }

    #endregion

    #region Edit Draft

    [Fact]
    public async Task EditDraft_ModifyFieldName_SavesAndShowsUpdatedField()
    {
        await LoginAsAdminAsync();

        // Arrange
        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid id = await CreateDraftAsync(
            client,
            "Blog Posts",
            new { name = "Title", type = "Text", isRequired = false }
        );

        // Act
        await Page.GotoAsync($"/content-types/{id}/edit");
        await Page.WaitForSelectorAsync("[aria-label='Edit content type form']");

        ILocator nameInput = Page
            .GetByPlaceholder(T.ContentType.Create.Field.NamePlaceholder)
            .First;
        await nameInput.ClearAsync();
        await nameInput.FillAsync("Headline");

        await Page.ClickAsync("[type=submit][form='edit-content-type']");

        // Assert: wait for the detail page to render with the updated field name
        // (GetByText matches text nodes only, not input values, so this confirms navigation)
        await Assertions
            .Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Headline" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task EditDraft_AddNewField_ShowsNewFieldOnDetailPage()
    {
        await LoginAsAdminAsync();

        // Arrange
        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid id = await CreateDraftAsync(
            client,
            "Blog Posts",
            new { name = "Title", type = "Text", isRequired = false }
        );

        // Act
        await Page.GotoAsync($"/content-types/{id}/edit");
        await Page.WaitForSelectorAsync("[aria-label='Edit content type form']");

        await Page.ClickAsync("text=Add Field");

        // Wait for the new field row to appear before interacting with it.
        ILocator newNameInput = Page
            .GetByPlaceholder(T.ContentType.Create.Field.NamePlaceholder)
            .Nth(1);
        await newNameInput.WaitForAsync();
        await newNameInput.FillAsync("Summary");

        // Find the type combobox that still shows the placeholder (the new field's).
        ILocator newTypeCombobox = Page
            .GetByRole(AriaRole.Combobox)
            .Filter(new LocatorFilterOptions { HasText = T.ContentType.Create.Field.TypePlaceholder });
        await newTypeCombobox.ClickAsync();
        await Page.Keyboard.PressAsync("Enter");

        await Page.ClickAsync("[type=submit][form='edit-content-type']");

        // Assert: detail page shows the new field
        await Assertions
            .Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "Summary" }))
            .ToBeVisibleAsync(new() { Timeout = 15_000 });
    }

    [Fact]
    public async Task EditDraft_ShowsCannotEditAlert_WhenContentTypeIsPublished()
    {
        await LoginAsAdminAsync();

        // Arrange
        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await CreateDraftAsync(
            client,
            "Blog Posts",
            new { name = "Title", type = "Text", isRequired = false }
        );

        Guid publishedId = await PublishAndGetIdAsync(client, "Blog Posts");

        // Act: navigate directly to the published content type's edit URL
        await Page.GotoAsync($"/content-types/{publishedId}/edit");

        // Assert: cannot-edit guard is shown
        await Assertions.Expect(Page.GetByText("Cannot Edit")).ToBeVisibleAsync();
        await Assertions
            .Expect(Page.GetByText("Only draft content types can be edited"))
            .ToBeVisibleAsync();
    }

    #endregion

    #region Publish Draft

    [Fact]
    public async Task PublishDraft_FirstPublish_NavigatesToPublishedDetail()
    {
        await LoginAsAdminAsync();

        // Arrange
        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid id = await CreateDraftAsync(
            client,
            "Blog Posts",
            new { name = "Title", type = "Text", isRequired = false }
        );

        // Act: navigate to detail and click Publish
        await Page.GotoAsync($"/content-types/{id}");
        await Page.WaitForSelectorAsync("text=Edit Draft");

        await Page.ClickAsync("text=Publish");
        await Page.WaitForSelectorAsync($"text=Publish \"Blog Posts\"");

        await Page.ClickAsync("[role=dialog] button:has-text('Publish')");

        await Assertions
            .Expect(Page.GetByText("PUBLISHED", new() { Exact = true }))
            .ToBeVisibleAsync(new() { Timeout = 10_000 });
    }

    [Fact]
    public async Task EditDraftButton_IsNotVisible_ForPublishedContentType()
    {
        await LoginAsAdminAsync();

        // Arrange
        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        await CreateDraftAsync(
            client,
            "Blog Posts",
            new { name = "Title", type = "Text", isRequired = false }
        );

        Guid publishedId = await PublishAndGetIdAsync(client, "Blog Posts");

        // Navigate directly to the published content type's detail page
        await Page.GotoAsync($"/content-types/{publishedId}");
        await Page.WaitForSelectorAsync("h1");

        await Assertions
            .Expect(Page.GetByText("PUBLISHED", new() { Exact = true }))
            .ToBeVisibleAsync();
        await Assertions
            .Expect(Page.GetByRole(AriaRole.Link, new() { Name = "Edit Draft" }))
            .ToBeHiddenAsync();
    }

    #endregion

    private static async Task<Guid> CreateDraftAsync(HttpClient client, string name, object field)
    {
        var command = new { name, fields = new[] { field } };
        HttpResponseMessage response = await client.PostAsJsonAsync("/content-types", command);
        response.EnsureSuccessStatusCode();

        string location = response.Headers.Location!.ToString();
        return Guid.Parse(location.Split('/').Last());
    }

    private static async Task<Guid> PublishAndGetIdAsync(HttpClient client, string name)
    {
        HttpResponseMessage response = await client.PostAsJsonAsync(
            $"/content-types/{Uri.EscapeDataString(name)}/publish",
            new { MigrationMode = "Lazy" }
        );
        response.EnsureSuccessStatusCode();

        using JsonDocument doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
        return doc.RootElement.GetProperty("id").GetGuid();
    }

    private static async Task<string> GetAdminTokenAsync(HttpClient client)
    {
        HttpResponseMessage res = await client.PostAsJsonAsync(
            "/auth/sign-in",
            new
            {
                email = E2EEnv.Require("E2E_ADMIN_EMAIL"),
                password = E2EEnv.Require("E2E_ADMIN_PASSWORD"),
            }
        );
        res.EnsureSuccessStatusCode();
        SignInResponse? data = await res.Content.ReadFromJsonAsync<SignInResponse>();
        return data!.AccessToken;
    }
}
