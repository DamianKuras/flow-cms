using Api.Responses;
using Domain.Common;

namespace Api.Mappers;

/// <summary>
/// Provides mapping functions for converting between domain models and API response models.
/// </summary>
public static class ResponseMapper
{
    /// <summary>
    /// Maps a <see cref="PagedList{T}"/> from the domain layer to a <see cref="PagedResponse{T}"/> for API responses.
    /// </summary>
    /// <typeparam name="T">The type of items in the paged collection.</typeparam>
    /// <param name="pagedList">The domain paged list to map.</param>
    /// <returns>A <see cref="PagedResponse{T}"/> containing the items, current page, page size, total count, and calculated total pages.</returns>
    public static PagedResponse<T> MapPagedListToPagedResult<T>(PagedList<T> pagedList) =>
        new(
            pagedList.Items,
            pagedList.Page,
            pagedList.PageSize,
            pagedList.TotalCount,
            (int)Math.Ceiling(pagedList.TotalCount / (double)pagedList.PageSize)
        );
}
