using System.Reflection;

namespace NTierGenerator.Engine.Templating;

/// <summary>
/// Seçeneklerden türetilen şablon sembolleri (koşullar için) ve token değerleri.
/// </summary>
internal sealed class TemplateContext
{
    public static readonly IReadOnlySet<string> KnownSymbols = new HashSet<string>(StringComparer.Ordinal)
    {
        "WebApi", "WebUi", "Auth", "Sample", "Tests", "SqlServer", "PostgreSql", "Sqlite"
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
        var symbols = new HashSet<string>(StringComparer.Ordinal);
        AddIf(symbols, "WebApi", options.IncludeWebApi);
        AddIf(symbols, "WebUi", options.IncludeWebUi);
        AddIf(symbols, "Auth", options.UsesAuthentication);
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
            ["ContextName"] = shortName + "DbContext",
            ["DatabaseName"] = database.DatabaseName,
            ["ConnectionString"] = database.JsonConnectionString,
            ["DbPackage"] = database.PackageId,
            ["DbUseMethod"] = database.UseMethod,
            ["DbDisplayName"] = database.DisplayName,
            ["StartupProject"] = options.IncludeWebApi ? "WebAPI" : "WebUI",
            ["SolutionFile"] = name + (options.SolutionFormat == SolutionFormat.Slnx ? ".slnx" : ".sln"),
            ["JwtSecret"] = random.JwtSecret,
            ["ApiHttpPort"] = random.ApiHttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["ApiHttpsPort"] = random.ApiHttpsPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["UiHttpPort"] = random.UiHttpPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["UiHttpsPort"] = random.UiHttpsPort.ToString(System.Globalization.CultureInfo.InvariantCulture),
            ["GeneratorVersion"] = GetGeneratorVersion()
        };

        return new TemplateContext(symbols, tokens);
    }

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

    /// <param name="JsonConnectionString">appsettings.json içine yazılacağı için JSON kaçışlı bağlantı cümlesi.</param>
    private sealed record DatabaseSettings(string DisplayName, string PackageId, string UseMethod, string DatabaseName, string JsonConnectionString)
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
                    databaseName => $@"Server=(localdb)\\MSSQLLocalDB;Database={databaseName};Trusted_Connection=True;MultipleActiveResultSets=true;TrustServerCertificate=True")
            };
        }

        private static DatabaseSettings Create(string displayName, string packageId, string useMethod, string databaseName, Func<string, string> connectionString) =>
            new(displayName, packageId, useMethod, databaseName, connectionString(databaseName));
    }
}
