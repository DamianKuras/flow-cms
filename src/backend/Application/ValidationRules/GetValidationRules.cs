using Application.Interfaces;
using Domain.Common;
using Domain.Fields.Validations;

using Domain.Permissions;
using Microsoft.Extensions.Logging;

namespace Application.ValidationRules;

/// <summary>
/// Query to retrieve all available validation rule names.
/// </summary>
public sealed record GetValidationRulesQuery();

/// <summary>
/// Response containing the list of available validation rule names.
/// </summary>
/// <param name="ValidationRuleNames">A read-only list of validation rule names.</param>
public sealed record GetValidationRulesResponse(IReadOnlyList<string> ValidationRuleNames);

/// <summary>
/// Handler for retrieving all available validation rules.
/// </summary>
/// <param name="validationRuleRegistry">The registry containing validation rules.</param>
/// <param name="authorizationService">The service for role-based authorization.</param>
/// <param name="logger">The logger instance.</param>
public sealed class GetValidationRulesQueryHandler(
    IValidationRuleRegistry validationRuleRegistry,
    IAuthorizationService authorizationService,
    ILogger<GetValidationRulesQueryHandler> logger
) : IQueryHandler<GetValidationRulesQuery, GetValidationRulesResponse>
{
    /// <summary>
    /// Handles the query to retrieve validation rule names.
    /// </summary>
    /// <param name="query">The query request.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A result containing the response with rule names.</returns>
    public async Task<Result<GetValidationRulesResponse>> Handle(
        GetValidationRulesQuery query,
        CancellationToken cancellationToken
    )
    {
        bool allowed = await authorizationService.IsAllowedForTypeAsync(
            CmsAction.Read,
            ResourceType.ContentType,
            cancellationToken
        );

        if (!allowed)
        {
            logger.LogWarning("Authorization failed for GetValidationRules");
            return Result<GetValidationRulesResponse>.Failure(Error.Forbidden("Forbidden"));
        }

        IReadOnlyList<string> names = validationRuleRegistry.GetAllRules();
        return Result<GetValidationRulesResponse>.Success(new GetValidationRulesResponse(names));
    }
}
