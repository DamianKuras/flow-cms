using Microsoft.AspNetCore.Identity;

namespace Infrastructure.Users;

/// <summary>
/// Represents a user within the CMS identity system, extending ASP.NET Core Identity.
/// </summary>
public sealed class AppUser : IdentityUser<Guid>
{
    /// <summary>
    /// Gets or sets the display name for the user, typically used in UI representations.
    /// </summary>
    public string DisplayName { get; set; } = "";
}

/// <summary>
/// Represents a role within the CMS identity system, extending ASP.NET Core Identity.
/// Used to group users and assign permissions collectively.
/// </summary>
public sealed class AppRole : IdentityRole<Guid> { }
