using Application.Fields;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Permissions;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.ContentTypes;

/// <summary>
/// Command to create a new content type.
/// </summary>
/// <param name="Name">The name of the content type.</param>
/// <param name="Fields">The field definitions for the content type.</param>
public sealed record CreateContentTypeCommand(string Name, List<CreateFieldDto> Fields)
{
    /// <summary>
    /// Validates the command data.
    /// </summary>
    /// <returns>Result indicating whether validation succeeded or failed.</returns>
    public MultiFieldValidationResult Validate(
        IValidationRuleRegistry validationRuleRegistry,
        ITransformationRuleRegistry transformationRuleRegistry
    )
    {
        var multiFieldValidationResult = new MultiFieldValidationResult();
        var nameFieldValidationResult = new ValidationResult("Name");
        if (string.IsNullOrWhiteSpace(Name))
        {
            nameFieldValidationResult.AddError(ValidationMessages.NAME_REQUIRED);
        }
        multiFieldValidationResult.AddValidationResult(nameFieldValidationResult);
        var fieldsFieldValidationResult = new ValidationResult("Fields");
        if (Fields == null || Fields.Count == 0)
        {
            fieldsFieldValidationResult.AddError(ValidationMessages.FIELDS_REQUIRED);
        }
        else
        {
            foreach (CreateFieldDto field in Fields)
            {
                var currentFieldValidationResult = new ValidationResult(field.Name);

                if (!Enum.IsDefined(field.Type))
                {
                    currentFieldValidationResult.AddError("Invalid field type.");
                }
                if (field.ValidationRules is not null)
                {
                    foreach (CreateValidationRuleDto rule in field.ValidationRules)
                    {
                        if (!validationRuleRegistry.TryCreate(rule.Type, rule.Parameters, out _))
                        {
                            currentFieldValidationResult.AddError(
                                ValidationMessages.UnknownValidationRule(rule.Type, field.Name)
                            );
                        }
                    }
                }
                if (field.TransformationRules is not null)
                {
                    foreach (CreateTransformationRuleDto rule in field.TransformationRules)
                    {
                        if (
                            !transformationRuleRegistry.TryCreate(rule.Type, rule.Parameters, out _)
                        )
                        {
                            currentFieldValidationResult.AddError(
                                ValidationMessages.UnknownTransformationRule(rule.Type, field.Name)
                            );
                        }
                    }
                }

                multiFieldValidationResult.AddValidationResult(currentFieldValidationResult);
            }
        }
        multiFieldValidationResult.AddValidationResult(fieldsFieldValidationResult);

        return multiFieldValidationResult;
    }
}

