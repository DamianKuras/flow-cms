using Domain.Common;
using Domain.Fields.Transformers;
using Xunit;

namespace Domain.Tests.Fields.Transformers;

public class TextFieldTransformersTests
{
    [Theory]
    [InlineData("hello world", "Hello World")]
    [InlineData("HELLO WORLD", "Hello World")]
    [InlineData("hello WORLD", "Hello World")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void CapitalizeTransformer_ShouldCapitalizeFirstLetterOfEachWord(string? input, string? expected)
    {
        // Arrange
        var transformer = new CapitalizeTransformer();

        // Act
        object? result = transformer.ApplyTransformation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello World", "hello world")]
    [InlineData("HELLO WORLD", "hello world")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void LowercaseTransformer_ShouldConvertToLowercase(string? input, string? expected)
    {
        // Arrange
        var transformer = new LowercaseTransformer();

        // Act
        object? result = transformer.ApplyTransformation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello   World", "Hello World")]
    [InlineData("  Hello World  ", "Hello World")]
    [InlineData("Hello\t\tWorld\n", "Hello World")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void NormalizeWhitespaceTransformer_ShouldNormalizeSpacesAndTrim(string? input, string? expected)
    {
        // Arrange
        var transformer = new NormalizeWhitespaceTransformer();

        // Act
        object? result = transformer.ApplyTransformation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello! World@123", "Hello World123")]
    [InlineData("Value-$12.99", "Value1299")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void RemoveSpecialCharsTransformer_ShouldKeepOnlyAlphanumericAndSpaces(string? input, string? expected)
    {
        // Arrange
        var transformer = new RemoveSpecialCharsTransformer();

        // Act
        object? result = transformer.ApplyTransformation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("hello world", "HELLO WORLD")]
    [InlineData("Hello World", "HELLO WORLD")]
    [InlineData("", "")]
    [InlineData(null, null)]
    public void UppercaseTransformer_ShouldConvertToUppercase(string? input, string? expected)
    {
        // Arrange
        var transformer = new UppercaseTransformer();

        // Act
        object? result = transformer.ApplyTransformation(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Fact]
    public void ALLTransformers_ShouldHaveStandardTextCapability()
    {
        // Assert
        Assert.Equal(new Capability(Capability.Standard.TEXT), new CapitalizeTransformer().RequiredCapability);
        Assert.Equal(new Capability(Capability.Standard.TEXT), new LowercaseTransformer().RequiredCapability);
        Assert.Equal(new Capability(Capability.Standard.TEXT), new NormalizeWhitespaceTransformer().RequiredCapability);
        Assert.Equal(new Capability(Capability.Standard.TEXT), new RemoveSpecialCharsTransformer().RequiredCapability);
        Assert.Equal(new Capability(Capability.Standard.TEXT), new UppercaseTransformer().RequiredCapability);
    }

    [Fact]
    public void ALLTransformers_ShouldReturnPassedNonStringValuesAsIs()
    {
        // Arrange
        var obj = new { Prop = 1 };
        
        // Assert
        Assert.Equal(obj, new CapitalizeTransformer().ApplyTransformation(obj));
        Assert.Equal(obj, new LowercaseTransformer().ApplyTransformation(obj));
        Assert.Equal(obj, new NormalizeWhitespaceTransformer().ApplyTransformation(obj));
        Assert.Equal(obj, new RemoveSpecialCharsTransformer().ApplyTransformation(obj));
        Assert.Equal(obj, new UppercaseTransformer().ApplyTransformation(obj));
    }
}
