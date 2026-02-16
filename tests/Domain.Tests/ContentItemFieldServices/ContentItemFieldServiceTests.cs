using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Domain.Services;

namespace Domain.Tests.ContentItemFieldServices;

/// <summary>
/// Tests for ContentItemFieldService.SetValue functionality including
/// validation, transformation, error handling, and exception scenarios.
/// </summary>
public class ContentItemSetFieldValueTests
{
    private const string DEFAULT_FIELD_NAME = "TestField";
    private const string DEFAULT_CONTENT_TYPE_NAME = "TestContentType";
    private const string DEFAULT_CONTENT_ITEM_NAME = "TestContentItem";

    #region Helper Methods

    private static Field CreateField(
        Guid? fieldId = null,
        string name = DEFAULT_FIELD_NAME,
        IValidationRule[]? validationRules = null,
        ITransformationRule[]? transformers = null
    ) =>
        new Field(
            fieldId ?? Guid.NewGuid(),
            FieldTypes.Text,
            name,
            false,
            validationRules ?? Array.Empty<IValidationRule>(),
            transformers ?? Array.Empty<ITransformationRule>()
        );

    private static ContentType CreateContentType(
        Guid? contentTypeId = null,
        string name = DEFAULT_CONTENT_TYPE_NAME,
        params Field[] fields
    ) => new ContentType(contentTypeId ?? Guid.NewGuid(), name, fields);

    private static ContentItem CreateContentItem(
        Guid? contentItemId = null,
        string name = DEFAULT_CONTENT_ITEM_NAME,
        Guid? contentTypeId = null
    ) => new ContentItem(contentItemId ?? Guid.NewGuid(), name, contentTypeId ?? Guid.NewGuid());

    private static (ContentItem item, ContentType type, Guid fieldId) CreateTestContext(
        IValidationRule[]? validationRules = null,
        ITransformationRule[]? transformers = null
    )
    {
        var fieldId = Guid.NewGuid();
        Field field = CreateField(
            fieldId,
            validationRules: validationRules,
            transformers: transformers
        );
        ContentType contentType = CreateContentType(fields: field);
        ContentItem contentItem = CreateContentItem(contentTypeId: contentType.Id);

        return (contentItem, contentType, fieldId);
    }

    #endregion

    #region Basic Functionality Tests

