using Application.ContentTypes;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace Application.Tests.ContentTypes;

public class DeleteContentTypeHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<DeleteContentTypeHandler>> _mockLogger;
    private readonly DeleteContentTypeHandler _handler;

    public DeleteContentTypeHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<DeleteContentTypeHandler>>();
        _handler = new DeleteContentTypeHandler(
            _mockRepository.Object,
            _mockAuth.Object,
            _mockLogger.Object
        );
    }

    [Fact]
    public async Task Handle_WhenAuthorized_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteContentTypeCommand(Guid.NewGuid());
        var contentType = new ContentType(command.Id, "TestType", []);

        _mockRepository
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a =>
                a.IsAllowedForTypeAsync(
                    CmsAction.Delete,
                    ResourceType.ContentType,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);

        _mockRepository.Setup(r => r.SoftDelete(contentType)).Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(command.Id, result.Value);
        _mockRepository.Verify(r => r.SoftDelete(contentType), Times.Once);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_WhenContentTypeNotFound_ReturnsNotFound()
    {
        // Arrange
        var command = new DeleteContentTypeCommand(Guid.NewGuid());

        _mockRepository
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRepository.Verify(r => r.SoftDelete(It.IsAny<ContentType>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        var command = new DeleteContentTypeCommand(Guid.NewGuid());
        var contentType = new ContentType(command.Id, "TestType", []);

        _mockRepository
            .Setup(r => r.GetByIdAsync(command.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        _mockAuth
            .Setup(a =>
                a.IsAllowedForTypeAsync(
                    CmsAction.Delete,
                    ResourceType.ContentType,
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.SoftDelete(It.IsAny<ContentType>()), Times.Never);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
