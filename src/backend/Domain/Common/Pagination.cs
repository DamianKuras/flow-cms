namespace Domain.Common;

/// <summary>
/// Represents pagination parameters for querying paged data.
/// </summary>
/// <param name="page">The page number to retrieve (1-based index).</param>
/// <param name="pageSize">The number of items per page.</param>
public class PaginationParameters(int page, int pageSize)
{
    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// Gets the page number to retrieve (1-based index).
    /// </summary>
    public int Page { get; } = page;
}

/// <summary>
/// Represents a page of items along with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items in the paged list.</typeparam>
/// <param name="items">The collection of items in the current page.</param>
/// <param name="page">The current page number (1-based index).</param>
/// <param name="pageSize">The number of items per page.</param>
/// <param name="totalCount">The total number of items across all pages.</param>
public class PagedList<T>(IReadOnlyList<T> items, int page, int pageSize, int totalCount)
{
    /// <summary>
    /// Gets the collection of items in the current page.
    /// </summary>
    public IReadOnlyList<T> Items { get; } = items;

    /// <summary>
    /// Gets the number of items per page.
    /// </summary>
    public int PageSize { get; } = pageSize;

    /// <summary>
    /// Gets the current page number (1-based index).
    /// </summary>
    public int Page { get; } = page;

    /// <summary>
    /// Gets the total number of items across all pages.
    /// </summary>
    public int TotalCount { get; } = totalCount;
}
