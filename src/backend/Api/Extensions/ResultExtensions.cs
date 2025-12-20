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
    /// Maps a domain error to an appropriate HTTP problem result based on the error type.
    /// </summary>
    /// <param name="error">The domain error to map.</param>
    /// <returns>An <see cref="IResult"/> representing the HTTP problem response corresponding to the error type.</returns>
    public static IResult MapErrorToResult(Error error) =>
        _errorMappers.TryGetValue(error.Type, out Func<Error, IResult>? mapper)
            ? mapper(error)
            : TypedResults.Problem(
                statusCode: StatusCodes.Status500InternalServerError,
                title: "Unknown error",
                detail: error.Message
            );

    /// <summary>
    /// Maps a multi-field validation result to an HTTP validation problem result.
    /// </summary>
    /// <param name="validation">The validation result containing field-level validation errors.</param>
    /// <returns>An <see cref="IResult"/> representing an HTTP 400 Bad Request response with validation error details.</returns>
    public static IResult MapValidationToResult(MultiFieldValidationResult validation)
    {
        var errorsDictionary = validation
            .ValidationResults.Where(vr => vr.ValidationErrors.Count > 0)
            .ToDictionary(vr => vr.FieldName, vr => vr.ValidationErrors.ToArray());

        return TypedResults.ValidationProblem(errorsDictionary, title: "Validation failed");
    }
}

/// <summary>
/// Provides extension methods for handling and transforming command responses.
/// </summary>
public static class CommandResponseExtensions
{
    /// <summary>
    /// Applies pattern matching to a command response, invoking the appropriate handler based on the response state.
    /// </summary>
    /// <typeparam name="T">The type of data contained in the command response.</typeparam>
    /// <param name="response">The command response to match against.</param>
    /// <param name="onSuccess">The function to invoke when the command succeeds, receiving the success data.</param>
    /// <param name="onValidationFailure">The function to invoke when validation fails, receiving the validation result.</param>
    /// <param name="onFailure">The function to invoke when the command fails, receiving the error.</param>
    /// <returns>An <see cref="IResult"/> produced by one of the handler functions based on the response state.</returns>
    public static IResult Match<T>(
        this CommandResponse<T> response,
        Func<T, IResult> onSuccess,
        Func<MultiFieldValidationResult, IResult> onValidationFailure,
        Func<Error, IResult> onFailure
    )
    {
        if (response.Validation?.IsFailure == true)
        {
            return onValidationFailure(response.Validation);
        }

        if (!response.Result!.IsSuccess)
        {
            return onFailure(response.Result.Error!);
        }

        return onSuccess(response.Result.Value!);
    }
}
