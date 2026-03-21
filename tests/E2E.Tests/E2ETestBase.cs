using E2E.Tests.Fixtures;
using E2E.Tests.Helpers;
using Microsoft.Playwright;

namespace E2E.Tests;

/// <summary>
/// Base class for all E2E tests.
/// Creates a fresh browser context and page per test, resets the database,
/// and provides a login helper.
/// </summary>
[Collection(E2ECollectionFixture.NAME)]
public abstract class E2ETestBase : IAsyncLifetime
{
    protected readonly E2ECollectionFixture _fixture;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    protected E2ETestBase(E2ECollectionFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.Factory.ResetDatabaseAsync();

        Context = await _fixture.Browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = "http://localhost:5173",
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            }
        );

        Page = await Context.NewPageAsync();
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();
        await Context.CloseAsync();
    }

    /// <summary>Logs in as admin via the login form.</summary>
    protected Task LoginAsAdminAsync() => AuthHelper.LoginAsAdminAsync(Page);
}

