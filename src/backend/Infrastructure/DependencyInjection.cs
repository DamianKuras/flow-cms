using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Infrastructure;

public static class ServiceRegistration
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration
    )
    {
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseNpgsql(configuration.GetConnectionString("Default"))
        );
        return services;
    }
}
