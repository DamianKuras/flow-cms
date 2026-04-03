using E2E.Tests.Fixtures;
using E2E.Tests.Helpers;
using Microsoft.Playwright;

namespace E2E.Tests;

[Collection(E2ECollectionFixture.NAME)]
public abstract class E2ETestBase : IAsyncLifetime
{
    protected readonly E2ECollectionFixture _fixture;
    protected IBrowserContext Context { get; private set; } = null!;
    protected IPage Page { get; private set; } = null!;

    protected E2ETestBase(E2ECollectionFixture fixture) => _fixture = fixture;

    public async Task InitializeAsync()
    {
        await _fixture.Docker.ResetDatabaseAsync();

        Context = await _fixture.Browser.NewContextAsync(
            new BrowserNewContextOptions
            {
                BaseURL = _fixture.Docker.FrontendUrl,
                ViewportSize = new ViewportSize { Width = 1280, Height = 720 },
            }
        );

        await Context.Tracing.StartAsync(new TracingStartOptions
        {
            Screenshots = true,
            Snapshots = true,
            Sources = false,
        });

        Page = await Context.NewPageAsync();

        Page.Console += (_, msg) =>
        {
            if (msg.Type is "error" or "warning")
                Console.WriteLine($"[browser {msg.Type}] {msg.Text}");
        };
    }

    public async Task DisposeAsync()
    {
        await Page.CloseAsync();

        string traceDir = Environment.GetEnvironmentVariable("PLAYWRIGHT_TRACES_DIR") ?? "/tmp/playwright-traces";
        Directory.CreateDirectory(traceDir);
        string tracePath = Path.Combine(traceDir, $"{GetType().Name}-{DateTime.UtcNow:yyyyMMdd-HHmmss-fff}.zip");
        await Context.Tracing.StopAsync(new TracingStopOptions { Path = tracePath });

        await Context.CloseAsync();
    }

    /// <summary>Logs in as admin via the login form.</summary>
    protected Task LoginAsAdminAsync() => AuthHelper.LoginAsAdminAsync(Page);
}
