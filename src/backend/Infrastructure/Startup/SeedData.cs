using Infrastructure.Data;
using Infrastructure.Users;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.Startup;

/// <summary>
/// Provides extension methods for seeding initial data into the application database.
/// </summary>
public static class SeedDataStartup
{
    /// <summary>
    /// Seeds the database with initial data including users, roles, and other entities.
    /// This method applies pending migrations and initializes the database with default data.
    /// </summary>
    /// <param name="app">The application builder instance used to access services.</param>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public static async Task SeedData(this IApplicationBuilder app)
    {
        using IServiceScope scope = app.ApplicationServices.CreateScope();
        IServiceProvider services = scope.ServiceProvider;
        ILogger<DataSeeder>? logger = services.GetService<ILogger<DataSeeder>>();

        try
        {
            AppDbContext context = services.GetRequiredService<AppDbContext>();
            UserManager<AppUser> userManager = services.GetRequiredService<UserManager<AppUser>>();
            RoleManager<AppRole> roleManager = services.GetRequiredService<RoleManager<AppRole>>();
            IHostEnvironment environment = services.GetRequiredService<IHostEnvironment>();
            logger?.LogInformation("Starting database migration...");
            await context.Database.MigrateAsync();
            logger?.LogInformation("Database migration completed successfully");

            logger?.LogInformation("Starting data seeding...");
            var seeder = new DataSeeder(context, userManager, roleManager, environment);
            await seeder.SeedAsync();
            logger?.LogInformation("Data seeding completed successfully");
        }
        catch (Exception ex)
        {
            logger?.LogError(ex, "An error occurred while seeding the database");
            throw;
        }
    }
}
