using System.Text.Json;
using System.Text.Json.Serialization;

namespace Api.Startup;

/// <summary>
/// Provides extension methods for configuring JSON serialization.
/// </summary>
public static class Json
{
    /// <summary>
    /// Configures JSON serialization options.
    /// </summary>
    /// <param name="builder">The web application builder instance.</param>
    public static void ConfigureJson(this WebApplicationBuilder builder)
    {
        bool isDevelopment = builder.Environment.IsDevelopment();
        builder.Services.ConfigureHttpJsonOptions(options =>
        {
            options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
            options.SerializerOptions.PropertyNameCaseInsensitive = true;

            // Indent JSON in development for readability.
            options.SerializerOptions.WriteIndented = isDevelopment;

            // Ignore null values in JSON responses.
            options.SerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;

            // Serialize enums as strings instead of numbers.
            options.SerializerOptions.Converters.Add(
                new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
            );
        });
    }
}
