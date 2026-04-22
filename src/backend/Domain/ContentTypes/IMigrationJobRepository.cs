namespace Domain.ContentTypes;

/// <summary>Persistence operations for <see cref="MigrationJob"/> entities.</summary>
public interface IMigrationJobRepository
{
    Task<MigrationJob?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>Returns all migration jobs whose target schema belongs to the given content type name.</summary>
    Task<IReadOnlyList<MigrationJob>> GetByContentTypeNameAsync(
        string contentTypeName,
        CancellationToken ct = default
    );

    /// <summary>Finds the active lazy migration job for the given source schema, if one exists.</summary>
    Task<MigrationJob?> FindLazyJobForSchemaAsync(
        Guid fromSchemaId,
        CancellationToken ct = default
    );

    /// <summary>Returns all pending eager migration jobs, ordered by creation time.</summary>
    Task<IReadOnlyList<MigrationJob>> GetPendingEagerJobsAsync(CancellationToken ct = default);

    Task AddAsync(MigrationJob job, CancellationToken ct = default);
    Task UpdateAsync(MigrationJob job, CancellationToken ct = default);
    Task SaveChangesAsync(CancellationToken ct = default);
}
