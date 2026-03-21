using E2E.Tests.Infrastructure;
using Microsoft.Playwright;

namespace E2E.Tests.Fixtures;

/// <summary>
/// Collection fixture shared across all E2E test classes.
/// Manages expensive startup: PostgreSQL container, API Kestrel server, Vite dev server,
/// and the Playwright browser instance.
/// </summary>
[CollectionDefinition(E2ECollectionFixture.NAME)]
public class E2ECollection : ICollectionFixture<E2ECollectionFixture> { }

public sealed class E2ECollectionFixture : IAsyncLifetime
{
    public const string NAME = "E2E";

    public E2EWebApplicationFactory Factory { get; } = new();
    public IBrowser Browser { get; private set; } = null!;

    private IPlaywright _playwright = null!;
    private ViteDevServer _viteServer = null!;

    public async Task InitializeAsync()
    {
        // Start the API (PostgreSQL + Kestrel on port 5252)
        await Factory.InitializeAsync();

        // Start the Vite dev server (reads .env.e2e → VITE_CMS_API_URL=http://localhost:5252)
        _viteServer = await ViteDevServer.StartAsync();

        // Launch Playwright and a shared browser
        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(new BrowserTypeLaunchOptions
        {
            Headless = true,
        });
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
        {
            await Browser.DisposeAsync();
        }

        _playwright?.Dispose();
        if (_viteServer is not null)
        {
            await _viteServer.DisposeAsync();
        }

        await Factory.DisposeAsync();
    }
}
