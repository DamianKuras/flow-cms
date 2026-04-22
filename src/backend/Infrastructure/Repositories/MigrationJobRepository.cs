using Domain;
using Domain.ContentTypes;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

public class MigrationJobRepository(AppDbContext db) : IMigrationJobRepository
{
    public async Task<MigrationJob?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await db.MigrationJobs.FirstOrDefaultAsync(j => j.Id == id, ct);

    public async Task<IReadOnlyList<MigrationJob>> GetByContentTypeNameAsync(
        string contentTypeName,
        CancellationToken ct = default
    )
    {
        // Jobs are linked to content type IDs; we join through content_types to filter by name.
        var schemaIds = await db.ContentTypes
            .IgnoreQueryFilters()
            .Where(ct2 => ct2.Name == contentTypeName)
            .Select(ct2 => ct2.Id)
            .ToListAsync(ct);

        return await db.MigrationJobs
            .Where(j => schemaIds.Contains(j.ToSchemaId))
            .OrderByDescending(j => j.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<MigrationJob?> FindLazyJobForSchemaAsync(
        Guid fromSchemaId,
        CancellationToken ct = default
    ) =>
        await db.MigrationJobs.FirstOrDefaultAsync(
            j => j.FromSchemaId == fromSchemaId && j.Mode == MigrationMode.Lazy,
            ct
        );

    public async Task<IReadOnlyList<MigrationJob>> GetPendingEagerJobsAsync(
        CancellationToken ct = default
    ) =>
        await db.MigrationJobs
            .Where(j => j.Mode == MigrationMode.Eager && j.Status == MigrationJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

    public async Task AddAsync(MigrationJob job, CancellationToken ct = default) =>
        await db.MigrationJobs.AddAsync(job, ct);

    public Task UpdateAsync(MigrationJob job, CancellationToken ct = default)
    {
        db.MigrationJobs.Update(job);
        return Task.CompletedTask;
    }

    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await db.SaveChangesAsync(ct);
}
