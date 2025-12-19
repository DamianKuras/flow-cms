using Domain.Common;
using Domain.ContentItems;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Repositories;

/// <summary>
/// Repository for managing content items in the database.
/// </summary>
/// <param name="db">The database context.</param>
public sealed class ContentItemRepository(AppDbContext db) : IContentItemRepository
{
    private readonly AppDbContext _db = db;

    /// <inheritdoc/>
    public async Task AddAsync(ContentItem contentItem, CancellationToken ct = default) =>
        await _db.ContentItems.AddAsync(contentItem);

    /// <inheritdoc/>
    public async Task<int> CountAsync(Guid contentTypeId, CancellationToken ct = default) =>
        await _db.ContentItems.Where(x => x.ContentTypeId == contentTypeId).CountAsync(ct);

    /// <inheritdoc/>
    public async Task DeleteAsync(ContentItem contentItem) => _db.ContentItems.Remove(contentItem);

    /// <inheritdoc/>
    public async Task<IReadOnlyList<PagedContentItem>> Get(
        Guid contentTypeId,
        PaginationParameters paginationParameters,
        CancellationToken cancellationToken = default
    )
    {
        List<PagedContentItem> contentItems = await _db
            .ContentItems.Where(x => x.ContentTypeId == contentTypeId)
            .Skip((paginationParameters.Page - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize)
            .Select(x => new PagedContentItem(x.Id, x.Title))
            .ToListAsync(cancellationToken);
        return contentItems;
    }

    /// <inheritdoc/>
    public async Task<ContentItem?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    ) => await _db.ContentItems.FirstOrDefaultAsync(ci => ci.Id == id, cancellationToken);

    /// <inheritdoc/>
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);

    /// <inheritdoc/>
    public async Task UpdateAsync(ContentItem contentItem) => _db.ContentItems.Update(contentItem);
}
