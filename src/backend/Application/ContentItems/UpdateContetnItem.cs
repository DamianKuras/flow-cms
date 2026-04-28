using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Domain.Permissions;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

/// <param name="ContentItemId">ID of the content item to update.</param>
/// <param name="Values">Dictionary of field values keyed by field ID.</param>
public record UpdateContentItemCommand(Guid ContentItemId, Dictionary<Guid, object?> Values);

/// <param name="contentItemRepository">Repository for content item data access.</param>
/// <param name="contentTypeRepository">Repository for content type data access.</param>
/// <param name="authorizationService">Service for permission validation.</param>
/// <param name="logger">Logger for diagnostic information.</param>
public sealed class UpdateContentItemHandler(
    IContentItemRepository contentItemRepository,
    IContentTypeRepository contentTypeRepository,
    IAuthorizationService authorizationService,
    ILogger<UpdateContentItemHandler> logger
) : ICommandHandler<UpdateContentItemCommand, Guid>
{
    /// <inheritdoc/>
    public async Task<Result<Guid>> Handle(
        UpdateContentItemCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentItem? contentItem = await contentItemRepository.GetByIdAsync(
            command.ContentItemId,
            cancellationToken
        );

        if (contentItem is null)
        {
            return Result<Guid>.Failure(
                Error.NotFound($"Content item with ID '{command.ContentItemId}' was not found")
            );
        }

        if (contentItem.Status != ContentItemStatus.Draft)
        {
            return Result<Guid>.Failure(
                Error.Conflict("Only draft content items can be updated.")
            );
        }

        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            contentItem.ContentTypeId,
            cancellationToken
        );

        if (contentType is null)
        {
            logger.LogError(
                "Content type {ContentTypeId} not found for content item {ContentItemId}",
                contentItem.ContentTypeId,
                command.ContentItemId
            );
            return Result<Guid>.Failure(
                Error.Infrastructure(
                    $"Content type '{contentItem.ContentTypeId}' does not exist for content item '{command.ContentItemId}'"
                )
            );
        }

        bool isAllowed = await authorizationService.IsAllowedAsync(
            CmsAction.Update,
            new ContentTypeResource(contentType.Name),
            cancellationToken
        );
        if (!isAllowed)
        {
            return Result<Guid>.Failure(
                Error.Forbidden("You do not have permission to update this content item")
            );
        }

        List<Guid> unknownFields = command
            .Values.Keys.Where(fieldId => !contentType.HasField(fieldId))
            .ToList();

        if (unknownFields.Count != 0)
        {
            return Result<Guid>.Failure(
                Error.Conflict($"Unknown field(s) in update: {string.Join(", ", unknownFields)}")
            );
        }

        var fieldValidationResult = new MultiFieldValidationResult();
        foreach (KeyValuePair<Guid, object?> kv in command.Values)
        {
            Result setValueResult = ContentItemFieldService.SetValue(
                contentItem,
                contentType,
                kv.Key,
                kv.Value
            );
            if (setValueResult.IsFailure)
            {
                if (setValueResult.FailureKind == FailureKind.FieldValidation)
                {
                    fieldValidationResult.AddValidationResult(setValueResult.ValidationResult!);
                }
                else
                {
                    return Result<Guid>.Failure(setValueResult.Error!);
                }
            }
        }

        if (fieldValidationResult.IsFailure)
        {
            return Result<Guid>.MultiFieldValidationFailure(fieldValidationResult);
        }

        await contentItemRepository.UpdateAsync(contentItem);
        await contentItemRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(command.ContentItemId);
    }
}
