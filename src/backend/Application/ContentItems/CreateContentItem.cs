using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Domain.Services;
using Microsoft.Extensions.Logging;

namespace Application.ContentItems;

/// <summary>
/// Command to create a new content item with validation against a content type definition.
/// </summary>
/// <param name="Title">Title of the new content item. Cannot be null or empty.</param>
/// <param name="ContentTypeId">ID of the content type for this content item.</param>
/// <param name="Values">Dictionary of field values keyed by field ID. Keys must correspond to valid fields defined in the content type.</param>
public sealed record CreateContentItemCommand(
    string Title,
    Guid ContentTypeId,
    Dictionary<Guid, object?> Values
)
{
    /// <summary>
    /// Validates the command data.
    /// </summary>
    /// <returns>
    /// A <see cref="MultiFieldValidationResult"/> containing validation errors for Title and ContentTypeId fields.
    /// Returns a successful result if all structural validations pass.
    /// </returns>
    public MultiFieldValidationResult Validate()
    {
        var multiFieldValidationResult = new MultiFieldValidationResult();

        // Validate Title.
        var titleFieldValidationResult = new ValidationResult("Title");
        if (string.IsNullOrEmpty(Title))
        {
            titleFieldValidationResult.AddError(CONTENT_ITEM_TITLE_IS_REQUIRED);
        }
        multiFieldValidationResult.AddValidationResult(titleFieldValidationResult);

        // Validate ContentTypeId.
        var contentTypeIdValidation = new ValidationResult("ContentTypeId");
        if (ContentTypeId == Guid.Empty)
        {
            contentTypeIdValidation.AddError(CONTENT_TYPE_ID_IS_REQUIRED);
        }

        multiFieldValidationResult.AddValidationResult(contentTypeIdValidation);

        return multiFieldValidationResult;
    }

    private const string CONTENT_ITEM_TITLE_IS_REQUIRED = "Content item title is required.";
    private const string CONTENT_TYPE_ID_IS_REQUIRED = "ContentTypeId is required.";
}

/// <summary>
/// Handles the creation of new content items with validation against content type definitions.
/// </summary>
/// <param name="contentTypeRepository">Repository for accessing content type definitions.</param>
/// <param name="contentItemRepository">Repository for persisting content items.</param>
/// <param name="logger">Logger instance for tracking content item creation operations.</param>
public sealed class CreateContentItemHandler(
    IContentTypeRepository contentTypeRepository,
    IContentItemRepository contentItemRepository,
    ILogger<GetContentItemByIdHandler> logger
) : ICommandHandler<CreateContentItemCommand, Guid>
{
    /// <summary>
    /// Handles the content item creation command by validating the input, creating the content item entity, and persisting it.
    /// </summary>
    /// <param name="command">The command containing content item creation details.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A <see cref="Result{T}"/> containing the newly created content item ID on success, or error details on failure.</returns>
    public async Task<Result<Guid>> Handle(
        CreateContentItemCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Creating content item - Title: {Title}, ContentTypeId: {ContentTypeId}, Values count: {ValuesCount}",
            command.Title,
            command.ContentTypeId,
            command.Values?.Count ?? 0
        );

        // Validate command structure.
        MultiFieldValidationResult validateCommandResult = command.Validate();
        if (validateCommandResult.IsFailure)
        {
            return Result<Guid>.MultiFieldValidationFailure(validateCommandResult);
        }

        // Validate content type exists.
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            command.ContentTypeId,
            cancellationToken
        );

        if (contentType is null)
        {
            return Result<Guid>.Failure(Error.Validation(CONTENT_TYPE_NOT_FOUND));
        }

        // Validate all fields specified in command exist in content type.
        var unknownFields = command
            ?.Values?.Keys.Where(fieldId => !contentType.HasField(fieldId))
            .ToList();

        if (unknownFields is not null && unknownFields.Count != 0)
        {
            logger.LogWarning(
                "Unknown fields found: {UnknownFields}",
                string.Join(", ", unknownFields)
            );
            return Result<Guid>.Failure(
                Error.Conflict($"{UNKNOWN_FIELD_IN_COMMAND}: {string.Join(", ", unknownFields)}")
            );
        }

        var contentItemId = Guid.NewGuid();

        var contentItem = new ContentItem(contentItemId, command.Title, command.ContentTypeId);

        foreach (KeyValuePair<Guid, object?> kv in command.Values)
        {
            ContentItemFieldService.SetValue(contentItem, contentType, kv.Key, kv.Value);
        }

        foreach (Field field in contentType.Fields)
        {
            if (field.IsRequired == false)
            {
                continue;
            }
            if (!contentItem.Values.TryGetValue(field.Id, out ContentFieldValue? value))
            {
                return Result<Guid>.Failure(
                    Error.Validation($"Missing required field with name {field.Name}")
                );
            }
        }

        await contentItemRepository.AddAsync(contentItem, cancellationToken);

        await contentItemRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(contentItemId);
    }

    private const string UNKNOWN_FIELD_IN_COMMAND = "Command has unknown field.";
    private const string CONTENT_TYPE_NOT_FOUND = "Content type not found.";
}
