using System.Text.Json;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

/// <summary>
/// Query to retrieve a content item by its unique identifier.
/// </summary>
/// <param name="Id">The unique identifier of the content item to retrieve.</param>
public record GetContentItemQuery(Guid Id);

/// <summary>
/// Handler for retrieving a content item by ID.
/// </summary>
/// <param name="contentItemRepository">Repository for content item data access.</param>
/// <param name="contentTypeRepository">Repository for content type data access.</param>
/// <param name="logger">Logger for diagnostic information.</param>
/// <param name="authorizationService">Service for permission validation.</param>
public sealed class GetContentItemByIdHandler(
    IContentItemRepository contentItemRepository,
    IContentTypeRepository contentTypeRepository,
    ILogger<GetContentItemByIdHandler> logger,
    IAuthorizationService authorizationService
) : IQueryHandler<GetContentItemQuery, ContentItemDto>
{
    /// <summary>
    /// Handles the content item retrieval query with authorization checks and field mapping.
    /// </summary>
    /// <param name="query">The query containing the content item ID.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the content item DTO or an error.</returns>
    public async Task<Result<ContentItemDto>> Handle(
        GetContentItemQuery query,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Retrieving content item with ID: {ContentItemId}", query.Id);

        // Retrieve content item.
        ContentItem? contentItem = await contentItemRepository.GetByIdAsync(
            query.Id,
            cancellationToken
        );

        if (contentItem is null)
        {
            logger.LogWarning("Content item with ID {ContentItemId} not found", query.Id);
            return Result<ContentItemDto>.Failure(
                Error.NotFound($"Content item with ID '{query.Id}' was not found")
            );
        }

        // Retrieve content type.
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            contentItem.ContentTypeId,
            cancellationToken
        );

        if (contentType is null)
        {
            logger.LogError(
                "Content type with ID {ContentTypeId} not found for content item {ContentItemId}",
                contentItem.ContentTypeId,
                query.Id
            );
            return Result<ContentItemDto>.Failure(
                Error.Infrastructure(
                    $"Content type '{contentItem.ContentTypeId}' does not exist for content item '{query.Id}'"
                )
            );
        }
        // Check permission to read this content item.
        bool isAllowed = await authorizationService.IsAllowedAsync(
            CmsAction.Read,
            new ContentTypeResource(contentItem.ContentTypeId),
            cancellationToken
        );
        if (!isAllowed)
        {
            logger.LogWarning(
                "User not authorized to read content item {ContentItemId} of type {ContentTypeId}",
                query.Id,
                contentItem.ContentTypeId
            );
            return Result<ContentItemDto>.Failure(
                Error.Forbidden("You do not have permission to read this content item")
            );
        }

        // Map to response format.
        var valuesDto = new Dictionary<string, ContentFieldValueDto>();
        var fieldLookup = contentType.Fields.ToDictionary(f => f.Id, f => f.Name);

        foreach ((Guid fieldId, ContentFieldValue? fieldValue) in contentItem.Values)
        {
            if (!fieldLookup.TryGetValue(fieldId, out string? fieldName))
            {
                logger.LogWarning(
                    "Field with ID {FieldId} not found in content type {ContentTypeId}",
                    fieldId,
                    contentType.Id
                );
                return Result<ContentItemDto>.Failure(
                    Error.Infrastructure(
                        $"Field '{fieldId}' in content item does not exist in content type definition"
                    )
                );
            }

            valuesDto[fieldName] = new ContentFieldValueDto(
                JsonSerializer.SerializeToElement(fieldValue.Value)
            );
        }
        var contentItemDto = new ContentItemDto(
            contentItem.Id,
            contentItem.Title,
            contentItem.ContentTypeId,
            contentItem.Status.ToString(),
            valuesDto
        );

        logger.LogInformation("Successfully retrieved content item {ContentItemId}", query.Id);
        return Result<ContentItemDto>.Success(contentItemDto);
    }
}
