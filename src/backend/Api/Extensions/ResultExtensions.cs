using Application.Interfaces;
using Domain.Common;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Http.HttpResults;

namespace Api.Extensions;

/// <summary>
/// Provides extension methods for mapping domain errors and validation results to HTTP results.
/// </summary>
public static class ResultExtensions
{
    private static readonly Dictionary<ErrorTypes, Func<Error, IResult>> _errorMappers = new()
    {
        [ErrorTypes.NotFound] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found",
                detail: error.Message
            ),

        [ErrorTypes.Validation] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status400BadRequest,
                title: "Bad request",
                detail: error.Message
            ),

        [ErrorTypes.Conflict] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status409Conflict,
                title: "Conflict",
                detail: error.Message
            ),

        [ErrorTypes.Forbidden] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status403Forbidden,
                title: "Forbidden",
                detail: error.Message
            ),

        [ErrorTypes.Unauthorized] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Unauthorized",
                detail: error.Message
            ),

        [ErrorTypes.Infrastructure] = error =>
            TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Internal server error",
                detail: error.Message
            ),
    };

    /// <summary>
    /// Maps a domain <see cref="Error"/> into the appropriate HTTP problem result.
    /// </summary>
    /// <param name="error">The domain error to convert.</param>
    /// <returns>An <see cref="IResult"/> representing the HTTP problem response corresponding to the error type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="error"/> is null.</exception>
    public static IResult MapErrorToResult(Error error) =>
        _errorMappers.TryGetValue(error.Type, out Func<Error, IResult>? mapper)
            ? mapper(error)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unknown error",
                detail: error.Message
            );

    /// <summary>
    /// Maps a <see cref="MultiFieldValidationResult"/> to a validation problem response.
    /// </summary>
    /// <param name="validation">The validation result.</param>
    /// <returns>An <see cref="IResult"/> representing a 400 Bad Request with field errors.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="validation"/> is null.</exception>
    public static IResult MapValidationToResult(MultiFieldValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(validation);
        var errorsDictionary = validation
            .ValidationResults.Where(vr => vr.ValidationErrors.Count > 0)
            .ToDictionary(vr => vr.FieldName, vr => vr.ValidationErrors.ToArray());

        return TypedResults.ValidationProblem(errorsDictionary, title: "Validation failed");
    }

    /// <summary>
    /// Matches a <see cref="Result{TResult}"/> to either a success handler or an HTTP error result.
    /// </summary>
    /// <typeparam name="TResult">The success value type.</typeparam>
    /// <param name="result">The result to match.</param>
    /// <param name="onSuccess">The handler to execute on success.</param>
    /// <returns>An <see cref="IResult"/> corresponding to success or failure.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> or <paramref name="onSuccess"/> is null.</exception>
    public static IResult Match<TResult>(
        this Result<TResult> result,
        Func<TResult, IResult> onSuccess
    )
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(onSuccess);

        return result.FailureKind switch
        {
            FailureKind.None => onSuccess(result.Value!),
            FailureKind.DomainError => MapErrorToResult(result.Error!),
            FailureKind.MultiFieldValidation => MapValidationToResult(
                result.MultiFieldValidationResult!
            ),
            _ => Results.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unhandled result state",
                detail: "Encountered an unknown failure kind."
            ),
        };
    }
}
