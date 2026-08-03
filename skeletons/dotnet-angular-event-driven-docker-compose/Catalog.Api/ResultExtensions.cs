using Microsoft.AspNetCore.Http;
using Common.Lib.Errors;

namespace Catalog.Api;

/// <summary>Maps Result errors to Problem Details (adrs/dotnet/result-pattern-errors.md
/// meets RFC 7807 at the edge).</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Error error) => Results.Problem(
        statusCode: error.Type switch
        {
            ErrorType.Validation => 400,
            ErrorType.NotFound => 404,
            ErrorType.Conflict => 409,
            _ => 500,
        },
        title: error.Type switch
        {
            ErrorType.Validation => "Validation Failed",
            ErrorType.NotFound => "Not Found",
            ErrorType.Conflict => "Conflict",
            _ => "Internal Server Error",
        },
        detail: error.Message,
        extensions: new Dictionary<string, object?> { ["code"] = error.Code });
}
