using Castle.DynamicProxy;

namespace __Name__.Core.Utilities.Interceptors;

/// <summary>
/// Metodun yaşam döngüsüne kanca (hook) sağlayan aspect temeli. Async metotlarda
/// <see cref="OnSuccess"/>, <see cref="OnException"/> ve <see cref="OnAfter"/> görev tamamlandıktan sonra çalışır.
/// </summary>
public abstract class MethodInterception : MethodInterceptionBaseAttribute
{
    public override void Intercept(IInvocation invocation)
    {
        OnBefore(invocation);
        invocation.ProceedWithCallbacks(
            onSuccess: result => OnSuccess(invocation, result),
            onException: exception => OnException(invocation, exception),
            onFinally: () => OnAfter(invocation));
    }

    /// <summary>Metot çalışmadan önce. Burada fırlatılan hata metodun çalışmasını engeller.</summary>
    protected virtual void OnBefore(IInvocation invocation)
    {
    }

    /// <summary>Metot hatasız tamamlandığında. <paramref name="result"/>: dönen değer (async ise await edilmiş hali).</summary>
    protected virtual void OnSuccess(IInvocation invocation, object? result)
    {
    }

    /// <summary>Metot hata fırlattığında; hata yine de yukarı iletilir.</summary>
    protected virtual void OnException(IInvocation invocation, Exception exception)
    {
    }

    /// <summary>Başarılı ya da hatalı, her durumda en sonda.</summary>
    protected virtual void OnAfter(IInvocation invocation)
    {
    }
}
