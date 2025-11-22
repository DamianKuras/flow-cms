namespace Domain.Fields;

public enum FieldTypes
{
    Text,
    Numeric,
    Boolean,
    Richtext,
    Markdown,
}

public static class FieldTypeDefaults
{
    public static object? GetDefaultValue(FieldTypes type) =>
        type switch
        {
            FieldTypes.Boolean => false,
            FieldTypes.Numeric => 0,
            FieldTypes.Text or FieldTypes.Markdown or FieldTypes.Richtext => "",
            _ => throw new ArgumentOutOfRangeException(
                nameof(type),
                $"Unexpected field type: {type}"
            ),
        };
}
