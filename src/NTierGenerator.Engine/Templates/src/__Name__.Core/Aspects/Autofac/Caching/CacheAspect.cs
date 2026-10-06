using __Name__.Core.CrossCuttingConcerns.Caching;
using __Name__.Core.Utilities.Interceptors;
using __Name__.Core.Utilities.IoC;
using __Name__.Core.Utilities.Results;
using Castle.DynamicProxy;

namespace __Name__.Core.Aspects.Autofac.Caching;

/// <summary>
/// Metodun sonucunu, "Namespace.Arayüz.Metot(parametreler)" anahtarıyla verilen süre (dakika) boyunca cache'ler.
/// Başarısız <see cref="IResult"/> sonuçları cache'lenmez. Async metotlarda görevin sonucu cache'lenir.
/// <para>Kullanım: <c>[CacheAspect(duration: 10)]</c></para>
/// </summary>
public sealed class CacheAspect(int duration = 60) : MethodInterceptionBaseAttribute
{
    public override void Intercept(IInvocation invocation)
    {
        if (!invocation.ReturnsValue())
        {
            invocation.Proceed();
            return;
        }

        var cacheManager = ServiceTool.GetRequiredService<ICacheManager>();
        var key = CreateKey(invocation);

        if (cacheManager.TryGet(key, out var cachedValue))
        {
            invocation.ReturnValue = invocation.ToReturnValue(cachedValue);
            return;
        }

        invocation.ProceedWithCallbacks(onSuccess: result =>
        {
            if (result is not IResult { Success: false })
            {
                cacheManager.Add(key, result, TimeSpan.FromMinutes(duration));
            }
        });
    }

    private static string CreateKey(IInvocation invocation)
    {
        var method = invocation.Method;
        var arguments = invocation.Arguments
            .Where(argument => argument is not CancellationToken)
            .Select(argument => argument?.ToString() ?? "<null>");

        return $"{method.ReflectedType?.FullName}.{method.Name}({string.Join(",", arguments)})";
    }
}
