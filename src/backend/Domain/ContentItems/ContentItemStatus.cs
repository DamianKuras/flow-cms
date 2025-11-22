namespace Domain.ContentItems;

/// <summary>
/// Enumeration of possible statuses for a content item throughout its lifecycle.
/// </summary>
public enum ContentItemStatus
{
    Archive,
    Draft,
    Published,
    InReview,
    Scheduled,
    Rejected,
    Deleted,
}
