using System;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
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
///
/// </summary>
/// <param name="Type"></param>
/// <param name="Parameters"></param>
internal record TransformationRuleDto(string Type, Dictionary<string, object>? Parameters);

/// <summary>
/// Repository for managing ContentType entities and their associated fields.
/// </summary>
/// <param name="db">The database context for data access.</param>
/// <param name="validationRuleRegistry">Registry for creating validation rule instances.</param>
/// <param name="transformationRuleRegistry">Registry for creating validation rule instances.</param>
public class ContentTypeRepository(
    AppDbContext db,
    IValidationRuleRegistry validationRuleRegistry,
    ITransformationRuleRegistry transformationRuleRegistry
) : IContentTypeRepository
{
    private const string VALIDATION_RULES_JSON_SHADOW_PROPERTY = "ValidationRulesJson";

    private const string TRANSFORMATION_RULES_JSON_SHADOW_PROPERTY = "TransformationRulesJson";
    private readonly AppDbContext _db = db;
    private readonly IValidationRuleRegistry _validationRuleRegistry = validationRuleRegistry;
    private readonly ITransformationRuleRegistry _transformationRuleRegistry =
        transformationRuleRegistry;

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
    /// Validation and transformation rules are serialized to JSON before persisting to the database
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
    public List<IValidationRule> DeserializeValidationRules(string? json)
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

            return dtos.Select(dto => _validationRuleRegistry.Create(dto.Type, dto.Parameters))
                .ToList();
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
    /// Deserializes transformation rules from JSON format into rule objects.
    /// </summary>
    /// <param name="json">JSON string containing serialized validation rules.</param>
    /// <returns>A list of validation rule objects. Returns an empty list if input is null or whitespace.</returns>
    /// <exception cref="InvalidOperationException">Thrown when rule creation fails.</exception>
    public List<ITransformationRule> DeserializeTransformationRules(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return new List<ITransformationRule>();
        }

        try
        {
            List<TransformationRuleDto>? dtos = JsonSerializer.Deserialize<
                List<TransformationRuleDto>
            >(json);

            if (dtos == null)
            {
                return new List<ITransformationRule>();
            }

            return dtos.Select(dto => _transformationRuleRegistry.Create(dto.Type, dto.Parameters))
                .ToList();
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException(
                $"Failed to deserialize transformation rules from JSON: {json}",
                ex
            );
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException(
                "Failed to create transformation rules from deserialized data.",
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
    public string? SerializeValidationRules(IEnumerable<IValidationRule>? rules)
    {
        if (rules == null || !rules.Any())
        {
            return null;
        }

        IEnumerable<ValidationRuleDto> ruleSerialized = rules.Select(r =>
        {
            if (r is ParameterizedValidationRuleBase p)
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
    /// Serializes validation rules into JSON format for database storage.
    /// </summary>
    /// <param name="rules">The collection of validation rules to serialize.</param>
    /// <returns>A JSON string representation of the rules, or null if the collection is null or empty.</returns>
    /// <exception cref="InvalidOperationException">Thrown when serialization fails.</exception>
    public string? SerializeTransformationRules(IEnumerable<ITransformationRule>? rules)
    {
        if (rules == null || !rules.Any())
        {
            return null;
        }

        IEnumerable<TransformationRuleDto> ruleSerialized = rules.Select(r =>
        {
            if (r is ParameterizedTransformationRuleBase p)
            {
                return new TransformationRuleDto(p.Type, p.Parameters);
            }
            else
            {
                return new TransformationRuleDto(r.Type, null);
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
                string? validation_rules_json = (string?)
                    _db.Entry(field).Property(VALIDATION_RULES_JSON_SHADOW_PROPERTY).CurrentValue;
                List<IValidationRule> validation_rules = DeserializeValidationRules(
                    validation_rules_json
                );
                field.SetValidationRules(validation_rules);

                string? transformation_rules_json = (string?)
                    _db.Entry(field)
                        .Property(TRANSFORMATION_RULES_JSON_SHADOW_PROPERTY)
                        .CurrentValue;
                List<ITransformationRule> transformation_rules = DeserializeTransformationRules(
                    transformation_rules_json
                );
                field.SetTransformationRules(transformation_rules);
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
                string? validation_rules_json = SerializeValidationRules(field.ValidationRules);
                _db.Entry(field).Property(VALIDATION_RULES_JSON_SHADOW_PROPERTY).CurrentValue =
                    validation_rules_json;

                string? transformation_rules_json = SerializeTransformationRules(
                    field.FieldTransformers
                );
                _db.Entry(field).Property(TRANSFORMATION_RULES_JSON_SHADOW_PROPERTY).CurrentValue =
                    transformation_rules_json;
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

    /// <inheritdoc/>
    public async Task<PagedList<PagedContentType>> Get(
        PaginationParameters paginationParameters,
        string sort,
        string status,
        string filter,
        CancellationToken cancellationToken
    )
    {
        IQueryable<ContentType> query = _db.ContentTypes.AsNoTracking();

        // Filter by name.
        if (!string.IsNullOrWhiteSpace(filter))
        {
            query = query.Where(ct => ct.Name.ToLower().Contains(filter.ToLower()));
        }

        // Filter by status.
        if (
            Enum.TryParse<ContentTypeStatus>(
                status,
                ignoreCase: true,
                out ContentTypeStatus parsedStatus
            )
        )
        {
            query = query.Where(ct => ct.Status == parsedStatus);
        }

        // Sorting.
        query = ApplySorting(query, sort);

        // Pagination.
        query = query
            .Skip((paginationParameters.Page - 1) * paginationParameters.PageSize)
            .Take(paginationParameters.PageSize);

        // Projection to return type.
        int totalCount = await query.CountAsync(cancellationToken);
        List<PagedContentType> contentTypes = await query
            .Include(ct => ct.Fields)
            .Select(x => new PagedContentType(
                x.Id,
                x.Name,
                x.Status.ToString(),
                x.Version,
                x.CreatedAt
            ))
            .ToListAsync(cancellationToken);
        return new PagedList<PagedContentType>(
            contentTypes,
            paginationParameters.Page,
            paginationParameters.PageSize,
            totalCount
        );
    }

    private static IQueryable<ContentType> ApplySorting(IQueryable<ContentType> query, string? sort)
    {
        if (string.IsNullOrWhiteSpace(sort))
        {
            return query.OrderBy(ct => ct.CreatedAt); // default
        }

        string[] parts = sort.Split('.', StringSplitOptions.RemoveEmptyEntries);
        if (parts.Length != 2)
        {
            return query;
        }

        string field = parts[0];
        string direction = parts[1];

        bool descending = direction == "desc";

        return field switch
        {
            "name" => descending
                ? query.OrderByDescending(ct => ct.Name)
                : query.OrderBy(ct => ct.Name),

            "status" => descending
                ? query.OrderByDescending(ct => ct.Status)
                : query.OrderBy(ct => ct.Status),

            "createdAt" => descending
                ? query.OrderByDescending(ct => ct.CreatedAt)
                : query.OrderBy(ct => ct.CreatedAt),

            "version" => descending
                ? query.OrderByDescending(ct => ct.Version)
                : query.OrderBy(ct => ct.Version),

            _ => query,
        };
    }

    /// <inheritdoc/>
    public async Task<ContentType?> GetLatestDraftVersion(
        string contentTypeName,
        CancellationToken ct = default
    )
    {
        ContentType? contentType = await _db
            .ContentTypes.Where(ct =>
                ct.Name == contentTypeName && ct.Status == ContentTypeStatus.DRAFT
            )
            .Include(ct => ct.Fields)
            .OrderByDescending(ct => ct.Version)
            .FirstOrDefaultAsync();

        if (contentType is not null)
        {
            HydrateFields(contentType);
        }
        return contentType;
    }

    /// <inheritdoc/>
    public async Task<ContentType?> GetLatestsPublishedVersion(
        string contentTypeName,
        CancellationToken ct = default
    )
    {
        ContentType? contentType = await _db
            .ContentTypes.Where(ct =>
                ct.Name == contentTypeName && ct.Status == ContentTypeStatus.PUBLISHED
            )
            .Include(ct => ct.Fields)
            .OrderByDescending(ct => ct.Version)
            .FirstOrDefaultAsync(cancellationToken: ct);
        if (contentType is not null)
        {
            HydrateFields(contentType);
        }
        return contentType;
    }

    /// <inheritdoc/>
    public async Task SoftDelete(ContentType contentType) => _db.ContentTypes.Remove(contentType);
}
