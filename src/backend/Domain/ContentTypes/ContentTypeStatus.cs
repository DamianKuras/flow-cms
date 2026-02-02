namespace Domain.ContentTypes;

/// <summary>
/// Enumeration of possible states for a content type definition throughout its lifecycle.
/// </summary>
public enum ContentTypeStatus
{
    /// <summary>
    /// Indicates that the content type definition is in draft status and not yet ready for use.
    /// </summary>
    DRAFT,

    /// <summary>
    /// Indicates that the content type definition has been archived and is no longer available for new content creation.
    /// </summary>
    ARCHIVE,

    /// <summary>
    /// Indicates that the content type definition is currently under review by an authorized reviewer or architect.
    /// </summary>
    IN_REVIEW,

    /// <summary>
    /// Indicates that the content type definition is scheduled for automatic activation at a future date and time.
    /// </summary>
    SCHEDULED,

    /// <summary>
    /// Indicates that the content type definition has been published and is available for creating new content items.
    /// </summary>
    PUBLISHED,
}
