using System.Collections.Generic;
using Application.Helpers;
using Xunit;

namespace Application.Tests.Helpers;

public class HelperFunctions_IsValidEmail_Tests
{
    public static IEnumerable<object[]> ValidEmails()
    {
        yield return new object[] { "test@example.com" };
        yield return new object[] { "user.name+tag+sorting@example.com" };
        yield return new object[] { "user_name@example.co.uk" };
        yield return new object[] { "user-name@example.travel" };
        yield return new object[] { " mixed.case+tag@Example.COM " }; // trims + case-insensitive
    }

    public static IEnumerable<object[]> InvalidEmails()
    {
        // null / empty / whitespace
        yield return new object[] { null };
        yield return new object[] { "" };
        yield return new object[] { "   " };

        // missing parts
        yield return new object[] { "plainaddress" };
        yield return new object[] { "missing-at-sign.example.com" };
        yield return new object[] { "user@.com" };
        yield return new object[] { "@example.com" };
        yield return new object[] { "user@example" };

        // invalid characters / formats
        yield return new object[] { "user..double-dot@example.com" };
        yield return new object[] { "user@example..com" };
        yield return new object[] { "user@exa mple.com" };
        yield return new object[] { "user@example.com (comment)" };
    }

    [Theory]
    [MemberData(nameof(ValidEmails))]
    public void IsValidEmail_ReturnsTrue_ForValidAddresses(string email)
    {
        // Act
        bool result = HelperFunctions.IsValidEmail(email);

        // Assert
        Assert.True(result);
    }

    [Theory]
    [MemberData(nameof(InvalidEmails))]
    public void IsValidEmail_ReturnsFalse_ForInvalidAddresses(string email)
    {
        // Act
        bool result = HelperFunctions.IsValidEmail(email);

        // Assert
        Assert.False(result);
    }

    [Fact]
    public void IsValidEmail_TrimsInputBeforeValidation()
    {
        // Arrange
        string email = "  user@example.com  ";

        // Act
        bool result = HelperFunctions.IsValidEmail(email);

        // Assert
        Assert.True(result);
    }

    [Fact]
    public void IsValidEmail_IsCaseInsensitive()
    {
        // Arrange
        string lower = "user@example.com";
        string upper = "USER@EXAMPLE.COM";

        // Act
        bool lowerResult = HelperFunctions.IsValidEmail(lower);
        bool upperResult = HelperFunctions.IsValidEmail(upper);

        // Assert
        Assert.True(lowerResult);
        Assert.True(upperResult);
    }
}
