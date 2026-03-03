using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

/// <summary>
/// Command to delete a content item by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the content item to delete.</param>
public sealed record DeleteContentItemCommand(Guid Id);

/// <summary>
/// Handler for deleting a content item.
/// </summary>
/// <param name="contentItemRepository">The repository for content items.</param>
/// <param name="authorizationService">The service for role-based authorization.</param>
/// <param name="logger">The logger instance.</param>
public sealed class DeleteContentItemCommandHandler(
    IContentItemRepository contentItemRepository,
    IAuthorizationService authorizationService,
    ILogger<DeleteContentItemCommandHandler> logger
) : ICommandHandler<DeleteContentItemCommand, Guid>
{
    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(
        DeleteContentItemCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentItem? contentItem = await contentItemRepository.GetByIdAsync(
            command.Id,
            cancellationToken
        );
        
        if (contentItem is null)
        {
            logger.LogWarning("Content item with id '{Id}' not found for deletion", command.Id);
            return Result<Guid>.Failure(Error.NotFound($"Content item with id {command.Id} not found"));
        }

        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Delete,
            new ContentItemResource(contentItem.Id),
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning("Authorization failed for deleting ContentItem.Id={Id}", command.Id);
            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));
        }

        logger.LogInformation("Deleting content item '{Id}'", command.Id);

        await contentItemRepository.DeleteAsync(contentItem);
        
        logger.LogInformation("Successfully deleted content item '{Id}'", command.Id);
        
        return Result<Guid>.Success(command.Id);
    }
}
