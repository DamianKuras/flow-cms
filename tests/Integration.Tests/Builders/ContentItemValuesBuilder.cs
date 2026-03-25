using Application.ContentTypes;
using Domain.ContentItems;

namespace Integration.Tests.Builders;

public sealed class ContentItemValuesBuilder
{
    private readonly ContentTypeDto _type;
    private readonly Dictionary<Guid, object?> _values = [];

    public ContentItemValuesBuilder(ContentTypeDto type) => _type = type;

    public ContentItemValuesBuilder Set(string fieldName, object value)
    {
        FieldDto field = _type.Fields.Single(f => f.Name == fieldName);
        _values[field.Id] = value;
        return this;
    }

    public Dictionary<Guid, object?> Build() => _values;
}
