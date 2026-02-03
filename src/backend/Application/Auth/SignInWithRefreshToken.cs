using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace Application.Auth;

/// <summary>
/// Command for signing in a user using a refresh token.
/// </summary>
/// <param name="RefreshToken">The refresh token to authenticate with.</param>
public record SignInWithRefreshTokenCommand(string RefreshToken)
{
    /// <summary>
    /// Validates the refresh token command by checking if the token is provided.
    /// </summary>
    /// <returns>
    /// A <see cref="MultiFieldValidationResult"/> containing validation errors if validation fails,
    /// or a successful result if all validations pass.
    /// </returns>
    public MultiFieldValidationResult Validate()
    {
        var multiFieldValidationResult = new MultiFieldValidationResult();
        var tokenFieldValidationResult = new ValidationResult("RefreshToken");

        if (string.IsNullOrWhiteSpace(RefreshToken))
        {
            tokenFieldValidationResult.AddError("Refresh token is required");
        }

        multiFieldValidationResult.AddValidationResult(tokenFieldValidationResult);
        return multiFieldValidationResult;
    }
}

/// <summary>
/// Handles the refresh token authentication flow by validating the existing token,
/// revoking it, and issuing new access and refresh tokens.
/// </summary>
/// <param name="authService">Service for generating authentication tokens.</param>
/// <param name="refreshTokenRepository">Repository for managing refresh tokens.</param>
/// <param name="userRepository">Repository for retrieving user data.</param>
/// <param name="jwtOptions">Configuration options for JWT tokens.</param>
/// <param name="logger">Logger instance for tracking token refresh operations.</param>
public sealed class SignInWithRefreshTokenCommandHandler(
    IAuthService authService,
    IRefreshTokenRepository refreshTokenRepository,
    IUserRepository userRepository,
    IOptions<JwtOptions> jwtOptions,
    ILogger<SignInWithRefreshTokenCommandHandler> logger
) : ICommandHandler<SignInWithRefreshTokenCommand, SignInResponse>
{
    /// <summary>
    /// Processes the refresh token command by validating the token, revoking it,
    /// and generating new authentication tokens for the user.
    /// </summary>
    /// <param name="command">The command containing the refresh token.</param>
    /// <param name="cancellationToken">Token to monitor for cancellation requests.</param>
    /// <returns>
    /// A result containing the sign-in response with new authentication tokens on success,
    /// or an error result if the refresh token is invalid, expired, or revoked.
    /// </returns>
    public async Task<Result<SignInResponse>> Handle(
        SignInWithRefreshTokenCommand command,
        CancellationToken cancellationToken
    )
    {
        logger.LogInformation("Starting refresh token authentication flow.");

        MultiFieldValidationResult validationResult = command.Validate();
        if (validationResult.IsFailure)
        {
            logger.LogWarning(
                "Validation failed for SignInWithRefreshTokenCommand. Errors: {Errors}",
                validationResult
            );
            return Result<SignInResponse>.MultiFieldValidationFailure(validationResult);
        }

        RefreshToken? existingToken = await refreshTokenRepository.GetByTokenValueAsync(
            command.RefreshToken,
            cancellationToken
        );
        Result tokenValidationResult = ValidateRefreshToken(existingToken);
        if (tokenValidationResult.IsFailure)
        {
            return Result<SignInResponse>.Failure(tokenValidationResult.Error!);
        }

        logger.LogInformation(
            "Valid refresh token found, revoking old token (TokenId: {TokenId}, UserId: {UserId})",
            existingToken?.Id,
            existingToken?.UserId
        );

        existingToken.Revoke();

        string newRefreshToken = authService.GenerateRefreshToken();

        var dbNewRefreshToken = new RefreshToken
        {
            Id = Guid.NewGuid(),
            Token = newRefreshToken,
            UserId = existingToken.UserId,
            ExpiresOnUtc = DateTime.UtcNow.AddDays(jwtOptions.Value.RefreshTokenExpirationInDays),
        };
        logger.LogDebug(
            "Creating new refresh token (TokenId: {NewTokenId}, UserId: {UserId}, ExpiresOn: {ExpirationDate})",
            dbNewRefreshToken.Id,
            dbNewRefreshToken.UserId,
            dbNewRefreshToken.ExpiresOnUtc
        );

        await refreshTokenRepository.AddAsync(dbNewRefreshToken, cancellationToken);

        User? user = await userRepository.GetByIdAsync(existingToken.UserId, cancellationToken);
        if (user is null)
        {
            logger.LogError(
                "User not found during token refresh (UserId: {UserId}, TokenId: {TokenId})",
                existingToken.UserId,
                existingToken.Id
            );
            return Result<SignInResponse>.Failure(
                Error.Unauthorized("The refresh token is invalid or expired.")
            );
        }

        logger.LogDebug(
            "Generating new access token for user (UserId: {UserId}, Email: {Email})",
            user.Id,
            user.Email
        );

        string newAccessToken = await authService.GenerateAccessTokenAsync(user);

        await refreshTokenRepository.SaveChangesAsync(cancellationToken);

        logger.LogInformation(
            "Refresh token authentication completed successfully (UserId: {UserId}, NewTokenId: {NewTokenId})",
            user.Id,
            dbNewRefreshToken.Id
        );

        return Result<SignInResponse>.Success(
            new SignInResponse(
                AccessToken: newAccessToken,
                TokenType: "Bearer",
                ExpiresIn: Convert.ToString(jwtOptions.Value.ExpirationInMinutes * 60) ?? "3600",
                RefreshToken: newRefreshToken
            )
        );
    }

    /// <summary>
    /// Validates the refresh token by checking if it exists, is not revoked, and has not expired.
    /// </summary>
    /// <param name="existingToken">The refresh token to validate.</param>
    /// <returns>A result indicating success if the token is valid, or a failure with an error message.</returns>
    private Result ValidateRefreshToken(RefreshToken? existingToken)
    {
        if (existingToken is null || existingToken.IsRevoked)
        {
            logger.LogWarning("Refresh token authentication failed: Token not found");
            return Result.Failure(Error.Unauthorized("The refresh token is invalid"));
        }
        if (existingToken.IsRevoked)
        {
            logger.LogWarning(
                "Refresh token authentication failed: Token already revoked (TokenId: {TokenId}, UserId: {UserId})",
                existingToken.Id,
                existingToken.UserId
            );
            return Result.Failure(Error.Unauthorized("The refresh token is invalid or expired."));
        }

        if (existingToken.ExpiresOnUtc < DateTime.UtcNow)
        {
            logger.LogWarning(
                "Refresh token authentication failed: Token expired at {ExpirationDate} (TokenId: {TokenId}, UserId: {UserId})",
                existingToken.ExpiresOnUtc,
                existingToken.Id,
                existingToken.UserId
            );
            return Result.Failure(Error.Unauthorized("The refresh token is invalid or expired."));
        }
        return Result.Success();
    }
}
