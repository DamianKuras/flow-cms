using Application.Interfaces;
using Application.Schemas;
using Domain;

namespace Api.Endpoints;

using Api.Extensions;
using Application.ContentItems;
using Application.ContentTypes;
using Domain.Common;

public static class ContentItemEndpoints
{
    public static void RegisterContentItemEndpoints(
        this IEndpointRouteBuilder endpoints
    )
    {
        var group = endpoints
            .MapGroup("/content-items")
            .WithTags("Content Items");

        group
            .MapGet("/", GetContentItems)
            .WithName("GetContentItems")
            .Produces<List<ContentItemListDto>>(StatusCodes.Status200OK)
            .WithOpenApi();

        group
            .MapPost("/", CreateContentItem)
            .WithName("CreateContentItem")
            .Produces<Guid>(StatusCodes.Status201Created)
            .WithOpenApi();

        group
            .MapGet("/{id}", GetContentItemById)
            .WithName("GetContentItemById")
            .Produces<ContentItemDto>(StatusCodes.Status201Created)
            .WithOpenApi();

        group
            .MapPatch("/{id}", UpdateContentItem)
            .WithName("UpdateContentItem")
            .WithOpenApi();

        group
            .MapDelete("/{id}", DeleteContentItem)
            .WithName("DeleteContentItem")
            .WithOpenApi();
    }

    private static async Task<IResult> GetContentItems(
        GetContentItemsQuery query,
        IQueryHandler<GetContentItemsQuery, GetContentItemsResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var result = await handler.Handle(query, cancellationToken);

        return result.Match(
            onSuccess: response => Results.Ok(response),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    public static async Task<IResult> CreateContentItem(
        CreateContentItemCommand command,
        ICommandHandler<CreateContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: id =>
                Results.CreatedAtRoute(
                    routeName: "GetContentItemById",
                    routeValues: new { id },
                    value: new { Id = id }
                ),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> GetContentItemById(
        Guid id,
        IQueryHandler<GetContentItemQuery, ContentItemDto> handler,
        CancellationToken cancellationToken
    )
    {
        GetContentItemQuery query = new GetContentItemQuery(id);
        Result<ContentItemDto> result = await handler.Handle(
            query,
            cancellationToken
        );
        return result.Match(
            onSuccess: schemas => Results.Ok(schemas),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> UpdateContentItem(
        Guid id,
        UpdateContentItemCommand command,
        ICommandHandler<UpdateContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: _ => Results.Ok(),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }

    private static async Task<IResult> DeleteContentItem(
        Guid id,
        ICommandHandler<DeleteContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        DeleteContentItemCommand command = new DeleteContentItemCommand(id);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(
            onSuccess: _ => Results.NoContent(),
            onFailure: error => ResultExtensions.MapErrorToResult(error)
        );
    }
}
