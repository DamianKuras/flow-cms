using System.Text.Json;
using Api.Extensions;
using Api.Responses;
using Application.ContentItems;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Infrastructure;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Defines HTTP endpoints for managing content items in the CMS.
/// </summary>
public static class ContentItemEndpoints
{
    /// <summary>
    /// Registers all content item-related endpoints with the application's endpoint route builder.
    /// </summary>
    /// <param name="endpoints"></param>
    public static void RegisterContentItemEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder group = endpoints
            .MapGroup("/content-items")
            .WithTags("Content Items");

        group
            .MapGet("/", GetContentItems)
            .WithName("GetContentItems")
            .RequireAuthorization()
            .Produces<PagedResponse<PagedContentType>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapPost("/", CreateContentItem)
            .WithName("CreateContentItem")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status201Created)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapGet("/{id}", GetContentItemById)
            .WithName("GetContentItemById")
            .RequireAuthorization()
            .Produces<ContentItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapPatch("/{id}", UpdateContentItem)
            .WithName("UpdateContentItem")
            .RequireAuthorization()
            .Produces<ContentItemDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status400BadRequest)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapDelete("/{id}", DeleteContentItem)
            .WithName("DeleteContentItem")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapPost("/{id}/publish", PublishContentItem)
            .WithName("PublishContentItem")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status404NotFound)
            .Produces(StatusCodes.Status401Unauthorized);
    }

    private static async Task<IResult> GetContentItems(
        [FromServices] IQueryHandler<GetContentItemsQuery, GetContentItemsResponse> handler,
        [FromQuery] Guid contentTypeId,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10
    )
    {
        var query = new GetContentItemsQuery(
            contentTypeId,
            new PaginationParameters(pageNumber, pageSize)
        );
        Result<GetContentItemsResponse> result = await handler.Handle(query, cancellationToken);

        return result.Match(onSuccess: response => Results.Ok(response));
    }

    private static async Task<IResult> CreateContentItem(
        [FromBody] CreateContentItemDto dto,
        ICommandHandler<CreateContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var values = new Dictionary<Guid, object?>();
        foreach (KeyValuePair<string, JsonElement?> kv in dto.Values)
        {
            if (kv.Value is not null)
            {
                values.Add(Guid.Parse(kv.Key), JsonTypeConverter.Convert(kv.Value));
            }
        }
        var command = new CreateContentItemCommand(dto.Title, dto.ContentTypeId, values);
        Result<Guid> response = await handler.Handle(command, cancellationToken);
        return response.Match(onSuccess: id =>
            Results.CreatedAtRoute(
                routeName: "GetContentItemById",
                routeValues: new { id },
                value: new { Id = id }
            )
        );
    }

    private static async Task<IResult> GetContentItemById(
        [FromRoute] Guid id,
        IQueryHandler<GetContentItemQuery, ContentItemDto> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetContentItemQuery(id);
        Result<ContentItemDto> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: schemas => Results.Ok(schemas));
    }

    private static async Task<IResult> UpdateContentItem(
        [FromRoute] Guid id,
        UpdateContentItemCommand command,
        ICommandHandler<UpdateContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: _ => Results.Ok());
    }

    private static async Task<IResult> PublishContentItem(
        [FromRoute] Guid id,
        ICommandHandler<PublishContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var command = new PublishContentItemCommand(id);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: _ => Results.Ok());
    }

    private static async Task<IResult> DeleteContentItem(
        [FromRoute] Guid id,
        ICommandHandler<DeleteContentItemCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var command = new DeleteContentItemCommand(id);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: _ => Results.NoContent());
    }
}
