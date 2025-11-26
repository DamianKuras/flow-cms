using System;

namespace Infrastructure.Fields.Entities
{
    public class ValidationRuleParameterEntity
    {
        public Guid Id { get; set; }
        public Guid ValidationRuleId { get; set; }
        public string Key { get; set; } = default!;
        public string Value { get; set; } = default!;
        public string ValueType { get; set; } = default!;
    }
}
