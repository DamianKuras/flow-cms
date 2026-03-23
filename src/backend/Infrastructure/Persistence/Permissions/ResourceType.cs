namespace Infrastructure.Persistence.Permissions;

/// <summary>
/// Persistence-layer discriminator for the type of resource a permission rule targets.
/// Stored as an integer column in the database.
/// </summary>
public enum ResourceType
{
    /// <summary>A content type definition.</summary>
    ContentType = 1,

    /// <summary>A content item instance.</summary>
    ContentItem = 2,

    /// <summary>A field definition within a content type.</summary>
    Field = 3,

    /// <summary>A user account.</summary>
    User = 4,
}
