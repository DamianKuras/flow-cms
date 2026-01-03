using Api.Endpoints;

namespace Api.Startup;

/// <summary>
/// Provides extension methods for mapping API endpoints.
/// </summary>
public static class Endpoints
{
    /// <summary>
    /// Maps all API endpoints to the application.
    /// </summary>
    /// <param name="app">The web application instance.</param>
    public static void MapEndpoints(this WebApplication app)
    {
        app.RegisterContentTypeEndpoints();
        app.RegisterContentItemEndpoints();

        app.RegisterAuthEndpoints();

        app.RegisterUsersEndpoints();
    }
}
