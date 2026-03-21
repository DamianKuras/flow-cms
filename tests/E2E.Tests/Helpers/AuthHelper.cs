using E2E.Tests.Fixtures;
using Microsoft.Playwright;

namespace E2E.Tests.Helpers;

/// <summary>
/// Helpers for authenticating a browser context in E2E tests.
/// Calls the API directly to sign in (sets the HttpOnly refresh cookie),
/// then sets the token expiry in localStorage so the frontend restores the session.
/// </summary>
public static class AuthHelper
{
    private const string AdminEmail = "admin@admin.com";
    private const string AdminPassword = "Admin@123";
    private const string AccessTokenExpiryKey = "access-token-expiry";

    /// <summary>
    /// Logs in as admin and navigates to the dashboard.
    /// Uses the login UI form so the full auth flow is exercised.
    /// </summary>
    public static async Task LoginAsAdminAsync(IPage page)
    {
        await page.GotoAsync("http://localhost:5173/login");
        await page.WaitForSelectorAsync("#username");

        await page.FillAsync("#username", AdminEmail);
        await page.FillAsync("#password", AdminPassword);
        await page.ClickAsync("[type=submit]");

        // Wait until the app redirects away from /login
        await page.WaitForURLAsync(url => !url.Contains("/login"), new PageWaitForURLOptions
        {
            Timeout = 10_000,
        });
    }

    /// <summary>
    /// Saves the authenticated browser context state to a file.
    /// Use this after <see cref="LoginAsAdminAsync"/> to avoid logging in for every test.
    /// </summary>
    public static Task SaveStateAsync(IBrowserContext context, string path) =>
        context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
}
