using System.Security.Claims;
using Application.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Users;

/// <summary>
/// An implementation of <see cref="IUserContext"/> that extracts user identity and role information 
/// from the current HTTP request context.
/// </summary>
/// <param name="http">The HTTP context accessor providing access to the current HTTP pipeline context.</param>
/// <param name="userManager">The ASP.NET Core Identity user manager used for extended role validations.</param>
public sealed class HttpUserContext(IHttpContextAccessor http, UserManager<AppUser> userManager)
    : IUserContext
{
    private readonly IHttpContextAccessor _http = http;

    /// <summary>
    /// Gets a value indicating whether the current HTTP request is made by an authenticated user.
    /// </summary>
    public bool IsAuthenticated => _http.HttpContext?.User?.Identity?.IsAuthenticated == true;

    /// <summary>
    /// Extracts the user's unique identifier from their <see cref="ClaimTypes.NameIdentifier"/> claim.
    /// </summary>
    public Guid UserId =>
        Guid.Parse(_http.HttpContext!.User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    /// <summary>
    /// Extracts all role identifiers assigned to the user from their <see cref="ClaimTypes.Role"/> claims.
    /// </summary>
    public IReadOnlyCollection<Guid> RoleIds =>
        _http.HttpContext!.User.FindAll(ClaimTypes.Role).Select(r => Guid.Parse(r.Value)).ToList();

    /// <summary>
    /// Asynchronously queries the identity system to verify if the context user belongs to the specified role name.
    /// </summary>
    /// <param name="roleName">The name of the role to check against.</param>
    /// <returns>True if the user is in the specified role; otherwise, false.</returns>
    public async Task<bool> IsInRoleAsync(string roleName)
    {
        ClaimsPrincipal? principal = _http.HttpContext?.User;
        if (principal is null)
        {
            return false;
        }

        AppUser? user = await userManager.GetUserAsync(principal);
        if (user is null)
        {
            return false;
        }

        return await userManager.IsInRoleAsync(user, roleName);
    }
}
