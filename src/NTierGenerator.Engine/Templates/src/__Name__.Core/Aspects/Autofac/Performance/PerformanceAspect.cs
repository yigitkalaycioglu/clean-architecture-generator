using System.Diagnostics;
using __Name__.Core.Utilities.Interceptors;
using __Name__.Core.Utilities.IoC;
using Castle.DynamicProxy;
using Microsoft.Extensions.Logging;

namespace __Name__.Core.Aspects.Autofac.Performance;

/// <summary>
/// Metodun çalışma süresi verilen eşiği (saniye) aşarsa uyarı loglar.
/// Süre her çağrı için ayrı ölçülür; eşzamanlı isteklerde birbirini etkilemez.
/// <para>Kullanım: <c>[PerformanceAspect(intervalInSeconds: 3)]</c></para>
/// </summary>
public sealed class PerformanceAspect(int intervalInSeconds) : MethodInterceptionBaseAttribute
{
    public override void Intercept(IInvocation invocation)
    {
        var startTimestamp = Stopwatch.GetTimestamp();
        invocation.ProceedWithCallbacks(onFinally: () =>
        {
            var elapsed = Stopwatch.GetElapsedTime(startTimestamp);
            if (elapsed.TotalSeconds <= intervalInSeconds)
            {
                return;
            }

            var logger = ServiceTool.GetRequiredService<ILoggerFactory>().CreateLogger<PerformanceAspect>();
            logger.LogWarning(
                "Performans uyarısı: {Method} {ElapsedMilliseconds:N0} ms sürdü (eşik {Threshold} sn).",
                $"{invocation.TargetType?.Name}.{invocation.Method.Name}",
                elapsed.TotalMilliseconds,
                intervalInSeconds);
        });
    }
}
