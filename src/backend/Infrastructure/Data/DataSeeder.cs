using Domain.Users;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace Infrastructure.Data;

/// <summary>
/// Holds constant identifiers for seeded administrative data.
/// </summary>
public static class AdminData
{
    /// <summary>
    /// The unique identifier for the default administrator account.
    /// This ID is used to link the Identity user with the domain user entity.
    /// </summary>
    public static readonly Guid AdminId = Guid.Parse("00000000-0000-0000-0000-000000000001");
}

/// <summary>
/// Provides data seeding functionality for initializing the database with default roles and users.
/// Seeds both ASP.NET Core Identity data and domain entities.
/// </summary>
/// <param name="context">The application database context for domain entity operations.</param>
/// <param name="userManager">The ASP.NET Core Identity user manager for user operations.</param>
/// <param name="roleManager">The ASP.NET Core Identity role manager for role operations.</param>
public class DataSeeder(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager
)
{
    private const string ADMIN_ROLE_NAME = "Admin";
    private const string ADMIN_EMAIL = "admin@admin.com";
    private const string ADMIN_PASSWORD = "Admin@123";
    private const string ADMIN_DISPLAY_NAME = "admin";

    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<AppRole> _roleManager = roleManager;

    /// <summary>
    /// Executes the complete seeding process for roles, users, and domain data.
    /// </summary>
    /// <returns>A task representing the asynchronous seeding operation.</returns>
    public async Task SeedAsync()
    {
        await SeedDomainDataAsync();
        await SeedRolesAsync();
        await SeedIdentityDataAsync();
    }

    /// <summary>
    /// Seeds the default administrator role into the Identity system.
    /// </summary>
    /// <returns>A task representing the asynchronous role seeding operation.</returns>
    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(ADMIN_ROLE_NAME))
        {
            await _roleManager.CreateAsync(new AppRole { Name = ADMIN_ROLE_NAME });
        }
    }

    /// <summary>
    /// Seeds the default administrator user account into the Identity system.
    /// </summary>
    /// <returns>A task representing the asynchronous user seeding operation.</returns>
    private async Task SeedIdentityDataAsync()
    {
        if (await _userManager.FindByEmailAsync(ADMIN_EMAIL) == null)
        {
            var adminUser = new AppUser
            {
                Id = AdminData.AdminId,
                UserName = ADMIN_EMAIL,
                Email = ADMIN_EMAIL,
                EmailConfirmed = true,
            };

            IdentityResult result = await _userManager.CreateAsync(adminUser, ADMIN_PASSWORD);

            if (result.Succeeded && !await _userManager.IsInRoleAsync(adminUser, ADMIN_ROLE_NAME))
            {
                await _userManager.AddToRoleAsync(adminUser, ADMIN_ROLE_NAME);
            }
        }
    }

    /// <summary>
    /// Seeds the default administrator domain user entity into the database.
    /// </summary>
    /// <returns>A task representing the asynchronous domain data seeding operation.</returns>
    private async Task SeedDomainDataAsync()
    {
        User? adminDomainUser = await _context
            .Set<User>()
            .FirstOrDefaultAsync(u => u.DisplayName == ADMIN_DISPLAY_NAME);

        if (adminDomainUser is null)
        {
            _context.Set<User>().Add(new User(AdminData.AdminId, ADMIN_EMAIL, ADMIN_DISPLAY_NAME));
            await _context.SaveChangesAsync();
        }
    }
}
