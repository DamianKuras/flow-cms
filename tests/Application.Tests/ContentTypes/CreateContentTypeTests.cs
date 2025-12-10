using Application.ContentTypes;
using Application.Fields;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Validations;
using Infrastructure.Interfaces;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace Application.Tests.ContentTypes;

public class CreateContentTypeCommandTests
{
    [Fact]
    public void Validate_WithValidData_ReturnsSuccess()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "TestContentType",
            new List<CreateFieldDto> { new CreateFieldDto("Text", FieldTypes.Text, true) }
        );

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void Validate_WithEmptyName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "",
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true) }
        );

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors!, e => e.Message == "Name field is empty.");
    }

    [Fact]
    public void Validate_WithNullName_ReturnsFailure()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            null!,
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true) }
        );

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors!, e => e.Message == "Name field is empty.");
    }

    [Fact]
    public void Validate_WithNullFields_ReturnsFailure()
    {
        // Arrange
        var command = new CreateContentTypeCommand("TestContentType", null!);

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors!, e => e.Message == "Fields field is empty.");
    }

    [Fact]
    public void Validate_WithEmptyFields_ReturnsFailure()
    {
        // Arrange
        var command = new CreateContentTypeCommand("TestContentType", new List<CreateFieldDto>());

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors!, e => e.Message == "Fields field is empty.");
    }

    [Fact]
    public void Validate_WithMultipleErrors_ReturnsAllErrors()
    {
        // Arrange
        var command = new CreateContentTypeCommand("", new List<CreateFieldDto>());

        // Act
        var result = command.Validate();

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(2, result.Errors!.Count);
    }
}

