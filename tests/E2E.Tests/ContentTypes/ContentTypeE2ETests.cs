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
    public async Task ContentTypeList_ShowsCreatedContentType()
    {
        await LoginAsAdminAsync();

        // Seed a content type via the API client (faster and more reliable than UI)
        HttpClient client = _fixture.Factory.CreateClient();
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
