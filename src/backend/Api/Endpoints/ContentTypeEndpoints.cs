using Application.Interfaces;
using Application.Schemas;
using Domain;

namespace Api.Endpoints;

using Api.Extensions;
using Application.ContentTypes;
using Domain.Common;

public static class ContentTypeEndpoints
{
    public static void RegisterContentTypeEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/content-type")
            .WithTags("Content Type");

        group
            .MapGet("/", GetContentTypes)
            .WithName("GetContentTypes")
            .Produces<List<ContentTypeDto>>(StatusCodes.Status200OK)
            .WithOpenApi();

        group
            .MapPost("/", CreateContentType)
            .WithName("CreateContentType")
            .Produces<Guid>(StatusCodes.Status201Created)
            .WithOpenApi();

        group
            .MapGet("/{id}", GetContentTypeById)
            .WithName("GetContentTypeById")
            .Produces<ContentTypeDto>(StatusCodes.Status200OK)
            .WithOpenApi();

        group
            .MapDelete("/{id}", DeleteContentType)
            .WithName("DeleteContentType")
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound)
            .WithOpenApi();
    }

    private static async Task<IResult> GetContentTypes(
        IQueryHandler<GetContentTypesQuery, List<ContentTypeListDto>> handler,
        CancellationToken cancellationToken
    )
    {
        GetContentTypesQuery query = new() { };
        Result<List<ContentTypeListDto>> result = await handler.Handle(
            query,
            cancellationToken
        );
        return result.Match(
            onSuccess: schemas => Results.Ok(schemas),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> CreateContentType(
        CreateContentTypeCommand command,
        ICommandHandler<CreateContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: id =>
                Results.CreatedAtRoute(
                    routeName: "GetContentTypeById",
                    routeValues: new { id },
                    value: new { Id = id }
                ),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> GetContentTypeById(
        Guid id,
        IQueryHandler<GetContentTypeQuery, ContentTypeDto> handler,
        CancellationToken cancellationToken
    )
    {
        GetContentTypeQuery query = new();
        query.Id = id;
        Result<ContentTypeDto> result = await handler.Handle(
            query,
            cancellationToken
        );
        return result.Match(
            onSuccess: schemas => Results.Ok(schemas),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> DeleteContentType(
        Guid id,
        ICommandHandler<DeleteContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        DeleteContentTypeCommand command = new DeleteContentTypeCommand(id);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: guid => Results.NoContent(),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }
}
