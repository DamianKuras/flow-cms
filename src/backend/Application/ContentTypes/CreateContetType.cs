using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;

namespace Application.ContentTypes;

/// <summary>
/// Command to create a new content type.
/// </summary>
/// <param name="Name">The name of the content type.</param>
/// <param name="Fields">The field definitions for the content type.</param>
public sealed record CreateContentTypeCommand(
    string Name,
    List<CreateFieldDto> Fields
)
{
    /// <summary>
    /// Validates the command data.
    /// </summary>
    /// <returns>Result indicating whether validation succeeded or failed.</returns>
    public Result Validate()
    {
        List<Error> errors = [];

        if (string.IsNullOrWhiteSpace(Name))
        {
            errors.Add(Error.Validation("Name field is empty."));
        }

        if (Fields == null || Fields.Count == 0)
        {
            errors.Add(Error.Validation("Fields field is empty."));
        }

        return errors.Count > 0 ? Result.Failure(errors) : Result.Success();
    }
}

/// <summary>
/// Handler for creating a new content type with versioning support.
/// </summary>
/// <param name="contentTypeRepository">Content type repository.</param>
/// <param name="validationRuleRegistry">Registry for validation rules.</param>
public sealed class CreateContentTypeCommandHandler(
    IContentTypeRepository contentTypeRepository,
    IValidationRuleRegistry validationRuleRegistry
) : ICommandHandler<CreateContentTypeCommand, Guid>
{
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
        // Validate command structure.
        var validateCommandResult = command.Validate();
        if (validateCommandResult.IsFailure)
        {
            return Result<Guid>.Failure(validateCommandResult.Errors!);
        }
        // Validate all validation rules are valid and registered.
        var validationRuleValidationResult = ValidateValidationRules(
            command.Fields,
            validationRuleRegistry
        );
        if (validationRuleValidationResult.IsFailure)
            return Result<Guid>.Failure(validationRuleValidationResult.Errors!);

        // Get version for content.
        int latestVersion = await contentTypeRepository.GetLatestVersion(
            command.Name,
            cancellationToken
        );
        int version = latestVersion + 1;
        Guid contentTypeId = Guid.NewGuid();

        var domainFields = BuildDomainFields(command.Fields);

        var contentType = new ContentType(
            contentTypeId,
            command.Name,
            domainFields,
            version
        );

        await contentTypeRepository.AddAsync(contentType, cancellationToken);

        await contentTypeRepository.SaveChangesAsync(cancellationToken);

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

        foreach (var fieldDto in fields)
        {
            var field = new Field(
                Guid.NewGuid(),
                fieldDto.Type,
                fieldDto.Name,
                fieldDto.IsRequired
            );
            List<IValidationRule> rules = new();
            if (fieldDto.ValidationRules is not null)
            {
                foreach (var rule in fieldDto.ValidationRules)
                {
                    IValidationRule rule_out = validationRuleRegistry.Create(
                        rule.Type,
                        rule.Parameters
                    );
                    rules.Add(rule_out);
                }
            }
            field.SetValidationRules(rules);
            list.Add(field);
        }

        return list;
    }

    /// <summary>
    /// Validates that all referenced validation rule types are registered.
    /// </summary>
    /// <param name="fields">Fields with validation rules to validate.</param>
    /// <param name="validationRuleRegistry">Validation rule registry.</param>
    /// <returns>Result of the validation.</returns>
    private Result ValidateValidationRules(
        List<CreateFieldDto> fields,
        IValidationRuleRegistry validationRuleRegistry
    )
    {
        foreach (var field in fields)
        {
            if (
                field.ValidationRules is null
                || field.ValidationRules.Count == 0
            )
                continue;

            foreach (var rule in field.ValidationRules)
            {
                if (!validationRuleRegistry.TryCreate(rule.Type, null, out _))
                {
                    return Result.Failure(
                        Error.Validation(
                            $"Unknown validation rule type '{rule.Type}' "
                                + $"in field '{field.Name}'."
                        )
                    );
                }
            }
        }

        return Result.Success();
    }
}
