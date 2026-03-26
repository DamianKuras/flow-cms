using System.Text.Json;

namespace Infrastructure;

/// <summary>
/// Converts <see cref="JsonElement"/> values to their corresponding CLR primitives.
/// Used when deserializing field values that arrive as raw JSON from the database or API.
/// </summary>
public static class JsonTypeConverter
{
    /// <summary>
    /// Converts a <see cref="JsonElement"/> to its CLR equivalent (int, long, string, or bool).
    /// Non-JsonElement values are returned as-is.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the JsonElement kind has no supported CLR mapping.
    /// </exception>
    public static object Convert(object value)
    {
        if (value is JsonElement je)
        {
            return je.ValueKind switch
            {
                JsonValueKind.Number when je.TryGetInt32(out int i) => i,
                JsonValueKind.Number when je.TryGetInt64(out long l) => l,
                JsonValueKind.String => je.GetString()!,
                JsonValueKind.True => true,
                JsonValueKind.False => false,
                _ => throw new InvalidOperationException(
                    $"Unsupported JsonElement type: {je.ValueKind}"
                ),
            };
        }

        return value;
    }
}
