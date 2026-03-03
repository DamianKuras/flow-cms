using Application.Interfaces;
using Domain.Common;
using Domain.Permissions;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Users;

/// <summary>
/// Query for retrieving a paginated list of users.
/// </summary>
/// <param name="PaginationParameters">Pagination settings including page number and page size.</param>
public record GetUsersQuery(PaginationParameters PaginationParameters);

/// <summary>
/// Response containing paginated user data.
/// </summary>
/// <param name="PagedList">The paginated list of users.</param>
/// <param name="TotalCount">The total number of users matching the query.</param>
public record GetUsersResponse(IReadOnlyList<PagedUser> PagedList, int TotalCount);

/// <summary>
/// Handler for processing <see cref="GetUsersQuery"/> requests.
/// </summary>
/// <param name="userRepository">Repository for user data access.</param>
/// <param name="authorizationService">Service for validating user permissions.</param>
/// <param name="logger">Logger for logging diagnostics.</param>
public sealed class GetUsersQueryHandler(
    IUserRepository userRepository,
    IAuthorizationService authorizationService,
    ILogger<GetUsersQueryHandler> logger
) : IQueryHandler<GetUsersQuery, GetUsersResponse>
{
    /// <summary>
    /// Handles the retrieve users query by checking permissions and querying the database.
    /// </summary>
    /// <param name="query">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the paginated users list.</returns>
    public async Task<Result<GetUsersResponse>> Handle(
        GetUsersQuery query,
        CancellationToken cancellationToken
    )
    {
        bool isAllowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.List,
            ResourceType.User,
            cancellationToken
        );

        if (!isAllowed)
        {
            logger.LogWarning("Authorization failed for GetUsers");
            return Result<GetUsersResponse>.Failure(Error.Forbidden("Forbidden"));
        }

        int totalCount = await userRepository.CountAsync(cancellationToken);

        // Retrieve paginated users.
        IReadOnlyList<PagedUser> users = await userRepository.Get(
            query.PaginationParameters,
            cancellationToken
        );
        
        return Result<GetUsersResponse>.Success(new GetUsersResponse(users, totalCount));
    }
}
