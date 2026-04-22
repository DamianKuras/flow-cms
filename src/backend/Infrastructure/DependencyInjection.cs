using Application.Interfaces;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Roles;
using Domain.Users;
using Infrastructure.BackgroundServices;
using Infrastructure.Data;
using Infrastructure.Extensions;
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

/// <summary>Registers infrastructure-layer services: database, identity, repositories, and plugins.</summary>
public static class ServiceRegistration
{
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
        services.AddScoped<IMigrationJobRepository, MigrationJobRepository>();
        services.AddHostedService<EagerMigrationBackgroundService>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
        services.AddScoped<IRoleRepository, RoleRepository>();
        services.AddScoped<IRolePermissionService, RolePermissionService>();

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
        services.AddScoped<DataSeeder>();

        return services;
    }
}
