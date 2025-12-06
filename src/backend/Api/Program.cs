using System.Text.Json;
using System.Text.Json.Serialization;
using Api.Endpoints;
using Application.ContentTypes;
using Application.Interfaces;
using Application.Schemas;
using Infrastructure;
using Infrastructure.Validation;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure(builder.Configuration);

builder.Services.AddScoped<
    IQueryHandler<GetContentTypesQuery, List<ContentTypeListDto>>,
    GetContentTypesHandler
>();

builder.Services.AddScoped<
    IQueryHandler<GetContentTypeQuery, ContentTypeDto>,
    GetContentTypeHandler
>();

builder.Services.AddScoped<
    ICommandHandler<CreateContentTypeCommand, Guid>,
    CreateContentTypeCommandHandler
>();

builder.Services.AddScoped<
    ICommandHandler<DeleteContentTypeCommand, Guid>,
    DeleteContentType
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

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    options.SerializerOptions.WriteIndented = true;
    options.SerializerOptions.Converters.Add(
        new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)
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

app.RegisterContentTypeEndpoints();

app.Run();
