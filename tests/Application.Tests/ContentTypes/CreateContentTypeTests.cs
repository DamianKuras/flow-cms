using Application.ContentTypes;
using Application.Interfaces;
using Castle.Core.Logging;
using Domain.Common;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;

namespace Application.Tests.ContentTypes;

public class CreateContentTypeCommandHandlerTests
{
    private readonly Mock<IContentTypeRepository> _mockRepository;
    private readonly Mock<IValidationRuleRegistry> _mockValidationRulesRegistry;

    private readonly Mock<ITransformationRuleRegistry> _mockTransformationRulesRegistry;

    private readonly Mock<IAuthorizationService> _mockAuth;
    private readonly Mock<ILogger<CreateContentTypeCommandHandler>> _mockLogger;
    private readonly CreateContentTypeCommandHandler _handler;

    public CreateContentTypeCommandHandlerTests()
    {
        _mockRepository = new Mock<IContentTypeRepository>();
        _mockValidationRulesRegistry = new Mock<IValidationRuleRegistry>();
        _mockTransformationRulesRegistry = new Mock<ITransformationRuleRegistry>();
        _mockAuth = new Mock<IAuthorizationService>();
        _mockLogger = new Mock<ILogger<CreateContentTypeCommandHandler>>();
        _mockAuth
            .Setup(r =>
                r.IsAllowedAsync(
                    It.IsAny<Domain.Permissions.CmsAction>(),
                    It.IsAny<Domain.Permissions.Resource>(),
                    It.IsAny<CancellationToken>()
                )
            )
            .ReturnsAsync(true);
        _handler = new CreateContentTypeCommandHandler(
            _mockRepository.Object,
            _mockValidationRulesRegistry.Object,
            _mockTransformationRulesRegistry.Object,
            _mockAuth.Object,
            _mockLogger.Object
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess);
        Assert.NotEqual(Guid.Empty, response.Value);
    }

