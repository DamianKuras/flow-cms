using Domain.Users;

namespace Domain.Tests;

public class UserTests
{
    private static User CreateUser() => new(Guid.NewGuid(), "test@example.com", "Test User");

    [Fact]
    public void New_user_is_active()
    {
        User user = CreateUser();

        Assert.Equal(UserStatus.Active, user.Status);
    }

    [Fact]
    public void Constructor_SetsAllProperties()
    {
        var id = Guid.NewGuid();
        DateTime before = DateTime.UtcNow;

        var user = new User(id, "a@b.com", "Alice");

        Assert.Equal(id, user.Id);
        Assert.Equal("a@b.com", user.Email);
        Assert.Equal("Alice", user.DisplayName);
        Assert.Equal(UserStatus.Active, user.Status);
        Assert.InRange(user.CreatedAt, before, DateTime.UtcNow);
        Assert.Empty(user.Roles);
    }

    [Fact]
    public void Constructor_WithEmptyGuid_Throws() =>
        Assert.Throws<ArgumentException>(() => new User(Guid.Empty, "a@b.com", "Alice"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankEmail_Throws(string email) =>
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), email, "Alice"));

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithBlankDisplayName_Throws(string name) =>
        Assert.Throws<ArgumentException>(() => new User(Guid.NewGuid(), "a@b.com", name));

    [Fact]
    public void User_can_be_deactivated()
    {
        User user = CreateUser();

        user.Deactivate();

        Assert.Equal(UserStatus.Disabled, user.Status);
    }

    [Fact]
    public void Rename_ChangesDisplayName()
    {
        User user = CreateUser();

        user.Rename("New Name");

        Assert.Equal("New Name", user.DisplayName);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Rename_WithBlankName_Throws(string name)
    {
        User user = CreateUser();

        Assert.Throws<ArgumentException>(() => user.Rename(name));
    }

    [Fact]
    public void AssignRole_AddsRoleToUser()
    {
        User user = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);

        Assert.Contains(roleId, user.Roles);
    }

    [Fact]
    public void AssignRole_SameRoleTwice_DoesNotDuplicate()
    {
        User user = CreateUser();
        var roleId = Guid.NewGuid();

        user.AssignRole(roleId);
        user.AssignRole(roleId);

        Assert.Single(user.Roles);
    }

    [Fact]
    public void RemoveRole_RemovesAssignedRole()
    {
        User user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        user.RemoveRole(roleId);

        Assert.DoesNotContain(roleId, user.Roles);
    }

    [Fact]
    public void IsAdmin_WhenUserHasMatchingRole_ReturnsTrue()
    {
        User user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        bool result = user.IsAdmin([roleId.ToString()]);

        Assert.True(result);
    }

    [Fact]
    public void IsAdmin_WhenUserHasNoMatchingRole_ReturnsFalse()
    {
        User user = CreateUser();
        user.AssignRole(Guid.NewGuid());

        bool result = user.IsAdmin([Guid.NewGuid().ToString()]);

        Assert.False(result);
    }

    [Fact]
    public void IsAdmin_IsCaseInsensitive()
    {
        User user = CreateUser();
        var roleId = Guid.NewGuid();
        user.AssignRole(roleId);

        bool result = user.IsAdmin([roleId.ToString().ToUpperInvariant()]);

        Assert.True(result);
    }
}