/// <summary>
/// Handler for creating a new content type with versioning support.
/// </summary>
/// <param name="contentTypeRepository">Content type repository.</param>
/// <param name="validationRuleRegistry">Registry for validation rules.</param>
/// <param name="transformationRuleRegistry"></param>
/// <param name="authorizationService">Authorization service for checking if user is allowed to perform this action.</param>
/// <param name="logger"></param>
public sealed class CreateContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    IValidationRuleRegistry validationRuleRegistry,
    ITransformationRuleRegistry transformationRuleRegistry,
    IAuthorizationService authorizationService,
    ILogger<CreateContentTypeCommandHandler> logger
) : ICommandHandler<CreateContentTypeCommand, Guid>
{
    private const int INITIAL_VERSION = 1;

    /// <summary>
    /// Handles the content type creation command.
    /// </summary>
    /// <param name="command">The command containing content type data.</param>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>Result containing the ID of the created content type.</returns>
    public async Task<Result<Guid>> Handle(
        CreateContentTypeCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Handling CreateContentTypeCommand for Name={Name}", command.Name);
        bool allowed = await authorizationService.IsAllowedAsync(
            CmsAction.Create,
            new ContentTypeResource(Guid.Empty), // global create
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning(
                "Authorization failed for CreateContentTypeCommand Name={Name}",
                command.Name
            );

            return Result<Guid>.Failure(Error.Forbidden("Forbidden"));
        }
        // Validate command structure.
        MultiFieldValidationResult validateCommandResult = command.Validate(
            validationRuleRegistry,
            transformationRuleRegistry
        );

        if (validateCommandResult.IsFailure)
        {
            logger.LogWarning(
                "Validation failed for CreateContentTypeCommand Name={Name} Errors={Errors}",
                command.Name,
                validateCommandResult
            );
            return Result<Guid>.MultiFieldValidationFailure(validateCommandResult);
        }

        // Get version for content.
        int? latestVersion = await contentTypeRepository.GetLatestVersion(
            command.Name,
            cancellationToken
        );
        int version = latestVersion ?? INITIAL_VERSION;
        var contentTypeId = Guid.NewGuid();

        List<Field> domainFields = BuildDomainFields(command.Fields);

        logger.LogInformation(
            "Creating content type '{Name}' version {Version} with {FieldCount} fields",
            command.Name,
            version,
            domainFields.Count
        );

        var contentType = new ContentType(contentTypeId, command.Name, domainFields, version);

        await contentTypeRepository.AddAsync(contentType, cancellationToken);
        await contentTypeRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation("Created content type with ID={ContentTypeId}", contentTypeId);

        return Result<Guid>.Success(contentTypeId);
    }

    /// <summary>
    /// Creates domain Field entities from DTOs.
    /// </summary>
    /// <param name="fields">List of CreateFieldDto objects used to construct the domain fields.</param>
    /// <returns>List of created fields.</returns>
    private List<Field> BuildDomainFields(IEnumerable<CreateFieldDto> fields)
    {
        var list = new List<Field>();

        foreach (CreateFieldDto fieldDto in fields)
        {
            var field = new Field(
                Guid.NewGuid(),
                fieldDto.Type,
                fieldDto.Name,
                fieldDto.IsRequired
            );
            List<IValidationRule> validationRules = new();
            if (fieldDto.ValidationRules is not null)
            {
                foreach (CreateValidationRuleDto rule in fieldDto.ValidationRules)
                {
                    IValidationRule ruleOut = validationRuleRegistry.Create(
                        rule.Type,
                        rule.Parameters
                    );
                    validationRules.Add(ruleOut);
                }
            }
            field.SetValidationRules(validationRules);
            List<ITransformationRule> transformationRules = new();
            if (fieldDto.TransformationRules is not null)
            {
                foreach (CreateTransformationRuleDto rule in fieldDto.TransformationRules)
                {
                    ITransformationRule rule_out = transformationRuleRegistry.Create(
                        rule.Type,
                        rule.Parameters
                    );
                    transformationRules.Add(rule_out);
                }
            }
            field.SetTransformationRules(transformationRules);
            list.Add(field);
        }

        return list;
    }
}

/// <summary>
/// Centralized validation messages for content type operations.
/// </summary>
public static class ValidationMessages
{
    /// <summary>
    /// Name is required.
    /// </summary>
    public const string NAME_REQUIRED = "Name is required.";

    /// <summary>
    /// Fields are required.
    /// </summary>
    public const string FIELDS_REQUIRED = "Fields field is empty.";

    /// <summary>
    /// Unknown Validation Rule.
    /// </summary>
    /// <param name="ruleType">The rule type of the validation rule.</param>
    /// <param name="fieldName">The field name of field containing this validation rule. </param>
    /// <returns>String with error message</returns>
    public static string UnknownValidationRule(string ruleType, string fieldName) =>
        $"Unknown validation rule type '{ruleType}' in field '{fieldName}'.";

    /// <summary>
    /// Unknown Transformation Rule.
    /// </summary>
    /// <param name="ruleType">The rule type of the transformation rule.</param>
    /// <param name="fieldName">The field name of field containing this transformation rule.</param>
    /// <returns>String with error message</returns>
    public static string UnknownTransformationRule(string ruleType, string fieldName) =>
        $"Unknown transformation rule type '{ruleType}' in field '{fieldName}'.";
}