    [Fact]
    public async Task Handle_WithInvalidCommand_ReturnsValidationErrors()
    {
        // Arrange
        var command = new CreateContentTypeCommand("", []);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.MultiFieldValidationResult);
        ValidationResult? nameFieldResult = response.MultiFieldValidationResult.GetFieldResult(
            "Name"
        );
        Assert.NotNull(nameFieldResult);
        Assert.Contains(ValidationMessages.NAME_REQUIRED, nameFieldResult.ValidationErrors);
        ValidationResult? fieldsFieldResult = response.MultiFieldValidationResult.GetFieldResult(
            "Fields"
        );
        Assert.NotNull(fieldsFieldResult);
        Assert.Contains(ValidationMessages.FIELDS_REQUIRED, fieldsFieldResult.ValidationErrors);
    }

    [Fact]
    public async Task Handle_WithUnknownValidationRule_ReturnsFailure()
    {
        // Arrange
        string unknownRuleType = "NonExistentRule";
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

        _mockValidationRulesRegistry
            .Setup(r => r.TryCreate("UnknownRule", null, out It.Ref<IValidationRule?>.IsAny))
            .Returns(false);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.NotNull(response.MultiFieldValidationResult);
        Assert.Contains(
            ValidationMessages.UnknownValidationRule(unknownRuleType, "Title"),
            response.MultiFieldValidationResult.GetAllErrors()
        );
    }

    [Fact]
    public async Task Handle_WithUnknownTransformationRule_ReturnsValidationFailure()
    {
        // Arrange
        const string unknownTransformationType = "NonExistentTransformer";

        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    ValidationRules: null,
                    TransformationRules: new List<CreateTransformationRuleDto>
                    {
                        new CreateTransformationRuleDto(unknownTransformationType, null),
                    }
                ),
            }
        );

        _mockTransformationRulesRegistry
            .Setup(r =>
                r.TryCreate(unknownTransformationType, null, out It.Ref<ITransformationRule?>.IsAny)
            )
            .Returns(false);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.MultiFieldValidationResult);

        Assert.Contains(
            ValidationMessages.UnknownTransformationRule(unknownTransformationType, "Title"),
            result.MultiFieldValidationResult.GetAllErrors()
        );
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
                        new CreateValidationRuleDto(
                            MaximumLengthValidationRule.TYPE_NAME,
                            maxLengthRule.Parameters
                        ),
                    }
                ),
            }
        );

        IValidationRule? outputRule = mockRule.Object;
        _mockValidationRulesRegistry
            .Setup(r =>
                r.TryCreate(
                    MaximumLengthValidationRule.TYPE_NAME,
                    maxLengthRule.Parameters,
                    out outputRule
                )
            )
            .Returns(true);

        _mockValidationRulesRegistry
            .Setup(r => r.Create(MaximumLengthValidationRule.TYPE_NAME, maxLengthRule.Parameters))
            .Returns(mockRule.Object);

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        _mockValidationRulesRegistry.Verify(
            r => r.Create(MaximumLengthValidationRule.TYPE_NAME, maxLengthRule.Parameters),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithValidTransformationRule_Succeeds()
    {
        // Arrange
        var mockTransformationRule = new Mock<ITransformationRule>();
        var truncateRule = new TruncateByLengthTransformationRule(10);

        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    ValidationRules: null,
                    TransformationRules: new List<CreateTransformationRuleDto>
                    {
                        new CreateTransformationRuleDto(
                            TruncateByLengthTransformationRule.TYPE_NAME,
                            truncateRule.Parameters
                        ),
                    }
                ),
            }
        );

        ITransformationRule? outRule = mockTransformationRule.Object;

        _mockTransformationRulesRegistry
            .Setup(r =>
                r.TryCreate(
                    TruncateByLengthTransformationRule.TYPE_NAME,
                    truncateRule.Parameters,
                    out outRule
                )
            )
            .Returns(true);

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockTransformationRulesRegistry.Verify(
            r =>
                r.TryCreate(
                    TruncateByLengthTransformationRule.TYPE_NAME,
                    truncateRule.Parameters,
                    out It.Ref<ITransformationRule?>.IsAny
                ),
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal(3, capturedContentType.Fields.Count);
    }

    [Fact]
    public async Task Handle_WithMultipleTransformationRules_ValidatesAll()
    {
        // Arrange
        var parameters1 = new Dictionary<string, object> { { "value", "lower" } };
        var parameters2 = new Dictionary<string, object> { { "max", 50 } };

        var command = new CreateContentTypeCommand(
            "Article",
            new List<CreateFieldDto>
            {
                new CreateFieldDto(
                    "Title",
                    FieldTypes.Text,
                    true,
                    ValidationRules: null,
                    TransformationRules: new List<CreateTransformationRuleDto>
                    {
                        new CreateTransformationRuleDto("Lowercase", parameters1),
                        new CreateTransformationRuleDto("Truncate", parameters2),
                    }
                ),
            }
        );

        ITransformationRule? outRule = null;

        _mockTransformationRulesRegistry
            .Setup(r => r.TryCreate("Lowercase", parameters1, out outRule))
            .Returns(true);

        _mockTransformationRulesRegistry
            .Setup(r => r.TryCreate("Truncate", parameters2, out outRule))
            .Returns(true);

        _mockRepository
            .Setup(r => r.GetLatestVersion("Article", It.IsAny<CancellationToken>()))
            .ReturnsAsync(0);

        _mockRepository
            .Setup(r => r.AddAsync(It.IsAny<ContentType>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        _mockRepository
            .Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> result = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(result.IsSuccess);

        _mockTransformationRulesRegistry.Verify(
            r => r.TryCreate("Lowercase", parameters1, out It.Ref<ITransformationRule?>.IsAny),
            Times.Once
        );
        _mockTransformationRulesRegistry.Verify(
            r => r.TryCreate("Truncate", parameters2, out It.Ref<ITransformationRule?>.IsAny),
            Times.Once
        );
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        _mockValidationRulesRegistry.Verify(
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
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
                        new CreateValidationRuleDto(
                            MaximumLengthValidationRule.TYPE_NAME,
                            maxLengthRule.Parameters
                        ),
                        new CreateValidationRuleDto(minLengthRule.Type, minLengthRule.Parameters),
                    }
                ),
            }
        );

        IValidationRule? outputRule1 = mockRule1.Object;
        IValidationRule? outputRule2 = mockRule2.Object;

        _mockValidationRulesRegistry
            .Setup(r =>
                r.TryCreate(
                    MaximumLengthValidationRule.TYPE_NAME,
                    maxLengthRule.Parameters,
                    out outputRule1
                )
            )
            .Returns(true);
        _mockValidationRulesRegistry
            .Setup(r => r.TryCreate(minLengthRule.Type, minLengthRule.Parameters, out outputRule2))
            .Returns(true);
        _mockValidationRulesRegistry
            .Setup(r => r.Create(MaximumLengthValidationRule.TYPE_NAME, maxLengthRule.Parameters))
            .Returns(mockRule1.Object);
        _mockValidationRulesRegistry
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        _mockValidationRulesRegistry.Verify(
            r => r.Create(MaximumLengthValidationRule.TYPE_NAME, maxLengthRule.Parameters),
            Times.Once
        );
        _mockValidationRulesRegistry.Verify(
            r => r.Create(minLengthRule.Type, minLengthRule.Parameters),
            Times.Once
        );
    }

    [Fact]
    public async Task Handle_WithMultipleFieldsWithValidationRules_ValidatesAllRules()
    {
        // Arrange
        var maxLengthRule = new MaximumLengthValidationRule(5);
        var minLengthRule = new MinimumLengthValidationRule(10);
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
                        new CreateValidationRuleDto(
                            MaximumLengthValidationRule.TYPE_NAME,
                            maxLengthRule.Parameters
                        ),
                    }
                ),
                new CreateFieldDto(
                    "Body",
                    FieldTypes.Text,
                    true,
                    new List<CreateValidationRuleDto>
                    {
                        new CreateValidationRuleDto(minLengthRule.Type, minLengthRule.Parameters),
                    }
                ),
            }
        );

        IValidationRule? outputRule = null;
        _mockValidationRulesRegistry
            .Setup(r =>
                r.TryCreate(
                    MaximumLengthValidationRule.TYPE_NAME,
                    maxLengthRule.Parameters,
                    out outputRule
                )
            )
            .Returns(true);
        _mockValidationRulesRegistry
            .Setup(r => r.TryCreate(minLengthRule.Type, minLengthRule.Parameters, out outputRule))
            .Returns(true);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.True(response.IsSuccess);
        _mockValidationRulesRegistry.Verify(
            r =>
                r.TryCreate(
                    MaximumLengthValidationRule.TYPE_NAME,
                    maxLengthRule.Parameters,
                    out It.Ref<IValidationRule?>.IsAny
                ),
            Times.Once
        );
        _mockValidationRulesRegistry.Verify(
            r =>
                r.TryCreate(
                    minLengthRule.Type,
                    minLengthRule.Parameters,
                    out It.Ref<IValidationRule?>.IsAny
                ),
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
            .Returns(Task.CompletedTask);

        // Act
        Result<Guid> response = await _handler.Handle(command, CancellationToken.None);

        // Assert
        Assert.NotNull(response);
        Assert.True(response.IsSuccess);
        Assert.NotNull(capturedContentType);
        Assert.Equal("BlogPost", capturedContentType.Name);
        Assert.Equal(3, capturedContentType.Version);
        Assert.Equal(2, capturedContentType.Fields.Count);
        Assert.NotEqual(Guid.Empty, capturedContentType.Id);

        Field titleField = capturedContentType.Fields.First(f => f.Name == "Title");
        Assert.True(titleField.IsRequired);
        Assert.Equal(FieldTypes.Text, titleField.Type);

        Field contentField = capturedContentType.Fields.First(f => f.Name == "Content");
        Assert.False(contentField.IsRequired);
    }
}
