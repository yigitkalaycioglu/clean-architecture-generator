namespace CleanArchitectureGenerator.Engine;

/// <summary>
/// Üretilebilecek projeler. Şablon klasöründeki "{Klasör}/__Name__.{Katman}" dizini o projeye aittir;
/// proje seçili değilse altındaki tüm şablonlar atlanır.
/// </summary>
internal static class ProjectCatalog
{
    public const string StartupLayer = "Api";

    public static readonly IReadOnlyList<ProjectDefinition> All =
    [
        new("Domain", "src", _ => true),
        new("Application", "src", _ => true),
        new("Infrastructure", "src", _ => true),
        new(StartupLayer, "src", _ => true),
        new("UnitTests", "tests", options => options.IncludeTests),
        new("ArchitectureTests", "tests", options => options.IncludeTests),
        new("IntegrationTests", "tests", options => options.IncludeTests)
    ];
}

internal sealed record ProjectDefinition(string Layer, string SolutionFolder, Func<GeneratorOptions, bool> IsIncluded)
{
    /// <summary>Şablonlar içindeki proje klasörü, örneğin "src/__Name__.Domain".</summary>
    public string TemplateDirectory => $"{SolutionFolder}/__Name__.{Layer}";

    public PlannedProject ToPlannedProject(string solutionName)
    {
        var projectName = $"{solutionName}.{Layer}";
        return new PlannedProject(projectName, $"{SolutionFolder}/{projectName}/{projectName}.csproj", SolutionFolder, Layer);
    }
}
