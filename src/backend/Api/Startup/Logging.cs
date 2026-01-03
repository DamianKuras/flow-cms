using Microsoft.Extensions.Primitives;
using Serilog;
using Serilog.Events;

namespace Api.Startup;

/// <summary>
/// Provides extension methods for configuring Serilog logging.
/// </summary>
public static class Logging
{
    /// <summary>
    /// Configures Serilog as the logging provider for the application.
    /// </summary>
    /// <param name="builder">The web application builder instance.</param>
    public static void ConfigureSerilog(this WebApplicationBuilder builder)
    {
        string logPath =
            builder.Configuration.GetValue<string>("Logging:FilePath") ?? "logs/log-.txt";
        int retainedFileCountLimit =
            builder.Configuration.GetValue<int?>("Logging:RetainedFileCountLimit") ?? 31;
        builder.Services.AddSerilog(
            (services, loggerConfiguration) =>
                loggerConfiguration
                    .ReadFrom.Configuration(builder.Configuration)
                    .ReadFrom.Services(services)
                    .Enrich.FromLogContext()
                    .Enrich.WithProperty("Environment", builder.Environment.EnvironmentName)
                    .WriteTo.Console(
                        outputTemplate: "[{Timestamp:HH:mm:ss} {Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
                    )
                    .WriteTo.File(
                        logPath,
                        rollingInterval: RollingInterval.Day,
                        retainedFileCountLimit: retainedFileCountLimit,
                        fileSizeLimitBytes: 100_000_000,
                        rollOnFileSizeLimit: true,
                        outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} [{Level:u3}] {Message:lj} {Properties:j}{NewLine}{Exception}"
                    ),
            preserveStaticLogger: true
        );
    }

    /// <summary>
    /// Configures HTTP request logging middleware with structured logging.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    public static void UseRequestLogging(this WebApplication app) =>
        app.UseSerilogRequestLogging(options =>
        {
            options.MessageTemplate =
                "HTTP {RequestMethod} {RequestPath} responded {StatusCode} in {Elapsed:0.0000} ms";

            options.GetLevel = GetLogLevel;

            options.EnrichDiagnosticContext = EnrichDiagnosticContext;
        });

    /// <summary>
    /// Determines the appropriate log level based on HTTP context and response.
    /// </summary>
    /// <param name="httpContext">The HTTP context.</param>
    /// <param name="elapsedMs">Request duration in milliseconds.</param>
    /// <param name="ex">Exception if request failed.</param>
    /// <returns>The log event level to use.</returns>
    private static LogEventLevel GetLogLevel(
        HttpContext httpContext,
        double elapsedMs,
        Exception? ex
    )
    {
        // Always log exceptions as errors.
        if (ex != null)
        {
            return LogEventLevel.Error;
        }

        int statusCode = httpContext.Response.StatusCode;

        return statusCode switch
        {
            >= 500 => LogEventLevel.Error, // Server errors
            >= 400 when statusCode != 401 => LogEventLevel.Warning, // Client errors (except auth)
            401 => LogEventLevel.Information,
            _ => LogEventLevel.Information, // Success responses
        };
    }

    /// <summary>
    /// Enriches the diagnostic context with additional request information.
    /// </summary>
    /// <param name="diagnosticContext">The diagnostic context to enrich.</param>
    /// <param name="httpContext">The HTTP context.</param>
    private static void EnrichDiagnosticContext(
        IDiagnosticContext diagnosticContext,
        HttpContext httpContext
    )
    {
        HttpRequest request = httpContext.Request;
        HttpResponse response = httpContext.Response;

        // Request information.
        diagnosticContext.Set("RequestHost", request.Host.Value);
        diagnosticContext.Set("RequestScheme", request.Scheme);
        diagnosticContext.Set("RequestMethod", request.Method);
        diagnosticContext.Set("RequestContentType", request.ContentType);
        diagnosticContext.Set("RequestContentLength", request.ContentLength);

        // Client information.
        diagnosticContext.Set("ClientIp", httpContext.Connection.RemoteIpAddress?.ToString());
        diagnosticContext.Set("UserAgent", request.Headers.UserAgent.ToString());

        // Response information.
        diagnosticContext.Set("StatusCode", response.StatusCode);
        diagnosticContext.Set("ResponseContentType", response.ContentType);
    }
}
