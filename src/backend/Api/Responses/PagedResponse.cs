namespace Api.Responses;

/// <summary>
/// Represents a paginated response containing a subset of data along with pagination metadata.
/// </summary>
/// <typeparam name="T">The type of items contained in the paginated data collection.</typeparam>
/// <param name="Data">The collection of items for the current page.</param>
/// <param name="Page">The current page number (typically 1-indexed).</param>
/// <param name="PageSize">The number of items per page.</param>
/// <param name="TotalCount">The total number of items across all pages.</param>
/// <param name="TotalPages">The total number of pages available.</param>
public record PagedResponse<T>(
    IReadOnlyList<T> Data,
    int Page,
    int PageSize,
    int TotalCount,
    int TotalPages
);
