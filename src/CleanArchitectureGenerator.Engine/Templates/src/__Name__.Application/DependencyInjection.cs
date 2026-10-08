using System.Globalization;
using System.Reflection;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Application.Behaviors;
using __Name__.Application.Messaging;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace __Name__.Application;

public static class DependencyInjection
{
    /// <summary>
    /// İşleyicileri, doğrulayıcıları ve istek davranışlarını kaydeder. Yeni bir komut, sorgu, doğrulayıcı ya da
    /// olay işleyicisi eklediğinizde ayrıca kayıt yapmanız gerekmez; bu derlemedeki türler otomatik bulunur.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = typeof(DependencyInjection).Assembly;

        services.AddScoped<ISender, Sender>();
        services.AddScoped<IDomainEventDispatcher, DomainEventDispatcher>();
        services.AddImplementationsOf(typeof(IRequestHandler<,>), assembly);
        services.AddImplementationsOf(typeof(IDomainEventHandler<>), assembly);

        // Sıra önemlidir: ilk kaydedilen davranış en dışta çalışır (önce loglama, sonra doğrulama).
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddScoped(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));

        services.AddValidatorsFromAssembly(assembly, includeInternalTypes: true);
        ValidatorOptions.Global.LanguageManager.Culture = CultureInfo.GetCultureInfo("tr-TR");

        return services;
    }

    private static void AddImplementationsOf(this IServiceCollection services, Type openGenericInterface, Assembly assembly)
    {
        var registrations = assembly.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false, IsGenericTypeDefinition: false })
            .SelectMany(type => type.GetInterfaces()
                .Where(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == openGenericInterface)
                .Select(contract => (Contract: contract, Implementation: type)));

        foreach (var (contract, implementation) in registrations)
        {
            services.AddScoped(contract, implementation);
        }
    }
}
