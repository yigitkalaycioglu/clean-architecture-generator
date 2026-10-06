using System.Collections.Concurrent;
using System.Reflection;
using Castle.DynamicProxy;

namespace __Name__.Core.Utilities.Interceptors;

/// <summary>
/// Castle DynamicProxy ile async metotları (Task / Task&lt;T&gt;) doğru yakalamak için yardımcılar.
/// Klasik "invocation.Proceed(); sonra işlem yap" yaklaşımı async metotlarda görev bitmeden çalışır;
/// buradaki metotlar görevin sonucunu bekleyip öyle devam eder.
/// </summary>
public static class InvocationExtensions
{
    private static readonly MethodInfo WrapGenericTaskMethod =
        typeof(InvocationExtensions).GetMethod(nameof(WrapGenericTaskAsync), BindingFlags.NonPublic | BindingFlags.Static)!;

    private static readonly MethodInfo TaskFromResultMethod =
        typeof(Task).GetMethod(nameof(Task.FromResult))!;

    private static readonly ConcurrentDictionary<Type, MethodInfo> WrapGenericTaskCache = new();
    private static readonly ConcurrentDictionary<Type, MethodInfo> TaskFromResultCache = new();

    /// <summary>Metot <c>Task&lt;T&gt;</c> döndürüyorsa T, aksi halde null.</summary>
    public static Type? GetAsyncResultType(this IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        return returnType.IsGenericType && returnType.GetGenericTypeDefinition() == typeof(Task<>)
            ? returnType.GetGenericArguments()[0]
            : null;
    }

    /// <summary>Metot geriye değer döndürüyorsa (void ya da düz Task değilse) true.</summary>
    public static bool ReturnsValue(this IInvocation invocation)
    {
        var returnType = invocation.Method.ReturnType;
        return returnType != typeof(void) && returnType != typeof(Task);
    }

    /// <summary>
    /// Metodu çalıştırır. Sonuç hazır olduğunda (async ise görev bitince) <paramref name="onSuccess"/>,
    /// hata olursa <paramref name="onException"/>, her durumda en sonda <paramref name="onFinally"/> çağrılır.
    /// </summary>
    public static void ProceedWithCallbacks(
        this IInvocation invocation,
        Action<object?>? onSuccess = null,
        Action<Exception>? onException = null,
        Action? onFinally = null)
    {
        try
        {
            invocation.Proceed();
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
            onFinally?.Invoke();
            throw;
        }

        if (invocation.ReturnValue is Task task)
        {
            var resultType = invocation.GetAsyncResultType();
            invocation.ReturnValue = resultType is null
                ? WrapTaskAsync(task, onSuccess, onException, onFinally)
                : WrapGenericTaskCache
                    .GetOrAdd(resultType, type => WrapGenericTaskMethod.MakeGenericMethod(type))
                    .Invoke(null, [task, onSuccess, onException, onFinally]);
            return;
        }

        try
        {
            onSuccess?.Invoke(invocation.ReturnValue);
        }
        finally
        {
            onFinally?.Invoke();
        }
    }

    /// <summary>
    /// Değeri metodun dönüş tipine uygun hale getirir: async metotlarda tamamlanmış <c>Task&lt;T&gt;</c> içine sarar.
    /// Cache'ten dönen değerler için kullanılır.
    /// </summary>
    public static object? ToReturnValue(this IInvocation invocation, object? value)
    {
        var resultType = invocation.GetAsyncResultType();
        return resultType is null
            ? value
            : TaskFromResultCache
                .GetOrAdd(resultType, type => TaskFromResultMethod.MakeGenericMethod(type))
                .Invoke(null, [value]);
    }

    private static async Task WrapTaskAsync(Task task, Action<object?>? onSuccess, Action<Exception>? onException, Action? onFinally)
    {
        try
        {
            await task.ConfigureAwait(false);
            onSuccess?.Invoke(null);
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
            throw;
        }
        finally
        {
            onFinally?.Invoke();
        }
    }

    private static async Task<T> WrapGenericTaskAsync<T>(Task task, Action<object?>? onSuccess, Action<Exception>? onException, Action? onFinally)
    {
        try
        {
            var result = await ((Task<T>)task).ConfigureAwait(false);
            onSuccess?.Invoke(result);
            return result;
        }
        catch (Exception exception)
        {
            onException?.Invoke(exception);
            throw;
        }
        finally
        {
            onFinally?.Invoke();
        }
    }
}
