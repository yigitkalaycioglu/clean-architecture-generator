using System.Reflection;
using __Name__.Application.Abstractions.Messaging;
using __Name__.Domain.Common;
using FluentValidation;

namespace __Name__.ArchitectureTests;

/// <summary>Kod kuralları: her isteğin tek işleyicisi vardır, işleyiciler dışarı açılmaz, varlıklar kapsüllüdür.</summary>
public class ConventionTests
{
    [Fact]
    public void EveryRequest_HasExactlyOneHandler()
    {
        var types = Layers.Application.GetTypes();
        var violations = new List<string>();

        foreach (var requestType in types.Where(type => type is { IsAbstract: false, IsInterface: false }))
        {
            var requestInterface = requestType.GetInterfaces()
                .FirstOrDefault(contract => contract.IsGenericType && contract.GetGenericTypeDefinition() == typeof(IRequest<>));
            if (requestInterface is null)
            {
                continue;
            }

            var handlerInterface = typeof(IRequestHandler<,>).MakeGenericType(requestType, requestInterface.GetGenericArguments()[0]);
            var handlerCount = types.Count(type => type is { IsAbstract: false, IsInterface: false } && handlerInterface.IsAssignableFrom(type));
            if (handlerCount != 1)
            {
                violations.Add($"{requestType.Name}: {handlerCount} işleyici");
            }
        }

        Assert.Empty(violations);
    }

    [Fact]
    public void HandlersAndValidators_AreSealedAndInternal()
    {
        Type[] openContracts = [typeof(IRequestHandler<,>), typeof(IDomainEventHandler<>), typeof(IValidator<>)];

        var violations = Layers.Application.GetTypes()
            .Where(type => type is { IsClass: true, IsAbstract: false } && type.GetInterfaces()
                .Any(contract => contract.IsGenericType && openContracts.Contains(contract.GetGenericTypeDefinition())))
            .Where(type => type.IsPublic || !type.IsSealed)
            .Select(type => type.FullName)
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Entities_DoNotExposePublicSetters()
    {
        // Varlıkların durumu yalnızca kendi metotlarıyla değişir; dışarıdan atama yapılamaz.
        var violations = Layers.Domain.GetTypes()
            .Where(type => typeof(Entity).IsAssignableFrom(type))
            .SelectMany(type => type.GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.DeclaredOnly))
            .Where(property => property.SetMethod is { IsPublic: true })
            .Select(property => $"{property.DeclaringType!.Name}.{property.Name}")
            .ToList();

        Assert.Empty(violations);
    }
}
