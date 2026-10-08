using System.Reflection;
using __Name__.Domain.Common;
using ApiLayer = __Name__.Api.DependencyInjection;
using ApplicationLayer = __Name__.Application.DependencyInjection;
using InfrastructureLayer = __Name__.Infrastructure.DependencyInjection;

namespace __Name__.ArchitectureTests;

internal static class Layers
{
    public static readonly Assembly Domain = typeof(Entity).Assembly;
    public static readonly Assembly Application = typeof(ApplicationLayer).Assembly;
    public static readonly Assembly Infrastructure = typeof(InfrastructureLayer).Assembly;
    public static readonly Assembly Api = typeof(ApiLayer).Assembly;

    public static IReadOnlyList<string> ReferencedAssemblyNames(this Assembly assembly) =>
        assembly.GetReferencedAssemblies().Select(reference => reference.Name ?? string.Empty).ToList();
}
