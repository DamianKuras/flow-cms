using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Publishes a draft content type by name.
/// <paramref name="MigrationMode"/> is ignored when there is no previous published version.
/// </summary>
public sealed record PublishContentTypeCommand(
    string ContentTypeName,
    MigrationMode MigrationMode = MigrationMode.Lazy
);

/// <summary>Transitions the latest draft to published, archives the previous publication, and creates a migration job when needed.</summary>
public sealed class PublishContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    IContentItemRepository contentItemRepository,
    IMigrationJobRepository migrationJobRepository,
    IAuthorizationService authorizationService,
    IUserContext userContext,
    ILogger<PublishContentTypeCommandHandler> logger
) : ICommandHandler<PublishContentTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        PublishContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentType? draft = await contentTypeRepository.GetLatestDraftVersion(
            command.ContentTypeName,
            cancellationToken
        );
        if (draft is null)
        {
            logger.LogWarning(
                "Content type '{ContentTypeName}' not found",
                command.ContentTypeName
            );
            return Result<Guid>.Failure(
                Error.NotFound(
                    $"The content type with name {command.ContentTypeName} was not found."
                )
            );
        }

        logger.LogInformation(
            "Starting publish operation for content type '{ContentTypeName}'",
            command.ContentTypeName
        );

        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Publish,
            new ContentTypeResource(draft.Name),
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning(
                "Authorization failed for ContentTypeName={ContentTypeName}",
                command.ContentTypeName
            );

            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));
        }

        logger.LogDebug(
            "Found draft content type '{ContentTypeName}' with ID {ContentTypeId} and version {Version}",
            draft.Name,
            draft.Id,
            draft.Version
        );

        if (draft.Status != ContentTypeStatus.DRAFT)
        {
            logger.LogWarning(
                "Cannot publish content type '{ContentTypeName}' with status {Status}. Only DRAFT status can be published",
                command.ContentTypeName,
                draft.Status
            );
            return Result<Guid>.Failure(
                Error.Conflict($"Only drafts can be published. Current status: {draft.Status}.")
            );
        }

        ContentType? previousPublication = await contentTypeRepository.GetLatestsPublishedVersion(
            command.ContentTypeName,
            cancellationToken
        );

        int newVersion = previousPublication is not null ? previousPublication.Version + 1 : 1;

        if (previousPublication is not null)
        {
            logger.LogInformation(
                "Found previous published version {PreviousVersion} for content type '{ContentTypeName}'. New version will be {NewVersion}",
                previousPublication.Version,
                command.ContentTypeName,
                newVersion
            );
        }
        else
        {
            logger.LogInformation(
                "No previous published version found for content type '{ContentTypeName}'. This will be version {NewVersion}",
                command.ContentTypeName,
                newVersion
            );
        }

        ContentType publishedContentType = draft.PublishFrom(
            previousPublished: previousPublication
        );

        if (previousPublication is not null)
        {
            await contentTypeRepository.SoftDelete(previousPublication);
        }

        await contentTypeRepository.AddAsync(publishedContentType, cancellationToken);
        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        // When upgrading an existing published schema, create a migration job so that
        // items still pointing at the old schema row can be updated.
        if (previousPublication is not null)
        {
            int itemCount = await contentItemRepository.CountAsync(
                previousPublication.Id,
                cancellationToken
            );
            if (itemCount > 0)
            {
                string author = userContext.IsAuthenticated ? userContext.UserId.ToString() : "system";
                var job = new MigrationJob(
                    id: Guid.NewGuid(),
                    fromSchemaId: previousPublication.Id,
                    toSchemaId: publishedContentType.Id,
                    mode: command.MigrationMode,
                    createdBy: author,
                    totalItemsCount: itemCount
                );
                await migrationJobRepository.AddAsync(job, cancellationToken);
                await migrationJobRepository.SaveChangesAsync(cancellationToken);

                logger.LogInformation(
                    "Created {Mode} migration job {JobId} for {Count} items ({From} → {To})",
                    command.MigrationMode, job.Id, itemCount, previousPublication.Id, publishedContentType.Id
                );
            }
        }

        logger.LogInformation(
            "Successfully published content type '{ContentTypeName}' with ID {PublishedId} and version {Version}",
            command.ContentTypeName,
            publishedContentType.Id,
            publishedContentType.Version
        );

        return Result<Guid>.Success(publishedContentType.Id);
    }
}
