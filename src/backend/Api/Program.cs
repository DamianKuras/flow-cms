using Api.Endpoints;
using Application.Interfaces;
using Application.Schemas;
using Infrastructure;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<ICommandHandler<CreateSchemaCommand, Guid>, CreateSchemaHandler>();
builder.Services.AddScoped<IQueryHandler<GetSchemaByIdQuery, SchemaDto>, GetSchemaByIdHandler>();
builder.Services.AddScoped<
    IQueryHandler<GetSchemasQuery, List<SchemaListItemDto>>,
    GetSchemasHandler
>();

builder.Services.AddOpenApi();

// Configure CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy(
        "AllowAll",
        builder =>
        {
            builder.AllowAnyOrigin().AllowAnyHeader().AllowAnyMethod();
        }
    );
});

var app = builder.Build();

app.UseCors("AllowAll");

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();

    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/openapi/v1.json", "v1");
    });
}

app.RegisterSchemaEndpoints();

app.MapGet("/", () => "Hello World!");

app.Run();
