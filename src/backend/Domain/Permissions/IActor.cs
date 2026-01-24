namespace Domain.Permissions;

/// <summary>
/// Defines the type of actor in the system.
/// </summary>
public enum ActorType
{
    /// <summary>
    /// Represents a human user of the system.
    /// </summary>
    User,

    /// <summary>
    /// Represents an automated system process or service.
    /// </summary>
    SystemProcess,
}

/// <summary>
/// Represents an entity that can perform actions on resources within the system.
/// </summary>
public interface IActor
{
    /// <summary>
    /// Gets the unique identifier for this actor.
    /// </summary>
    Guid Id { get; }

    /// <summary>
    /// Gets the type of this actor, determining which permission rules apply.
    /// </summary>
    ActorType Type { get; }
}
