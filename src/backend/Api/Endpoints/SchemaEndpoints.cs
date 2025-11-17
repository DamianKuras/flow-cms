using Application.Interfaces;
using Application.Schemas;
using Domain;

namespace Api.Endpoints;

using Api.Extensions;
using Microsoft.AspNetCore.Mvc;

public static class SchemaEndpoints
{
    public static void RegisterSchemaEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/schemas").WithTags("Schemas");

        group
            .MapGet("/", GetSchemas)
            .WithName("GetSchemas")
            .Produces<List<SchemaListItemDto>>(StatusCodes.Status200OK)
            .WithOpenApi();

        group
            .MapGet("/{id}", GetSchemaById)
            .WithName("GetSchemaById")
            .Produces<SchemaDto>(StatusCodes.Status200OK)
            .WithOpenApi();

        group
            .MapPost("/", CreateSchema)
            .WithName("CreateSchema")
            .Produces<Guid>(StatusCodes.Status201Created)
            .WithOpenApi();
    }

    private static async Task<IResult> GetSchemas(
        int page,
        IQueryHandler<GetSchemasQuery, List<SchemaListItemDto>> handler,
        CancellationToken cancellationToken
    )
    {
        GetSchemasQuery query = new() { Page = page };
        Result<List<SchemaListItemDto>> result = await handler.Handle(query, cancellationToken);
        return result.Match(
            onSuccess: schemas => Results.Ok(schemas),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> CreateSchema(
        CreateSchemaCommand command,
        ICommandHandler<CreateSchemaCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: id =>
                Results.CreatedAtRoute(
                    routeName: "GetSchemaById",
                    routeValues: new { id },
                    value: new { Id = id }
                ),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> GetSchemaById(
        Guid id,
        IQueryHandler<GetSchemaByIdQuery, SchemaDto> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetSchemaByIdQuery { Id = id };
        Result<SchemaDto> result = await handler.Handle(query, cancellationToken);
        return result.Match(
            onSuccess: schema => Results.Ok(schema),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }
}
