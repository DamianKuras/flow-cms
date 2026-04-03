using Microsoft.Playwright;

namespace E2E.Tests.Fixtures;

/// <summary>
/// Collection fixture shared across all E2E test classes.
/// Starts the full Docker Compose E2E stack (db + backend + frontend)
/// and a shared Playwright browser instance.
/// </summary>
[CollectionDefinition(E2ECollectionFixture.NAME)]
public class E2ECollection : ICollectionFixture<E2ECollectionFixture> { }

public sealed class E2ECollectionFixture : IAsyncLifetime
{
    public const string NAME = "E2E";

    public DockerComposeFixture Docker { get; private set; } = null!;
    public IBrowser Browser { get; private set; } = null!;

    private IPlaywright _playwright = null!;

    public async Task InitializeAsync()
    {
        Docker = await DockerComposeFixture.StartAsync();

        _playwright = await Playwright.CreateAsync();
        Browser = await _playwright.Chromium.LaunchAsync(
            new BrowserTypeLaunchOptions { Headless = true }
        );
    }

    public async Task DisposeAsync()
    {
        if (Browser is not null)
            await Browser.DisposeAsync();

        _playwright?.Dispose();

        if (Docker is not null)
            await Docker.DisposeAsync();
    }
}
