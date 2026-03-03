using Application.ContentTypes;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ContentTypes;

public class ArchiveContentTypeCommandHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<ArchiveContentTypeCommandHandler>> _mockLogger;
    private readonly ArchiveContentTypeCommandHandler _handler;

    public ArchiveContentTypeCommandHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<ArchiveContentTypeCommandHandler>>();
        
        _handler = new ArchiveContentTypeCommandHandler(
            _mockRepository.Object,
            _mockAuth.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommandAndAuthorized_ReturnsSuccess()
    {
        // Arrange
        var command = new ArchiveContentTypeCommand("Article");
        var contentType = new ContentType(Guid.NewGuid(), "Article", []);

        _mockRepository
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Archive, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(contentType.Id, result.Value);
        Assert.Equal(ContentTypeStatus.ARCHIVE, contentType.Status);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WithNonExistentContentType_ReturnsNotFound()
    {
        // Arrange
        var command = new ArchiveContentTypeCommand("NonExistent");

        _mockRepository
            .Setup(r => r.GetLatestsPublishedVersion("NonExistent", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var command = new ArchiveContentTypeCommand("Article");
        var contentType = new ContentType(Guid.NewGuid(), "Article", []);

        _mockRepository
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Archive, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
