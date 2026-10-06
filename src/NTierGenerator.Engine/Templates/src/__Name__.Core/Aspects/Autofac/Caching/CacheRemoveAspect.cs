using __Name__.Core.CrossCuttingConcerns.Caching;
using __Name__.Core.Utilities.Interceptors;
using __Name__.Core.Utilities.IoC;
using Castle.DynamicProxy;

namespace __Name__.Core.Aspects.Autofac.Caching;

/// <summary>
/// Metot başarıyla tamamlandığında, anahtarı verilen desene (regex) uyan cache kayıtlarını siler.
/// Veriyi değiştiren metotlara eklenir.
/// <para>Kullanım: <c>[CacheRemoveAspect("IProductService.Get")]</c></para>
/// </summary>
public sealed class CacheRemoveAspect(string pattern) : MethodInterception
{
    protected override void OnSuccess(IInvocation invocation, object? result)
    {
        ServiceTool.GetRequiredService<ICacheManager>().RemoveByPattern(pattern);
    }
}
