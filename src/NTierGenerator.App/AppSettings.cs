using System.Text.Json;
using System.Text.Json.Serialization;
using NTierGenerator.Engine;

namespace NTierGenerator.App;

/// <summary>
/// Son kullanılan seçenekler; bir sonraki açılışta aynı ayarlarla başlanır.
/// %LOCALAPPDATA%\NTierGenerator\settings.json dosyasında tutulur.
/// </summary>
internal sealed class AppSettings
{
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() }
    };

    private static string FilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "NTierGenerator", "settings.json");

    public string SolutionName { get; set; } = "Firma.Proje";

    public string OutputDirectory { get; set; } = GetDefaultOutputDirectory();

    public DatabaseProvider Database { get; set; } = DatabaseProvider.SqlServer;

    public SolutionFormat SolutionFormat { get; set; } = SolutionFormat.Sln;

    public bool IncludeWebApi { get; set; } = true;

    public bool IncludeWebUi { get; set; }

    public bool IncludeAuthentication { get; set; } = true;

    public bool IncludeSampleModule { get; set; } = true;

    public bool IncludeTests { get; set; } = true;

    public bool InitializeGit { get; set; } = true;

    public bool BuildAfterGeneration { get; set; }

    public bool OpenFolderAfterGeneration { get; set; } = true;

    public static AppSettings Load()
    {
        try
        {
            if (File.Exists(FilePath))
            {
                return JsonSerializer.Deserialize<AppSettings>(File.ReadAllText(FilePath), JsonOptions) ?? new AppSettings();
            }
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException or JsonException)
        {
            // Bozuk ya da okunamayan ayar dosyası varsayılanlarla değiştirilir.
        }

        return new AppSettings();
    }

    public void Save()
    {
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(FilePath)!);
            File.WriteAllText(FilePath, JsonSerializer.Serialize(this, JsonOptions));
        }
        catch (Exception exception) when (exception is IOException or UnauthorizedAccessException)
        {
            // Ayarların kaydedilememesi uygulamanın çalışmasını engellemez.
        }
    }

    /// <summary>Visual Studio'nun varsayılan proje klasörü varsa o, yoksa Belgeler.</summary>
    private static string GetDefaultOutputDirectory()
    {
        var repos = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "source", "repos");
        return Directory.Exists(repos) ? repos : Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
    }
}
