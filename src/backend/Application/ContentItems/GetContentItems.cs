using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

/// <summary>
/// Query to retrieve a paginated list of content items for a specific content type.
/// </summary>
/// <param name="ContentTypeId">The unique identifier of the content type to filter by.</param>
/// <param name="PaginationParameters">Pagination settings including page number and page size.</param>
public record GetContentItemsQuery(Guid ContentTypeId, PaginationParameters PaginationParameters);

/// <summary>
/// Response containing a paginated list of content items and total count.
/// </summary>
/// <param name="PagedList">The list of content items for the current page.</param>
/// <param name="TotalCount">The total number of content items across all pages.</param>
public record GetContentItemsResponse(IReadOnlyList<PagedContentItem> PagedList, int TotalCount);

/// <summary>
/// Handler for retrieving paginated content items by content type with authorization.
/// </summary>
/// <param name="contentItemRepository">Repository for content item data access.</param>
/// <param name="contentTypeRepository">Repository for content type data access.</param>
/// <param name="authorizationService">Service for permission validation.</param>
/// <param name="logger">Logger for diagnostic information.</param>
public sealed class GetContentItemsHandler(
    IContentItemRepository contentItemRepository,
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService,
    ILogger<GetContentItemsHandler> logger
) : IQueryHandler<GetContentItemsQuery, GetContentItemsResponse>
{
    /// <summary>
    /// Handles the content items retrieval query with authorization and pagination.
    /// </summary>
    /// <param name="query">The query containing content type ID and pagination parameters.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing paginated content items or an error.</returns>
    public async Task<Result<GetContentItemsResponse>> Handle(
        GetContentItemsQuery query,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Retrieving content items for content type {ContentTypeId} (Page: {Page}, PageSize: {PageSize})",
            query.ContentTypeId,
            query.PaginationParameters.Page,
            query.PaginationParameters.PageSize
        );
        // Retrieve content type.
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(query.ContentTypeId);
        if (contentType is null)
        {
            return Result<GetContentItemsResponse>.Failure(
                Error.NotFound($"Content type with id {query.ContentTypeId} doesn't exist")
            );
        }

        // Check permission to list content items of this type.
        bool isAllowed = await authorizationService.IsAllowedAsync(
            CmsAction.Read,
            new ContentTypeResource(query.ContentTypeId),
            cancellationToken
        );

        if (!isAllowed)
        {
            logger.LogWarning(
                "User not authorized to list content items of type {ContentTypeId}",
                query.ContentTypeId
            );

            return Result<GetContentItemsResponse>.Failure(
                Error.NotFound($"Content type with ID '{query.ContentTypeId}' was not found")
            );
        }

        // Retrieve paginated content items.
        IReadOnlyList<PagedContentItem> items = await contentItemRepository.Get(
            query.ContentTypeId,
            query.PaginationParameters,
            cancellationToken
        );

        int totalCount = await contentItemRepository.CountAsync(
            query.ContentTypeId,
            cancellationToken
        );

        logger.LogInformation(
            "Successfully retrieved {ItemCount} content items (Total: {TotalCount}) for content type {ContentTypeId}",
            items.Count,
            totalCount,
            query.ContentTypeId
        );

        return Result<GetContentItemsResponse>.Success(
            new GetContentItemsResponse(items, totalCount)
        );
    }
}
