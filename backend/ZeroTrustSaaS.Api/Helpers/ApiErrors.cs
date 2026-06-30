using Microsoft.AspNetCore.Mvc;
using ZeroTrustSaaS.Domain.Common;

namespace ZeroTrustSaaS.Api.Helpers;

internal static class ApiErrors
{
    internal static IResult Problem(Error error)
    {
        int statusCode = error.Type switch
        {
            ErrorType.Validation => StatusCodes.Status400BadRequest,
            ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
            ErrorType.Forbidden => StatusCodes.Status403Forbidden,
            ErrorType.NotFound => StatusCodes.Status404NotFound,
            ErrorType.Conflict => StatusCodes.Status409Conflict,
            _ => StatusCodes.Status500InternalServerError,
        };

        var problemDetails = new ProblemDetails
        {
            Status = statusCode,
            Title = error.Code,
            Detail = error.Description,
        };

        return Results.Problem(problemDetails);
    }
}
