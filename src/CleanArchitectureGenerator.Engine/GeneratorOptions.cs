namespace CleanArchitectureGenerator.Engine;

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

    public AuthenticationMode Authentication { get; init; } = AuthenticationMode.Local;

    /// <summary>Tüm katmanları uçtan uca gösteren örnek özellik (TodoItems).</summary>
    public bool IncludeSampleModule { get; init; } = true;

    /// <summary>Birim, mimari ve entegrasyon test projeleri.</summary>
    public bool IncludeTests { get; init; } = true;

    /// <summary>Hedef klasör boş değilse var olan dosyaların üzerine yazılmasına izin verir.</summary>
    public bool OverwriteExisting { get; init; }

    /// <summary>Port numaraları, JWT imzalama anahtarı ve user-secrets kimliği gibi projeye özel rastgele değerler.</summary>
    public ProjectRandomValues RandomValues { get; init; } = ProjectRandomValues.Create();

    /// <summary>
    /// Geliştirme gizli değerlerinin (secrets.json) yazılacağı kök klasör. Boşsa dotnet user-secrets'ın kullandığı
    /// klasör kullanılır (%APPDATA%\Microsoft\UserSecrets). Testler geçici bir klasör verir.
    /// </summary>
    public string? UserSecretsRoot { get; init; }

    public string SolutionDirectory => Path.Combine(OutputDirectory, SolutionName);
}
