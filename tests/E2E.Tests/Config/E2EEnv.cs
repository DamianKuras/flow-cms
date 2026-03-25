namespace E2E.Tests.Config;

/// <summary>
/// Loads E2E test configuration from a .env file (copied to the build output directory)
/// with a fallback to system environment variables.
/// </summary>
public static class E2EEnv
{
    private static readonly Dictionary<string, string> _fileValues = LoadDotEnv();

    private static Dictionary<string, string> LoadDotEnv()
    {
        string envFile = Path.Combine(AppContext.BaseDirectory, ".env");
        if (!File.Exists(envFile))
        {
            return [];
        }

        return File.ReadAllLines(envFile)
            .Where(line => !string.IsNullOrWhiteSpace(line) && !line.TrimStart().StartsWith('#'))
            .Select(line => line.Split('=', 2))
            .Where(parts => parts.Length == 2)
            .ToDictionary(parts => parts[0].Trim(), parts => parts[1].Trim());
    }

    /// <summary>
    /// Returns the value for <paramref name="key"/>, checking the .env file first,
    /// then system environment variables.
    /// Throws <see cref="InvalidOperationException"/> with a clear message if not found.
    /// </summary>
    public static string Require(string key)
    {
        if (_fileValues.TryGetValue(key, out string? fileValue) && !string.IsNullOrEmpty(fileValue))
        {
            return fileValue;
        }

        string? envValue = Environment.GetEnvironmentVariable(key);
        if (!string.IsNullOrEmpty(envValue))
        {
            return envValue;
        }

        throw new InvalidOperationException(
            $"Required E2E configuration key '{key}' is not set. "
                + $"Add it to tests/E2E.Tests/.env (copy from .env.example)."
        );
    }
}
