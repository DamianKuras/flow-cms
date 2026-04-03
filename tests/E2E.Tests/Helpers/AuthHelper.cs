using E2E.Tests.Config;
using Microsoft.Playwright;

namespace E2E.Tests.Helpers;

/// <summary>
/// Helpers for authenticating a browser context in E2E tests.
/// Credentials are loaded from tests/E2E.Tests/.env (see .env.example).
/// </summary>
public static class AuthHelper
{
    /// <summary>
    /// Logs in as admin and navigates to the dashboard.
    /// Uses the login UI form so the full auth flow is exercised.
    /// </summary>
    public static async Task LoginAsAdminAsync(IPage page)
    {
        string email = E2EEnv.Require("E2E_ADMIN_EMAIL");
        string password = E2EEnv.Require("E2E_ADMIN_PASSWORD");

        await page.GotoAsync("/login");
        await page.WaitForSelectorAsync("#username");

        await page.FillAsync("#username", email);
        await page.FillAsync("#password", password);
        await page.ClickAsync($"button:has-text('{T.Auth.Submit}')");

        // Wait until the app redirects away from /login
        await page.WaitForURLAsync(
            url => !url.Contains("/login"),
            new PageWaitForURLOptions { Timeout = 10_000 }
        );
    }

    /// <summary>
    /// Saves the authenticated browser context state to a file.
    /// Use this after <see cref="LoginAsAdminAsync"/> to avoid logging in for every test.
    /// </summary>
    public static Task SaveStateAsync(IBrowserContext context, string path) =>
        context.StorageStateAsync(new BrowserContextStorageStateOptions { Path = path });
}
