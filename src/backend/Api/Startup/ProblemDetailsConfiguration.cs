using System.Diagnostics;
using Microsoft.AspNetCore.Http.Features;

namespace Api.Startup;

/// <summary>
/// Provides extension methods for configuring RFC 9457 Problem Details responses.
/// </summary>
public static class ProblemDetailsConfiguration
{
    /// <summary>
    /// Configures Problem Details for standardized error responses across the API.
    /// </summary>
    /// <param name="builder">The web application builder instance.</param>
    public static void ConfigureProblemDetails(this WebApplicationBuilder builder)
    {
        bool isDevelopment = builder.Environment.IsDevelopment();

        builder.Services.AddProblemDetails(options =>
            options.CustomizeProblemDetails = context =>
            {
                HttpContext httpContext = context.HttpContext;
                Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails = context.ProblemDetails;

                AddStandardMetadata(problemDetails, httpContext);

                // Development-only exception details.
                if (isDevelopment && context.Exception != null)
                {
                    AddExceptionDetails(context, problemDetails);
                }
                // Auto-fill RFC 9110 compliant type URI if missing.
                if (string.IsNullOrEmpty(problemDetails.Type) && problemDetails.Status.HasValue)
                {
                    SetDefaultTypeUri(problemDetails);
                }

                // Auto-fill title if missing (based on status code).
                if (string.IsNullOrEmpty(problemDetails.Title) && problemDetails.Status.HasValue)
                {
                    SetDefaultTitle(problemDetails);
                }
            }
        );
    }

    private static void SetDefaultTitle(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails) =>
        problemDetails.Title = (problemDetails.Status ?? 500) switch
        {
            400 => "Bad Request",
            401 => "Unauthorized",
            403 => "Forbidden",
            404 => "Not Found",
            405 => "Method Not Allowed",
            406 => "Not Acceptable",
            408 => "Request Timeout",
            409 => "Conflict",
            410 => "Gone",
            415 => "Unsupported Media Type",
            422 => "Unprocessable Entity",
            429 => "Too Many Requests",
            500 => "Internal Server Error",
            501 => "Not Implemented",
            502 => "Bad Gateway",
            503 => "Service Unavailable",
            504 => "Gateway Timeout",
            _ => "An error occurred",
        };

    private static void SetDefaultTypeUri(Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails) =>
        problemDetails.Type = problemDetails.Status.Value switch
        {
            400 => "https://tools.ietf.org/html/rfc9110#section-15.5.1", // Bad Request
            401 => "https://tools.ietf.org/html/rfc9110#section-15.5.2", // Unauthorized
            403 => "https://tools.ietf.org/html/rfc9110#section-15.5.4", // Forbidden
            404 => "https://tools.ietf.org/html/rfc9110#section-15.5.5", // Not Found
            405 => "https://tools.ietf.org/html/rfc9110#section-15.5.6", // Method Not Allowed
            406 => "https://tools.ietf.org/html/rfc9110#section-15.5.7", // Not Acceptable
            408 => "https://tools.ietf.org/html/rfc9110#section-15.5.9", // Request Timeout
            409 => "https://tools.ietf.org/html/rfc9110#section-15.5.10", // Conflict
            410 => "https://tools.ietf.org/html/rfc9110#section-15.5.11", // Gone
            415 => "https://tools.ietf.org/html/rfc9110#section-15.5.16", // Unsupported Media Type
            422 => "https://tools.ietf.org/html/rfc4918#section-11.2", // Unprocessable Entity
            429 => "https://tools.ietf.org/html/rfc6585#section-4", // Too Many Requests
            500 => "https://tools.ietf.org/html/rfc9110#section-15.6.1", // Internal Server Error
            501 => "https://tools.ietf.org/html/rfc9110#section-15.6.2", // Not Implemented
            502 => "https://tools.ietf.org/html/rfc9110#section-15.6.3", // Bad Gateway
            503 => "https://tools.ietf.org/html/rfc9110#section-15.6.4", // Service Unavailable
            504 => "https://tools.ietf.org/html/rfc9110#section-15.6.5", // Gateway Timeout
            _ => $"https://httpstatuses.com/{problemDetails.Status.Value}",
        };

    private static void AddExceptionDetails(
        ProblemDetailsContext context,
        Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails
    )
    {
        problemDetails.Extensions.TryAdd("exceptionType", context.Exception?.GetType().Name);
        problemDetails.Extensions.TryAdd("exceptionMessage", context.Exception?.Message);
        problemDetails.Extensions.TryAdd("stackTrace", context.Exception?.StackTrace);

        // Inner exception if present.
        if (context.Exception?.InnerException != null)
        {
            problemDetails.Extensions.TryAdd(
                "innerException",
                new
                {
                    type = context.Exception.InnerException.GetType().Name,
                    message = context.Exception.InnerException.Message,
                }
            );
        }
    }

    private static void AddStandardMetadata(
        Microsoft.AspNetCore.Mvc.ProblemDetails problemDetails,
        HttpContext httpContext
    )
    {
        problemDetails.Instance = $"{httpContext.Request.Method} {httpContext.Request.Path}";

        problemDetails.Extensions.TryAdd("requestId", httpContext.TraceIdentifier);

        Activity? activity = httpContext.Features.Get<IHttpActivityFeature>()?.Activity;

        problemDetails.Extensions.TryAdd("traceId", activity?.Id);

        problemDetails.Extensions.TryAdd("timestamp", DateTime.UtcNow.ToString("O"));
    }
}
