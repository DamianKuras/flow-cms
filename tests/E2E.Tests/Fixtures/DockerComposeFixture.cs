using System.Diagnostics;
using System.Net;
using Npgsql;

namespace E2E.Tests.Fixtures;

/// <summary>
/// Manages the E2E stack lifecycle.
///
/// Attached mode (inside Docker): E2E_API_URL env var is set by docker-compose.e2e.yml.
///   Services are already running — just wait for readiness and reset the DB between tests.
///
/// Managed mode (local): no E2E_API_URL env var.
///   Starts the stack via docker-compose.e2e.yml and tears it down on dispose.
/// </summary>
public sealed class DockerComposeFixture : IAsyncDisposable
{
    private const string COMPOSE_FILE_NAME = "docker-compose.e2e.yml";
    private const int STARTUP_TIMEOUT_SECONDS = 300;

    public string ApiUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_API_URL") ?? "http://localhost:5252";

    public string FrontendUrl { get; } =
        Environment.GetEnvironmentVariable("E2E_FRONTEND_URL") ?? "http://localhost:5173";

    private string DbConnectionString { get; } =
        Environment.GetEnvironmentVariable("E2E_DB_CONNECTION_STRING")
        ?? "Host=localhost;Port=5433;Database=e2e_testdb;Username=testuser;Password=testpassword";

    private static bool IsAttachedMode =>
        !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("E2E_API_URL"));

    private readonly string? _composeFile;

    private DockerComposeFixture(string? composeFile) => _composeFile = composeFile;

    public static async Task<DockerComposeFixture> StartAsync()
    {
        if (IsAttachedMode)
        {
            var fixture = new DockerComposeFixture(null);
            await fixture.WaitForReadyAsync();
            return fixture;
        }

        string composeFile = FindComposeFile();
        var managed = new DockerComposeFixture(composeFile);
        await RunComposeAsync(composeFile, "up --build -d");
        await managed.WaitForReadyAsync();
        return managed;
    }

    public HttpClient CreateApiClient() => new() { BaseAddress = new Uri(ApiUrl) };

    public async Task ResetDatabaseAsync()
    {
        await using var conn = new NpgsqlConnection(DbConnectionString);
        await conn.OpenAsync();
        await using var cmd = new NpgsqlCommand(
            "TRUNCATE \"Fields\", content_items, content_types CASCADE;",
            conn
        );
        await cmd.ExecuteNonQueryAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (!IsAttachedMode && _composeFile is not null)
            await RunComposeAsync(_composeFile, "down -v");
    }

    private async Task WaitForReadyAsync()
    {
        using var http = new HttpClient();
        DateTime deadline = DateTime.UtcNow.AddSeconds(STARTUP_TIMEOUT_SECONDS);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync($"{ApiUrl}/health");
                if (response.StatusCode == HttpStatusCode.OK)
                    break;
            }
            catch { }

            await Task.Delay(1000);
        }

        if (DateTime.UtcNow >= deadline)
            throw new TimeoutException(
                $"Backend did not become healthy at {ApiUrl}/health within {STARTUP_TIMEOUT_SECONDS}s."
            );

        deadline = DateTime.UtcNow.AddSeconds(30);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync(FrontendUrl);
                if (response.StatusCode == HttpStatusCode.OK)
                    return;
            }
            catch { }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Frontend did not become ready at {FrontendUrl} within 30s after backend was healthy."
        );
    }

    private static async Task RunComposeAsync(string composeFile, string args)
    {
        var startInfo = new ProcessStartInfo
        {
            FileName = "docker",
            Arguments = $"compose -f \"{composeFile}\" {args}",
            UseShellExecute = false,
            RedirectStandardError = true,
            CreateNoWindow = true,
        };

        using var process = new Process { StartInfo = startInfo };
        process.Start();
        await process.WaitForExitAsync();

        if (process.ExitCode != 0)
        {
            string error = await process.StandardError.ReadToEndAsync();
            throw new InvalidOperationException($"docker compose {args} failed:\n{error}");
        }
    }

    private static string FindComposeFile()
    {
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, COMPOSE_FILE_NAME);
            if (File.Exists(candidate))
                return candidate;
            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new FileNotFoundException(
            $"Could not find '{COMPOSE_FILE_NAME}' walking up from '{AppContext.BaseDirectory}'."
        );
    }
}
