using Domain.Common;

namespace Domain.ContentItems;

/// <summary>
/// Defines the contract for content item repository operations.
/// </summary>
public interface IContentItemRepository
{
    /// <summary>
    /// Retrieves a content item by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the content item.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The content item if found; otherwise, null.</returns>
    Task<ContentItem?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated list of content items for a specific content type.
    /// </summary>
    /// <param name="contentTypeId">The unique identifier of the content type.</param>
    /// <param name="paginationParameters">The pagination parameters (page number and page size).</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>A read-only list of paged content items.</returns>
    Task<IReadOnlyList<PagedContentItem>> Get(
        Guid contentTypeId,
        PaginationParameters paginationParameters,
        CancellationToken ct = default
    );

    /// <summary>
    /// Adds a new content item to the repository.
    /// </summary>
    /// <param name="contentItem">The content item to add.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns></returns>
    Task AddAsync(ContentItem contentItem, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing content item in the repository.
    /// </summary>
    /// <param name="contentItem">The content item to update.</param>
    Task UpdateAsync(ContentItem contentItem);

    /// <summary>
    /// Marks a content item for deletion from the repository.
    /// </summary>
    /// <param name="contentItem">The content item to delete.</param>
    Task DeleteAsync(ContentItem contentItem);

    /// <summary>
    /// Counts the number of content items for a specific content type.
    /// </summary>
    /// <param name="contentTypeId">The unique identifier of the content type.</param>
    /// <param name="ct">The cancellation token.</param>
    /// <returns>The total count of content items.</returns>
    Task<int> CountAsync(Guid contentTypeId, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the latest draft for a content item identified by title and content type.
    /// </summary>
    Task<ContentItem?> GetLatestDraftAsync(
        string title,
        Guid contentTypeId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves the latest published version for a content item identified by title and content type.
    /// </summary>
    Task<ContentItem?> GetLatestPublishedAsync(
        string title,
        Guid contentTypeId,
        CancellationToken ct = default
    );

    /// <summary>
    /// Marks a content item as soft-deleted without removing it from the database.
    /// </summary>
    Task SoftDelete(ContentItem contentItem);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    Task SaveChangesAsync(CancellationToken ct = default);
}
