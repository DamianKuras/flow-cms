using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

public sealed record PublishContentItemCommand(Guid Id);

public sealed class PublishContentItemCommandHandler(
    IContentItemRepository contentItemRepository,
    IAuthorizationService authorizationService,
    ILogger<PublishContentItemCommandHandler> logger
) : ICommandHandler<PublishContentItemCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        PublishContentItemCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentItem? draft = await contentItemRepository.GetByIdAsync(
            command.Id,
            cancellationToken
        );

        if (draft is null)
        {
            logger.LogWarning("Content item '{Id}' not found", command.Id);
            return Result<Guid>.Failure(
                Error.NotFound($"The content item with id {command.Id} was not found.")
            );
        }

        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Publish,
            new ContentItemResource(draft.Id),
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning("Authorization failed for ContentItem.Id={Id}", command.Id);
            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));
        }

        if (draft.Status != ContentItemStatus.Draft)
        {
            logger.LogWarning(
                "Cannot publish content item '{Id}' with status {Status}",
                command.Id,
                draft.Status
            );
            return Result<Guid>.Failure(
                Error.Conflict($"Only drafts can be published. Current status: {draft.Status}.")
            );
        }

        ContentItem? previousPublished = await contentItemRepository.GetLatestPublishedAsync(
            draft.Title,
            draft.ContentTypeId,
            cancellationToken
        );

        ContentItem published = draft.PublishFrom(previousPublished);

        if (previousPublished is not null)
        {
            await contentItemRepository.SoftDelete(previousPublished);
        }

        await contentItemRepository.AddAsync(published, cancellationToken);
        await contentItemRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Published content item '{Title}' as version {Version} with id '{PublishedId}'",
            published.Title,
            published.Version,
            published.Id
        );

        return Result<Guid>.Success(published.Id);
    }
}
