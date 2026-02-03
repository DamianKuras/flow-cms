using Domain.Permissions;

namespace Domain.Users;

/// <summary>
/// Represents a user as an actor within the permission system.
/// </summary>
/// <param name="id">The unique identifier for the user actor.</param>
public class UserActor(Guid id) : IActor
{
    /// <summary>
    /// Gets the unique identifier for this user actor.
    /// </summary>
    public Guid Id { get; } = id;

    /// <summary>
    /// Gets the actor type, which is always <see cref="ActorType.User"/> for instances of this class.
    /// </summary>
    public ActorType Type { get; } = ActorType.User;
}
