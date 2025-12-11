using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;

namespace Application.ContentTypes;

/// <summary>
/// Query for retrieving a content type.
/// </summary>
/// <param name="Id">The unique identifier of the content type to retrieve.</param>
public sealed record GetContentTypeQuery(Guid Id);

/// <summary>
/// Handles the retrieval of a content type and its associated fields and validation rules.
/// </summary>
/// <param name="contentTypeRepository">Content type repository.</param>
public sealed class GetContentTypeHandler(IContentTypeRepository contentTypeRepository)
    : IQueryHandler<GetContentTypeQuery, ContentTypeDto>
{
    /// <summary>
    /// Handles the query to retrieve a content type by ID.
    /// </summary>
    /// <param name="query">The query containing the content type ID.</param>
    /// <param name="cancellationToken">Token to cancel the asynchronous operation.</param>
    /// <returns>
    /// A result containing the content type DTO if found, or a failure result with a NotFound error if not found.
    /// </returns>
    public async Task<Result<ContentTypeDto>> Handle(
        GetContentTypeQuery query,
        CancellationToken cancellationToken
    )
    {
        ContentType? contentType = await contentTypeRepository.GetByIdAsync(
            query.Id,
            cancellationToken
        );

        if (contentType is null)
        {
            return Result<ContentTypeDto>.Failure(
                Error.NotFound($"ContentType with ID '{query.Id}' was not found")
            );
        }

        ContentTypeDto dto = MapToDto(contentType);
        return Result<ContentTypeDto>.Success(dto);
    }

    /// <summary>
    /// Maps a domain content type entity to a content type DTO.
    /// </summary>
    /// <param name="contentType">The content type entity to map.</param>
    /// <returns>A DTO representing the content type with its status, fields, and version.</returns>
    private static ContentTypeDto MapToDto(ContentType contentType)
    {
        var fields = contentType.Fields.Select(MapFieldToDto).ToList();

        return new ContentTypeDto(contentType.Status.ToString(), fields, contentType.Version);
    }

    /// <summary>
    /// Maps a domain field entity to a field DTO.
    /// </summary>
    /// <param name="field">The field entity to map.</param>
    /// <returns>A DTO representing the field with its properties and validation rules.</returns>
    private static FieldDto MapFieldToDto(Field field)
    {
        var validationRules = field.ValidationRules.Select(MapValidationRuleToDto).ToList();

        return new FieldDto(
            field.Id,
            field.Name,
            field.Type.ToString(),
            field.IsRequired,
            validationRules
        );
    }

    /// <summary>
    /// Maps a validation rule to a validation rule DTO.
    /// </summary>
    /// <param name="validationRule">The validation rule to map.</param>
    /// <returns>
    /// A DTO representing the validation rule with its type and parameters.
    /// Parameters will be null for non-parameterized rules.
    /// </returns>
    private static ValidationRuleDto MapValidationRuleToDto(IValidationRule validationRule)
    {
        Dictionary<string, object>? parameters = validationRule
            is ParameterizedRuleBase parameterizedRule
            ? parameterizedRule.Parameters
            : null;

        return new ValidationRuleDto(validationRule.Type, parameters);
    }
}
