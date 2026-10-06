using System.Reflection;
using Castle.DynamicProxy;

namespace __Name__.Core.Utilities.Interceptors;

/// <summary>
/// Bir metot çağrıldığında, sınıf ve metot üzerindeki aspect attribute'larını toplayıp
/// önceliğe göre sıralayarak interceptor olarak döndürür.
/// </summary>
public sealed class AspectInterceptorSelector : IInterceptorSelector
{
    public IInterceptor[] SelectInterceptors(Type type, MethodInfo method, IInterceptor[] interceptors)
    {
        var classAttributes = type.GetCustomAttributes<MethodInterceptionBaseAttribute>(inherit: true);

        // Arayüz metodunun sınıftaki karşılığı; aşırı yüklemeleri (overload) ayırt etmek için parametre tipleriyle aranır.
        var parameterTypes = method.GetParameters().Select(parameter => parameter.ParameterType).ToArray();
        var implementation = type.GetMethod(method.Name, parameterTypes) ?? method;
        var methodAttributes = implementation.GetCustomAttributes<MethodInterceptionBaseAttribute>(inherit: true);

        return [.. classAttributes.Concat(methodAttributes).OrderBy(attribute => attribute.Priority), .. interceptors];
    }
}
