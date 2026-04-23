using Domain.Common;
using Domain.Fields.Validations;

namespace Domain.ContentTypes;

/// <summary>Repository for content type persistence operations.</summary>
public interface IContentTypeRepository
{
    Task<ContentType?> GetByIdAsync(Guid id, CancellationToken ct = default);

    Task<PagedList<PagedContentType>> Get(
        PaginationParameters paginationParameters,
        string sort,
        string status,
        string nameFilter,
        CancellationToken ct = default
    );

    Task AddAsync(ContentType contentType, CancellationToken ct = default);

    Task UpdateAsync(ContentType contentType, CancellationToken ct = default);

    Task SaveChangesAsync(CancellationToken ct = default);

    /// <summary>Returns the highest version number for the given name, or null if no row exists.</summary>
    Task<int?> GetLatestVersion(string contentTypeName, CancellationToken ct = default);

    Task<ContentType?> GetLatestDraftVersion(
        string contentTypeName,
        CancellationToken ct = default
    );

    Task<ContentType?> GetLatestsPublishedVersion(
        string contentTypeName,
        CancellationToken ct = default
    );

    Task SoftDelete(ContentType contentType);

    /// <summary>
    /// Returns one summary per content type name, grouping the current published
    /// and draft row IDs together. Results are ordered by name.
    /// </summary>
    Task<IReadOnlyList<ContentTypeNameSummary>> GetNameSummariesAsync(CancellationToken ct = default);
}
