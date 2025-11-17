using Domain;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Extensions;

public static class ResultExtensions
{
    private static readonly Dictionary<ErrorTypes, Func<Error, IResult>> _errorMappers = new()
    {
        [ErrorTypes.Validation] = error => TypedResults.BadRequest(error.Message),
        [ErrorTypes.NotFound] = error => TypedResults.NotFound(error.Message),
        [ErrorTypes.Conflict] = error => TypedResults.Conflict(error.Message),
        [ErrorTypes.Forbidden] = error => TypedResults.Forbid(),
        [ErrorTypes.Unauthorized] = error => TypedResults.Unauthorized(),
        [ErrorTypes.Infrastructure] = error => TypedResults.InternalServerError(),
    };

    public static IResult MapErrorToResult(Error error)
    {
        return _errorMappers.TryGetValue(error.Type, out var mapper)
            ? mapper(error)
            : TypedResults.InternalServerError();
    }
}
