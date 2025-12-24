namespace Application.Auth;

/// <summary>
/// Represents the response data transfer object returned after a successful sign-in operation.
/// Contains authentication tokens and metadata required for subsequent authenticated requests.
/// </summary>
/// <param name="AccessToken">The JWT access token used to authenticate API requests.</param>
/// <param name="TokenType">The type of token issued (e.g., "Bearer").</param>
/// <param name="ExpiresIn">The duration until the access token expires (e.g., "3600" for seconds or "60min").</param>
/// <param name="RefreshToken">The token used to obtain a new access token when the current one expires.</param>
/// <param name="Scope">The scope of access granted by the token (e.g., "read write").</param>
public record SignInResponseDTO(
    string AccessToken,
    string TokenType,
    string ExpiresIn,
    string RefreshToken,
    string Scope
);
