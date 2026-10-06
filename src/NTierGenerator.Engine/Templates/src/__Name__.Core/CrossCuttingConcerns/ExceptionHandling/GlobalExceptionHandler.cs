using FluentValidation;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace __Name__.Core.CrossCuttingConcerns.ExceptionHandling;

/// <summary>
/// Yakalanmamış tüm hataları tek yerde ele alır ve RFC 9457 ProblemDetails biçiminde JSON döndürür:
/// doğrulama hatası 400, yetkisiz erişim 401/403, diğerleri 500 (ayrıntılar yalnızca loglanır).
/// <para>Program.cs: <c>AddExceptionHandler&lt;GlobalExceptionHandler&gt;()</c>, <c>AddProblemDetails()</c>, <c>UseExceptionHandler()</c></para>
/// </summary>
public sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var problemDetails = exception switch
        {
            ValidationException validationException => CreateValidationProblem(validationException),
            AuthorizationDeniedException when httpContext.User.Identity?.IsAuthenticated == true =>
                new ProblemDetails { Status = StatusCodes.Status403Forbidden, Title = "Yetkisiz işlem.", Detail = exception.Message },
            AuthorizationDeniedException =>
                new ProblemDetails { Status = StatusCodes.Status401Unauthorized, Title = "Kimlik doğrulama gerekli.", Detail = exception.Message },
            _ => new ProblemDetails { Status = StatusCodes.Status500InternalServerError, Title = "Beklenmeyen bir hata oluştu." }
        };

        var statusCode = problemDetails.Status ?? StatusCodes.Status500InternalServerError;
        if (statusCode == StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "İşlenmeyen hata: {Message}", exception.Message);
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problemDetails,
            Exception = exception
        });
    }

    private static HttpValidationProblemDetails CreateValidationProblem(ValidationException exception)
    {
        var errors = exception.Errors
            .GroupBy(error => error.PropertyName)
            .ToDictionary(group => group.Key, group => group.Select(error => error.ErrorMessage).ToArray());

        return new HttpValidationProblemDetails(errors)
        {
            Status = StatusCodes.Status400BadRequest,
            Title = "Doğrulama hatası."
        };
    }
}
