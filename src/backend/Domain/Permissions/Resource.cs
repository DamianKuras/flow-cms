// namespace Domain.Permissions;

// /// <summary>
// /// Represents a specific resource instance in the system.
// /// </summary>
// public abstract record Resource;

// /// <summary>
// ///
// /// </summary>
// /// <param name="ContentTypeId"></param>
// public sealed record ContentTypeResource(Guid ContentTypeId) : Resource;

// /// <summary>
// ///
// /// </summary>
// /// <param name="ContentItemId"></param>
// public sealed record ContentItemResource(Guid ContentItemId) : Resource;

// /// <summary>
// ///
// /// </summary>
// /// <param name="FieldId"></param>
// public sealed record FieldResource(Guid FieldId) : Resource;

// /// <summary>
// ///
// /// </summary>
// public abstract record ResourceType;

// /// <summary>
// ///
// /// </summary>
// public sealed record AllContentTypeResource : ResourceType;

namespace Domain.Permissions;

/// <summary>
/// Represents a specific resource instance in the system.
/// </summary>
public abstract record Resource
{
    /// <summary>
    /// Gets the type of this resource.
    /// </summary>
    public abstract ResourceType Type { get; }
}

/// <summary>
/// Represents a specific content type instance.
/// </summary>
public sealed record ContentTypeResource(Guid ContentTypeId) : Resource
{
    /// <inheritdoc/>
    public override ResourceType Type => ResourceType.ContentType;
}

/// <summary>
/// Represents a specific content item instance.
/// </summary>
public sealed record ContentItemResource(Guid ContentItemId) : Resource
{
    /// <inheritdoc/>
    public override ResourceType Type => ResourceType.ContentItem;
}

/// <summary>
/// Represents a specific field instance.
/// </summary>
public sealed record FieldResource(Guid FieldId) : Resource
{
    /// <inheritdoc/>
    public override ResourceType Type => ResourceType.Field;
}

/// <summary>
/// Represents categories of resources in the system.
/// </summary>
public enum ResourceType
{
    /// <summary>
    /// Represents content type resources that define the structure and schema of content.
    /// </summary>
    ContentType,

    /// <summary>
    /// Represents content item resources that contain actual content data.
    /// </summary>
    ContentItem,

    /// <summary>
    /// Represents field resources that define individual data fields within content types.
    /// </summary>
    Field,

    User,
}
