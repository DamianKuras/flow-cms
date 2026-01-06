using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Application.Auth;
using Application.Interfaces;
using Domain.Common;
using Domain.Users;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Infrastructure.Services;

/// <summary>
/// Provides authentication services including user sign-in, token generation, and validation.
/// Implements JWT-based authentication using ASP.NET Core Identity.
/// </summary>
/// <param name="userManager">The ASP.NET Core Identity user manager for authentication operations.</param>
/// <param name="signInManager">The ASP.NET Core Identity sign-in manager for credential validation.</param>
/// <param name="userRepository">The repository for accessing domain user entities.</param>
/// <param name="jwtOptions">The JWT configuration options for token generation.</param>
public sealed class AuthService(
    UserManager<AppUser> userManager,
    SignInManager<AppUser> signInManager,
    IUserRepository userRepository,
    IOptions<JwtOptions> jwtOptions
) : IAuthService
{
    /// <inheritdoc/>
    public async Task<Result<SignInData>> SignInAsync(SignInCommand command, CancellationToken ct)
    {
        AppUser? user = await userManager.FindByEmailAsync(command.Email);

        if (user is null)
        {
            return Result<SignInData>.Failure(Error.Unauthorized("Invalid credentials"));
        }

        User? domainUser = await userRepository.GetByIdAsync(user.Id);

        if (domainUser is null)
        {
            return Result<SignInData>.Failure(
                Error.Conflict("There is no domain user corresponding ot AppUser")
            );
        }

        SignInResult result = await signInManager.CheckPasswordSignInAsync(
            user,
            command.Password,
            lockoutOnFailure: false
        );

        if (!result.Succeeded)
        {
            return Result<SignInData>.Failure(Error.Unauthorized("Invalid credentials"));
        }
        string accessToken = await GenerateAccessTokenAsync(domainUser);
        string refreshToken = GenerateRefreshToken();
        return Result<SignInData>.Success(new SignInData(user.Id, accessToken, refreshToken));
    }

    /// <inheritdoc/>
    public string GenerateRefreshToken()
    {
        byte[] randomBytes = RandomNumberGenerator.GetBytes(64);
        return Convert.ToBase64String(randomBytes);
    }

    /// <inheritdoc/>
    public async Task<string> GenerateAccessTokenAsync(User user)
    {
        var signinKey = new SymmetricSecurityKey(
            Encoding.UTF8.GetBytes(jwtOptions.Value.SecretKey!)
        );
        var credentials = new SigningCredentials(signinKey, SecurityAlgorithms.HmacSha256);

        List<Claim> claims =
        [
            new(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new(JwtRegisteredClaimNames.Email, user.Email!),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
        ];
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(claims),
            Expires = DateTime.UtcNow.AddMinutes(jwtOptions.Value.ExpirationInMinutes),
            SigningCredentials = credentials,
            Issuer = jwtOptions.Value.Issuer,
            Audience = jwtOptions.Value.Audience,
        };

        var tokenHandler = new JsonWebTokenHandler();
        string access_token = tokenHandler.CreateToken(tokenDescriptor);
        return access_token;
    }
}
