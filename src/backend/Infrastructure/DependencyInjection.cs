using Application.Interfaces;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Users;
using Infrastructure.Data;
using Infrastructure.Fields;
using Infrastructure.Interceptors;
using Infrastructure.Repositories;
using Infrastructure.Services;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Infrastructure;

/// <summary>
/// Provides extension methods for registering infrastructure-layer services in the dependency injection container.
/// </summary>
public static class ServiceRegistration
{
    /// <summary>
    /// Configures and registers infrastructure-layer services such as the database context, identity, repositories, and plugins.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="configuration">The application configuration for accessing settings like connection strings.</param>
    /// <returns>The original <see cref="IServiceCollection"/> for chaining.</returns>
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<AppDbContext>(
            (sp, options) =>
            {
                string? connectionString = configuration.GetConnectionString("Default");

                var dataSourceBuilder = new NpgsqlDataSourceBuilder(connectionString);

                dataSourceBuilder.EnableDynamicJson();

                NpgsqlDataSource dataSource = dataSourceBuilder.Build();

                options.UseNpgsql(dataSource);

                options.AddInterceptors(new SoftDeleteInterceptor());
            }
        );

        services
            .AddIdentity<AppUser, AppRole>(options =>
                options.Stores.SchemaVersion = IdentitySchemaVersions.Version3
            )
            .AddEntityFrameworkStores<AppDbContext>()
            .AddDefaultTokenProviders();

        services.AddScoped<IContentTypeRepository, ContentTypeRepository>();
        services.AddScoped<IContentItemRepository, ContentItemRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();

        string pluginsPath = Path.Combine(AppContext.BaseDirectory, "plugins");
        if (!Directory.Exists(pluginsPath))
        {
            Directory.CreateDirectory(pluginsPath);
        }
        var assemblies = PluginLoaderExtensions.LoadPluginAssemblies(pluginsPath).ToList();

        services.AddSingleton<IValidationRuleRegistry>(_ =>
        {
            var registry = new ReflectionValidationRuleRegistry();
            registry.DiscoverRulesFromAssemblies(assemblies);
            return registry;
        });

        services.AddSingleton<ITransformationRuleRegistry>(_ =>
        {
            var registry = new ReflectionTransformationRuleRegistry();
            registry.DiscoverRulesFromAssemblies(assemblies);
            return registry;
        });

        services.AddScoped<IAuthorizationService, AuthorizationService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IPermissionProvider, EfPermissionProvider>();

        services.AddScoped<IUserContext, HttpUserContext>();

        return services;
    }
}
