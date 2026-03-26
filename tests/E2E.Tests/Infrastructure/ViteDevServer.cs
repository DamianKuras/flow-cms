using System.Diagnostics;
using System.Net;

namespace E2E.Tests.Infrastructure;

/// <summary>
/// Manages the lifecycle of the Vite dev server for E2E tests.
/// Starts the server with --mode e2e so it picks up .env.e2e (VITE_CMS_API_URL=http://localhost:5252).
/// </summary>
public sealed class ViteDevServer : IAsyncDisposable
{
    private const string FRONTEND_URL = "http://localhost:5173";
    private const int STARTUP_TIMEOUT_SECONDS = 60;

    private readonly Process _process;

    private ViteDevServer(Process process) => _process = process;

    public static async Task<ViteDevServer> StartAsync()
    {
        string frontendDir = GetFrontendDirectory();

        var startInfo = new ProcessStartInfo
        {
            FileName = "npm",
            Arguments = "run dev -- --mode e2e",
            WorkingDirectory = frontendDir,
            UseShellExecute = false,
            // Do NOT redirect stdout/stderr — unread buffers would block the process
            CreateNoWindow = true,
        };

        // Windows requires cmd /c for npm
        if (OperatingSystem.IsWindows())
        {
            startInfo.FileName = "cmd.exe";
            startInfo.Arguments = "/c npm run dev -- --mode e2e";
        }

        var process = new Process { StartInfo = startInfo };
        process.Start();

        await WaitForReadyAsync();

        return new ViteDevServer(process);
    }

    private static async Task WaitForReadyAsync()
    {
        using var http = new HttpClient();
        DateTime deadline = DateTime.UtcNow.AddSeconds(STARTUP_TIMEOUT_SECONDS);

        while (DateTime.UtcNow < deadline)
        {
            try
            {
                HttpResponseMessage response = await http.GetAsync(FRONTEND_URL);
                if (response.StatusCode == HttpStatusCode.OK)
                {
                    return;
                }
            }
            catch
            {
                // Server not ready yet
            }

            await Task.Delay(500);
        }

        throw new TimeoutException(
            $"Vite dev server did not become ready at {FRONTEND_URL} within {STARTUP_TIMEOUT_SECONDS}s."
        );
    }

    private static string GetFrontendDirectory()
    {
        // Walk up from the test binary directory to the repo root, then into src/frontend
        string? dir = AppContext.BaseDirectory;
        while (dir is not null)
        {
            string candidate = Path.Combine(dir, "src", "frontend");
            if (Directory.Exists(candidate))
            {
                return candidate;
            }

            dir = Directory.GetParent(dir)?.FullName;
        }
        throw new DirectoryNotFoundException(
            "Could not locate src/frontend from test binary directory."
        );
    }

    public async ValueTask DisposeAsync()
    {
        if (!_process.HasExited)
        {
            if (OperatingSystem.IsWindows())
            {
                // taskkill /F /T reliably kills the entire tree on Windows,
                // including grandchildren spawned by cmd.exe → npm → node
                using var kill = Process.Start(
                    new ProcessStartInfo
                    {
                        FileName = "taskkill",
                        Arguments = $"/F /T /PID {_process.Id}",
                        UseShellExecute = false,
                        CreateNoWindow = true,
                    }
                );
                kill?.WaitForExit();
            }
            else
            {
                _process.Kill(entireProcessTree: true);
            }

            await _process.WaitForExitAsync();
        }

        _process.Dispose();
    }
}
