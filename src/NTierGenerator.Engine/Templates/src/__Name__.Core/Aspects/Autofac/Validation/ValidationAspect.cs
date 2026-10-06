using __Name__.Core.CrossCuttingConcerns.Validation;
using __Name__.Core.Utilities.Interceptors;
using Castle.DynamicProxy;
using FluentValidation;

namespace __Name__.Core.Aspects.Autofac.Validation;

/// <summary>
/// Metodun parametrelerini verilen FluentValidation doğrulayıcısıyla, metot çalışmadan önce doğrular.
/// Geçersizse <see cref="ValidationException"/> fırlatır (API'de 400 Bad Request olarak döner).
/// <para>Kullanım: <c>[ValidationAspect(typeof(ProductValidator))]</c></para>
/// </summary>
public sealed class ValidationAspect : MethodInterception
{
    private readonly Type _validatorType;
    private readonly Type _validatedType;

    public ValidationAspect(Type validatorType)
    {
        var validatorInterface = validatorType.GetInterfaces()
            .FirstOrDefault(type => type.IsGenericType && type.GetGenericTypeDefinition() == typeof(IValidator<>));

        if (validatorInterface is null || validatorType.IsAbstract)
        {
            throw new ArgumentException($"'{validatorType.Name}' bir doğrulama sınıfı değil (AbstractValidator<T> türetilmeli).", nameof(validatorType));
        }

        _validatorType = validatorType;
        _validatedType = validatorInterface.GetGenericArguments()[0];
    }

    protected override void OnBefore(IInvocation invocation)
    {
        var validator = (IValidator)Activator.CreateInstance(_validatorType)!;
        foreach (var argument in invocation.Arguments)
        {
            if (argument is not null && _validatedType.IsInstanceOfType(argument))
            {
                ValidationTool.Validate(validator, argument);
            }
        }
    }
}
