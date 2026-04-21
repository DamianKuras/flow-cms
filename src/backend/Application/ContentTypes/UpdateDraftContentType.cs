using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// DTO for a field in an update-draft command. Set <see cref="ExistingId"/> to preserve
/// an existing field (and its ID); leave it null to create a brand-new field.
/// </summary>
public record UpdateFieldDto(
    Guid? ExistingId,
    string Name,
    FieldTypes Type,
    bool IsRequired,
    List<CreateValidationRuleDto>? ValidationRules = null,
    List<CreateTransformationRuleDto>? TransformationRules = null
);

/// <summary>
/// Replaces the field definitions on a draft content type. Fields present in the current draft
/// but absent from <see cref="Fields"/> are deleted. New entries (null <c>ExistingId</c>) are
/// inserted. Entries with a known <c>ExistingId</c> are updated in-place.
/// </summary>
public sealed record UpdateDraftContentTypeCommand(Guid Id, List<UpdateFieldDto> Fields);

public sealed class UpdateDraftContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    IValidationRuleRegistry validationRuleRegistry,
    ITransformationRuleRegistry transformationRuleRegistry,
    IAuthorizationService authorizationService,
    ILogger<UpdateDraftContentTypeCommandHandler> logger
) : ICommandHandler<UpdateDraftContentTypeCommand, Guid>
{
    public async Task<Result<Guid>> Handle(
        UpdateDraftContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        ContentType? draft = await contentTypeRepository.GetByIdAsync(command.Id, cancellationToken);
        if (draft is null)
        {
            return Result<Guid>.Failure(Error.NotFound($"Content type '{command.Id}' not found."));
        }

        if (draft.Status != ContentTypeStatus.DRAFT)
        {
            return Result<Guid>.Failure(
                Error.Conflict($"Only draft content types can be edited. Current status: {draft.Status}.")
            );
        }

        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Update,
            new ContentTypeResource(draft.Name),
            cancellationToken
        );
        if (!allowed)
            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));

        var validation = new MultiFieldValidationResult();
        foreach (UpdateFieldDto fieldDto in command.Fields)
        {
            var fieldValidation = new ValidationResult(fieldDto.Name);
            if (string.IsNullOrWhiteSpace(fieldDto.Name))
                fieldValidation.AddError("Field name is required.");
            if (!Enum.IsDefined(fieldDto.Type))
                fieldValidation.AddError("Invalid field type.");
            if (fieldDto.ValidationRules is not null)
            {
                foreach (CreateValidationRuleDto rule in fieldDto.ValidationRules)
                {
                    if (!validationRuleRegistry.TryCreate(rule.Type, rule.Parameters, out _))
                        fieldValidation.AddError(ValidationMessages.UnknownValidationRule(rule.Type, fieldDto.Name));
                }
            }
            if (fieldDto.TransformationRules is not null)
            {
                foreach (CreateTransformationRuleDto rule in fieldDto.TransformationRules)
                {
                    if (!transformationRuleRegistry.TryCreate(rule.Type, rule.Parameters, out _))
                        fieldValidation.AddError(ValidationMessages.UnknownTransformationRule(rule.Type, fieldDto.Name));
                }
            }
            validation.AddValidationResult(fieldValidation);
        }
        if (validation.IsFailure)
            return Result<Guid>.MultiFieldValidationFailure(validation);

        // Build the reconciled field list.
        // - For command entries with ExistingId: find the tracked entity and mutate it.
        // - For command entries without ExistingId: create a new Field.
        // EF cascade-deletes any draft fields not present in the new list.
        var existingById = draft.Fields.ToDictionary(f => f.Id);
        var updatedFields = new List<Field>();

        foreach (UpdateFieldDto dto in command.Fields)
        {
            Field field;
            if (dto.ExistingId.HasValue && existingById.TryGetValue(dto.ExistingId.Value, out Field? existing))
            {
                existing.Update(dto.Name, dto.Type, dto.IsRequired);
                field = existing;
            }
            else
            {
                field = new Field(Guid.NewGuid(), dto.Type, dto.Name, dto.IsRequired);
            }

            var validationRules = new List<IValidationRule>();
            foreach (CreateValidationRuleDto rule in dto.ValidationRules ?? [])
                validationRules.Add(validationRuleRegistry.Create(rule.Type, rule.Parameters));
            field.SetValidationRules(validationRules);

            var transformationRules = new List<ITransformationRule>();
            foreach (CreateTransformationRuleDto rule in dto.TransformationRules ?? [])
                transformationRules.Add(transformationRuleRegistry.Create(rule.Type, rule.Parameters));
            field.SetTransformationRules(transformationRules);

            updatedFields.Add(field);
        }

        draft.UpdateFields(updatedFields);

        await contentTypeRepository.UpdateAsync(draft, cancellationToken);
        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Updated draft content type '{Name}' (ID: {Id}) with {Count} fields",
            draft.Name, draft.Id, updatedFields.Count
        );

        return Result<Guid>.Success(draft.Id);
    }
}
