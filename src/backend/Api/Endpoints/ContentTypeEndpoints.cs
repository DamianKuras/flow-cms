using Api.Extensions;
using Api.Mappers;
using Application.ContentTypes;
using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentTypes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Defines HTTP endpoints for managing content types in the CMS.
/// </summary>
public static class ContentTypeEndpoints
{
    /// <summary>
    /// Registers all content type-related endpoints with the application's routing configuration.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder to register routes with.</param>
    public static void RegisterContentTypeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder group = endpoints.MapGroup("/content-types").WithTags("Content Type");

        group
            .MapGet("/", GetContentTypes)
            .WithName("GetContentTypes")
            .RequireAuthorization()
            .Produces<PagedList<ContentTypeDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapPost("/", CreateContentType)
            .WithName("CreateContentType")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapGet("/{id}", GetContentTypeById)
            .WithName("GetContentTypeById")
            .RequireAuthorization()
            .Produces<ContentTypeDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group
            .MapDelete("/{id}", DeleteContentType)
            .WithName("DeleteContentType")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group
            .MapPost("{contentTypeName}/publish", PublishContentType)
            .WithName("PublishContentType")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetContentTypes(
        [FromServices] IQueryHandler<GetContentTypesQuery, GetContentTypeResponse> handler,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var paginationParameters = new PaginationParameters(pageNumber, pageSize);
        var query = new GetContentTypesQuery(paginationParameters);
        Result<GetContentTypeResponse> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: response =>
            Results.Ok(Mapper.MapPagedListToPagedResult(response.Data))
        );
    }

    private static async Task<IResult> CreateContentType(
        [FromBody] CreateContentTypeCommand command,
        [FromServices] ICommandHandler<CreateContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: id =>
            Results.CreatedAtRoute(
                routeName: "GetContentTypeById",
                routeValues: new { id },
                value: new { Id = id }
            )
        );
    }

    private static async Task<IResult> GetContentTypeById(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetContentTypeQuery, ContentTypeDto> handler,
        CancellationToken cancellationToken
    )
    {
        GetContentTypeQuery query = new(id);
        Result<ContentTypeDto> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: schemas => Results.Ok(schemas));
    }

    private static async Task<IResult> DeleteContentType(
        [FromRoute] Guid id,
        [FromServices] ICommandHandler<DeleteContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        DeleteContentTypeCommand command = new(id);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: guid => Results.NoContent());
    }

    private static async Task<IResult> PublishContentType(
        [FromRoute] string contentTypeName,
        [FromServices] ICommandHandler<PublishContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var command = new PublishContentTypeCommand(contentTypeName);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: guid => Results.NoContent());
    }
}
