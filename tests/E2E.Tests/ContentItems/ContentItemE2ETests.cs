using System.Net.Http.Json;
using Application.Auth;
using E2E.Tests.Config;
using E2E.Tests.Fixtures;
using E2E.Tests.Helpers;
using Microsoft.Playwright;

namespace E2E.Tests.ContentItems;

public class ContentItemE2ETests : E2ETestBase
{
    public ContentItemE2ETests(E2ECollectionFixture fixture)
        : base(fixture) { }

    #region GET /content-items/:id (detail page)

    [Fact]
    public async Task ContentItem_DetailPage_ShowsItemName()
    {
        await LoginAsAdminAsync();

        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid contentTypeId = await CreateContentTypeAsync(client);
        Guid itemId = await CreateContentItemAsync(client, contentTypeId, "My Test Article");

        await Page.GotoAsync($"/content-items/{itemId}");

        await Assertions
            .Expect(Page.GetByRole(AriaRole.Heading, new() { Name = "My Test Article" }))
            .ToBeVisibleAsync();
    }

    #endregion

    #region DELETE /content-items/:id

    [Fact]
    public async Task ContentItem_Delete_RedirectsToHome()
    {
        await LoginAsAdminAsync();

        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid contentTypeId = await CreateContentTypeAsync(client);
        Guid itemId = await CreateContentItemAsync(client, contentTypeId, "Item To Delete");

        await Page.GotoAsync($"/content-items/{itemId}");
        await Page.WaitForSelectorAsync("text=Item To Delete");

        Page.Dialog += async (_, dialog) => await dialog.AcceptAsync();
        await Page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();

        string idStr = itemId.ToString();
        await Page.WaitForFunctionAsync(
            $"!window.location.href.includes('{idStr}')",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 }
        );

        Assert.Equal("/", new Uri(Page.Url).AbsolutePath);
    }

    #endregion

    #region PATCH /content-items/:id (edit page)

    [Fact]
    public async Task ContentItem_EditPage_SavesAndNavigatesToDetail()
    {
        await LoginAsAdminAsync();

        using HttpClient client = _fixture.Docker.CreateApiClient();
        string token = await GetAdminTokenAsync(client);
        client.DefaultRequestHeaders.Authorization =
            new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", token);

        Guid contentTypeId = await CreateContentTypeAsync(client);
        Guid itemId = await CreateContentItemAsync(client, contentTypeId, "Edit Test Item");

        await Page.GotoAsync($"/content-items/{itemId}/edit");
        await Page.WaitForSelectorAsync("input#title");

        await Page.FillAsync("input#title", "Updated Title");
        await Page.GetByRole(AriaRole.Button, new() { Name = "Save Changes" }).ClickAsync();

        string idStr = itemId.ToString();
        await Page.WaitForFunctionAsync(
            $"window.location.href.includes('{idStr}') && !window.location.href.includes('edit')",
            null,
            new PageWaitForFunctionOptions { Timeout = 10_000 }
        );

        Assert.DoesNotContain("edit", Page.Url);
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

    private static async Task<Guid> CreateContentTypeAsync(HttpClient client)
    {
        var command = new
        {
            name = "Articles",
            fields = new[]
            {
                new
                {
                    name = "Body",
                    type = "Text",
                    isRequired = false,
                    validationRules = Array.Empty<object>(),
                    transformationRules = Array.Empty<object>(),
                },
            },
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/content-types", command);
        response.EnsureSuccessStatusCode();
        CreatedResponse? data = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return data!.Id;
    }

    private static async Task<Guid> CreateContentItemAsync(
        HttpClient client,
        Guid contentTypeId,
        string title
    )
    {
        var command = new
        {
            title,
            contentTypeId,
            values = new Dictionary<string, object>(),
        };

        HttpResponseMessage response = await client.PostAsJsonAsync("/content-items", command);
        response.EnsureSuccessStatusCode();
        CreatedResponse? data = await response.Content.ReadFromJsonAsync<CreatedResponse>();
        return data!.Id;
    }

    private record CreatedResponse(Guid Id);
}
