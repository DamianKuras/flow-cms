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
/// Represents a specific content type instance, identified by its stable name.
/// </summary>
public sealed record ContentTypeResource(string Name) : Resource
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
/// Represents a specific user instance.
/// </summary>
public sealed record UserResource(Guid UserId) : Resource
{
    /// <inheritdoc/>
    public override ResourceType Type => ResourceType.User;
}

/// <summary>
/// Represents categories of resources in the system.
/// </summary>
public enum ResourceType
{
    /// <summary>Content type resources that define the structure and schema of content.</summary>
    ContentType,

    /// <summary>Content item resources that contain actual content data.</summary>
    ContentItem,

    /// <summary>Field resources that define individual data fields within content types.</summary>
    Field,

    /// <summary>User resources.</summary>
    User,
}
