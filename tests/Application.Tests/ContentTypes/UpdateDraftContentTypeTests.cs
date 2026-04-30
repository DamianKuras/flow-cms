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

namespace Application.Tests.ContentTypes;

public class UpdateDraftContentTypeCommandHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository = new();
    private readonly Mock<IValidationRuleRegistry> _mockValidationRegistry = new();
    private readonly Mock<ITransformationRuleRegistry> _mockTransformationRegistry = new();
    private readonly Mock<IAuthorizationService> _mockAuth = new();
    private readonly UpdateDraftContentTypeCommandHandler _handler;

    public UpdateDraftContentTypeCommandHandlerTests()
    {
        _mockRepository
            .Setup(r => r.UpdateAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Update, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        _handler = new UpdateDraftContentTypeCommandHandler(
            _mockRepository.Object,
            _mockValidationRegistry.Object,
            _mockTransformationRegistry.Object,
            _mockAuth.Object,
            Mock.Of<ILogger<UpdateDraftContentTypeCommandHandler>>()
        );
    }

    private static ContentType MakeDraft(params string[] fieldNames)
    {
        var fields = fieldNames
            .Select(n => new Field(Guid.NewGuid(), FieldTypes.Text, n, isRequired: false))
            .ToList();
        return new ContentType(Guid.NewGuid(), "Article", fields);
    }

    [Fact]
    public async Task Handle_WithNewField_ReturnsSuccess()
    {
        // Arrange
        ContentType draft = MakeDraft();
        var command = new UpdateDraftContentTypeCommand(
            draft.Id,
            [new UpdateFieldDto(null, "Title", FieldTypes.Text, false)]
        );
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(draft.Id, result.Value);
        Assert.Single(draft.Fields);
        Assert.Equal("Title", draft.Fields[0].Name);
    }

    [Fact]
    public async Task Handle_WithExistingFieldId_UpdatesInPlace()
    {
        // Arrange
        ContentType draft = MakeDraft("OldName");
        Guid existingFieldId = draft.Fields[0].Id;

        var command = new UpdateDraftContentTypeCommand(
            draft.Id,
            [new UpdateFieldDto(existingFieldId, "NewName", FieldTypes.Text, true)]
        );
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Single(draft.Fields);
        // Same field ID — updated in place, not replaced
        Assert.Equal(existingFieldId, draft.Fields[0].Id);
        Assert.Equal("NewName", draft.Fields[0].Name);
        Assert.True(draft.Fields[0].IsRequired);
    }

    [Fact]
    public async Task Handle_OmittedField_IsRemovedFromDraft()
    {
        // Arrange
        ContentType draft = MakeDraft("Keep", "Remove");
        Guid keepId = draft.Fields[0].Id;

        var command = new UpdateDraftContentTypeCommand(
            draft.Id,
            [new UpdateFieldDto(keepId, "Keep", FieldTypes.Text, false)]
        );
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);

        // Act
        await _handler.Handle(command, CancellationToken.None);

        // Assert — only the kept field remains
        Assert.Single(draft.Fields);
        Assert.Equal(keepId, draft.Fields[0].Id);
    }

    [Fact]
    public async Task Handle_WhenDraftNotFound_ReturnsNotFound()
    {
        // Arrange
        var id = Guid.NewGuid();
        _mockRepository.Setup(r => r.GetByIdAsync(id, It.IsAny<CancellationToken>())).ReturnsAsync((ContentType?)null);
        var command = new UpdateDraftContentTypeCommand(id, [new UpdateFieldDto(null, "Title", FieldTypes.Text, false)]);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.NotFound, result.Error?.Type);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenNotDraft_ReturnsConflict()
    {
        // Arrange
        var published = new ContentType(
            Guid.NewGuid(), "Article",
            [new Field(Guid.NewGuid(), FieldTypes.Text, "Title", false)],
            version: 1, status: ContentTypeStatus.PUBLISHED
        );
        _mockRepository.Setup(r => r.GetByIdAsync(published.Id, It.IsAny<CancellationToken>())).ReturnsAsync(published);
        var command = new UpdateDraftContentTypeCommand(published.Id, [new UpdateFieldDto(null, "Title", FieldTypes.Text, false)]);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Conflict, result.Error?.Type);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WhenUnauthorized_ReturnsForbidden()
    {
        // Arrange
        ContentType draft = MakeDraft("Title");
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        _mockAuth
            .Setup(a => a.IsAllowedAsync(CmsAction.Update, It.IsAny<ContentTypeResource>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(false);
        var command = new UpdateDraftContentTypeCommand(draft.Id, [new UpdateFieldDto(null, "Title", FieldTypes.Text, false)]);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(ErrorTypes.Forbidden, result.Error?.Type);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithBlankFieldName_ReturnsValidationFailure()
    {
        // Arrange
        ContentType draft = MakeDraft();
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        var command = new UpdateDraftContentTypeCommand(draft.Id, [new UpdateFieldDto(null, "", FieldTypes.Text, false)]);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        _mockRepository.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Handle_WithUnknownValidationRule_ReturnsValidationFailure()
    {
        // Arrange
        ContentType draft = MakeDraft();
        _mockRepository.Setup(r => r.GetByIdAsync(draft.Id, It.IsAny<CancellationToken>())).ReturnsAsync(draft);
        _mockValidationRegistry
            .Setup(r => r.TryCreate("UnknownRule", It.IsAny<Dictionary<string, object>?>(), out It.Ref<IValidationRule?>.IsAny))
            .Returns(false);

        var command = new UpdateDraftContentTypeCommand(
            draft.Id,
            [new UpdateFieldDto(null, "Title", FieldTypes.Text, false,
                ValidationRules: [new CreateValidationRuleDto("UnknownRule", null)])]
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.False(result.IsSuccess);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
    }
}
