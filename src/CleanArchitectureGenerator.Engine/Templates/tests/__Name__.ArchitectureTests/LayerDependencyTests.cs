namespace __Name__.ArchitectureTests;

/// <summary>
/// Bağımlılık kuralı: bağımlılıklar yalnızca içe doğru akar. Api -> Infrastructure -> Application -> Domain.
/// Domain hiçbir şeyi, Application yalnızca Domain'i bilir.
/// </summary>
public class LayerDependencyTests
{
    [Fact]
    public void Domain_DependsOnlyOnTheFramework()
    {
        var violations = Layers.Domain.ReferencedAssemblyNames()
            .Where(name => name != "netstandard" && !name.StartsWith("System", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Application_DoesNotDependOnOuterLayers()
    {
        var references = Layers.Application.ReferencedAssemblyNames();

        Assert.DoesNotContain(Layers.Infrastructure.GetName().Name!, references);
        Assert.DoesNotContain(Layers.Api.GetName().Name!, references);
    }

    [Fact]
    public void Application_DoesNotKnowAboutHttp()
    {
        var violations = Layers.Application.ReferencedAssemblyNames()
            .Where(name => name.StartsWith("Microsoft.AspNetCore", StringComparison.Ordinal))
            .ToList();

        Assert.Empty(violations);
    }

    [Fact]
    public void Infrastructure_DoesNotDependOnApi()
    {
        Assert.DoesNotContain(Layers.Api.GetName().Name!, Layers.Infrastructure.ReferencedAssemblyNames());
    }
}
