using System.Diagnostics;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using Microsoft.Extensions.Logging;

namespace __Name__.Application.Behaviors;

/// <summary>
/// Her isteğin adını, sonucunu ve süresini loglar. İsteğin içeriği bilerek loglanmaz: parola, token ya da
/// kişisel veri içerebilir.
/// </summary>
internal sealed class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
    where TResponse : Result, IFailureFactory<TResponse>
{
    public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(next);

        var requestName = typeof(TRequest).Name;
        var startedAt = Stopwatch.GetTimestamp();
        var response = await next();
        var elapsedMilliseconds = Stopwatch.GetElapsedTime(startedAt).TotalMilliseconds;

        if (response.IsSuccess)
        {
            logger.LogInformation("{RequestName} tamamlandı ({ElapsedMilliseconds:0} ms)", requestName, elapsedMilliseconds);
        }
        else
        {
            logger.LogWarning("{RequestName} başarısız: {ErrorCode} ({ElapsedMilliseconds:0} ms)", requestName, response.Error.Code, elapsedMilliseconds);
        }

        return response;
    }
}
