namespace Domain.ContentItems;

/// <summary>
/// Enumeration of possible statuses for a content item throughout its lifecycle.
/// </summary>
public enum ContentItemStatus
{
    /// <summary>
    /// Indicates that the content item has been archived and is no longer actively used.
    /// </summary>
    Archive,

    /// <summary>
    /// Indicates that the content item is in draft status and not yet ready for publication.
    /// </summary>
    Draft,

    /// <summary>
    /// Indicates that the content item has been published and is publicly visible.
    /// </summary>
    Published,

    /// <summary>
    /// Indicates that the content item is currently under review by an authorized reviewer.
    /// </summary>
    InReview,

    /// <summary>
    /// Indicates that the content item is scheduled for automatic publication at a future date and time.
    /// </summary>
    Scheduled,

    /// <summary>
    /// Indicates that the content item has been rejected during the review process.
    /// </summary>
    Rejected,

    /// <summary>
    /// Indicates that the content item has been permanently deleted.
    /// </summary>
    Deleted,
}
