namespace Domain.Fields.Transformers;

/// <summary>
/// Registry for discovering, registering, and creating transformation rule instances.
/// </summary>
public interface ITransformationRuleRegistry
{
    /// <summary>
    /// Discovers and registers all transformation rule types from the specified assemblies.
    /// </summary>
    /// <param name="assemblies">
    /// The assemblies to scan for types implementing <see cref="ITransformationRule"/>.
    /// Only types with a public parameterless constructor will be registered.
    /// </param>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="assemblies"/> is null.
    /// </exception>
    void DiscoverRulesFromAssemblies(IEnumerable<System.Reflection.Assembly> assemblies);

    /// <summary>
    /// Creates an instance of a transformation rule by its type identifier.
    /// </summary>
    /// <param name="type">
    /// The type identifier of the transformation rule to create.
    /// </param>
    /// <param name="parameters">
    /// Optional configuration parameters for the transformation rule as key-value pairs.
    /// Parameter names and expected types vary by transformation rule type.
    /// Use null or an empty dictionary for rules that don't require configuration.
    /// </param>
    /// <returns>
    /// A fully configured instance of the requested transformation rule,
    /// ready to transform field values.
    /// </returns>
    /// <exception cref="ArgumentException">
    /// Thrown when <paramref name="type"/> is null, empty, or whitespace.
    /// </exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transformation rule type is not registered,
    /// when instantiation fails, or when parameter configuration fails
    /// (e.g., missing required parameters, invalid parameter values).
    /// </exception>
    ITransformationRule Create(string type, Dictionary<string, object>? parameters);

    /// <summary>
    /// Attempts to create an instance of a transformation rule without throwing exceptions.
    /// </summary>
    /// <param name="type">
    /// The type identifier of the transformation rule to create.
    /// Must match a registered transformation rule type.
    /// </param>
    /// <param name="parameters">
    /// Optional configuration parameters for the transformation rule.
    /// Use null or an empty dictionary for rules without parameters.
    /// </param>
    /// <param name="rule">
    /// When this method returns, contains the created transformation rule if successful;
    /// otherwise, null. This parameter is passed uninitialized.
    /// </param>
    /// <returns>
    /// <c>true</c> if the transformation rule was successfully created and configured;
    /// <c>false</c> if the type is not registered, if the type identifier is invalid,
    /// or if any errors occurred during instantiation or parameter configuration.
    /// </returns>
    bool TryCreate(
        string type,
        Dictionary<string, object>? parameters,
        out ITransformationRule? rule
    );

    /// <summary>
    /// Retrieves the list of all registered rule types.
    /// </summary>
    /// <returns>A read-only list containing the names of all registered rule types.</returns>
    IReadOnlyList<string> GetAllRules();
}
