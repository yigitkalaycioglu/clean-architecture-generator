namespace NTierGenerator.Engine;

/// <summary>
/// Diske yazılmadan önce bellekte hazırlanan çözüm: tüm dosyalar ve içerikleri.
/// Arayüzdeki önizleme de bu plandan çizilir.
/// </summary>
public sealed class GenerationPlan
{
    public GenerationPlan(string solutionName, string solutionFileName, IReadOnlyList<PlannedProject> projects, IReadOnlyList<PlannedFile> files)
    {
        SolutionName = solutionName;
        SolutionFileName = solutionFileName;
        Projects = projects;
        Files = files;
        Directories = files
            .SelectMany(file => GetParentDirectories(file.RelativePath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .Order(StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public string SolutionName { get; }

    /// <summary>Çözüm kök klasörüne göre çözüm dosyasının adı, örneğin "Firma.Proje.sln".</summary>
    public string SolutionFileName { get; }

    public IReadOnlyList<PlannedProject> Projects { get; }

    /// <summary>Çözüm kök klasörüne göre '/' ile ayrılmış göreli yollar, alfabetik sırada.</summary>
    public IReadOnlyList<PlannedFile> Files { get; }

    /// <summary>Dosyaların bulunduğu tüm klasörler (kök hariç), alfabetik sırada.</summary>
    public IReadOnlyList<string> Directories { get; }

    public PlannedFile? FindFile(string relativePath) =>
        Files.FirstOrDefault(file => string.Equals(file.RelativePath, relativePath, StringComparison.OrdinalIgnoreCase));

    private static IEnumerable<string> GetParentDirectories(string relativePath)
    {
        var index = relativePath.IndexOf('/');
        while (index > 0)
        {
            yield return relativePath[..index];
            index = relativePath.IndexOf('/', index + 1);
        }
    }
}

/// <param name="RelativePath">Çözüm kök klasörüne göre '/' ile ayrılmış yol.</param>
public sealed record PlannedFile(string RelativePath, string Content)
{
    public string FileName => RelativePath[(RelativePath.LastIndexOf('/') + 1)..];
}

/// <param name="Name">Proje adı, örneğin "Firma.Proje.Business".</param>
/// <param name="RelativePath">Çözüm kök klasörüne göre .csproj yolu.</param>
/// <param name="SolutionFolder">Çözümdeki sanal klasör: "src" ya da "tests".</param>
/// <param name="Layer">Katman adı, örneğin "Business".</param>
public sealed record PlannedProject(string Name, string RelativePath, string SolutionFolder, string Layer);

/// <param name="SolutionDirectory">Çözümün yazıldığı tam klasör yolu.</param>
/// <param name="SolutionFilePath">Çözüm dosyasının tam yolu.</param>
public sealed record GenerationResult(string SolutionDirectory, string SolutionFilePath, GenerationPlan Plan);

public readonly record struct GenerationProgress(int Completed, int Total, string CurrentFile);
