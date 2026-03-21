using Infrastructure.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Testcontainers.PostgreSql;

namespace E2E.Tests.Fixtures;

/// <summary>
/// WebApplicationFactory that starts two hosts:
/// An TestServer for seeding/asserting via HttpClient.
/// A Kestrel server on <see cref="API_BASE_URL"/> so the browser can reach the API.
/// </summary>
public class E2EWebApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    public const string API_BASE_URL = "http://localhost:5252";
    public const string FRONTEND_ORIGIN = "http://localhost:5173";

    private readonly PostgreSqlContainer _dbContainer = new PostgreSqlBuilder()
        .WithImage("postgres:latest")
        .WithDatabase("e2e_testdb")
        .WithUsername("testuser")
        .WithPassword("testpassword")
        .WithCleanUp(true)
        .Build();

    private IHost? _kestrelHost;

    public async Task InitializeAsync()
    {
        await _dbContainer.StartAsync();

        // Trigger lazy host creation (runs ConfigureWebHost + CreateHost).
        // The TestServer host runs SeedData (migrations + seed) once.
        // The Kestrel host has SkipAutoSeed=true so it does not run migrations again.
        _ = Server;
    }

    public new async Task DisposeAsync()
    {
        if (_kestrelHost is not null)
        {
            await _kestrelHost.StopAsync();
            _kestrelHost.Dispose();
        }
        await _dbContainer.StopAsync();
        await base.DisposeAsync();
    }

    /// <summary>
    /// Starts a second  Kestrel host alongside the in-process TestServer.
    /// The browser talks to the Kestrel host, HttpClient uses the TestServer.
    /// </summary>
    protected override IHost CreateHost(IHostBuilder builder)
    {
        IHost testHost = builder.Build();

        builder.ConfigureWebHost(b =>
        {
            b.UseKestrel();
            b.UseUrls(API_BASE_URL);
        });

        // Prevent the Kestrel host from running migrations/seeding a second time
        builder.ConfigureAppConfiguration(config =>
            config.AddInMemoryCollection(
                new Dictionary<string, string?> { ["SkipAutoSeed"] = "true" }));

        _kestrelHost = builder.Build();
        _kestrelHost.Start();

        return testHost;
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder) =>
        builder.ConfigureServices(services =>
        {
            // Replace DbContext with test container
            ServiceDescriptor? descriptor = services.SingleOrDefault(d =>
                d.ServiceType == typeof(DbContextOptions<AppDbContext>));

            if (descriptor is not null)
            {
                services.Remove(descriptor);
            }

            services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(_dbContainer.GetConnectionString()));

            // Ensure Vite's origin is allowed by CORS
            services.AddCors(options =>
                options.AddPolicy("AllowSpecific", policy =>
                    policy.WithOrigins(FRONTEND_ORIGIN)
                          .AllowAnyMethod()
                          .AllowAnyHeader()
                          .AllowCredentials()));
        });

    public async Task ResetDatabaseAsync()
    {
        using IServiceScope scope = Services.CreateScope();
        AppDbContext dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
        await dbContext.Fields.ExecuteDeleteAsync();
        await dbContext.ContentItems.ExecuteDeleteAsync();
        await dbContext.ContentTypes.ExecuteDeleteAsync();
        await dbContext.SaveChangesAsync();
    }
}
