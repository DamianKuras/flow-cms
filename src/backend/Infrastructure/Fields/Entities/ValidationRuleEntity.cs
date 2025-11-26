using System;
using System.Collections.Generic;

namespace Infrastructure.Fields.Entities
{
    public class ValidationRuleEntity
    {
        public Guid Id { get; set; }

        public Guid FieldId { get; set; }
        public string Type { get; set; } = default!;

        public List<ValidationRuleParameterEntity> Parameters { get; set; } =
            new();
    }
}
