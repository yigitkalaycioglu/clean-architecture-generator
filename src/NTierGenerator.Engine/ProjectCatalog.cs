namespace NTierGenerator.Engine;

/// <summary>
/// Üretilebilecek projeler. Şablon klasöründeki "{Klasör}/__Name__.{Katman}" dizini o projeye aittir;
/// proje seçili değilse altındaki tüm şablonlar atlanır.
/// </summary>
internal static class ProjectCatalog
{
    public static readonly IReadOnlyList<ProjectDefinition> All =
    [
        new("Core", "src", _ => true),
        new("Entities", "src", _ => true),
        new("DataAccess", "src", _ => true),
        new("Business", "src", _ => true),
        new("WebAPI", "src", options => options.IncludeWebApi),
        new("WebUI", "src", options => options.IncludeWebUi),
        new("Tests", "tests", options => options.IncludeTests)
    ];
}

internal sealed record ProjectDefinition(string Layer, string SolutionFolder, Func<GeneratorOptions, bool> IsIncluded)
{
    /// <summary>Şablonlar içindeki proje klasörü, örneğin "src/__Name__.Core".</summary>
    public string TemplateDirectory => $"{SolutionFolder}/__Name__.{Layer}";

    public PlannedProject ToPlannedProject(string solutionName)
    {
        var projectName = $"{solutionName}.{Layer}";
        return new PlannedProject(projectName, $"{SolutionFolder}/{projectName}/{projectName}.csproj", SolutionFolder, Layer);
    }
}
