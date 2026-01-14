using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Domain.Fields;
using Domain.Fields.Validations;
using Infrastructure;

namespace Infrastructure.Fields;

/// <summary>
/// Registry for validation rules that uses reflection to discover and instantiate
/// validation rule implementations at runtime.
/// </summary>
public sealed class ReflectionValidationRuleRegistry : IValidationRuleRegistry
{
    private readonly ConcurrentDictionary<
        string,
        Func<Dictionary<string, object>?, IValidationRule>
    > _factories = new();

    /// <inheritdoc/>
    public void DiscoverRulesFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        assemblies = assemblies.Append(typeof(MinimumLengthValidationRule).Assembly);
        foreach (Assembly asm in assemblies)
        {
            Type[] types;
            try
            {
                types = asm.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                types = ex.Types.Where(t => t != null).ToArray()!;
            }

            foreach (Type t in types)
            {
                if (t == null || t.IsAbstract)
                {
                    continue;
                }

                if (!typeof(IValidationRule).IsAssignableFrom(t))
                {
                    continue;
                }

                // Must have parameterless constructor for discovery and instantiation.
                ConstructorInfo? ctor = t.GetConstructor(Type.EmptyTypes);
                if (ctor == null)
                {
                    continue;
                }

                // Create a sample instance to read the Type identifier.
                var sample = (IValidationRule)Activator.CreateInstance(t)!;
                string typeId = sample.Type;
                if (string.IsNullOrWhiteSpace(typeId))
                {
                    continue;
                }

                // Create factory.
                Func<Dictionary<string, object>?, IValidationRule> factory = (parameters) =>
                {
                    var inst = (IValidationRule)Activator.CreateInstance(t)!;

                    // If parameterized, copy parameters into Parameters dictionary property
                    if (inst is ParameterizedValidationRuleBase parameterized && parameters != null)
                    {
                        foreach (KeyValuePair<string, object> kv in parameters)
                        {
                            parameterized.Parameters[kv.Key] = JsonTypeConverter.Convert(kv.Value);
                        }
                    }

                    return inst;
                };

                _factories[typeId] = factory;
            }
        }
    }

    /// <summary>
    /// Creates an instance of a validation rule by its type identifier.
    /// </summary>
    /// <param name="type">The type identifier of the validation rule.</param>
    /// <param name="parameters">Optional parameters to configure the rule.</param>
    /// <returns>A configured instance of the validation rule.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the validation rule type is not registered.
    /// This indicates a validation failure earlier in the process.
    /// </exception>
    public IValidationRule Create(string type, Dictionary<string, object>? parameters)
    {
        if (
            !_factories.TryGetValue(
                type,
                out Func<Dictionary<string, object>?, IValidationRule>? factory
            )
        )
        {
            throw new InvalidOperationException(
                $"Validation rule '{type}' not registered. This should have been validated earlier."
            );
        }

        return factory(parameters);
    }

    /// <summary>
    /// Attempts to create an instance of a validation rule by its type identifier.
    /// </summary>
    /// <param name="type">The type identifier of the validation rule.</param>
    /// <param name="parameters">Optional parameters to configure the rule.</param>
    /// <param name="rule">
    /// When this method returns, contains the created validation rule if successful;
    /// otherwise, null.
    /// </param>
    /// <returns>
    /// <c>true</c> if the validation rule was successfully created; otherwise, <c>false</c>.
    /// </returns>
    public bool TryCreate(
        string type,
        Dictionary<string, object>? parameters,
        out IValidationRule? rule
    )
    {
        rule = null;
        if (
            !_factories.TryGetValue(
                type,
                out Func<Dictionary<string, object>?, IValidationRule>? factory
            )
        )
        {
            return false;
        }

        rule = factory(parameters);
        return true;
    }

    /// <inheritdoc/>
    public IReadOnlyList<string> GetAllRules() => _factories.Keys.ToList();
}
