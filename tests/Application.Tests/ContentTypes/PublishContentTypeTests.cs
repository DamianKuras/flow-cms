using Application.ContentTypes;
using Application.Interfaces;
using Domain;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.ContentTypes;

public class PublishContentTypeCommandHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockContentTypeRepo = new();
    private readonly Mock<IContentItemRepository> _mockContentItemRepo = new();
    private readonly Mock<IMigrationJobRepository> _mockMigrationJobRepo = new();
    private readonly Mock<IAuthorizationService> _mockAuth = new();
    private readonly Mock<IUserContext> _mockUserContext = new();
    private readonly PublishContentTypeCommandHandler _handler;

    public PublishContentTypeCommandHandlerTests()
    {
        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Publish, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        _mockUserContext.Setup(u => u.IsAuthenticated).Returns(true);
        _mockUserContext.Setup(u => u.UserId).Returns(Guid.NewGuid());
        _mockContentTypeRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockMigrationJobRepo
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new PublishContentTypeCommandHandler(
            _mockContentTypeRepo.Object,
            _mockContentItemRepo.Object,
            _mockMigrationJobRepo.Object,
            _mockAuth.Object,
            _mockUserContext.Object,
            Mock.Of<ILogger<PublishContentTypeCommandHandler>>()
        );
    }

    private static ContentType MakeDraft(string name = "Article") =>
        new(Guid.NewGuid(), name, [new Field(Guid.NewGuid(), FieldTypes.Text, "Title", false)]);

    private static ContentType MakePublished(string name = "Article", int version = 1) =>
        new(Guid.NewGuid(), name, [new Field(Guid.NewGuid(), FieldTypes.Text, "Title", false)],
            version: version, status: ContentTypeStatus.PUBLISHED);

    [Fact]
    public async Task Handle_FirstPublish_ReturnsSuccessWithNoMigrationJob()
    {
        // Arrange
        ContentType draft = MakeDraft();
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        _mockContentTypeRepo
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        // Act
        Result<Guid> result = await _handler.Handle(
            new PublishContentTypeCommand("Article"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMigrationJobRepo.Verify(r => r.AddAsync(It.IsAny<MigrationJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SubsequentPublish_WithNoExistingItems_DoesNotCreateMigrationJob()
    {
        // Arrange
        ContentType draft = MakeDraft();
        ContentType previousPublished = MakePublished();
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        _mockContentTypeRepo
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousPublished);
        _mockContentItemRepo
            .Setup(r => r.CountAsync(previousPublished.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        Result<Guid> result = await _handler.Handle(
            new PublishContentTypeCommand("Article"), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMigrationJobRepo.Verify(r => r.AddAsync(It.IsAny<MigrationJob>(), It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_SubsequentPublish_WithExistingItems_CreatesMigrationJob()
    {
        // Arrange
        ContentType draft = MakeDraft();
        ContentType previousPublished = MakePublished();
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        _mockContentTypeRepo
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousPublished);
        _mockContentItemRepo
            .Setup(r => r.CountAsync(previousPublished.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(5);

        // Act
        Result<Guid> result = await _handler.Handle(
            new PublishContentTypeCommand("Article", MigrationMode.Eager), CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockMigrationJobRepo.Verify(
            r => r.AddAsync(
                It.Is<MigrationJob>(j =>
                    j.Mode == MigrationMode.Eager &&
                    j.TotalItemsCount == 5 &&
                    j.FromSchemaId == previousPublished.Id),
                It.IsAny<CancellationToken>()),
            Times.Once);
    }

    [Fact]
    public async Task Handle_WhenDraftNotFound_ReturnsNotFound()
    {
        // Arrange
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Ghost", It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        // Act
        Result<Guid> result = await _handler.Handle(
            new PublishContentTypeCommand("Ghost"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        ContentType draft = MakeDraft();
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Publish, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);

        // Act
        Result<Guid> result = await _handler.Handle(
            new PublishContentTypeCommand("Article"), CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockContentTypeRepo.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_ArchivesPreviousPublication_WhenPublishingNewVersion()
    {
        // Arrange
        ContentType draft = MakeDraft();
        ContentType previousPublished = MakePublished();
        _mockContentTypeRepo
            .Setup(r => r.GetLatestDraftVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(draft);
        _mockContentTypeRepo
            .Setup(r => r.GetLatestsPublishedVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(previousPublished);
        _mockContentItemRepo
            .Setup(r => r.CountAsync(previousPublished.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        // Act
        await _handler.Handle(new PublishContentTypeCommand("Article"), CancellationToken.None);

        // Assert
        _mockContentTypeRepo.Verify(r => r.SoftDelete(previousPublished), Times.Once);
    }
}
