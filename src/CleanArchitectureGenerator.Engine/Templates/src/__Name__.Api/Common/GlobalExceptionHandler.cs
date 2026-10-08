using Microsoft.AspNetCore.Diagnostics;

namespace __Name__.Api.Common;

/// <summary>
/// Yakalanmamış istisnaları ProblemDetails yanıtına çevirir. İstemciye yalnızca genel bir mesaj ve traceId
/// gider; istisnanın ayrıntısı yalnızca sunucu loglarına yazılır.
/// </summary>
internal sealed class GlobalExceptionHandler(IProblemDetailsService problemDetailsService, ILogger<GlobalExceptionHandler> logger)
    : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        var (statusCode, detail) = exception switch
        {
            // Okunamayan gövde, beklenmeyen alan, çok büyük istek…: istemci hatasıdır, 4xx döner.
            BadHttpRequestException badRequest => (badRequest.StatusCode, "İstek okunamadı. Gövdenin geçerli JSON olduğundan ve yalnızca beklenen alanları içerdiğinden emin olun."),
            _ => (StatusCodes.Status500InternalServerError, "Beklenmeyen bir hata oluştu.")
        };

        if (statusCode >= StatusCodes.Status500InternalServerError)
        {
            logger.LogError(exception, "İşlenmeyen istisna: {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }
        else
        {
            logger.LogDebug(exception, "Geçersiz istek: {Method} {Path}", httpContext.Request.Method, httpContext.Request.Path);
        }

        httpContext.Response.StatusCode = statusCode;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = { Status = statusCode, Detail = detail }
        });
    }
}
