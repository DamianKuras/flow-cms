using System.Text.Json;
using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http.Json;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Integration.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory for integration testing with PostgreSQL Testcontainers.
/// This factory manages the lifecycle of the test database and application.
/// </summary>
public class IntegrationTestWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .WithCleanUp(true)
        .Build();

    /// <summary>
    /// Initializes the PostgreSQL container and applies migrations.
    /// Called automatically by xUnit before running tests.
    /// </summary>
    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Create a scope to run migrations
        using IServiceScope scope = Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        await dbContext.Database.MigrateAsync();
    }

    /// <summary>
    /// Cleans up resources when tests are complete.
    /// Called automatically by xUnit after running tests.
    /// </summary>
    public new async Task DisposeAsync() => await _dbContainer.StopAsync();

    /// <summary>
    /// Configures the web host to use the test database.
    /// Override DbContext to use Testcontainers connection string.
    /// </summary>
    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            // Remove existing AppDbContext registration
            ServiceDescriptor? descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>)
            );

            if (descriptor != null)
            {
                services.Remove(descriptor);
            }

            // Add DbContext with test container connection string
            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString())
            );
        });

    /// <summary>
    /// The exact JsonSerializerOptions the API uses — resolved from the app's DI container.
    /// Use this in tests instead of re-declaring options manually.
    /// </summary>
    public JsonSerializerOptions JsonOptions =>
        Services.GetRequiredService<IOptions<JsonOptions>>().Value.SerializerOptions;

    public async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        // IgnoreQueryFilters ensures soft-deleted rows are included in the delete
        await dbContext.Fields.ExecuteDeleteAsync();
        await dbContext.ContentItems.IgnoreQueryFilters().ExecuteDeleteAsync();
        await dbContext.ContentTypes.ExecuteDeleteAsync();
        await dbContext.SaveChangesAsync();
    }
}
