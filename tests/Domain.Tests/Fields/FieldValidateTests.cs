using Domain.Common;
using Domain.ContentItems;
using Domain.ContentTypes;
using Domain.Fields;
using Domain.Fields.Transformers;
using Domain.Fields.Validations;
using Xunit;

namespace Domain.Tests.Fields;

/// <summary>
/// Test suite for Field.Validate method.
/// </summary>
public class FieldValidateTests
{
    [Fact]
    public void Validate_WithValidFieldValue_ReturnsSuccess()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.Text,
            "TestField",
            false,
            [new IsLowercaseRule()],
            []
        );

        // Act
        var validationResult = testField.Validate("lowercase text");

        // Assert
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void Validate_WithInvalidFieldValue_ReturnsFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.Text,
            "TestField",
            false,
            [new IsLowercaseRule()],
            []
        );

        // Act
        var validationResult = testField.Validate("UPPERCASE TEXT");

        // Assert
        Assert.False(validationResult.IsValid);
    }

    [Fact]
    public void Validate_PassingNullToNotRequiredField_ReturnsSuccess()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.Text,
            "TestField",
            isRequired: false,
            [new IsLowercaseRule()],
            []
        );

        // Act
        var validationResult = testField.Validate(null);

        // Assert
        Assert.True(validationResult.IsValid);
    }

    [Fact]
    public void Validate_PassingNullToRequiredField_ReturnsFailure()
    {
        // Arrange
        var fieldId = Guid.NewGuid();
        var testField = new Field(
            fieldId,
            FieldTypes.Text,
            "TestField",
            isRequired: true,
            [new IsLowercaseRule()],
            []
        );

        // Act
        var validationResult = testField.Validate(null);

        // Assert
        Assert.False(validationResult.IsValid);
    }
}
