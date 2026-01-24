using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Command to delete a content type by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the content type to delete.</param>
public sealed record DeleteContentTypeCommand(Guid Id);

/// <summary>
/// Handles the deletion of a content type.
/// </summary>
/// <param name="contentTypeRepository">Repository for content type data access.</param>
/// <param name="authorizationService">Service for permission verification.</param>
/// <param name="logger">Logger for tracking operations and diagnostics.</param>
public sealed class DeleteContentTypeHandler(
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService,
    ILogger<DeleteContentTypeHandler> logger
) : ICommandHandler<DeleteContentTypeCommand, Guid>
{
    /// <summary>
    /// Handles the delete content type command by validating existence, checking permissions,
    /// and performing a soft delete operation.
    /// </summary>
    /// <param name="command">The delete command containing the content type ID.</param>
    /// <param name="cancellationToken">Token to cancel the async operation.</param>
    /// <returns>A result containing the deleted content type ID on success, or an error.</returns>
    public async Task<Result<Guid>> Handle(
        DeleteContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Processing DeleteContentType command for ContentTypeId={ContentTypeId}",
            command.Id
        );

        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            command.Id,
            cancellationToken
        );

        if (contentType is null)
        {
            logger.LogWarning(
                "Content type not found for deletion. ContentTypeId={ContentTypeId}",
                command.Id
            );

            return Result<Guid>.Failure(
                Error.NotFound($"Content type with id {command.Id} not found")
            );
        }

        bool allowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.Delete,
            ResourceType.ContentType,
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning(
                "Authorization failed for DeleteContentType ContentTypeName={Name}",
                contentType.Name
            );

            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));
        }

        await contentTypeRepository.SoftDelete(contentType);
        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Successfully deleted content type. ContentTypeId={ContentTypeId}, ContentTypeName={ContentTypeName}",
            contentType.Id,
            contentType.Name
        );

        return Result<Guid>.Success(command.Id);
    }
}
