namespace NTierGenerator.Engine;

/// <summary>
/// Üretilecek çözümün ayarları.
/// </summary>
public sealed record GeneratorOptions
{
    /// <summary>Çözüm adı ve kök namespace, örneğin "Firma.Proje".</summary>
    public required string SolutionName { get; init; }

    /// <summary>Çözüm klasörünün oluşturulacağı üst klasör.</summary>
    public required string OutputDirectory { get; init; }

    public DatabaseProvider Database { get; init; } = DatabaseProvider.SqlServer;

    public SolutionFormat SolutionFormat { get; init; } = SolutionFormat.Sln;

    public bool IncludeWebApi { get; init; } = true;

    public bool IncludeWebUi { get; init; }

    /// <summary>JWT kimlik doğrulama altyapısı. Yalnızca Web API ile birlikte kullanılabilir.</summary>
    public bool IncludeAuthentication { get; init; } = true;

    /// <summary>Category ve Product üzerinden tüm katmanları gösteren örnek modül.</summary>
    public bool IncludeSampleModule { get; init; } = true;

    public bool IncludeTests { get; init; } = true;

    /// <summary>Hedef klasör boş değilse var olan dosyaların üzerine yazılmasına izin verir.</summary>
    public bool OverwriteExisting { get; init; }

    /// <summary>Port numaraları ve JWT anahtarı gibi projeye özel rastgele değerler.</summary>
    public ProjectRandomValues RandomValues { get; init; } = ProjectRandomValues.Create();

    /// <summary>Kimlik doğrulama, Web API seçili değilse devre dışı kalır.</summary>
    public bool UsesAuthentication => IncludeAuthentication && IncludeWebApi;

    public string SolutionDirectory => Path.Combine(OutputDirectory, SolutionName);
}
