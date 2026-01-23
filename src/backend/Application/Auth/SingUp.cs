using Application.Helpers;
using Application.Interfaces;
using Domain.Common;
using Domain.Permissions;
using Domain.Users;
using Microsoft.Extensions.Logging;

namespace Application.Auth;

/// <summary>
/// Command object containing data required to create a new user account.
/// </summary>
/// <param name="Email">The email address for the new user account (must be unique).</param>
/// <param name="DisplayName">The display name or username for the new user.</param>
/// <param name="Password">The password for the new user account.</param>
public record CreateUserCommand(string Email, string DisplayName, string Password)
{
    /// <summary>
    /// Validates the create user command by checking required fields and email format.
    /// </summary>
    /// <returns>
    /// A <see cref="MultiFieldValidationResult"/> containing validation errors for each field if validation fails,
    /// or a successful result if all validations pass.
    /// </returns>
    public MultiFieldValidationResult Validate()
    {
        var multiFieldValidationResult = new MultiFieldValidationResult();

        var emailFieldValidationResult = new ValidationResult("Email");
        if (string.IsNullOrEmpty(Email))
        {
            emailFieldValidationResult.AddError("Email is required");
        }
        else if (!HelperFunctions.IsValidEmail(Email))
        {
            emailFieldValidationResult.AddError("Email format is invalid");
        }
        multiFieldValidationResult.AddValidationResult(emailFieldValidationResult);

        var displayNameFieldValidationResult = new ValidationResult("DisplayName");
        if (string.IsNullOrEmpty(DisplayName))
        {
            displayNameFieldValidationResult.AddError("Display name is required");
        }
        multiFieldValidationResult.AddValidationResult(displayNameFieldValidationResult);

        var passwordFieldValidationResult = new ValidationResult("Password");
        if (string.IsNullOrEmpty(Password))
        {
            passwordFieldValidationResult.AddError("Password is required");
        }
        multiFieldValidationResult.AddValidationResult(passwordFieldValidationResult);

        return multiFieldValidationResult;
    }
};

/// <summary>
/// Handles the creation of new user accounts by persisting user data to the repository.
/// </summary>
/// <param name="userRepository">Repository for user data persistence operations.</param>
/// <param name="logger">Logger for tracking registration operations and errors.</param>
/// <param name="authorizationService">Service for permission validation.</param>
public sealed class CreateUserCommandHandler(
    IUserRepository userRepository,
    ILogger<CreateUserCommandHandler> logger,
    IAuthorizationService authorizationService
) : ICommandHandler<CreateUserCommand, Guid>
{
    /// <summary>
    /// Processes the create user command by creating a new user entity and persisting it to the database.
    /// </summary>
    /// <param name="command">The command containing new user registration data.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A command response containing the newly created user's unique identifier on success.</returns>
    public async Task<Result<Guid>> Handle(
        CreateUserCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Processing create user request for email: {Email}", command.Email);
        MultiFieldValidationResult validateCommandResult = command.Validate();
        if (validateCommandResult.IsFailure)
        {
            logger.LogWarning(
                "Validation failed for CreateUserCommandHandler. Errors: {Errors}",
                validateCommandResult
            );
            return Result<Guid>.MultiFieldValidationFailure(validateCommandResult);
        }

        bool isAllowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.Create,
            ResourceType.User,
            cancellationToken
        );
        if (!isAllowed)
        {
            logger.LogWarning("User not authorized to create new users.");
            return Result<Guid>.Failure(
                Error.Forbidden("You do not have permission to read this content item")
            );
        }

        logger.LogDebug(
            "Validation successful for email: {Email}. Checking authorization",
            command.Email
        );
        if (await userRepository.GetByEmailAsync(command.Email) != null)
        {
            logger.LogWarning(
                "User creation failed - email already exists: {Email}",
                command.Email
            );
            return Result<Guid>.Failure(
                Error.Conflict("User with specified Email is already in database.")
            );
        }
        var domainUser = new User(Guid.NewGuid(), command.Email, command.DisplayName);

        logger.LogDebug(
            "Creating user entity with UserId: {UserId}, Email: {Email}",
            domainUser.Id,
            command.Email
        );

        try
        {
            await userRepository.AddAsync(domainUser, command.Password, cancellationToken);
            logger.LogInformation(
                "User created successfully. UserId: {UserId}, Email: {Email}",
                domainUser.Id,
                command.Email
            );
            return Result<Guid>.Success(domainUser.Id);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to create user for email: {Email}", command.Email);
            return Result<Guid>.Failure(
                Error.Infrastructure("An error occurred while creating the user account.")
            );
        }
    }
}
