namespace Integration.Tests.Infrastructure;

/// <summary>
/// Credentials and identifiers that match the data seeded by migrations.
/// Single source of truth for all integration test auth setup.
/// </summary>
public static class TestConstants
{
    public const string AdminEmail = "admin@admin.com";
    public const string AdminPassword = "Admin@123";

    public const string DevUserPassword = "DevUser@123";
    public const string DevUserEmailPattern = "user{0}@test.com";
}
