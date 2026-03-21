using System.Net.Http.Json;
using System.Text.RegularExpressions;
using E2E.Tests.Fixtures;
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
        await Page.WaitForSelectorAsync("input[placeholder*='Blog Posts']");

        await Page.FillAsync("input[placeholder*='Blog Posts']", "Blog Posts");

        await Page.ClickAsync("text=Add field");
        // Wait for the first field row to appear
        await Page.WaitForSelectorAsync("[placeholder*='field name'], [placeholder*='Field name']");

        // Fill the first field's name — adjust selector to match FieldRow's actual input
        ILocator fieldNameInput = Page.Locator("input")
            .Filter(new LocatorFilterOptions { HasText = string.Empty })
            .Nth(1); // second input on page (after content type name)
        await fieldNameInput.FillAsync("Title");

        await Page.ClickAsync("[type=submit][form='create-content-type']");

        // After successful creation, the router navigates to /content-types/:id
        await Page.WaitForURLAsync(
            new Regex("/content-types/[^/]+$"),
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

        await Assertions.Expect(Page.GetByText("Articles")).ToBeVisibleAsync();
    }

    private static async Task<string> GetAdminTokenAsync(System.Net.Http.HttpClient client)
    {
        HttpResponseMessage res = await client.PostAsJsonAsync(
            "/auth/sign-in",
            new { email = "admin@admin.com", password = "Admin@123" }
        );
        res.EnsureSuccessStatusCode();
        SignInResult? data = await res.Content.ReadFromJsonAsync<SignInResult>();
        return data!.AccessToken;
    }

    private record SignInResult(string AccessToken);
}

