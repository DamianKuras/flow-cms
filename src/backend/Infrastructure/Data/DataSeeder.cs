using Domain.Users;
using Infrastructure.Users;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;

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
/// Credentials and counts are read from the <c>Seed</c> configuration section.
/// In production override via environment variables: <c>Seed__AdminEmail</c>, <c>Seed__AdminPassword</c>, etc.
/// </summary>
/// <param name="context">The application database context for domain entity operations.</param>
/// <param name="userManager">The ASP.NET Core Identity user manager for user operations.</param>
/// <param name="roleManager">The ASP.NET Core Identity role manager for role operations.</param>
/// <param name="environment">The hosting environment to determine seeding behavior.</param>
/// <param name="configuration">The application configuration for reading seed credentials.</param>
public class DataSeeder(
    AppDbContext context,
    UserManager<AppUser> userManager,
    RoleManager<AppRole> roleManager,
    IHostEnvironment environment,
    IConfiguration configuration
)
{
    private const string ADMIN_ROLE_NAME = "Admin";
    private const string ADMIN_DISPLAY_NAME = "admin";

    private string AdminEmail =>
        configuration["Seed:AdminEmail"]
        ?? throw new InvalidOperationException("Seed:AdminEmail is not configured.");

    private string AdminPassword =>
        configuration["Seed:AdminPassword"]
        ?? throw new InvalidOperationException("Seed:AdminPassword is not configured.");

    private string DevUserPassword =>
        configuration["Seed:DevUserPassword"]
        ?? throw new InvalidOperationException("Seed:DevUserPassword is not configured.");

    private int DevUserCount =>
        configuration.GetValue<int?>("Seed:DevUserCount")
        ?? throw new InvalidOperationException("Seed:DevUserCount is not configured.");

    private readonly AppDbContext _context = context;
    private readonly UserManager<AppUser> _userManager = userManager;
    private readonly RoleManager<AppRole> _roleManager = roleManager;

    /// <summary>
    /// Executes the complete seeding process for roles, users, and domain data.
    /// </summary>
    public async Task SeedAsync()
    {
        await SeedDomainDataAsync();
        await SeedRolesAsync();
        await SeedIdentityDataAsync();
        if (environment.IsDevelopment())
        {
            await SeedDevelopmentUsersAsync(DevUserCount);
        }
    }

    private async Task SeedRolesAsync()
    {
        if (!await _roleManager.RoleExistsAsync(ADMIN_ROLE_NAME))
        {
            await _roleManager.CreateAsync(new AppRole { Name = ADMIN_ROLE_NAME });
        }
    }

    private async Task SeedIdentityDataAsync()
    {
        if (await _userManager.FindByEmailAsync(AdminEmail) == null)
        {
            var adminUser = new AppUser
            {
                Id = AdminData.AdminId,
                UserName = AdminEmail,
                Email = AdminEmail,
                EmailConfirmed = true,
            };

            IdentityResult result = await _userManager.CreateAsync(adminUser, AdminPassword);

            if (result.Succeeded && !await _userManager.IsInRoleAsync(adminUser, ADMIN_ROLE_NAME))
            {
                await _userManager.AddToRoleAsync(adminUser, ADMIN_ROLE_NAME);
            }
        }
    }

    private async Task SeedDomainDataAsync()
    {
        User? adminDomainUser = await _context
            .Set<User>()
            .FirstOrDefaultAsync(u => u.DisplayName == ADMIN_DISPLAY_NAME);

        if (adminDomainUser is null)
        {
            _context.Set<User>().Add(new User(AdminData.AdminId, AdminEmail, ADMIN_DISPLAY_NAME));
            await _context.SaveChangesAsync();
        }
    }

    private async Task SeedDevelopmentUsersAsync(int count)
    {
        bool anyDevUsersExist = await _userManager.Users.AnyAsync(u =>
            u.Email!.EndsWith("@test.com")
        );

        if (anyDevUsersExist)
        {
            return;
        }

        for (int i = 1; i <= count; i++)
        {
            var userId = Guid.NewGuid();
            string email = $"user{i}@test.com";
            string displayName = $"user{i}";

            var identityUser = new AppUser
            {
                Id = userId,
                UserName = email,
                Email = email,
                EmailConfirmed = true,
            };

            IdentityResult result = await _userManager.CreateAsync(identityUser, DevUserPassword);

            if (!result.Succeeded)
            {
                continue;
            }

            _context.Set<User>().Add(new User(userId, email, displayName));
        }

        await _context.SaveChangesAsync();
    }
}
