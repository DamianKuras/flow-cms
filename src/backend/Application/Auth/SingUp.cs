using Application.Interfaces;
using Domain.Users;

namespace Application.Auth;

/// <summary>
/// Command object containing data required to create a new user account.
/// </summary>
/// <param name="Email">The email address for the new user account (must be unique).</param>
/// <param name="DisplayName">The display name or username for the new user.</param>
/// <param name="Password">The password for the new user account (should meet security requirements).</param>
public record CreateUserCommand(string Email, string DisplayName, string Password);

/// <summary>
/// Handles the creation of new user accounts by persisting user data to the repository.
/// </summary>
/// <param name="userRepository">Repository for user data persistence operations.</param>
public sealed class CreateUserCommandHandler(IUserRepository userRepository)
    : ICommandHandler<CreateUserCommand, Guid>
{
    /// <summary>
    /// Processes the create user command by creating a new user entity and persisting it to the database.
    /// </summary>
    /// <param name="command">The command containing new user registration data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A command response containing the newly created user's unique identifier on success.</returns>
    public async Task<CommandResponse<Guid>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken
    )
    {
        var domainUser = new User(Guid.NewGuid(), command.Email, command.DisplayName);

        await userRepository.AddAsync(domainUser, command.Password, cancellationToken);
        await userRepository.SaveChangesAsync(cancellationToken);

        return CommandResponse<Guid>.Success(domainUser.Id);
    }
}
