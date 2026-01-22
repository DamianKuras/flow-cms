using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Query for retrieving a paginated list of content types with optional filtering and sorting.
/// </summary>
/// <param name="PaginationParameters">Pagination settings including page number and page size.</param>
/// <param name="Sort">Sort expression (e.g., "name.asc", "version.desc").</param>
/// <param name="Status">Filter by content type status (e.g., "draft", "published", "archived").</param>
/// <param name="NameFilter">Content type name search filter.</param>
public sealed record GetContentTypesQuery(
    PaginationParameters PaginationParameters,
    string Sort,
    string Status,
    string NameFilter
);

/// <summary>
/// Response containing paginated content type data.
/// </summary>
/// <param name="Data">Paginated list of paged content types with metadata.</param>
public sealed record GetContentTypeResponse(PagedList<PagedContentType> Data);

/// <summary>
/// Handler for processing <see cref="GetContentTypesQuery"/> requests.
/// </summary>
/// <param name="contentTypeRepository">Repository for content type data access.</param>
/// <param name="authorizationService">Service for validating user permissions.</param>
/// <param name="logger">Logger for logging information.</param>
public sealed class GetContentTypesHandler(
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService,
    ILogger<GetContentTypesHandler> logger
) : IQueryHandler<GetContentTypesQuery, GetContentTypeResponse>
{
    /// <inheritdoc/>
    public async Task<Result<GetContentTypeResponse>> Handle(
        GetContentTypesQuery query,
        CancellationToken cancellationToken
    )
    {
        bool allowed = await authorizationService.IsAllowedForAllAsync(
            CmsAction.List,
            ResourceType.ContentType,
            cancellationToken
        );
        if (!allowed)
        {
            logger.LogWarning("Authorization failed for GetContentTypes");

            return Result<GetContentTypeResponse>.Failure(Error.Forbidden("Forbidden"));
        }

        logger.LogDebug(
            "Retrieving content types with Sort: {Sort}, Status: {Status}, NameFilter: {NameFilter}",
            query.Sort,
            query.Status,
            query.NameFilter
        );
        PagedList<PagedContentType> contentTypes = await contentTypeRepository.Get(
            query.PaginationParameters,
            query.Sort,
            query.Status,
            query.NameFilter,
            cancellationToken
        );
        return Result<GetContentTypeResponse>.Success(new GetContentTypeResponse(contentTypes));
    }
}
