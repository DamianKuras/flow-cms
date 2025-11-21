using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Moq;
using Xunit;

namespace Domain.Tests.ContentItems;

/// <summary>
/// Test suite for ContentItem.SetFieldValue method.
/// </summary>
public class ContentItemSetFieldValueTests
{
    [Fact]
    public void SetFieldValue_WithValidFieldAndValue_ReturnsSuccess()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(fieldId, FieldTypes.TEXT, "TestField", false, [], []);
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", [testField]);
        var initialValues = new Dictionary<Guid, ContentFieldValue>
        {
            { fieldId, new ContentFieldValue("value") },
        };
        var contentItem = new ContentItem(
            Guid.NewGuid(),
            "TestContentItem",
            contentType,
            initialValues
        );
        // Act
        var result = contentItem.SetFieldValue(testField, "new value");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new value", contentItem.GetFieldValue(testField.Id).Value);
    }

    [Fact]
    public void SetFieldValue_AppliesTransformers()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.TEXT,
            "TestField",
            false,
            [],
            [new LowercaseTransformer()]
        );
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", [testField]);
        var initialValues = new Dictionary<Guid, ContentFieldValue>
        {
            { fieldId, new ContentFieldValue("value") },
        };
        var contentItem = new ContentItem(
            Guid.NewGuid(),
            "TestContentItem",
            contentType,
            initialValues
        );
        // Act
        var result = contentItem.SetFieldValue(testField, "NEW VALUE");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new value", contentItem.GetFieldValue(testField.Id).Value);
    }

    [Fact]
    public void SetFieldValue_ValidatesTransformedValue_NotRawValue()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.TEXT,
            "TestField",
            false,
            [new IsLowercaseRule()],
            [new LowercaseTransformer()]
        );
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", [testField]);
        var initialValues = new Dictionary<Guid, ContentFieldValue>
        {
            { fieldId, new ContentFieldValue("value") },
        };
        var contentItem = new ContentItem(
            Guid.NewGuid(),
            "TestContentItem",
            contentType,
            initialValues
        );
        // Act
        var result = contentItem.SetFieldValue(testField, "NEW VALUE");

        // Assert
        Assert.True(result.IsSuccess);
        Assert.Equal("new value", contentItem.GetFieldValue(testField.Id).Value);
    }

    [Fact]
    public void SetFieldValue_BreakingValidationRule_ReturnsFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.TEXT,
            "TestField",
            false,
            [new IsLowercaseRule()],
            []
        );
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", [testField]);
        var initialValues = new Dictionary<Guid, ContentFieldValue>
        {
            { fieldId, new ContentFieldValue("value") },
        };
        var contentItem = new ContentItem(
            Guid.NewGuid(),
            "TestContentItem",
            contentType,
            initialValues
        );
        var originalValue = contentItem.GetFieldValue(testField.Id).Value;
        // Act
        var result = contentItem.SetFieldValue(testField, "NEW VALUE");

        // Assert
        Assert.True(result.IsFailure);
        Assert.Equal(originalValue, contentItem.GetFieldValue(testField.Id).Value);
    }

    [Fact]
    public void SetFieldValue_WithUndefinedField_ReturnsFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.TEXT,
            "TestField",
            false,
            [new IsLowercaseRule()],
            []
        );
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", []);

        var contentItem = new ContentItem(Guid.NewGuid(), "TestContentItem", contentType, []);
        // Act
        var result = contentItem.SetFieldValue(testField, "NEW VALUE");

        // Assert
        Assert.True(result.IsFailure);
    }

    [Fact]
    public void SetFieldValue_UpdatesTimestamp()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(fieldId, FieldTypes.TEXT, "TestField", false, [], []);
        var contentType = new ContentType(Guid.NewGuid(), "TestContentType", [testField]);

        var initialValues = new Dictionary<Guid, ContentFieldValue>
        {
            { fieldId, new ContentFieldValue("value") },
        };
        var contentItem = new ContentItem(
            Guid.NewGuid(),
            "TestContentItem",
            contentType,
            initialValues
        );
        var timestamp = contentItem.Values[testField.Id].UpdatedAt;

        // Act
        contentItem.SetFieldValue(testField, "new value_2");

        // Assert
        Assert.NotEqual(timestamp, contentItem.Values[testField.Id].UpdatedAt);
    }
}
