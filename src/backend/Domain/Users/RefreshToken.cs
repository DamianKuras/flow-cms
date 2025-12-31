namespace Domain.Users;

/// <summary>
/// Represents a JWT refresh token used for obtaining new access tokens without re-authentication.
/// </summary>
public sealed class RefreshToken
{
    /// <summary>
    /// Gets the unique identifier for this refresh token.
    /// </summary>
    public Guid Id { get; init; }

    /// <summary>
    /// Gets the cryptographically secure token string used for authentication.
    /// </summary>
    public string Token { get; set; }

    /// <summary>
    /// Gets the identifier of the user to whom this refresh token belongs.
    /// </summary>
    public Guid UserId { get; init; }

    /// <summary>
    /// Gets the UTC date and time when this refresh token was created.
    /// </summary>
    public DateTime CreatedOnUtc { get; init; }

    /// <summary>
    /// Gets the UTC date and time when this refresh token expires.
    /// </summary>
    public DateTime ExpiresOnUtc { get; init; }

    /// <summary>
    /// Gets a value indicating whether this refresh token has been revoked.
    /// </summary>
    public bool IsRevoked { get; private set; }

    /// <summary>
    /// Gets the UTC date and time when this refresh token was revoked, if applicable.
    /// </summary>
    public DateTime? RevokedOnUtc { get; private set; }

    /// <summary>
    /// Determines whether this refresh token is currently valid (not expired and not revoked).
    /// </summary>
    /// <returns>True if the token is valid; otherwise, false.</returns>
    public bool IsValid() => !IsRevoked && !IsExpired();

    /// <summary>
    /// Determines whether this refresh token has expired based on the current UTC time.
    /// </summary>
    /// <returns>True if the token has expired; otherwise, false.</returns>
    public bool IsExpired() => DateTime.UtcNow >= ExpiresOnUtc;

    /// <summary>
    /// Revokes this refresh token, preventing it from being used for authentication.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the token is already revoked.</exception>
    public void Revoke()
    {
        if (IsRevoked)
        {
            throw new InvalidOperationException("Refresh token is already revoked.");
        }

        IsRevoked = true;
        RevokedOnUtc = DateTime.UtcNow;
    }
}
