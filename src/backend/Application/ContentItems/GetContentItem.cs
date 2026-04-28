using System.Text.Json;
using Application.Interfaces;
using Domain;
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
    IMigrationJobRepository migrationJobRepository,
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

        // Retrieve content type — may be null when the item still points at a superseded
        // (soft-deleted) published schema version.
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            contentItem.ContentTypeId,
            cancellationToken
        );

        if (contentType is null)
        {
            // Look for a lazy migration job that covers this item's schema.
            MigrationJob? lazyJob = await migrationJobRepository.FindLazyJobForSchemaAsync(
                contentItem.ContentTypeId,
                cancellationToken
            );

            if (lazyJob is null)
            {
                logger.LogError(
                    "Content type {ContentTypeId} not found and no lazy migration job exists for item {ContentItemId}",
                    contentItem.ContentTypeId, query.Id
                );
                return Result<ContentItemDto>.Failure(
                    Error.Infrastructure(
                        $"Content type '{contentItem.ContentTypeId}' does not exist for content item '{query.Id}'"
                    )
                );
            }

            contentType = await contentTypeRepository.GetByIdAsync(lazyJob.ToSchemaId, cancellationToken);
            if (contentType is null)
            {
                logger.LogError(
                    "Target schema {ToSchemaId} of migration job {JobId} not found",
                    lazyJob.ToSchemaId, lazyJob.Id
                );
                return Result<ContentItemDto>.Failure(
                    Error.Infrastructure($"Target schema '{lazyJob.ToSchemaId}' not found.")
                );
            }

            contentItem.MigrateToSchema(contentType);
            await contentItemRepository.UpdateAsync(contentItem);
            lazyJob.RecordItemMigrated();
            await migrationJobRepository.UpdateAsync(lazyJob, cancellationToken);
            await contentItemRepository.SaveChangesAsync(cancellationToken);

            logger.LogInformation(
                "Lazy-migrated content item {ItemId} from schema {From} to {To}",
                contentItem.Id, lazyJob.FromSchemaId, lazyJob.ToSchemaId
            );
        }
        // Check permission to read this content item.
        bool isAllowed = await authorizationService.IsAllowedAsync(
            CmsAction.Read,
            new ContentTypeResource(contentType!.Name),
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
            // Values whose field no longer exists in the schema (orphaned by migration) are skipped.
            if (!fieldLookup.TryGetValue(fieldId, out string? fieldName))
                continue;

            valuesDto[fieldName] = new ContentFieldValueDto(
                JsonSerializer.SerializeToElement(fieldValue.Value)
            );
        }
        var contentItemDto = new ContentItemDto(
            contentItem.Id,
            contentItem.Title,
            contentItem.ContentTypeId,
            contentItem.Status.ToString(),
            contentItem.Version,
            valuesDto
        );

        logger.LogInformation("Successfully retrieved content item {ContentItemId}", query.Id);
        return Result<ContentItemDto>.Success(contentItemDto);
    }
}
