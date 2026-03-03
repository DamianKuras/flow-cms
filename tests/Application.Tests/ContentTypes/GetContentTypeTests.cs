using Application.ContentTypes;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ContentTypes;

public class GetContentTypeHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<GetContentTypeHandler>> _mockLogger;
    private readonly GetContentTypeHandler _handler;

    public GetContentTypeHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<GetContentTypeHandler>>();
        _handler = new GetContentTypeHandler(_mockRepository.Object, _mockAuth.Object, _mockLogger.Object);
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReturnsContentTypeDto()
    {
        // Arrange
        var query = new GetContentTypeQuery(Guid.NewGuid());
        var contentType = new ContentType(query.Id, "TestType", new List<Field>());

        _mockRepository
            .Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.Read, ResourceType.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(result.Value);
        Assert.Equal(contentType.Id, result.Value.Id);
        Assert.Equal(contentType.Name, result.Value.Name);
        _mockRepository.Verify(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var query = new GetContentTypeQuery(Guid.NewGuid());
        var contentType = new ContentType(query.Id, "TestType", new List<Field>());

        _mockRepository
            .Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a => a.IsAllowedForTypeAsync(CmsAction.Read, ResourceType.ContentType, It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenContentTypeNotFound_ReturnsNotFound()
    {
        // Arrange
        var query = new GetContentTypeQuery(Guid.NewGuid());

        _mockRepository
            .Setup(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        // Act
        var result = await _handler.Handle(query, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRepository.Verify(r => r.GetByIdAsync(query.Id, It.IsAny<CancellationToken>()), Times.Once);
        _mockAuth.Verify(a => a.IsAllowedForTypeAsync(It.IsAny<CmsAction>(), It.IsAny<ResourceType>(), It.IsAny<CancellationToken>()), Times.Never);
    }
}
