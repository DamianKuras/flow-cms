using System.Net.Mail;
using System.Text.RegularExpressions;

namespace Application.Helpers;

/// <summary>
/// Provides helper functions for common application-level operations.
/// </summary>
public static class HelperFunctions
{
    /// <summary>
    /// The compiled email validation regex for validating email.
    /// Source of the regex: https://emailregex.com/
    /// </summary>
    public static readonly Regex EmailValidationRegexCompiled = new Regex(
        @"^(?("")(""[^""]+(?<!\\)""@)|(([0-9a-z]((\.(?!\.))|[-!#\$%&'\*\+/=\?\^`\{\}\|~\w])*)(?<=[0-9a-z])@))(?(\[)(\[(\d{1,3}\.){3}\d{1,3}\])|(([0-9a-z][-\w]*[0-9a-z]*\.)+[a-z0-9][\-a-z0-9]{0,22}[a-z0-9]))$",
        RegexOptions.IgnoreCase
    );

    /// <summary>
    /// Validates whether the provided email address has a syntactically valid format.
    /// </summary>
    /// <param name="email">The email address to validate.</param>
    /// <param name="requireDotInDomainName"></param>
    /// <returns>
    /// <c>true</c> if the email format is valid; otherwise, <c>false</c>.
    /// </returns>
    public static bool IsValidEmail(string email, bool requireDotInDomainName = true)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            return false;
        }
        email = email.Trim(); // Remove leading/trailing whitespace.
        try
        {
            var mailAddress = new MailAddress(email);
            if (mailAddress.Address != email.Trim())
            {
                return false;
            }
        }
        catch (FormatException)
        {
            return false;
        }
        catch (ArgumentException)
        {
            return false;
        }

        bool isValid = EmailValidationRegexCompiled.IsMatch(email);

        if (isValid && requireDotInDomainName)
        {
            string[] arr = email.Split('@', StringSplitOptions.RemoveEmptyEntries);
            isValid = arr.Length == 2 && arr[1].Contains(".");
        }
        return isValid;
    }
}
