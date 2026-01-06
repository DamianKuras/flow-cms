using Application.Auth;
using Domain.Common;
using Domain.Users;

namespace Application.Interfaces;

/// <summary>
/// Configuration options for JWT token generation and validation.
/// </summary>
public sealed class JwtOptions
{
    /// <summary>
    /// The intended audience for the JWT tokens.
    /// </summary>
    public string Audience { get; set; } = string.Empty;

    /// <summary>
    /// The issuer of the JWT tokens.
    /// </summary>
    public string Issuer { get; set; } = string.Empty;

    /// <summary>
    /// The secret key used for signing JWT tokens. Must be at least 32 characters.
    /// </summary>
    public string SecretKey { get; set; } = string.Empty;

    /// <summary>
    /// Access token expiration time in minutes. Default is 60 minutes.
    /// </summary>
    public int ExpirationInMinutes { get; set; } = 60;

    /// <summary>
    /// Refresh token expiration time in days. Default is 7 days.
    /// </summary>
    public int RefreshTokenExpirationInDays { get; set; } = 7;
}

/// <summary>
/// Represents the data returned after a successful user sign-in operation.
/// </summary>
/// <param name="UserId">The unique identifier of the authenticated user.</param>
/// <param name="Token">The JWT access token used for API authentication.</param>
/// <param name="RefreshToken">The refresh token used to obtain new access tokens without re-authentication.</param>
public record SignInData(Guid UserId, string Token, string RefreshToken);

/// <summary>
/// Service for handling authentication and authorization operations.
/// </summary>
public interface IAuthService
{
    /// <summary>
    /// Authenticates a user and generates JWT access and refresh tokens.
    /// </summary>
    /// <param name="command">The sign-in credentials and request details.</param>
    /// <param name="ct">Cancellation token to cancel the operation.</param>
    /// <returns>
    /// A <see cref="Result{T}"/> containing <see cref="SignInData"/> with the user ID and tokens on success,
    /// or an error result if authentication fails.
    /// </returns>
    Task<Result<SignInData>> SignInAsync(SignInCommand command, CancellationToken ct);

    /// <summary>
    /// Generates a new JWT access token for the specified user.
    /// </summary>
    /// <param name="user">The user for whom to generate the access token.</param>
    /// <returns> A JWT access token string. </returns>
    Task<string> GenerateAccessTokenAsync(User user);

    /// <summary>
    /// Generates a cryptographically secure refresh token.
    /// </summary>
    /// <returns>
    /// A unique refresh token string that can be used to obtain new access tokens.
    /// </returns>
    string GenerateRefreshToken();
}
