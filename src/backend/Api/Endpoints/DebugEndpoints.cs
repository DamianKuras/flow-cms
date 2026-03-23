using Api.Extensions;
using Application.Interfaces;
using Application.ValidationRules;
using Domain.Common;
using Microsoft.AspNetCore.Mvc;

namespace Api.Endpoints;

/// <summary>
/// Provides extension methods for registering debug and diagnostic API endpoints.
/// </summary>
public static class DebugEndpoints
{
    /// <summary>
    /// Registers debug endpoints under the <c>/debug</c> route group.
    /// </summary>
    /// <param name="endpoints">The endpoint route builder.</param>
    public static void RegisterDebugEndpoints(this IEndpointRouteBuilder endpoints)
    {
        IEndpointRouteBuilder group = endpoints.MapGroup("/debug").WithTags("Debug");
        group.MapGet("/validationRules", GetValidationRules);
    }

    private static async Task<IResult> GetValidationRules(
        [FromServices] IQueryHandler<GetValidationRulesQuery, GetValidationRulesResponse> handler,
        CancellationToken cancellationToken
    )
    {
        var query = new GetValidationRulesQuery();
        Result<GetValidationRulesResponse> result = await handler.Handle(query, cancellationToken);
        return result.Match(onSuccess: response => Results.Ok(response.ValidationRuleNames));
    }
}