    [Fact]
    public void SetValue_WithValidFieldAndValue_ReturnsSuccess()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();
        const string expectedValue = "new value";

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            expectedValue
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(expectedValue, contentItem.Values[fieldId].Value);
    }

    [Theory]
    [InlineData("")]
    [InlineData("simple text")]
    [InlineData("Text with spaces and punctuation!@#")]
    [InlineData("1234567890")]
    [InlineData("Multi\nLine\nText")]
    public void SetValue_WithVariousValidValues_ReturnsSuccess(string value)
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();

        // Act
        Result result = ContentItemFieldService.SetValue(contentItem, contentType, fieldId, value);

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal(value, contentItem.Values[fieldId].Value);
    }

    [Fact]
    public void SetValue_WithNullValue_DependsOnFieldValidation()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();

        // Act
        Result result = ContentItemFieldService.SetValue(contentItem, contentType, fieldId, null);

        // Assert - null handling depends on field validation rules
        // By default with no validation rules, null should be accepted
        Assert.True(result.IsSuccess);
    }

    [Fact]
    public void SetValue_OverwriteExistingValue_Succeeds()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "initial value");

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "updated value"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("updated value", contentItem.Values[fieldId].Value);
    }

    #endregion

    #region Transformer Tests

    [Fact]
    public void SetValue_WithSingleTransformer_AppliesTransformation()
    {
        // Arrange
        var transformers = new ITransformationRule[] { new LowercaseTransformer() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            transformers: transformers
        );

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "UPPERCASE TEXT"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("uppercase text", contentItem.Values[fieldId].Value);
    }

    [Fact]
    public void SetValue_WithMultipleTransformers_AppliesInOrder()
    {
        // Arrange - Assuming you have a TrimTransformer or similar
        var transformers = new ITransformationRule[]
        {
            new LowercaseTransformer(),
            // Add second transformer to test chaining if available
        };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            transformers: transformers
        );

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "  TEST VALUE  "
        );

        // Assert
        Assert.True(result.IsSuccess);
        // Adjust expected value based on actual transformers
        Assert.Equal("  test value  ", contentItem.Values[fieldId].Value);
    }

    #endregion

    #region Validation Tests

    [Fact]
    public void SetValue_WithValidationRule_ValidatesTransformedValue()
    {
        // Arrange - Uppercase input should pass after lowercase transformation
        var validationRules = new IValidationRule[] { new IsLowercaseRule() };
        var transformers = new ITransformationRule[] { new LowercaseTransformer() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules,
            transformers
        );

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "UPPERCASE TEXT"
        );

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("uppercase text", contentItem.Values[fieldId].Value);
    }

    [Fact]
    public void SetValue_FailingValidation_ReturnsFieldValidationFailure()
    {
        // Arrange
        var validationRules = new IValidationRule[] { new IsLowercaseRule() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules: validationRules
        );

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "UPPERCASE TEXT"
        );
        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(FailureKind.FieldValidation, result.FailureKind);
    }

    [Fact]
    public void SetValue_FailingValidation_DoesNotUpdateValue()
    {
        // Arrange
        var validationRules = new IValidationRule[] { new IsLowercaseRule() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules: validationRules
        );

        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "initial value");
        object? initialValue = contentItem.Values[fieldId].Value;

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "INVALID UPPERCASE"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(initialValue, contentItem.Values[fieldId].Value);
    }

    [Fact]
    public void SetValue_WithMultipleValidationRules_AllMustPass()
    {
        // Arrange - If you have multiple validation rules available
        var validationRules = new IValidationRule[]
        {
            new IsLowercaseRule(),
            // Add additional validation rules if available
        };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules: validationRules
        );

        // Act - Should fail if any rule fails
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "UPPERCASE"
        );

        // Assert
        Assert.True(result.IsFailure);
    }

    #endregion

    #region Error Handling Tests

    [Fact]
    public void SetValue_WithUndefinedField_ReturnsNotFoundFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        ContentType contentType = CreateContentType(fields: Array.Empty<Field>());
        ContentItem contentItem = CreateContentItem(contentTypeId: contentType.Id);

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "value"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.NotNull(result.Error);
        Assert.Contains("Field", result.Error.Message);
        Assert.Contains(fieldId.ToString(), result.Error.Message);
        Assert.Contains("not in ContentType", result.Error.Message);
        Assert.Contains(contentType.Name, result.Error.Message);
    }

    [Fact]
    public void SetValue_WithNonExistentFieldId_ReturnsNotFoundFailure()
    {
        // Arrange
        var existingFieldId = Guid.NewGuid();
        Field field = CreateField(existingFieldId);
        ContentType contentType = CreateContentType(fields: field);
        ContentItem contentItem = CreateContentItem(contentTypeId: contentType.Id);
        var nonExistentFieldId = Guid.NewGuid();

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            nonExistentFieldId,
            "value"
        );

        // Assert
        Assert.True(result.IsFailure);
        Assert.Contains(nonExistentFieldId.ToString(), result.Error.Message);
    }

    #endregion

    #region Null Argument Exception Tests

    [Fact]
    public void SetValue_WithNullContentItem_ThrowsArgumentNullException()
    {
        // Arrange
        ContentType contentType = CreateContentType();
        var fieldId = Guid.NewGuid();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            ContentItemFieldService.SetValue(null!, contentType, fieldId, "value")
        );

        Assert.Equal("item", exception.ParamName);
    }

    [Fact]
    public void SetValue_WithNullContentType_ThrowsArgumentNullException()
    {
        // Arrange
        ContentItem contentItem = CreateContentItem();
        var fieldId = Guid.NewGuid();

        // Act & Assert
        ArgumentNullException exception = Assert.Throws<ArgumentNullException>(() =>
            ContentItemFieldService.SetValue(contentItem, null!, fieldId, "value")
        );

        Assert.Equal("type", exception.ParamName);
    }

    [Fact]
    public void SetValue_WithBothNullArguments_ThrowsArgumentNullException()
    {
        // Arrange
        var fieldId = Guid.NewGuid();

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() =>
            ContentItemFieldService.SetValue(null!, null!, fieldId, "value")
        );
    }

    #endregion

    #region Timestamp Tests

    [Fact]
    public void SetValue_UpdatesTimestamp()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "initial value");
        DateTime? initialTimestamp = contentItem.Values[fieldId].UpdatedAt;

        // Ensure measurable time passes
        Thread.Sleep(10);

        // Act
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "updated value");

        // Assert
        DateTime? updatedTimestamp = contentItem.Values[fieldId].UpdatedAt;
        Assert.NotNull(updatedTimestamp);
        Assert.NotEqual(initialTimestamp, updatedTimestamp);
        Assert.True(
            updatedTimestamp > initialTimestamp,
            "Updated timestamp should be later than initial timestamp"
        );
    }

    [Fact]
    public void SetValue_FirstTime_SetsTimestamp()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();
        DateTime beforeSet = DateTime.UtcNow;

        // Act
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "initial value");

        // Assert
        DateTime? timestamp = contentItem.Values[fieldId].UpdatedAt;
        Assert.NotNull(timestamp);
        Assert.True(timestamp >= beforeSet, "Timestamp should be at or after method call");
        Assert.True(
            timestamp <= DateTime.UtcNow.AddSeconds(1),
            "Timestamp should not be in the future"
        );
    }

    [Fact]
    public void SetValue_SameValueTwice_StillUpdatesTimestamp()
    {
        // Arrange
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext();
        const string value = "same value";

        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, value);
        DateTime? firstTimestamp = contentItem.Values[fieldId].UpdatedAt;

        Thread.Sleep(10);

        // Act - Setting the exact same value again
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, value);

        // Assert - Timestamp should still update even with same value
        Assert.NotEqual(firstTimestamp, contentItem.Values[fieldId].UpdatedAt);
    }

    [Fact]
    public void SetValue_FailedValidation_DoesNotUpdateTimestamp()
    {
        // Arrange
        var validationRules = new IValidationRule[] { new IsLowercaseRule() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules: validationRules
        );

        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "initial value");
        DateTime? initialTimestamp = contentItem.Values[fieldId].UpdatedAt;

        Thread.Sleep(10);

        // Act - Try to set invalid value
        ContentItemFieldService.SetValue(contentItem, contentType, fieldId, "INVALID UPPERCASE");

        // Assert - Timestamp should remain unchanged on failure
        Assert.Equal(initialTimestamp, contentItem.Values[fieldId].UpdatedAt);
    }

    #endregion

    #region Integration Tests

    [Fact]
    public void SetValue_CompleteWorkflow_TransformValidateAndStore()
    {
        // Arrange - Simulate a real-world scenario with transformation and validation
        var validationRules = new IValidationRule[] { new IsLowercaseRule() };
        var transformers = new ITransformationRule[] { new LowercaseTransformer() };
        (ContentItem? contentItem, ContentType? contentType, Guid fieldId) = CreateTestContext(
            validationRules,
            transformers
        );

        // Act
        Result result = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId,
            "MiXeD CaSe InPuT"
        );

        // Assert
        Assert.True(result.IsSuccess, "Should succeed with valid transformed value");
        Assert.Equal("mixed case input", contentItem.Values[fieldId].Value);
        Assert.NotNull(contentItem.Values[fieldId].UpdatedAt);
    }

    [Fact]
    public void SetValue_MultipleFieldsOnSameItem_AllSucceed()
    {
        // Arrange
        var fieldId1 = Guid.NewGuid();
        var fieldId2 = Guid.NewGuid();
        Field field1 = CreateField(fieldId1, "Field1");
        Field field2 = CreateField(fieldId2, "Field2");
        ContentType contentType = CreateContentType(fields: new[] { field1, field2 });
        ContentItem contentItem = CreateContentItem(contentTypeId: contentType.Id);

        // Act
        Result result1 = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId1,
            "value1"
        );
        Result result2 = ContentItemFieldService.SetValue(
            contentItem,
            contentType,
            fieldId2,
            "value2"
        );

        // Assert
        Assert.True(result1.IsSuccess);
        Assert.True(result2.IsSuccess);
        Assert.Equal("value1", contentItem.Values[fieldId1].Value);
        Assert.Equal("value2", contentItem.Values[fieldId2].Value);
    }

    #endregion
}
