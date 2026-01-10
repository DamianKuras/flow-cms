namespace Domain.Common;

/// <summary>
/// Defines a contract for entities that support soft deletion, where records are marked
/// as deleted rather than being physically removed from the database.
/// </summary>
public interface ISoftDeletable
{
    /// <summary>
    /// Gets a value indicating whether this entity has been soft deleted.
    /// </summary>
    /// <value>
    /// <c>true</c> if the entity is deleted; otherwise, <c>false</c>.
    /// </value>
    bool IsDeleted { get; }

    /// <summary>
    /// Gets the UTC date and time when this entity was soft deleted.
    /// </summary>
    /// <value>
    /// The UTC timestamp of deletion, or <c>null</c> if the entity has not been deleted.
    /// </value>
    DateTime? DeletedOnUtc { get; }

    /// <summary>
    /// Marks this entity as soft deleted by setting IsDeleted to true
    /// and recording the deletion timestamp.
    /// </summary>
    void SoftDelete();
}
