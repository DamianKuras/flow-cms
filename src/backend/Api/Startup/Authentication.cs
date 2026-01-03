using System.Text;
using Application.Interfaces;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;

namespace Api.Startup;

/// <summary>
/// Provides extension methods for configuring JWT authentication.
/// </summary>
public static class Authentication
{
    /// <summary>
    /// Configures JWT Bearer authentication for the application.
    /// </summary>
    /// <param name="builder">The web application builder instance.</param>
    /// <exception cref="InvalidOperationException">Thrown when JWT configuration is missing or invalid.</exception>
    public static void ConfigureAuthentication(this WebApplicationBuilder builder)
    {
        IConfigurationSection jwtSection = builder.Configuration.GetSection("Jwt");
        builder.Services.Configure<JwtOptions>(jwtSection);
        JwtOptions? jwtOptions = jwtSection.Get<JwtOptions>();
        ValidateJwtOptions(jwtOptions);
        builder
            .Services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var signingKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(jwtOptions!.SecretKey)
                );
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtOptions.Issuer,
                    ValidAudience = jwtOptions.Audience,
                    IssuerSigningKey = signingKey,
                };
            });
    }

    /// <summary>
    /// Validates the JWT configuration options.
    /// </summary>
    /// <param name="options">The JWT options to validate.</param>
    /// <exception cref="InvalidOperationException">Thrown when configuration is invalid.</exception>
    private static void ValidateJwtOptions(JwtOptions? options)
    {
        if (options is null)
        {
            throw new InvalidOperationException(
                "JWT configuration section 'Jwt' is missing from appsettings.json"
            );
        }

        if (string.IsNullOrWhiteSpace(options.SecretKey))
        {
            throw new InvalidOperationException("JWT SecretKey is required");
        }

        if (options.SecretKey.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT SecretKey must be at least 32 characters for security"
            );
        }

        if (string.IsNullOrWhiteSpace(options.Issuer))
        {
            throw new InvalidOperationException("JWT Issuer is required");
        }

        if (string.IsNullOrWhiteSpace(options.Audience))
        {
            throw new InvalidOperationException("JWT Audience is required");
        }
    }
}
