using Domain.Fields.Generators;
using Xunit;

namespace Domain.Tests.Fields.Generators;

public class SlugGeneratorTests
{
    [Theory]
    [InlineData("Hello World", "hello-world")]
    [InlineData("This is a Test!", "this-is-a-test")]
    [InlineData("  Leading and trailing spaces  ", "leading-and-trailing-spaces")]
    [InlineData("Multiple   spaces   between", "multiple-spaces-between")]
    [InlineData("Special@Characters#Removed$", "specialcharactersremoved")]
    [InlineData("Already-A-Slug", "already-a-slug")]
    [InlineData("---Lots-of-Dashes---", "lots-of-dashes")]
    public void GenerateValue_WithValidString_ReturnsSlug(string input, string expected)
    {
        // Arrange
        var generator = new SlugGenerator();

        // Act
        object? result = generator.GenerateValue(input);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData(null)]
    public void GenerateValue_WithNullOrEmptyString_ReturnsNull(string? input)
    {
        // Arrange
        var generator = new SlugGenerator();

        // Act
        object? result = generator.GenerateValue(input);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void GenerateValue_WithNonStringValue_ReturnsNull()
    {
        // Arrange
        var generator = new SlugGenerator();
        int nonStringVal = 12345;

        // Act
        object? result = generator.GenerateValue(nonStringVal);

        // Assert
        Assert.Null(result);
    }
}
