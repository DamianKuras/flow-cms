using System.Text.Json;
using Domain.ContentItems;

namespace Application.ContentItems;

/// <summary>
/// DTO for content items.
/// </summary>
public record ContentItemDto(
    Guid Id,
    string Name,
    Guid ContentTypeId,
    string Status,
    Dictionary<string, ContentFieldValueDto> Values
);

/// <summary>
/// DTO for individual field values within a content item.
/// </summary>
public record ContentFieldValueDto(JsonElement Value);

/// <summary>
/// List view data for list endpoints.
/// </summary>
public record ContentItemListDto(Guid Id, Guid ContentTypeId, string Status);

/// <summary>
/// Create content item request DTO.
/// </summary>
public record CreateContentItemDto(
    string Title,
    Guid ContentTypeId,
    Dictionary<string, JsonElement?> Values
);

/// <summary>
/// Update content item request DTO.
/// </summary>
public record UpdateContentItemDto(
    string? Name,
    ContentItemStatus? Status,
    Dictionary<Guid, object?>? Values
);

/// <summary>
/// Patch content item request DTO.
/// </summary>
public record PatchContentItemDto(
    string? Name,
    ContentItemStatus? Status,
    Dictionary<Guid, object?>? Values
);
