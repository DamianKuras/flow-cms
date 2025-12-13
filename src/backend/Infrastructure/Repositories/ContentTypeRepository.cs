using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Storage;

namespace Infrastructure.Repositories;

/// <summary>
/// Data transfer object for serializing and deserializing validation rules.
/// </summary>
/// <param name="Type">The type identifier of the validation rule.</param>
/// <param name="Parameters">Optional dictionary of parameters for the validation rule.</param>
internal record ValidationRuleDto(string Type, Dictionary<string, object>? Parameters);

/// <summary>
/// Repository for managing ContentType entities and their associated fields.
/// </summary>
/// <param name="db">The database context for data access.</param>
/// <param name="ruleRegistry">Registry for creating validation rule instances.</param>
public class ContentTypeRepository(AppDbContext db, IValidationRuleRegistry ruleRegistry)
    : IContentTypeRepository
{
    private const string ValidationRulesJsonShadowProperty = "ValidationRulesJson";
    private readonly AppDbContext _db = db;
    private readonly IValidationRuleRegistry _ruleRegistry = ruleRegistry;

    /// <inheritdoc />
    /// <remarks>
    /// Includes all associated fields and hydrates validation rules from JSON storage.
    /// </remarks>
    public async Task<ContentType?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default
    )
    {
        ContentType? contentType = await _db
            .ContentTypes.Include(ct => ct.Fields)
            .FirstOrDefaultAsync(ct => ct.Id == id, cancellationToken);
        if (contentType is not null)
        {
            HydrateFields(contentType);
        }
        return contentType;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Validation rules are serialized to JSON before persisting to the database
    /// using a shadow property for storage.
    /// </remarks>
    public async Task AddAsync(ContentType contentType, CancellationToken ct = default)
    {
        DehydrateFields(contentType);
        await _db.ContentTypes.AddAsync(contentType, ct);
    }

    /// <inheritdoc />
    /// <remarks>
    /// Validation rules are serialized to JSON before persisting to the database
    /// using a shadow property for storage.
    /// </remarks>
    public async Task UpdateAsync(ContentType contentType, CancellationToken ct = default)
    {
        DehydrateFields(contentType);
        _db.ContentTypes.Update(contentType);
    }

    /// <inheritdoc />
    public async Task SaveChangesAsync(CancellationToken ct = default) =>
        await _db.SaveChangesAsync(ct);

    /// <summary>
    /// Deserializes validation rules from JSON format into rule objects.
    /// </summary>
    /// <param name="json">JSON string containing serialized validation rules.</param>
    /// <returns>A list of validation rule objects. Returns an empty list if input is null or whitespace.</returns>
    /// <exception cref="InvalidOperationException">Thrown when rule creation fails.</exception>
    public List<IValidationRule> DeserializeRules(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<IValidationRule>();
        }

        try
        {
            List<ValidationRuleDto>? dtos = JsonSerializer.Deserialize<List<ValidationRuleDto>>(
                json
            );

            if (dtos == null)
            {
                return new List<IValidationRule>();
            }

            return dtos.Select(dto => _ruleRegistry.Create(dto.Type, dto.Parameters)).ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize validation rules from JSON: {json}",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create validation rules from deserialized data.",
                ex
            );
        }
    }

    /// <summary>
    /// Serializes validation rules into JSON format for database storage.
    /// </summary>
    /// <param name="rules">The collection of validation rules to serialize.</param>
    /// <returns>A JSON string representation of the rules, or null if the collection is null or empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    public string? SerializeRules(IEnumerable<IValidationRule>? rules)
    {
        if (rules == null || !rules.Any())
        {
            return null;
        }

        IEnumerable<ValidationRuleDto> ruleSerialized = rules.Select(r =>
        {
            if (r is ParameterizedRuleBase p)
            {
                return new ValidationRuleDto(p.Type, p.Parameters);
            }
            else
            {
                return new ValidationRuleDto(r.Type, null);
            }
        });
        return JsonSerializer.Serialize(ruleSerialized);
    }

    /// <summary>
    /// Loads validation rules from the database and populates them into the content type's fields.
    /// This process converts serialized JSON rules back into rule objects.
    /// </summary>
    /// <param name="contentType">The content type whose fields should be hydrated with validation rules.</param>
    private void HydrateFields(ContentType contentType)
    {
        foreach (Field field in contentType.Fields)
        {
            try
            {
                string? json = (string?)
                    _db.Entry(field).Property(ValidationRulesJsonShadowProperty).CurrentValue;

                List<IValidationRule> rules = DeserializeRules(json);
                field.SetValidationRules(rules);
            }
            catch (InvalidOperationException ex)
            {
                throw new InvalidOperationException(
                    $"Failed to hydrate validation rules for field '{field.Name}' (ID: {field.Id}) "
                        + $"in content type '{contentType.Name}' (ID: {contentType.Id}).",
                    ex
                );
            }
        }
    }

    /// <summary>
    /// Serializes validation rules from the content type's fields and stores them in the database shadow property.
    /// This prepares the fields for persistence by converting rule objects to JSON.
    /// </summary>
    /// <param name="contentType">The content type whose fields should be dehydrated for storage.</param>
    private void DehydrateFields(ContentType contentType)
    {
        foreach (Field field in contentType.Fields)
        {
            try
            {
                string? json = SerializeRules(field.ValidationRules);
                _db.Entry(field).Property(ValidationRulesJsonShadowProperty).CurrentValue = json;
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    $"Failed to dehydrate validation rules for field '{field.Name}' (ID: {field.Id}) "
                        + $"in content type '{contentType.Name}' (ID: {contentType.Id}).",
                    ex
                );
            }
        }
    }

    /// <summary>
    /// Retrieves the latest version number for a content type with the specified name.
    /// </summary>
    /// <param name="contentTypeName">The name of the content type.</param>
    /// <param name="cancellationToken">Cancellation token for async operation.</param>
    /// <returns>The highest version number, or default (int) if no content type with that name exists.</returns>
    /// <exception cref="ArgumentException">Thrown when contentTypeName is null or white space.</exception>
    public async Task<int?> GetLatestVersion(
        string contentTypeName,
        CancellationToken cancellationToken
    )
    {
        if (string.IsNullOrWhiteSpace(contentTypeName))
        {
            throw new ArgumentException(
                "Content type name cannot be null or empty.",
                nameof(contentTypeName)
            );
        }

        return await _db
            .ContentTypes.Where(ct => ct.Name == contentTypeName)
            .MaxAsync(ct => (int?)ct.Version, cancellationToken);
    }

    /// <summary>
    /// Marks a content type for deletion from the repository.
    /// </summary>
    /// <param name="contentType">The content type to delete.</param>
    public async Task DeleteAsync(ContentType contentType) => _db.ContentTypes.Remove(contentType);
}
