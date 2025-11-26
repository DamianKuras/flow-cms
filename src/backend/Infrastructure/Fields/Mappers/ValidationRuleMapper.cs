using Domain.Fields.Validations;
using Infrastructure.Fields.Entities;

namespace Infrastructure.Fields.Mappers
{
    public static class ValidationRuleMapper
    {
        /// <summary>
        /// Converts a persistence ValidationRuleEntity to a domain ValidationRule.
        /// </summary>
        /// <param name="entity">The persistence ValidationRuleEntity to convert to a domain model.</param>
        /// <param name="factory">Factory object used to create the appropriate domain ValidationRule type.</param>
        /// <returns>A domain ValidationRule object instantiated with parameters from the entity.</returns>
        /// <exception cref="InvalidOperationException">Thrown when the entity contains an unknown validation rule type.</exception>
        public static ValidationRule ToDomain(
            ValidationRuleEntity entity,
            ValidationRuleFactory factory
        )
        {
            var parameters = EntityMappers.ToParameters(entity.Parameters);

            if (!factory.TryCreate(entity.Type, parameters, out var rule))
            {
                throw new InvalidOperationException(
                    $"Unknown validation rule type: {entity.Type}"
                );
            }

            return rule!;
        }

        /// <summary>
        /// Converts a collection of ValidationRuleEntity objects to domain ValidationRule objects.
        /// </summary>
        /// <param name="entities">Collection of ValidationRuleEntity objects to convert.</param>
        /// <param name="factory">Factory object used to create domain ValidationRule instances.</param>
        /// <returns>A read-only list of domain ValidationRule objects converted from the persistence entities.</returns>
        /// <exception cref="InvalidOperationException">Thrown when an entity contains an unknown validation rule type.</exception>
        public static IReadOnlyList<ValidationRule> ToDomainBatch(
            IEnumerable<ValidationRuleEntity> entities,
            ValidationRuleFactory factory
        )
        {
            return entities
                .Select(e => ToDomain(e, factory))
                .ToList()
                .AsReadOnly();
        }
    }
}
