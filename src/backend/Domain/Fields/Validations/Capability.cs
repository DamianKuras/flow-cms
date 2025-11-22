namespace Domain.Fields.Validations;

/// <summary>
/// Represents a capability constraint for validation rules.
/// </summary>
/// <remarks>
/// Capabilities define the data types that a validation rule is designed to validate against.
/// This prevents incompatible rules from being applied to incompatible field types.
/// </remarks>
/// <param name="Name">The name of the capability constraint.</param>
public record Capability(string Name)
{
    /// <summary>
    /// Standard capability constraints for common field types.
    /// </summary>
    public static class Standard
    {
        public const string TEXT = "Text";
        public const string NUMERIC = "Numeric";
        public const string DATE = "Date";
    }
}
