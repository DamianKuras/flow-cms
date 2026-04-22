using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;

namespace Application.ContentTypes;

/// <summary>Snapshot of a migration job returned by the API.</summary>
public record MigrationJobDto(
    Guid Id,
    Guid FromSchemaId,
    Guid ToSchemaId,
    string Mode,
    string Status,
    string CreatedBy,
    DateTime CreatedAt,
    int TotalItemsCount,
    int MigratedItemsCount,
    int FailedItemsCount
);

public record GetMigrationJobQuery(Guid Id);

/// <summary>Returns a single migration job by ID.</summary>
public sealed class GetMigrationJobHandler(
    IMigrationJobRepository migrationJobRepository,
    IAuthorizationService authorizationService
) : IQueryHandler<GetMigrationJobQuery, MigrationJobDto>
{
    public async Task<Result<MigrationJobDto>> Handle(
        GetMigrationJobQuery query,
        CancellationToken cancellationToken
    )
    {
        MigrationJob? job = await migrationJobRepository.GetByIdAsync(query.Id, cancellationToken);
        if (job is null)
            return Result<MigrationJobDto>.Failure(Error.NotFound($"Migration job '{query.Id}' not found."));

        bool allowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.Read,
            ResourceType.ContentType,
            cancellationToken
        );
        if (!allowed)
            return Result<MigrationJobDto>.Failure(Error.Forbidden("Forbidden"));

        return Result<MigrationJobDto>.Success(MapToDto(job));
    }

    internal static MigrationJobDto MapToDto(MigrationJob job) =>
        new(
            job.Id,
            job.FromSchemaId,
            job.ToSchemaId,
            job.Mode.ToString(),
            job.Status.ToString(),
            job.CreatedBy,
            job.CreatedAt,
            job.TotalItemsCount,
            job.MigratedItemsCount,
            job.FailedItemsCount
        );
}

public record GetMigrationJobsQuery(string ContentTypeName);

/// <summary>Returns all migration jobs for a content type, ordered by creation time descending.</summary>
public sealed class GetMigrationJobsHandler(
    IMigrationJobRepository migrationJobRepository,
    IAuthorizationService authorizationService
) : IQueryHandler<GetMigrationJobsQuery, IReadOnlyList<MigrationJobDto>>
{
    public async Task<Result<IReadOnlyList<MigrationJobDto>>> Handle(
        GetMigrationJobsQuery query,
        CancellationToken cancellationToken
    )
    {
        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Read,
            new ContentTypeResource(query.ContentTypeName),
            cancellationToken
        );
        if (!allowed)
            return Result<IReadOnlyList<MigrationJobDto>>.Failure(Error.Forbidden("Forbidden"));

        IReadOnlyList<MigrationJob> jobs = await migrationJobRepository.GetByContentTypeNameAsync(
            query.ContentTypeName,
            cancellationToken
        );

        return Result<IReadOnlyList<MigrationJobDto>>.Success(
            jobs.Select(GetMigrationJobHandler.MapToDto).ToList()
        );
    }
}
