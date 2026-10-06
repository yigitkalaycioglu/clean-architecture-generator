using Castle.DynamicProxy;

namespace __Name__.Core.Utilities.Interceptors;

/// <summary>
/// Tüm aspect'lerin temeli: hem attribute hem de Castle DynamicProxy interceptor'ı.
/// Bir metoda ya da sınıfa eklenen aspect'ler <see cref="Priority"/> değeri küçük olandan büyüğe çalışır.
/// </summary>
[AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true, Inherited = true)]
public abstract class MethodInterceptionBaseAttribute : Attribute, IInterceptor
{
    public int Priority { get; set; }

    public abstract void Intercept(IInvocation invocation);
}
