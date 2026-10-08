using __Name__.Domain.Common;

namespace __Name__.Api.Common;

/// <summary>Application'dan dönen hataları HTTP durum kodlu ProblemDetails yanıtlarına çevirir.</summary>
public static class ResultExtensions
{
    public static IResult ToProblem(this Result result)
    {
        ArgumentNullException.ThrowIfNull(result);
        if (result.IsSuccess)
        {
            throw new InvalidOperationException("Başarılı bir sonuç hata yanıtına çevrilemez.");
        }

        return result.Error.ToProblem();
    }

    public static IResult ToProblem(this Error error)
    {
        ArgumentNullException.ThrowIfNull(error);
        var extensions = new Dictionary<string, object?> { ["code"] = error.Code };

        if (error.Type == ErrorType.Validation && error.Details is { Count: > 0 } details)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]>(details), detail: error.Description, extensions: extensions);
        }

        return TypedResults.Problem(detail: error.Description, statusCode: GetStatusCode(error.Type), extensions: extensions);
    }

    private static int GetStatusCode(ErrorType type) => type switch
    {
        ErrorType.Validation => StatusCodes.Status400BadRequest,
        ErrorType.NotFound => StatusCodes.Status404NotFound,
        ErrorType.Conflict => StatusCodes.Status409Conflict,
        ErrorType.Unauthorized => StatusCodes.Status401Unauthorized,
        ErrorType.Forbidden => StatusCodes.Status403Forbidden,
        _ => StatusCodes.Status400BadRequest
    };
}
