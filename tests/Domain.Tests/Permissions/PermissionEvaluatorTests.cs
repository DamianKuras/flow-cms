using Domain.Permissions;
using Domain.Users;
using Xunit;

namespace Domain.Tests.Permissions;

public class PermissionEvaluatorTests
{
    private readonly PermissionEvaluator _evaluator = new();
    private readonly UserActor _actor = new(Guid.NewGuid());

    [Fact]
    public void IsAllowed_WhenMatchingAllowRule_ReturnsTrue()
    {
        // Arrange
        var resource = new ContentItemResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(ActorType.User, CmsAction.Read, resource),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Read, resource, rules);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowed_WhenNoMatchingRule_ReturnsFalse()
    {
        // Arrange
        var resource = new ContentItemResource(Guid.NewGuid());

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Read, resource, []);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_DenyOverridesAllow()
    {
        // Arrange
        var resource = new ContentItemResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(
                ActorType.User,
                CmsAction.Update,
                resource,
                PermissionScope.Allow
            ),
            PermissionRule.ForResource(
                ActorType.User,
                CmsAction.Update,
                resource,
                PermissionScope.Deny
            ),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Update, resource, rules);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_WhenOnlyDenyRule_ReturnsFalse()
    {
        // Arrange
        var resource = new ContentItemResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(
                ActorType.User,
                CmsAction.Delete,
                resource,
                PermissionScope.Deny
            ),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Delete, resource, rules);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_WhenRuleIsForDifferentResource_ReturnsFalse()
    {
        // Arrange
        var targetResource = new ContentItemResource(Guid.NewGuid());
        var otherResource = new ContentItemResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(ActorType.User, CmsAction.Read, otherResource),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Read, targetResource, rules);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_WhenRuleIsForDifferentAction_ReturnsFalse()
    {
        // Arrange
        var resource = new ContentItemResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(ActorType.User, CmsAction.Create, resource),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Read, resource, rules);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowed_AggregatesAllowRulesAcrossMultipleRoles()
    {
        // Arrange
        var resource = new ContentTypeResource(Guid.NewGuid());
        var unrelatedResource = new ContentTypeResource(Guid.NewGuid());
        PermissionRule[] rules =
        [
            PermissionRule.ForResource(ActorType.User, CmsAction.Read, resource),
            PermissionRule.ForResource(ActorType.User, CmsAction.Delete, unrelatedResource),
        ];

        // Act
        bool result = _evaluator.IsAllowed(_actor, CmsAction.Read, resource, rules);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedForAll_WhenMatchingTypeLevelAllowRule_ReturnsTrue()
    {
        // Arrange
        PermissionRule[] rules =
        [
            PermissionRule.ForResourceType(
                ActorType.User,
                CmsAction.List,
                ResourceType.ContentType
            ),
        ];

        // Act
        bool result = _evaluator.IsAllowedForAll(
            _actor,
            CmsAction.List,
            ResourceType.ContentType,
            rules
        );

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsAllowedForAll_WhenNoMatchingRule_ReturnsFalse()
    {
        // Act
        bool result = _evaluator.IsAllowedForAll(
            _actor,
            CmsAction.List,
            ResourceType.ContentType,
            []
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedForAll_DenyOverridesAllow()
    {
        // Arrange
        PermissionRule[] rules =
        [
            PermissionRule.ForResourceType(
                ActorType.User,
                CmsAction.List,
                ResourceType.ContentType,
                PermissionScope.Allow
            ),
            PermissionRule.ForResourceType(
                ActorType.User,
                CmsAction.List,
                ResourceType.ContentType,
                PermissionScope.Deny
            ),
        ];

        // Act
        bool result = _evaluator.IsAllowedForAll(
            _actor,
            CmsAction.List,
            ResourceType.ContentType,
            rules
        );

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsAllowedForAll_WhenRuleIsForDifferentResourceType_ReturnsFalse()
    {
        // Arrange
        PermissionRule[] rules =
        [
            PermissionRule.ForResourceType(
                ActorType.User,
                CmsAction.List,
                ResourceType.ContentItem
            ),
        ];

        // Act
        bool result = _evaluator.IsAllowedForAll(
            _actor,
            CmsAction.List,
            ResourceType.ContentType,
            rules
        );

        // Assert
        Assert.False(result);
    }
}
