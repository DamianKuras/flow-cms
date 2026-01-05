using Application.Helpers;
using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Auth;

/// <summary>
/// Command object containing user credentials for sign-in authentication.
/// </summary>
/// <param name="Email">The user's email address used as login identifier.</param>
/// <param name="Password">The user's password for authentication.</param>
public record SignInCommand(string Email, string Password)
{
    /// <summary>
    /// Validates the sign-in command by checking required fields and email format.
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

        var passwordFieldValidationResult = new ValidationResult("Password");
        if (string.IsNullOrEmpty(Password))
        {
            passwordFieldValidationResult.AddError("Password is required");
        }
        multiFieldValidationResult.AddValidationResult(passwordFieldValidationResult);

        return multiFieldValidationResult;
    }
}

/// <summary>
/// Handles the sign-in authentication command by validating user credentials
/// and generating JWT authentication tokens upon successful authentication.
/// </summary>
/// <param name="authorizationService">Service responsible for user authentication operations.</param>
/// <param name="refreshTokenRepository">Repository for managing refresh token persistence.</param>
/// <param name="logger">Logger instance for tracking authentication operations and errors.</param>
/// <param name="jwtOptions">Configuration options for JWT token generation and expiration settings.</param>
public sealed class SignInCommandHandler(
    IAuthService authorizationService,
    IRefreshTokenRepository refreshTokenRepository,
    ILogger<SignInCommandHandler> logger,
    IOptions<JwtOptions> jwtOptions
) : ICommandHandler<SignInCommand, SignInResponse>
{
    /// <summary>
    /// Processes the sign-in command by authenticating the user and generating authentication tokens.
    /// </summary>
    /// <param name="command">The sign-in command containing user credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result containing the sign-in response with authentication tokens on success,
    /// or an error result if validation or authentication fails.
    /// </returns>
    public async Task<Result<SignInResponse>> Handle(
        SignInCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation(
            "Processing sign-in request for user with email: {Email}",
            command.Email
        );

        MultiFieldValidationResult validateCommandResult = command.Validate();
        if (validateCommandResult.IsFailure)
        {
            logger.LogWarning(
                "Validation failed for SignInCommandHandler. Errors: {Errors}",
                validateCommandResult
            );
            return Result<SignInResponse>.MultiFieldValidationFailure(validateCommandResult);
        }

        logger.LogDebug(
            "Validation successful for email: {Email}. Attempting authentication",
            command.Email
        );

        Result<SignInData> authResult = await AuthenticateUserAsync(command, cancellationToken);
        if (authResult.IsFailure || authResult.Value is null)
        {
            logger.LogWarning(
                "Authentication failed for email: {Email}. Reason: {FailureReason}",
                command.Email,
                authResult.Error?.Message ?? "Unknown authentication failure"
            );
            return Result<SignInResponse>.Failure(
                Error.Unauthorized("Username or password is incorrect.")
            );
        }

        logger.LogInformation(
            "Authentication successful for user: {Email}, UserId: {UserId}",
            command.Email,
            authResult.Value.UserId
        );

        await SaveRefreshTokenAsync(
            authResult.Value,
            cancellationToken,
            jwtOptions.Value.RefreshTokenExpirationInDays
        );

        logger.LogInformation(
            "Sign-in completed successfully for email: {Email}, UserId: {UserId}",
            command.Email,
            authResult.Value.UserId
        );

        return Result<SignInResponse>.Success(
            new SignInResponse(
                AccessToken: authResult.Value.Token,
                TokenType: "Bearer",
                ExpiresIn: Convert.ToString(jwtOptions.Value.ExpirationInMinutes * 60),
                RefreshToken: authResult.Value.RefreshToken
            )
        );
    }

    /// <summary>
    /// Authenticates the user using the provided credentials.
    /// </summary>
    /// <param name="command">The sign-in command containing user credentials.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>A result containing sign-in data if authentication succeeds.</returns>
    private async Task<Result<SignInData>> AuthenticateUserAsync(
        SignInCommand command,
        CancellationToken cancellationToken
    )
    {
        try
        {
            return await authorizationService.SignInAsync(command, cancellationToken);
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Exception occurred during sign-in authentication for email: {Email}",
                command.Email
            );
            throw;
        }
    }

    /// <summary>
    /// Creates and persists a refresh token for the authenticated user.
    /// </summary>
    /// <param name="signInData">The sign-in data containing user information and refresh token.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <param name="refreshTokenExpirationInDays">Refresh token expiration in days.</param>
    private async Task SaveRefreshTokenAsync(
        SignInData signInData,
        CancellationToken cancellationToken,
        int refreshTokenExpirationInDays
    )
    {
        var refreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = signInData.RefreshToken,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(refreshTokenExpirationInDays),
            UserId = signInData.UserId,
        };

        logger.LogDebug(
            "Creating refresh token for UserId: {UserId}, TokenId: {TokenId}, ExpiresOn: {ExpiresOn}",
            signInData.UserId,
            refreshToken.Id,
            refreshToken.ExpiresOnUtc
        );

        try
        {
            await refreshTokenRepository.AddAsync(refreshToken, cancellationToken);
            await refreshTokenRepository.SaveChangesAsync(cancellationToken);

            logger.LogDebug(
                "Refresh token saved successfully for UserId: {UserId}",
                signInData.UserId
            );
        }
        catch (Exception ex)
        {
            logger.LogError(
                ex,
                "Failed to save refresh token for UserId: {UserId}",
                signInData.UserId
            );
            throw;
        }
    }
}
