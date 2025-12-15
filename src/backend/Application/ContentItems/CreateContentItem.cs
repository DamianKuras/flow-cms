using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;

namespace Application.ContentItems;

/// <summary>
/// Command to create a new content item.
/// </summary>
/// <param name="Title">Title of the new content item</param>
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
    /// <returns>Result indicating whether validation succeeded or failed.</returns>
    public MultiFieldValidationResult Validate()
    {
        var multiFieldValidationResult = new MultiFieldValidationResult();

        // Validate Title.
        var TitleFieldValidationResult = new ValidationResult("Title");
        if (string.IsNullOrEmpty(Title))
        {
            TitleFieldValidationResult.AddError("Content item title is required.");
        }
        multiFieldValidationResult.AddValidationResult(TitleFieldValidationResult);

        // Validate ContentTypeId.
        var contentTypeIdValidation = new ValidationResult("ContentTypeId");
        if (ContentTypeId == Guid.Empty)
        {
            contentTypeIdValidation.AddError("ContentTypeId is required.");
        }

        multiFieldValidationResult.AddValidationResult(contentTypeIdValidation);

        // Validate Values.
        var ValuesFieldValidationResult = new ValidationResult("Values");
        if (Values == null || Values.Count == 0)
        {
            ValuesFieldValidationResult.AddError("Values field is empty.");
        }
        multiFieldValidationResult.AddValidationResult(ValuesFieldValidationResult);

        return multiFieldValidationResult;
    }
}

/// <summary>
/// Handles the creation of new content items with validation against content type definitions.
/// </summary>
/// <param name="contentTypeRepository">Repository for accessing content type definitions.</param>
/// <param name="contentItemRepository">Repository for persisting content items.</param>
public sealed class CreateContentItemHandler(
    IContentTypeRepository contentTypeRepository,
    IContentItemRepository contentItemRepository
) : ICommandHandler<CreateContentItemCommand, Guid>
{
    /// <summary>
    /// Handles the content item creation command by validating the input, creating the content item entity, and persisting it.
    /// </summary>
    /// <param name="command">The command containing content item creation details.</param>
    /// <param name="cancellationToken">Cancellation token for async operations.</param>
    /// <returns>A result containing the newly created content item ID on success, or error details on failure.</returns>
    public async Task<Result<Guid>> Handle(
        CreateContentItemCommand command,
        CancellationToken cancellationToken
    )
    {
        // Validate command structure.
        MultiFieldValidationResult validateCommandResult = command.Validate();
        if (validateCommandResult.IsFailure)
        {
            return Result<Guid>.Failure(Error.Validation(validateCommandResult));
        }

        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            command.ContentTypeId,
            cancellationToken
        );

        if (contentType is null)
        {
            return Result<Guid>.Failure(Error.NotFound("Content type not found."));
        }

        // Validate all fields specified in commands exists in content type.
        foreach (KeyValuePair<Guid, object?> kv in command.Values)
        {
            if (!contentType.HasField(kv.Key))
            {
                return Result<Guid>.Failure(Error.Conflict("Command has unknown field"));
            }
        }

        MultiFieldValidationResult fieldValuesValidationResult = ValidateFieldsAgainstContentType(
            command,
            contentType
        );

        if (fieldValuesValidationResult.IsFailure)
        {
            return Result<Guid>.Failure(Error.Validation(fieldValuesValidationResult));
        }

        var contentItemId = Guid.NewGuid();

        var contentFieldValues = new Dictionary<Guid, ContentFieldValue>();
        foreach (KeyValuePair<Guid, object?> kv in command.Values)
        {
            Field field = contentType.FieldsById[kv.Key];
            contentFieldValues.Add(field.Id, new ContentFieldValue(kv.Value));
        }

        var contentItem = new ContentItem(
            contentItemId,
            command.Title,
            command.ContentTypeId,
            contentFieldValues
        );

        await contentItemRepository.AddAsync(contentItem, cancellationToken);

        await contentItemRepository.SaveChangesAsync(cancellationToken);

        return Result<Guid>.Success(contentItemId);
    }

    private static MultiFieldValidationResult ValidateFieldsAgainstContentType(
        CreateContentItemCommand command,
        ContentType contentType
    )
    {
        var fieldValuesValidationResult = new MultiFieldValidationResult();
        foreach (KeyValuePair<Guid, object?> kv in command.Values)
        {
            ValidationResult fieldValidationResult = contentType
                .FieldsById[kv.Key]
                .Validate(kv.Value);
            if (!fieldValidationResult.IsValid)
            {
                fieldValuesValidationResult.AddValidationResult(fieldValidationResult);
            }
        }

        return fieldValuesValidationResult;
    }
}
