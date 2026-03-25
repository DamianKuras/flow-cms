using Application.ContentItems;
using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.ContentItems;

public class CreateContentItemHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockContentTypeRepository;
    private readonly Mock<IContentItemRepository> _mockContentItemRepository;
    private readonly CreateContentItemHandler _handler;

    public CreateContentItemHandlerTests()
    {
        _mockContentTypeRepository = new Mock<IContentTypeRepository>();
        _mockContentItemRepository = new Mock<IContentItemRepository>();
        var mockLogger = new Mock<ILogger<GetContentItemByIdHandler>>();

        _mockContentItemRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);
        _mockContentItemRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _handler = new CreateContentItemHandler(
            _mockContentTypeRepository.Object,
            _mockContentItemRepository.Object,
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
        ContentType contentType = BuildContentType(
            (fieldId, "Title", isRequired: true, rules: null)
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?> { [fieldId] = "Hello" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
        _mockContentItemRepository.Verify(
            r => r.SaveChangesAsync(It.IsAny<CancellationToken>()),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithEmptyTitle_ReturnsValidationFailure()
    {
        // Arrange
        var command = new CreateContentItemCommand(
            "",
            Guid.NewGuid(),
            new Dictionary<Guid, object?>()
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.NotNull(result.MultiFieldValidationResult?.GetFieldResult("Title"));
        Assert.True(result.MultiFieldValidationResult!.GetFieldResult("Title")!.IsInvalid);
    }

    [Fact]
    public async Task Handle_WithEmptyContentTypeId_ReturnsValidationFailure()
    {
        // Arrange
        var command = new CreateContentItemCommand(
            "My Item",
            Guid.Empty,
            new Dictionary<Guid, object?>()
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.NotNull(result.MultiFieldValidationResult?.GetFieldResult("ContentTypeId"));
        Assert.True(result.MultiFieldValidationResult!.GetFieldResult("ContentTypeId")!.IsInvalid);
    }

    [Fact]
    public async Task Handle_WhenContentTypeNotFound_ReturnsValidationError()
    {
        // Arrange
        var contentTypeId = Guid.NewGuid();
        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentTypeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync((ContentType?)null);

        var command = new CreateContentItemCommand(
            "My Item",
            contentTypeId,
            new Dictionary<Guid, object?>()
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Validation, result.Error?.Type);
    }

    [Fact]
    public async Task Handle_WithUnknownFieldId_ReturnsConflictError()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId, "Title", isRequired: false, rules: null)
        );
        var unknownFieldId = Guid.NewGuid();

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?> { [unknownFieldId] = "value" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Conflict, result.Error?.Type);
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithMissingRequiredField_ReturnsValidationError()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId, "Title", isRequired: true, rules: null)
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?>() // required field omitted
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(ErrorTypes.Validation, result.Error?.Type);
        Assert.Contains("Title", result.Error!.Message);
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenRequiredFieldValueFailsValidationRule_ReturnsFieldValidationError()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId, "Title", isRequired: true, rules: [new MaximumLengthValidationRule(5)])
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?> { [fieldId] = "This value exceeds max length of 5" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.NotNull(result.MultiFieldValidationResult);
        Assert.True(result.MultiFieldValidationResult.IsFailure);
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenOptionalFieldValueFailsValidationRule_ReturnsFieldValidationError()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId, "Description", isRequired: false, rules: [new MaximumLengthValidationRule(5)])
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?> { [fieldId] = "This value exceeds max length of 5" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.NotNull(result.MultiFieldValidationResult);
        Assert.True(result.MultiFieldValidationResult.IsFailure);
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenMultipleFieldsFailValidationRule_ReturnsAllErrors()
    {
        // Arrange
        var fieldId1 = Guid.NewGuid();
        var fieldId2 = Guid.NewGuid();
        ContentType contentType = BuildContentType(
            (fieldId1, "Title", isRequired: true, rules: [new MaximumLengthValidationRule(5)]),
            (fieldId2, "Subtitle", isRequired: true, rules: [new MaximumLengthValidationRule(5)])
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?>
            {
                [fieldId1] = "Too long value",
                [fieldId2] = "Also too long value",
            }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.MultiFieldValidation, result.FailureKind);
        Assert.NotNull(result.MultiFieldValidationResult);
        Assert.Equal(
            2,
            result.MultiFieldValidationResult.ValidationResults.Count(vr => vr.IsInvalid)
        );
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WhenTransformerThrows_ReturnsDomainFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var field = new Field(
            fieldId,
            FieldTypes.Text,
            "Body",
            isRequired: false,
            fieldTransformers: [new ThrowingTransformer()]
        );
        var contentType = new ContentType(
            Guid.NewGuid(),
            "TestType",
            new List<Field> { field }.AsReadOnly()
        );

        _mockContentTypeRepository
            .Setup(r => r.GetByIdAsync(contentType.Id, It.IsAny<CancellationToken>()))
            .ReturnsAsync(contentType);

        var command = new CreateContentItemCommand(
            "My Item",
            contentType.Id,
            new Dictionary<Guid, object?> { [fieldId] = "some value" }
        );

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert — transformer failure is not a field validation error, it bubbles as a domain failure
        Assert.True(result.IsFailure);
        Assert.NotEqual(FailureKind.MultiFieldValidation, result.FailureKind);
        _mockContentItemRepository.Verify(
            r => r.AddAsync(It.IsAny<ContentItem>(), It.IsAny<CancellationToken>()),
            Times.Never
        );
    }

    private sealed class ThrowingTransformer : ITransformationRule
    {
        public string Type => "throwing";
        public Capability RequiredCapability => new(Capability.Standard.TEXT);

        public object? ApplyTransformation(object? value) =>
            throw new InvalidOperationException("Transformer exploded.");
    }
}
