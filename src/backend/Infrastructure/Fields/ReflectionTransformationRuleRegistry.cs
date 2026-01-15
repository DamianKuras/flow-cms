using System.Collections.Concurrent;
using System.Reflection;
using System.Text.Json;
using Domain.Fields;
using Domain.Fields.Transformers;
using Infrastructure;

/// <summary>
/// Registry for transformation rules that uses reflection to discover and instantiate
/// transformation rule implementations at runtime.
/// </summary>
public sealed class ReflectionTransformationRuleRegistry : ITransformationRuleRegistry
{
    private readonly ConcurrentDictionary<
        string,
        Func<Dictionary<string, object>?, ITransformationRule>
    > _factories = new();

    /// <inheritdoc/>
    public void DiscoverRulesFromAssemblies(IEnumerable<Assembly> assemblies)
    {
        assemblies = assemblies.Append(typeof(UppercaseTransformer).Assembly);
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

                if (!typeof(ITransformationRule).IsAssignableFrom(t))
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
                var sample = (ITransformationRule)Activator.CreateInstance(t)!;
                string typeId = sample.Type;
                if (string.IsNullOrWhiteSpace(typeId))
                {
                    continue;
                }

                // Create factory
                Func<Dictionary<string, object>?, ITransformationRule> factory = (parameters) =>
                {
                    var inst = (ITransformationRule)Activator.CreateInstance(t)!;

                    // If parameterized, copy parameters into Parameters dictionary property
                    if (
                        inst is ParameterizedTransformationRuleBase parameterized
                        && parameters != null
                    )
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
    /// Creates an instance of a transformation rule by its type identifier.
    /// </summary>
    /// <param name="type">The type identifier of the transformation rule.</param>
    /// <param name="parameters">Optional parameters to configure the rule.</param>
    /// <returns>A configured instance of the transformation rule.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="type"/> is null or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the transformation rule type is not registered or when instantiation fails.
    /// </exception>
    public ITransformationRule Create(string type, Dictionary<string, object>? parameters)
    {
        if (
            !_factories.TryGetValue(
                type,
                out Func<Dictionary<string, object>?, ITransformationRule>? factory
            )
        )
        {
            throw new InvalidOperationException($"Transformation rule '{type}' not registered. ");
        }

        return factory(parameters);
    }

    /// <summary>
    /// Attempts to create an instance of a transformation rule by its type identifier.
    /// </summary>
    /// <param name="type">The type identifier of the transformation rule.</param>
    /// <param name="parameters">Optional parameters to configure the rule.</param>
    /// <param name="rule">
    /// When this method returns, contains the created transformation rule if successful;
    /// otherwise, null.
    /// </param>
    /// <returns>
    /// True if the transformation rule was successfully created; otherwise, false.
    /// </returns>
    public bool TryCreate(
        string type,
        Dictionary<string, object>? parameters,
        out ITransformationRule? rule
    )
    {
        rule = null;
        if (
            !_factories.TryGetValue(
                type,
                out Func<Dictionary<string, object>?, ITransformationRule>? factory
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
