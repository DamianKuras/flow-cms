namespace Domain.Fields.Validations;

/// <summary>
/// Registry for discovering, registering, and creating validation rule instances at runtime.
/// </summary>
public interface IValidationRuleRegistry
{
    /// <summary>
    /// Discovers and registers all validation rule types from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to scan for types implementing <see cref="IValidationRule"/>.
    /// Types must have a parameterless constructor to be discoverable.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assemblies"/> is null.
    /// </exception>
    void DiscoverRulesFromAssemblies(IEnumerable<System.Reflection.Assembly> assemblies);

    /// <summary>
    /// Creates an instance of a validation rule by its type identifier.
    /// </summary>
    /// <param name="type">
    /// The type identifier of the validation rule to create.
    /// Must match a registered validation rule type.
    /// </param>
    /// <param name="parameters">
    /// Optional dictionary of parameter names to values that configure the validation rule.
    /// Pass null or an empty dictionary for validation rules that don't require parameters.
    /// Parameter values should match the types expected by the validation rule.
    /// </param>
    /// <returns>A configured instance of the requested validation rule.</returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="type"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the validation rule type is not registered or when instantiation fails.
    /// </exception>
    IValidationRule Create(string type, Dictionary<string, object>? parameters);

    /// <summary>
    /// Attempts to create an instance of a validation rule by its type identifier.
    /// </summary>
    /// <param name="type">
    /// The type identifier of the validation rule to create.
    /// Must match a registered validation rule type.
    /// </param>
    /// <param name="parameters">
    /// Optional dictionary of parameter names to values that configure the validation rule.
    /// Pass null or an empty dictionary for validation rules that don't require parameters.
    /// Parameter values should match the types expected by the validation rule.
    /// </param>
    /// <param name="rule">
    /// When this method returns, contains the created validation rule if successful;
    /// otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the validation rule was successfully created; otherwise, <c>false</c>.
    /// Returns <c>false</c> if the type is not registered or if instantiation fails.
    /// </returns>
    bool TryCreate(string type, Dictionary<string, object>? parameters, out IValidationRule? rule);

    /// <summary>
    /// Retrieves the list of all registered rule types.
    /// </summary>
    /// <returns>A read-only list containing the names of all registered rule types.</returns>
    IReadOnlyList<string> GetAllRules();
}