public class CreateContentTypeCommandHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IValidationRuleRegistry> _mockRegistry;
    private readonly CreateContentTypeCommandHandler _handler;

    public CreateContentTypeCommandHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockRegistry = new Mock<IValidationRuleRegistry>();
        _handler = new CreateContentTypeCommandHandler(
            _mockRepository.Object,
            _mockRegistry.Object
        );
    }

    [Fact]
    public async Task Handle_WithValidCommand_ReturnsSuccessWithGuid()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true) }
        );

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotEqual(Guid.Empty, result.Value);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ReturnsFailure()
    {
        // Arrange
        var command = new CreateContentTypeCommand("", new List<CreateFieldDto>());

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Errors);
    }

    [Fact]
    public async Task Handle_WithUnknownValidationRule_ReturnsFailure()
    {
        // Arrange
        var unknownRuleType = "NonExistentRule";
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(unknownRuleType, null),
                    }
                ),
            }
        );

        _mockRegistry
            .Setup(r => r.TryCreate("UnknownRule", null, out It.Ref<IValidationRule?>.IsAny))
            .Returns(false);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(result.Errors!, e => e.Message.Contains("Unknown validation rule type"));
    }

    [Fact]
    public async Task Handle_WithValidValidationRules_CreatesContentType()
    {
        // Arrange
        var mockRule = new Mock<IValidationRule>();
        var maxLengthRule = new MaximumLengthValidationRule(100);

        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(maxLengthRule.Type, maxLengthRule.Parameters),
                    }
                ),
            }
        );

        IValidationRule? outputRule = mockRule.Object;
        _mockRegistry
            .Setup(r => r.TryCreate(maxLengthRule.Type, null, out outputRule))
            .Returns(true);

        _mockRegistry
            .Setup(r => r.Create(maxLengthRule.Type, maxLengthRule.Parameters))
            .Returns(mockRule.Object);

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRegistry.Verify(
            r => r.Create(maxLengthRule.Type, maxLengthRule.Parameters),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithMultipleFields_CreatesAllFields()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto("Title", FieldTypes.Text, true, null),
                new CreateFieldDto("Body", FieldTypes.Text, true, null),
                new CreateFieldDto("PublishedDate", FieldTypes.Text, false, null),
            }
        );

        ContentType? capturedContentType = null;

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Callback<ContentType, CancellationToken>((ct, token) => capturedContentType = ct)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal(3, capturedContentType.Fields.Count);
    }

    [Fact]
    public async Task Handle_WithFieldsWithoutValidationRules_Succeeds()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true, null) }
        );

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRegistry.Verify(
            r => r.Create(It.IsAny<string>(), It.IsAny<Dictionary<string, object>?>()),
            Times.Never
        );
    }

    [Fact]
    public async Task Handle_WithExistingContentType_IncrementsVersion()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true) }
        );

        ContentType? capturedContentType = null;

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(5); // Existing version is 5

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Callback<ContentType, CancellationToken>((ct, token) => capturedContentType = ct)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal(6, capturedContentType.Version); // Should be 5 + 1
        Assert.Equal("Article", capturedContentType.Name);
    }

    [Fact]
    public async Task Handle_WithNewContentType_CreatesVersionOne()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "NewType",
            new List<CreateFieldDto> { new CreateFieldDto("Title", FieldTypes.Text, true) }
        );

        ContentType? capturedContentType = null;

        _mockRepository
            .Setup(r => r.GetLatestVersion("NewType", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0); // No existing versions

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Callback<ContentType, CancellationToken>((ct, token) => capturedContentType = ct)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal(1, capturedContentType.Version); // Should be 0 + 1
        Assert.Equal("NewType", capturedContentType.Name);
    }

    [Fact]
    public async Task Handle_WithMultipleValidationRules_CreatesAllRules()
    {
        // Arrange
        var mockRule1 = new Mock<IValidationRule>();
        var mockRule2 = new Mock<IValidationRule>();
        var maxLengthRule = new MaximumLengthValidationRule(100);
        var minLengthRule = new MinimumLengthValidationRule(5);

        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(maxLengthRule.Type, maxLengthRule.Parameters),
                        new CreateValidationRuleDto(minLengthRule.Type, minLengthRule.Parameters),
                    }
                ),
            }
        );

        IValidationRule? outputRule1 = mockRule1.Object;
        IValidationRule? outputRule2 = mockRule2.Object;

        _mockRegistry
            .Setup(r => r.TryCreate(maxLengthRule.Type, null, out outputRule1))
            .Returns(true);
        _mockRegistry
            .Setup(r => r.TryCreate(minLengthRule.Type, null, out outputRule2))
            .Returns(true);
        _mockRegistry
            .Setup(r => r.Create(maxLengthRule.Type, maxLengthRule.Parameters))
            .Returns(mockRule1.Object);
        _mockRegistry
            .Setup(r => r.Create(minLengthRule.Type, minLengthRule.Parameters))
            .Returns(mockRule2.Object);

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        _mockRegistry.Verify(
            r => r.Create(maxLengthRule.Type, maxLengthRule.Parameters),
            Times.Once
        );
        _mockRegistry.Verify(
            r => r.Create(minLengthRule.Type, minLengthRule.Parameters),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithMultipleFieldsWithValidationRules_ValidatesAllRules()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto("MaximumLengthValidationRule", null),
                    }
                ),
                new CreateFieldDto(
                    "Body",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto("MinLength", null),
                    }
                ),
            }
        );

        IValidationRule? outputRule = null;
        _mockRegistry
            .Setup(r => r.TryCreate("MaximumLengthValidationRule", null, out outputRule))
            .Returns(true);
        _mockRegistry
            .Setup(r => r.TryCreate("MinimumLengthValidationRule", null, out outputRule))
            .Returns(true);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        _mockRegistry.Verify(
            r =>
                r.TryCreate(
                    "MaximumLengthValidationRule",
                    null,
                    out It.Ref<IValidationRule?>.IsAny
                ),
            Times.Once
        );
        _mockRegistry.Verify(
            r => r.TryCreate("MinLength", null, out It.Ref<IValidationRule?>.IsAny),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_CreatesContentTypeWithCorrectProperties()
    {
        // Arrange
        var command = new CreateContentTypeCommand(
            "BlogPost",
            new List<CreateFieldDto>
            {
                new CreateFieldDto("Title", FieldTypes.Text, true),
                new CreateFieldDto("Content", FieldTypes.Text, false),
            }
        );

        ContentType? capturedContentType = null;

        _mockRepository
            .Setup(r => r.GetLatestVersion("BlogPost", It.IsAny<CancellationToken>()))
            .ReturnsAsync(2);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Callback<ContentType, CancellationToken>((ct, token) => capturedContentType = ct)
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        // Act
        var result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal("BlogPost", capturedContentType.Name);
        Assert.Equal(3, capturedContentType.Version);
        Assert.Equal(2, capturedContentType.Fields.Count);
        Assert.NotEqual(Guid.Empty, capturedContentType.Id);

        var titleField = capturedContentType.Fields.First(f => f.Name == "Title");
        Assert.True(titleField.IsRequired);
        Assert.Equal(FieldTypes.Text, titleField.Type);

        var contentField = capturedContentType.Fields.First(f => f.Name == "Content");
        Assert.False(contentField.IsRequired);
    }
}
