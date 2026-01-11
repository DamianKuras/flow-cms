using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Command to publish a content type from draft to published status.
/// </summary>
/// <param name="ContentTypeName">The unique name of the content type to publish.</param>
public sealed record PublishContentTypeCommand(string ContentTypeName);

/// <summary>
/// Handles the publication of a content type by transitioning the latest draft version
/// to published status, managing versioning, and archiving previous publications.
/// </summary>
/// <param name="contentTypeRepository">Repository for content type persistence operations.</param>
/// <param name="logger">Logger instance for structured logging.</param>
public sealed class PublishContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    ILogger<PublishContentTypeCommandHandler> logger
) : ICommandHandler<PublishContentTypeCommand, Guid>
{
    /// <summary>
    /// Executes the publish content type command.
    /// </summary>
    /// <param name="command">The command containing the content type name to publish.</param>
    /// <param name="cancellationToken">Cancellation token to observe.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing the ID of the newly published content type on success,
    /// or an error if the operation fails.
    /// </returns>
    public async Task<Result<Guid>> Handle(
        PublishContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Starting publish operation for content type '{ContentTypeName}'",
            command.ContentTypeName
        );

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

        logger.LogInformation(
            "Successfully published content type '{ContentTypeName}' with ID {PublishedId} and version {Version}",
            command.ContentTypeName,
            publishedContentType.Id,
            publishedContentType.Version
        );

        return Result<Guid>.Success(publishedContentType.Id);
    }
}
