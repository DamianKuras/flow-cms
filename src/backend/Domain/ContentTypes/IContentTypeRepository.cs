using Domain.Common;
using Domain.Fields.Validations;

namespace Domain.ContentTypes;

/// <summary>
/// Repository for managing content type entities.
/// </summary>
public interface IContentTypeRepository
{
    /// <summary>
    /// Retrieves a content type by its unique identifier.
    /// </summary>
    /// <param name="id">The unique identifier of the content type.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The content type if found; otherwise, null.</returns>
    /// <exception cref="InvalidOperationException">
    /// Thrown when validation rules cannot be deserialized from the database.
    /// </exception>
    Task<ContentType?> GetByIdAsync(Guid id, CancellationToken ct = default);

    /// <summary>
    /// Retrieves a paginated, filtered, and sorted list of content types.
    /// </summary>
    /// <param name="paginationParameters">Pagination settings including page number and page size.</param>
    /// <param name="sort">Sort expression (e.g., "name.asc", "version.desc").</param>
    /// <param name="status">Filter by content type status (e.g., "draft", "published", "archived").</param>
    /// <param name="nameFilter">Content type name search filter.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>A paginated list of content types matching the specified criteria.</returns>
    Task<PagedList<PagedContentType>> Get(
        PaginationParameters paginationParameters,
        string sort,
        string status,
        string nameFilter,
        CancellationToken ct = default
    );

    /// <summary>
    /// Adds a new content type to the repository.
    /// </summary>
    /// <param name="contentType">The content type to add.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when validation rules cannot be serialized for storage.
    /// </exception>
    Task AddAsync(ContentType contentType, CancellationToken ct = default);

    /// <summary>
    /// Updates an existing content type in the repository.
    /// </summary>
    /// <param name="contentType">The content type to update.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <exception cref="InvalidOperationException">
    /// Thrown when validation rules cannot be serialized for storage.
    /// </exception>
    Task UpdateAsync(ContentType contentType, CancellationToken ct = default);

    /// <summary>
    /// Persists all pending changes to the database.
    /// </summary>
    /// <param name="ct">Cancellation token for async operation.</param>
    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>
    /// Retrieves the latest version number for a content type with the specified name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The highest version number, or null if no content type with that name exists.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="contentTypeName"/> is null or whitespace.
    /// </exception>
    Task<int?> GetLatestVersion(string contentTypeName, CancellationToken ct = default);

    /// <summary>
    /// Retrieves the latest draft version of the content type with the specified name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The latest draft content type if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="contentTypeName"/> is null or whitespace.
    /// </exception>
    Task<ContentType?> GetLatestDraftVersion(
        string contentTypeName,
        CancellationToken ct = default
    );

    /// <summary>
    /// Retrieves the latest published version of the content type with the specified name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type.</param>
    /// <param name="ct">Cancellation token for async operation.</param>
    /// <returns>The latest published content type if found; otherwise, null.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="contentTypeName"/> is null or whitespace.
    /// </exception>
    Task<ContentType?> GetLatestsPublishedVersion(
        string contentTypeName,
        CancellationToken ct = default
    );

    /// <summary>
    /// Marks a content type as deleted without removing it from the database (soft delete).
    /// </summary>
    /// <param name="contentType">The content type to soft delete.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
    Task SoftDelete(ContentType contentType);
}
