using System.Globalization;
using System.Reflection;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace CleanArchitectureGenerator.Engine.Templating;

/// <summary>
/// Seçeneklerden türetilen şablon sembolleri (koşullar için) ve token değerleri.
/// </summary>
internal sealed class TemplateContext
{
    public static readonly IReadOnlySet<string> KnownSymbols = new HashSet<string>(StringComparer.Ordinal)
    {
        "LocalAuth", "ExternalAuth", "Sample", "Tests", "SqlServer", "PostgreSql", "Sqlite"
    };

    private TemplateContext(IReadOnlySet<string> symbols, IReadOnlyDictionary<string, string> tokens)
    {
        Symbols = symbols;
        Tokens = tokens;
    }

    /// <summary>Bu çözüm için geçerli (true) olan semboller.</summary>
    public IReadOnlySet<string> Symbols { get; }

    public IReadOnlyDictionary<string, string> Tokens { get; }

    public static TemplateContext Create(GeneratorOptions options)
    {
        var symbols = new HashSet<string>(StringComparer.Ordinal)
        {
            options.Authentication == AuthenticationMode.External ? "ExternalAuth" : "LocalAuth"
        };
        AddIf(symbols, "Sample", options.IncludeSampleModule);
        AddIf(symbols, "Tests", options.IncludeTests);
        symbols.Add(options.Database switch
        {
            DatabaseProvider.PostgreSql => "PostgreSql",
            DatabaseProvider.Sqlite => "Sqlite",
            _ => "SqlServer"
        });

        var name = options.SolutionName;
        var shortName = name[(name.LastIndexOf('.') + 1)..];
        var database = DatabaseSettings.For(options.Database, name);
        var random = options.RandomValues;

        var tokens = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["Name"] = name,
            ["ShortName"] = shortName,
            ["DatabaseName"] = database.DatabaseName,
            ["ConnectionString"] = JsonEncode(database.ConnectionString),
            ["DbPackage"] = database.PackageId,
            ["DbUseMethod"] = database.UseMethod,
            ["DbDisplayName"] = database.DisplayName,
            ["StartupProject"] = ProjectCatalog.StartupLayer,
            ["SolutionFile"] = name + (options.SolutionFormat == SolutionFormat.Slnx ? ".slnx" : ".sln"),
            ["UserSecretsId"] = random.UserSecretsId.ToString(),
            ["ApiHttpPort"] = random.ApiHttpPort.ToString(CultureInfo.InvariantCulture),
            ["ApiHttpsPort"] = random.ApiHttpsPort.ToString(CultureInfo.InvariantCulture),
            ["GeneratorVersion"] = GetGeneratorVersion()
        };

        return new TemplateContext(symbols, tokens);
    }

    /// <summary>
    /// Geliştirme ortamının gizli değerleri: depoya girmemeleri için şablonlara değil, user-secrets'a yazılır.
    /// Anahtarlar .NET yapılandırma yolu biçimindedir ("Bölüm:Ayar").
    /// </summary>
    public static IReadOnlyDictionary<string, string> CreateDevelopmentSecrets(GeneratorOptions options)
    {
        var secrets = new SortedDictionary<string, string>(StringComparer.Ordinal);
        if (options.Authentication == AuthenticationMode.Local)
        {
            secrets["Jwt:SigningKey"] = options.RandomValues.JwtSigningKey;
        }

        // PostgreSQL bağlantı cümlesi parola içerdiği için appsettings.Development.json yerine burada tutulur.
        if (options.Database == DatabaseProvider.PostgreSql)
        {
            secrets["ConnectionStrings:DefaultConnection"] = DatabaseSettings.For(options.Database, options.SolutionName).ConnectionString;
        }

        return secrets;
    }

    private static string JsonEncode(string value) =>
        JsonEncodedText.Encode(value, JavaScriptEncoder.UnsafeRelaxedJsonEscaping).ToString();

    private static void AddIf(HashSet<string> symbols, string symbol, bool condition)
    {
        if (condition)
        {
            symbols.Add(symbol);
        }
    }

    private static string GetGeneratorVersion()
    {
        var version = typeof(TemplateContext).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion ?? "1.0.0";
        var metadataIndex = version.IndexOf('+');
        return metadataIndex > 0 ? version[..metadataIndex] : version;
    }

    /// <param name="ConnectionString">Yerel geliştirme veritabanının bağlantı cümlesi.</param>
    private sealed record DatabaseSettings(string DisplayName, string PackageId, string UseMethod, string DatabaseName, string ConnectionString)
    {
        public static DatabaseSettings For(DatabaseProvider provider, string solutionName)
        {
            var compactName = solutionName.Replace(".", string.Empty, StringComparison.Ordinal);
            return provider switch
            {
                DatabaseProvider.PostgreSql => Create(
                    "PostgreSQL",
                    "Npgsql.EntityFrameworkCore.PostgreSQL",
                    "UseNpgsql",
                    solutionName.Replace('.', '_').ToLowerInvariant(),
                    databaseName => $"Host=localhost;Port=5432;Database={databaseName};Username=postgres;Password=postgres"),
                DatabaseProvider.Sqlite => Create(
                    "SQLite",
                    "Microsoft.EntityFrameworkCore.Sqlite",
                    "UseSqlite",
                    compactName,
                    databaseName => $"Data Source={databaseName}.db"),
                _ => Create(
                    "SQL Server",
                    "Microsoft.EntityFrameworkCore.SqlServer",
                    "UseSqlServer",
                    compactName,
                    databaseName => $@"Server=(localdb)\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            };
        }

        private static DatabaseSettings Create(string displayName, string packageId, string useMethod, string databaseName, Func<string, string> connectionString) =>
            new(displayName, packageId, useMethod, databaseName, connectionString(databaseName));
    }
}
