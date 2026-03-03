using Application.ContentItems;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ContentItems;

public class DeleteContentItemCommandHandlerTests
{
    private readonly Mock<IContentItemRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<DeleteContentItemCommandHandler>> _mockLogger;
    private readonly DeleteContentItemCommandHandler _handler;

    public DeleteContentItemCommandHandlerTests()
    {
        _mockRepository = new Mock<IContentItemRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<DeleteContentItemCommandHandler>>();
        
        _handler = new DeleteContentItemCommandHandler(
            _mockRepository.Object,
            _mockAuth.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommandAndAuthorized_ReturnsSuccess()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        var command = new DeleteContentItemCommand(contentItemId);
        var contentItem = new ContentItem(contentItemId, "Test Item", Guid.NewGuid());

        _mockRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);

        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Delete, It.IsAny<ContentItemResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.DeleteAsync(contentItem))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(contentItemId, result.Value);
        _mockRepository.Verify(r => r.DeleteAsync(contentItem), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentContentItem_ReturnsNotFound()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        var command = new DeleteContentItemCommand(contentItemId);

        _mockRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentItem?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        var command = new DeleteContentItemCommand(contentItemId);
        var contentItem = new ContentItem(contentItemId, "Test Item", Guid.NewGuid());

        _mockRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);

        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Delete, It.IsAny<ContentItemResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.DeleteAsync(It.IsAny<ContentItem>()), Times.Never);
    }
}
