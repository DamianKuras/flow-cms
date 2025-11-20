namespace Domain.ContentItems;

/// <summary>
/// Enumeration of possible statuses for a content item throughout its lifecycle.
/// </summary>
public enum ContentItemStatus
{
    ARCHIVE,
    DRAFT,
    PUBLISHED,
    IN_REVIEW,
    SCHEDULED,
    REJECTED,
    DELETED,
}
