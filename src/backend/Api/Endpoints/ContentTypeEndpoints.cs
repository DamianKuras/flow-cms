using Api.Extensions;
using Api.Mappers;
using Application.ContentTypes;
using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentTypes;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

public record PublishContentTypeRequest(MigrationMode MigrationMode = MigrationMode.Lazy);

/// <summary>HTTP endpoints for managing content types.</summary>
public static class ContentTypeEndpoints
{
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
            .MapPut("/{id}/draft", UpdateDraftContentType)
            .WithName("UpdateDraftContentType")
            .RequireAuthorization()
            .Produces<Guid>(StatusCodes.Status200OK)
            .Produces<ProblemDetails>(StatusCodes.Status400BadRequest)
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
            .MapPost("/{contentTypeName}/archive", ArchiveContentType)
            .WithName("ArchiveContentType")
            .RequireAuthorization()
            .Produces(StatusCodes.Status204NoContent)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group
            .MapGet("/summaries", GetContentTypeSummaries)
            .WithName("GetContentTypeSummaries")
            .RequireAuthorization()
            .Produces<IReadOnlyList<ContentTypeSummaryDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapPost("{contentTypeName}/publish", PublishContentType)
            .WithName("PublishContentType")
            .RequireAuthorization()
            .Produces<object>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);

        group
            .MapGet("/{contentTypeName}/migration-jobs", GetMigrationJobs)
            .WithName("GetMigrationJobs")
            .RequireAuthorization()
            .Produces<IReadOnlyList<MigrationJobDto>>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized);

        group
            .MapGet("/migration-jobs/{id}", GetMigrationJob)
            .WithName("GetMigrationJob")
            .RequireAuthorization()
            .Produces<MigrationJobDto>(StatusCodes.Status200OK)
            .Produces(StatusCodes.Status401Unauthorized)
            .Produces(StatusCodes.Status404NotFound);
    }

    private static async Task<IResult> GetContentTypeSummaries(
        [FromServices] IQueryHandler<GetContentTypeSummariesQuery, IReadOnlyList<ContentTypeSummaryDto>> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetContentTypeSummariesQuery();
        Result<IReadOnlyList<ContentTypeSummaryDto>> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: summaries => Results.Ok(summaries));
    }

    private static async Task<IResult> GetContentTypes(
        [FromServices] IQueryHandler<GetContentTypesQuery, GetContentTypeResponse> handler,
        CancellationToken cancellationToken,
        [FromQuery] int pageNumber = 1,
        [FromQuery] int pageSize = 10,
        [FromQuery] string sort = "",
        [FromQuery] string status = "",
        [FromQuery] string filter = ""
    )
    {
        var paginationParameters = new PaginationParameters(pageNumber, pageSize);
        var query = new GetContentTypesQuery(paginationParameters, sort, status, filter);
        Result<GetContentTypeResponse> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: response =>
            Results.Ok(ResponseMapper.MapPagedListToPagedResult(response.Data))
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

    private static async Task<IResult> UpdateDraftContentType(
        [FromRoute] Guid id,
        [FromBody] UpdateDraftContentTypeCommand command,
        [FromServices] ICommandHandler<UpdateDraftContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        // Route id overrides any id in the body to prevent mismatch.
        var actualCommand = command with { Id = id };
        Result<Guid> result = await handler.Handle(actualCommand, cancellationToken);
        return result.Match(onSuccess: updatedId => Results.Ok(new { Id = updatedId }));
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

    private static async Task<IResult> ArchiveContentType(
        [FromRoute] string contentTypeName,
        [FromServices] ICommandHandler<ArchiveContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var command = new ArchiveContentTypeCommand(contentTypeName);
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: guid => Results.NoContent());
    }

    private static async Task<IResult> PublishContentType(
        [FromRoute] string contentTypeName,
        [FromBody] PublishContentTypeRequest? body,
        [FromServices] ICommandHandler<PublishContentTypeCommand, Guid> handler,
        CancellationToken cancellationToken
    )
    {
        var command = new PublishContentTypeCommand(
            contentTypeName,
            body?.MigrationMode ?? MigrationMode.Lazy
        );
        Result<Guid> result = await handler.Handle(command, cancellationToken);
        return result.Match(onSuccess: guid => Results.Ok(new { id = guid }));
    }

    private static async Task<IResult> GetMigrationJobs(
        [FromRoute] string contentTypeName,
        [FromServices] IQueryHandler<GetMigrationJobsQuery, IReadOnlyList<MigrationJobDto>> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetMigrationJobsQuery(contentTypeName);
        Result<IReadOnlyList<MigrationJobDto>> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: jobs => Results.Ok(jobs));
    }

    private static async Task<IResult> GetMigrationJob(
        [FromRoute] Guid id,
        [FromServices] IQueryHandler<GetMigrationJobQuery, MigrationJobDto> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetMigrationJobQuery(id);
        Result<MigrationJobDto> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: job => Results.Ok(job));
    }
}
