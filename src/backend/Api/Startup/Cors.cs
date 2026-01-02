namespace Api.Startup;

/// <summary>
/// Provides extension methods for configuring Cross-Origin Resource Sharing (CORS) policies.
/// </summary>
public static class Cors
{
    private const string POLICY_NAME = "AllowSpecific";
    private const string CONFIG_SECTION = "Cors:AllowedOrigins";

    /// <summary>
    /// Configures CORS policy based on allowed origins specified in application configuration.
    /// Reads from 'Cors:AllowedOrigins' configuration section.
    /// </summary>
    /// <param name="builder">The web application builder to configure.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when no allowed origins are configured or configuration section is missing.
    /// </exception>
    public static void ConfigureCors(this WebApplicationBuilder builder)
    {
        string[] origins = builder.Configuration.GetSection(CONFIG_SECTION).Get<string[]>() ?? [];

        if (origins.Length == 0)
        {
            throw new InvalidOperationException(
                $"No CORS origins configured. Please add origins to '{CONFIG_SECTION}' configuration section."
            );
        }
        builder.Services.AddCors(options =>
            options.AddPolicy(
                POLICY_NAME,
                policy =>
                    policy.WithOrigins(origins).AllowAnyMethod().AllowAnyHeader().AllowCredentials()
            )
        );
    }

    /// <summary>
    /// Applies the configured CORS policy to the application pipeline.
    /// Must be called after <see cref="ConfigureCors"/> and before authorization middleware.
    /// </summary>
    /// <param name="app">The web application to configure.</param>
    public static void UseCorsPolicy(this WebApplication app) => app.UseCors(POLICY_NAME);
}
