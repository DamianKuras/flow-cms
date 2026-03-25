using Domain.Permissions;
using Domain.Roles;

namespace Domain.Tests.Roles;

public class RoleTests
{
    [Fact]
    public void AddPermission_ShouldAddRuleToRole()
    {
        var role = new Role(Guid.NewGuid(), "Editor");
        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Create,
            new ContentItemResource(Guid.NewGuid()),
            PermissionScope.Allow
        );

        role.AddPermission(rule);

        Assert.Single(role.Permissions);
        Assert.Contains(rule, role.Permissions);
    }

    [Fact]
    public void AddPermission_ShouldNotAddDuplicateRules_WhenUsingSameReference()
    {
        var role = new Role(Guid.NewGuid(), "Editor");
        var rule = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Create,
            new ContentItemResource(Guid.NewGuid()),
            PermissionScope.Allow
        );

        role.AddPermission(rule);
        role.AddPermission(rule);

        Assert.Single(role.Permissions);
    }

    [Fact]
    public void AddPermission_ShouldAddMultipleDistinctRules()
    {
        var role = new Role(Guid.NewGuid(), "Editor");
        var rule1 = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Create,
            new ContentItemResource(Guid.NewGuid()),
            PermissionScope.Allow
        );
        var rule2 = PermissionRule.ForResource(
            ActorType.User,
            CmsAction.Read,
            new ContentItemResource(Guid.NewGuid()),
            PermissionScope.Allow
        );

        role.AddPermission(rule1);
        role.AddPermission(rule2);

        Assert.Equal(2, role.Permissions.Count);
        Assert.Contains(rule1, role.Permissions);
        Assert.Contains(rule2, role.Permissions);
    }
}
