using E2E.Tests.Config;
using E2E.Tests.Fixtures;
using E2E.Tests.Helpers;
using Microsoft.Playwright;

namespace E2E.Tests.Auth;

public class LoginE2ETests : E2ETestBase
{
    public LoginE2ETests(E2ECollectionFixture fixture)
        : base(fixture) { }

    [Fact]
    public async Task Login_WithValidCredentials_RedirectsToDashboard()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForSelectorAsync("#username");

        await Page.FillAsync("#username", E2EEnv.Require("E2E_ADMIN_EMAIL"));
        await Page.FillAsync("#password", E2EEnv.Require("E2E_ADMIN_PASSWORD"));
        await Page.ClickAsync($"button:has-text('{T.Auth.Submit}')");

        await Page.WaitForURLAsync(
            url => !url.Contains("/login"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.DoesNotContain("/login", Page.Url);
    }

    [Fact]
    public async Task Login_WithWrongPassword_ShowsInvalidCredentialsError()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForSelectorAsync("#username");

        await Page.FillAsync("#username", E2EEnv.Require("E2E_ADMIN_EMAIL"));
        await Page.FillAsync("#password", "WrongPassword@999");
        await Page.ClickAsync($"button:has-text('{T.Auth.Submit}')");

        await Assertions
            .Expect(Page.GetByText(T.Auth.InvalidCredentials))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5_000 });
    }

    [Fact]
    public async Task Login_WithWrongEmail_ShowsInvalidCredentialsError()
    {
        await Page.GotoAsync("/login");
        await Page.WaitForSelectorAsync("#username");

        await Page.FillAsync("#username", "nonexistent@example.com");
        await Page.FillAsync("#password", E2EEnv.Require("E2E_ADMIN_PASSWORD"));
        await Page.ClickAsync($"button:has-text('{T.Auth.Submit}')");

        await Assertions
            .Expect(Page.GetByText(T.Auth.InvalidCredentials))
            .ToBeVisibleAsync(new LocatorAssertionsToBeVisibleOptions { Timeout = 5_000 });
    }

    [Fact]
    public async Task Login_AlreadyAuthenticated_RedirectsAwayFromLoginPage()
    {
        await AuthHelper.LoginAsAdminAsync(Page);

        await Page.GotoAsync("/login");

        await Page.WaitForURLAsync(
            url => !url.Contains("/login"),
            new PageWaitForURLOptions { Timeout = 5_000 }
        );

        Assert.DoesNotContain("/login", Page.Url);
    }

    [Fact]
    public async Task UnauthenticatedAccess_ToProtectedRoute_RedirectsToLoginWithReturnUrl()
    {
        await Page.GotoAsync("/content-types");

        await Page.WaitForURLAsync(
            url => url.Contains("/login"),
            new PageWaitForURLOptions { Timeout = 5_000 }
        );

        Assert.Contains("/login", Page.Url);
        // The redirect param must encode the original destination so the user
        // is returned there after authenticating.
        Assert.Contains(Uri.EscapeDataString("/content-types"), Page.Url);
    }

    [Fact]
    public async Task Login_FromProtectedRoute_RedirectsBackAfterLogin()
    {
        await Page.GotoAsync("/content-types");

        await Page.WaitForURLAsync(
            url => url.Contains("/login"),
            new PageWaitForURLOptions { Timeout = 5_000 }
        );

        await Page.FillAsync("#username", E2EEnv.Require("E2E_ADMIN_EMAIL"));
        await Page.FillAsync("#password", E2EEnv.Require("E2E_ADMIN_PASSWORD"));
        await Page.ClickAsync($"button:has-text('{T.Auth.Submit}')");

        await Page.WaitForURLAsync(
            url => url.Contains("/content-types"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );

        Assert.Contains("/content-types", Page.Url);
    }
}
