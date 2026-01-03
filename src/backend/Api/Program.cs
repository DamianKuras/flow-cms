using Api.Startup;
using Application.ContentTypes;
using Application.Interfaces;
using Infrastructure;
using Infrastructure.Startup;
using Infrastructure.Users;
using Serilog;

// Configure bootstrap logger for startup logging.
Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .CreateBootstrapLogger();

try
{
    Log.Information("Starting web application");
    WebApplicationBuilder builder = WebApplication.CreateBuilder(args);

    builder.ConfigureSerilog();

    builder.ConfigureProblemDetails();

    builder.ConfigureCors();

    builder.Services.AddInfrastructure(builder.Configuration);

    // Auto-register all query and command handlers using Scrutor.
    builder.Services.Scan(scan =>
        scan.FromAssemblyOf<GetContentTypesHandler>()
            .AddClasses(classes => classes.AssignableTo(typeof(IQueryHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
            .AddClasses(classes => classes.AssignableTo(typeof(ICommandHandler<,>)))
            .AsImplementedInterfaces()
            .WithScopedLifetime()
    );

    builder.Services.AddOpenApi();

    builder.ConfigureJson();

    builder.Services.AddHttpContextAccessor();

    builder.ConfigureAuthentication();

    builder.Services.AddAuthorization();

    WebApplication app = builder.Build();

    app.UseCorsPolicy();

    app.UseRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();

        app.UseSwaggerUI(options => options.SwaggerEndpoint("/openapi/v1.json", "v1"));
    }

    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Error");
        app.UseHsts();
    }
    app.UseStatusCodePages();

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapEndpoints();

    await app.SeedData();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "Application terminated unexpectedly");
}
finally
{
    Log.CloseAndFlush();
}

/// <summary>
/// Explicit definition of Program as partial for integration tests.
/// </summary>
public partial class Program { }
