using Domain.ContentTypes;

namespace Domain;

/// <summary>Controls when content items are migrated to a new schema after publishing.</summary>
public enum MigrationMode
{
    /// <summary>Items are migrated on-the-fly the next time they are read.</summary>
    Lazy = 0,
    /// <summary>All items are migrated immediately by a background job.</summary>
    Eager = 1,
}

/// <summary>Lifecycle states of a migration job.</summary>
public enum MigrationJobStatus
{
    Pending = 0,
    Running = 1,
    Completed = 2,
    Failed = 3,
}

/// <summary>
/// Tracks the progress of a schema migration between two published content type versions.
/// A migration job is created whenever a new content type version is published and existing
/// content items need to be updated to conform to the new field schema.
/// </summary>
public class MigrationJob
{
    private MigrationJob() { }

    public MigrationJob(
        Guid id,
        Guid fromSchemaId,
        Guid toSchemaId,
        MigrationMode mode,
        string createdBy,
        int totalItemsCount
    )
    {
        Id = id;
        FromSchemaId = fromSchemaId;
        ToSchemaId = toSchemaId;
        Mode = mode;
        CreatedBy = createdBy;
        TotalItemsCount = totalItemsCount;
        Status = MigrationJobStatus.Pending;
        CreatedAt = DateTime.UtcNow;
    }

    public Guid Id { get; private set; }
    public Guid FromSchemaId { get; private set; }
    public Guid ToSchemaId { get; private set; }
    public MigrationMode Mode { get; private set; }
    public MigrationJobStatus Status { get; private set; }
    public string CreatedBy { get; private set; } = "";
    public DateTime CreatedAt { get; private set; }
    public int TotalItemsCount { get; private set; }
    public int MigratedItemsCount { get; private set; }
    public int FailedItemsCount { get; private set; }

    public void Start()
    {
        if (Status != MigrationJobStatus.Pending)
            throw new InvalidOperationException($"Cannot start a migration job with status {Status}.");
        Status = MigrationJobStatus.Running;
    }

    public void RecordItemMigrated() => MigratedItemsCount++;

    public void RecordItemFailed() => FailedItemsCount++;

    public void Complete()
    {
        if (Status != MigrationJobStatus.Running && Status != MigrationJobStatus.Pending)
            throw new InvalidOperationException($"Cannot complete a migration job with status {Status}.");
        Status = MigrationJobStatus.Completed;
    }

    public void Fail()
    {
        Status = MigrationJobStatus.Failed;
    }

    public bool IsFinished =>
        Status == MigrationJobStatus.Completed || Status == MigrationJobStatus.Failed;
}
