using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Command to archive a content type, preventing further modifications or usage in new content items.
/// </summary>
/// <param name="ContentTypeName">The unique name of the content type to archive.</param>
public sealed record ArchiveContentTypeCommand(string ContentTypeName);

/// <summary>
/// Handler for archiving a content type.
/// </summary>
/// <param name="contentTypeRepository">The repository for content types.</param>
/// <param name="authorizationService">The service for role-based authorization.</param>
/// <param name="logger">The logger instance.</param>
public sealed class ArchiveContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService,
    ILogger<ArchiveContentTypeCommandHandler> logger
) : ICommandHandler<ArchiveContentTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        ArchiveContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentType? publication = await contentTypeRepository.GetLatestsPublishedVersion(
            command.ContentTypeName,
            cancellationToken
        );
        if (publication is null)
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

        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Archive,
            new ContentTypeResource(publication.Name),
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

        logger.LogInformation(
            "Starting archive operation for content type '{ContentTypeName}'",
            command.ContentTypeName
        );

        publication.Archive();

        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully archived content type '{ContentTypeName}'",
            command.ContentTypeName
        );

        return Result<Guid>.Success(publication.Id);
    }
}
