using Application.ContentItems;
using Application.Interfaces;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Domain.Permissions;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.ContentItems;

public class UpdateContentItemHandlerTests
{
    private readonly Mock<IContentItemRepository> _mockContentItemRepository;
    private readonly Mock<IContentTypeRepository> _mockContentTypeRepository;
    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly UpdateContentItemHandler _handler;

    public UpdateContentItemHandlerTests()
    {
        _mockContentItemRepository = new Mock<IContentItemRepository>();
        _mockContentTypeRepository = new Mock<IContentTypeRepository>();
        _mockAuth = new Mock<IAuthorizationService>();
        var mockLogger = new Mock<ILogger<UpdateContentItemHandler>>();

        _mockContentItemRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ContentItem>()))
            .Returns(Task.CompletedTask);
        _mockContentItemRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new UpdateContentItemHandler(
            _mockContentItemRepository.Object,
            _mockContentTypeRepository.Object,
            _mockAuth.Object,
            mockLogger.Object
        );
    }

    private static ContentType BuildContentType(
        params (Guid id, string name, bool isRequired, IValidationRule[]? rules)[] fieldDefs
    )
    {
        var fields = fieldDefs
            .Select(f =>
            {
                var field = new Field(f.id, FieldTypes.Text, f.name, f.isRequired);
                field.SetValidationRules(f.rules?.ToList() ?? []);
                return field;
            })
            .ToList();

        return new ContentType(Guid.NewGuid(), "TestType", fields.AsReadOnly());
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccess()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType((fieldId, "Title", isRequired: false, rules: null));
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(contentItemId, "My Item", contentType.Id);

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);
        _mockAuth
            .Setup(a =>
                a.IsAllowedAsync(
                    CmsAction.Update,
                    It.IsAny<ContentTypeResource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new UpdateContentItemCommand(
            contentItemId,
            new Dictionary<Guid, object?> { [fieldId] = "Updated value" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(contentItemId, result.Value);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(contentItem), Times.Once);
        _mockContentItemRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WhenContentItemNotFound_ReturnsNotFound()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentItem?)null);

        var command = new UpdateContentItemCommand(contentItemId, new Dictionary<Guid, object?>());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenContentItemIsPublished_ReturnsConflict()
    {
        // Arrange
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(
            contentItemId,
            "My Item",
            Guid.NewGuid(),
            status: ContentItemStatus.Published
        );

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);

        var command = new UpdateContentItemCommand(contentItemId, new Dictionary<Guid, object?>());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Conflict, result.Error?.Type);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        ContentType contentType = BuildContentType();
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(contentItemId, "My Item", contentType.Id);

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);
        _mockAuth
            .Setup(a =>
                a.IsAllowedAsync(
                    CmsAction.Update,
                    It.IsAny<ContentTypeResource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(false);

        var command = new UpdateContentItemCommand(contentItemId, new Dictionary<Guid, object?>());

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownFieldId_ReturnsConflict()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType((fieldId, "Title", isRequired: false, rules: null));
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(contentItemId, "My Item", contentType.Id);
        var unknownFieldId = Guid.NewGuid();

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);
        _mockAuth
            .Setup(a =>
                a.IsAllowedAsync(
                    CmsAction.Update,
                    It.IsAny<ContentTypeResource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new UpdateContentItemCommand(
            contentItemId,
            new Dictionary<Guid, object?> { [unknownFieldId] = "value" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Conflict, result.Error?.Type);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenFieldFailsValidation_ReturnsMultiFieldValidationFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId, "Title", isRequired: false, rules: [new MaximumLengthValidationRule(5)])
        );
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(contentItemId, "My Item", contentType.Id);

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);
        _mockAuth
            .Setup(a =>
                a.IsAllowedAsync(
                    CmsAction.Update,
                    It.IsAny<ContentTypeResource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new UpdateContentItemCommand(
            contentItemId,
            new Dictionary<Guid, object?> { [fieldId] = "This is way too long" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenMultipleFieldsFailValidation_ReturnsAllErrors()
    {
        // Arrange
        var fieldId1 = Guid.NewGuid();
        var fieldId2 = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId1, "Title", isRequired: false, rules: [new MaximumLengthValidationRule(5)]),
            (fieldId2, "Body", isRequired: false, rules: [new MaximumLengthValidationRule(5)])
        );
        var contentItemId = Guid.NewGuid();
        var contentItem = new ContentItem(contentItemId, "My Item", contentType.Id);

        _mockContentItemRepository
            .Setup(r => r.GetByIdAsync(contentItemId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentItem);
        _mockAuth
            .Setup(a =>
                a.IsAllowedAsync(
                    CmsAction.Update,
                    It.IsAny<ContentTypeResource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new UpdateContentItemCommand(
            contentItemId,
            new Dictionary<Guid, object?>
            {
                [fieldId1] = "Too long value",
                [fieldId2] = "Also too long",
            }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.Equal(
            2,
            result.MultiFieldValidationResult!.ValidationResults.Count(vr => vr.IsInvalid)
        );
        _mockContentItemRepository.Verify(r => r.UpdateAsync(It.IsAny<ContentItem>()), Times.Never);
    }
}
