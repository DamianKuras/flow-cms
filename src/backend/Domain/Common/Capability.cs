namespace Domain.Common;

/// <summary>
/// Represents a capability constraint for validation and transformation rules.
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
        /// <summary>
        ///
        /// </summary>
        public const string TEXT = "Text";

        /// <summary>
        ///
        /// </summary>
        public const string NUMERIC = "Numeric";

        /// <summary>
        ///
        /// </summary>
        public const string DATE = "Date";
    }
}
