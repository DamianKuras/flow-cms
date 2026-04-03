using System.Net.Http.Json;
using System.Text.RegularExpressions;
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
        await Page.ClickAsync("[role=option]:has-text('Text')");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Page.WaitForURLAsync(
            new Regex(@"/content-types/[0-9a-f\-]+$"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.Matches(@"/content-types/[0-9a-f-]+", Page.Url);
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
        await Page.ClickAsync("[role=option]:has-text('Text')");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        await Page.WaitForURLAsync(
            new Regex(@"/content-types/[0-9a-f\-]+$"),
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
        await Page.ClickAsync("[role=option]:has-text('Text')");

        await Page.ClickAsync("[type=submit][form='create-content-type']");
        await Assertions.Expect(Page.GetByText(T.ContentType.Create.ErrorRetry)).ToBeVisibleAsync();

        await Page.ClickAsync("[type=submit][form='create-content-type']");
        await Assertions.Expect(Page.GetByText(T.ContentType.Create.ErrorRetry)).ToBeHiddenAsync();

        await Page.WaitForURLAsync(
            new Regex(@"/content-types/[0-9a-f\-]+$"),
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
        await Page.ClickAsync("[role=option]:has-text('Text')");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        // After successful creation, the router navigates to /content-types/:id
        await Page.WaitForURLAsync(
            new Regex(@"/content-types/[0-9a-f\-]+$"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.Matches(@"/content-types/[0-9a-f-]+", Page.Url);
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
        await Page.ClickAsync("[role=option]:has-text('Text')");

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
