using Domain;
using Domain.ContentItems;
using Domain.ContentTypes;
using Infrastructure.Data;
using Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Infrastructure.BackgroundServices;

/// <summary>
/// Processes pending eager migration jobs in the background.
/// Picks up jobs with Mode=Eager and Status=Pending, migrates all affected content items,
/// and marks the job as Completed (or Failed on error).
/// </summary>
public sealed class EagerMigrationBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<EagerMigrationBackgroundService> logger
) : BackgroundService
{
    private static readonly TimeSpan PollingInterval = TimeSpan.FromSeconds(15);

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("Eager migration background service started.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessPendingJobsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Unhandled exception in eager migration background service.");
            }

            await Task.Delay(PollingInterval, stoppingToken);
        }
    }

    private async Task ProcessPendingJobsAsync(CancellationToken ct)
    {
        using IServiceScope scope = scopeFactory.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        IReadOnlyList<MigrationJob> jobs = await db.MigrationJobs
            .Where(j => j.Mode == MigrationMode.Eager && j.Status == MigrationJobStatus.Pending)
            .OrderBy(j => j.CreatedAt)
            .ToListAsync(ct);

        foreach (MigrationJob job in jobs)
        {
            await RunJobAsync(db, job, ct);
        }
    }

    private async Task RunJobAsync(AppDbContext db, MigrationJob job, CancellationToken ct)
    {
        logger.LogInformation(
            "Starting eager migration job {JobId}: {From} → {To}",
            job.Id, job.FromSchemaId, job.ToSchemaId
        );

        job.Start();
        db.MigrationJobs.Update(job);
        await db.SaveChangesAsync(ct);

        // Load the target schema (not soft-deleted).
        ContentType? targetSchema = await db.ContentTypes
            .Include(x => x.Fields)
            .FirstOrDefaultAsync(ct2 => ct2.Id == job.ToSchemaId, ct);

        if (targetSchema is null)
        {
            logger.LogError("Target schema {ToSchemaId} not found for job {JobId}.", job.ToSchemaId, job.Id);
            job.Fail();
            db.MigrationJobs.Update(job);
            await db.SaveChangesAsync(ct);
            return;
        }

        const int BatchSize = 100;
        int offset = 0;

        while (true)
        {
            List<ContentItem> batch = await db.ContentItems
                .Where(i => i.ContentTypeId == job.FromSchemaId)
                .OrderBy(i => i.Id)
                .Skip(offset)
                .Take(BatchSize)
                .ToListAsync(ct);

            if (batch.Count == 0)
                break;

            foreach (ContentItem item in batch)
            {
                try
                {
                    item.MigrateToSchema(targetSchema);
                    db.ContentItems.Update(item);
                    job.RecordItemMigrated();
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to migrate content item {ItemId} in job {JobId}.", item.Id, job.Id);
                    job.RecordItemFailed();
                }
            }

            db.MigrationJobs.Update(job);
            await db.SaveChangesAsync(ct);

            if (batch.Count < BatchSize)
                break;

            offset += BatchSize;
        }

        job.Complete();
        db.MigrationJobs.Update(job);
        await db.SaveChangesAsync(ct);

        logger.LogInformation(
            "Completed eager migration job {JobId}: {Migrated} migrated, {Failed} failed.",
            job.Id, job.MigratedItemsCount, job.FailedItemsCount
        );
    }
}
