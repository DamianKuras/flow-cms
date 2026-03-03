using Application.Interfaces;
using Application.ValidationRules;
using Domain.Common;
using Domain.Fields.Validations;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ValidationRules;

public class GetValidationRulesQueryHandlerTests
{
    private readonly Mock<IValidationRuleRegistry> _mockRegistry;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<GetValidationRulesQueryHandler>> _mockLogger;
    private readonly GetValidationRulesQueryHandler _handler;

    public GetValidationRulesQueryHandlerTests()
    {
        _mockRegistry = new Mock<IValidationRuleRegistry>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GetValidationRulesQueryHandler>>();
        _handler = new GetValidationRulesQueryHandler(_mockRegistry.Object, _mockAuth.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReturnsAllValidationRuleNames()
    {
        // Arrange
        var query = new GetValidationRulesQuery();
        var expectedNames = new List<string> { "Rule1", "Rule2", "Rule3" };

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.Read, ResourceType.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRegistry
            .Setup(r => r.GetAllRules())
            .Returns(expectedNames);

        // Act
        Result<GetValidationRulesResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(3, result.Value.ValidationRuleNames.Count);
        Assert.Contains("Rule1", result.Value.ValidationRuleNames);
        Assert.Contains("Rule2", result.Value.ValidationRuleNames);
        Assert.Contains("Rule3", result.Value.ValidationRuleNames);
        _mockRegistry.Verify(r => r.GetAllRules(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenNoRulesExistAndAuthorized_ReturnsEmptyList()
    {
        // Arrange
        var query = new GetValidationRulesQuery();
        var expectedNames = new List<string>();

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.Read, ResourceType.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRegistry
            .Setup(r => r.GetAllRules())
            .Returns(expectedNames);

        // Act
        Result<GetValidationRulesResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Empty(result.Value.ValidationRuleNames);
        _mockRegistry.Verify(r => r.GetAllRules(), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var query = new GetValidationRulesQuery();

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.Read, ResourceType.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<GetValidationRulesResponse> result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRegistry.Verify(r => r.GetAllRules(), Times.Never);
    }
}
