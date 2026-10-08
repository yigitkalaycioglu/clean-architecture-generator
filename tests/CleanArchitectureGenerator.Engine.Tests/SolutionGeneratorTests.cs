using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace CleanArchitectureGenerator.Engine.Tests;

public partial class SolutionGeneratorTests
{
    private const string SolutionName = "Acme.Shop";

    private readonly SolutionGenerator _generator = new();

    [GeneratedRegex("__[A-Z][A-Za-z0-9]*__")]
    private static partial Regex LeftoverTokenRegex();

    /// <summary>Satırın herhangi bir yerinde kalmış şablon yönergesi (yalnızca satır başında değil).</summary>
    [GeneratedRegex(@"(//|<!--|@\*)#(if|elif|else|endif)\b")]
    private static partial Regex LeftoverDirectiveRegex();

    /// <summary>Tüm seçenek kombinasyonları: her veritabanı, kimlik doğrulama türü ve çözüm biçimiyle.</summary>
    public static TheoryData<GeneratorOptions> AllOptionCombinations()
    {
        var data = new TheoryData<GeneratorOptions>();
        foreach (var database in Enum.GetValues<DatabaseProvider>())
        {
            foreach (var authentication in Enum.GetValues<AuthenticationMode>())
            {
                for (var flags = 0; flags < 8; flags++)
                {
                    data.Add(CreateOptions() with
                    {
                        Database = database,
                        Authentication = authentication,
                        IncludeSampleModule = (flags & 1) != 0,
                        IncludeTests = (flags & 2) != 0,
                        SolutionFormat = (flags & 4) != 0 ? SolutionFormat.Slnx : SolutionFormat.Sln
                    });
                }
            }
        }

        return data;
    }

    [Theory]
    [MemberData(nameof(AllOptionCombinations))]
    public void CreatePlan_AnyCombination_ProducesConsistentSolution(GeneratorOptions options)
    {
        var plan = _generator.CreatePlan(options);

        foreach (var file in plan.Files)
        {
            Assert.DoesNotMatch(LeftoverTokenRegex(), file.RelativePath);
            Assert.False(LeftoverTokenRegex().IsMatch(file.Content), $"{file.RelativePath} içinde değiştirilmemiş token var.");
            Assert.False(LeftoverDirectiveRegex().IsMatch(file.Content), $"{file.RelativePath} içinde şablon yönergesi kaldı.");

            if (file.RelativePath.EndsWith(".json", StringComparison.Ordinal))
            {
                using var _ = JsonDocument.Parse(file.Content);
            }
            else if (file.RelativePath.EndsWith(".csproj", StringComparison.Ordinal) || file.RelativePath.EndsWith(".props", StringComparison.Ordinal)
                     || file.RelativePath.EndsWith(".config", StringComparison.Ordinal) || file.RelativePath.EndsWith(".slnx", StringComparison.Ordinal))
            {
                XDocument.Parse(file.Content);
            }
            else if (file.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && !file.RelativePath.EndsWith("Program.cs", StringComparison.Ordinal))
            {
                Assert.Contains($"namespace {SolutionName}.", file.Content);
            }
        }

        string[] expectedLayers = options.IncludeTests
            ? ["Domain", "Application", "Infrastructure", "Api", "UnitTests", "ArchitectureTests", "IntegrationTests"]
            : ["Domain", "Application", "Infrastructure", "Api"];
        Assert.Equal(expectedLayers.Order(), plan.Projects.Select(project => project.Layer).Order());

        var solutionFile = plan.FindFile(plan.SolutionFileName);
        Assert.NotNull(solutionFile);
        foreach (var project in plan.Projects)
        {
            Assert.NotNull(plan.FindFile(project.RelativePath));
            var pathInSolution = options.SolutionFormat == SolutionFormat.Sln ? project.RelativePath.Replace('/', '\\') : project.RelativePath;
            Assert.Contains(pathInSolution, solutionFile.Content);
        }

        var isLocal = options.Authentication == AuthenticationMode.Local;
        Assert.Equal(isLocal, plan.FindFile("src/Acme.Shop.Infrastructure/Identity/IdentityService.cs") is not null);
        Assert.Equal(isLocal, plan.FindFile("src/Acme.Shop.Api/Endpoints/AuthEndpoints.cs") is not null);
        Assert.Equal(!isLocal, plan.FindFile("src/Acme.Shop.Infrastructure/Authentication/ExternalAuthenticationOptions.cs") is not null);
        Assert.Equal(options.IncludeSampleModule, plan.FindFile("src/Acme.Shop.Domain/TodoItems/TodoItem.cs") is not null);

        // PostgreSQL bağlantı cümlesi parola içerdiği için yapılandırma dosyasına değil, user-secrets'a yazılır.
        var developmentSettings = plan.FindFile("src/Acme.Shop.Api/appsettings.Development.json")!.Content;
        Assert.Equal(options.Database != DatabaseProvider.PostgreSql, developmentSettings.Contains("DefaultConnection", StringComparison.Ordinal));
    }

    [Theory]
    [MemberData(nameof(AllOptionCombinations))]
    public void CreatePlan_NeverWritesSecretsIntoTheSolution(GeneratorOptions options)
    {
        var plan = _generator.CreatePlan(options);

        Assert.DoesNotContain(plan.Files, file => file.Content.Contains(options.RandomValues.JwtSigningKey, StringComparison.Ordinal));
        Assert.DoesNotContain(plan.Files, file => file.Content.Contains("Password=postgres", StringComparison.Ordinal));
        Assert.Contains(options.RandomValues.UserSecretsId.ToString(), plan.FindFile("src/Acme.Shop.Api/Acme.Shop.Api.csproj")!.Content);
    }

    [Fact]
    public void CreatePlan_DefaultOptions_ContainsCleanArchitectureStructure()
    {
        var plan = _generator.CreatePlan(CreateOptions());

        string[] expectedFiles =
        [
            "Acme.Shop.sln",
            "Directory.Build.props",
            "Directory.Packages.props",
            "nuget.config",
            "README.md",
            "SECURITY.md",
            ".editorconfig",
            ".gitignore",
            ".config/dotnet-tools.json",
            "src/Acme.Shop.Domain/Common/Result.cs",
            "src/Acme.Shop.Domain/TodoItems/TodoItem.cs",
            "src/Acme.Shop.Application/Messaging/Sender.cs",
            "src/Acme.Shop.Application/Behaviors/ValidationBehavior.cs",
            "src/Acme.Shop.Application/TodoItems/CreateTodoItem.cs",
            "src/Acme.Shop.Infrastructure/Persistence/ApplicationDbContext.cs",
            "src/Acme.Shop.Infrastructure/Identity/IdentityService.Sessions.cs",
            "src/Acme.Shop.Api/Common/SecurityHeaders.cs",
            "src/Acme.Shop.Api/Endpoints/TodoItemsEndpoints.cs",
            "tests/Acme.Shop.ArchitectureTests/LayerDependencyTests.cs",
            "tests/Acme.Shop.IntegrationTests/AuthenticationTests.cs"
        ];

        foreach (var expectedFile in expectedFiles)
        {
            Assert.NotNull(plan.FindFile(expectedFile));
        }

        var readme = plan.FindFile("README.md")!.Content;
        Assert.DoesNotContain("__FolderTree__", readme);
        Assert.Contains("├── src/", readme);
    }

    [Fact]
    public void CreatePlan_KeepsEmptyFoldersWithGitKeepOnlyWhenNeeded()
    {
        var minimal = _generator.CreatePlan(CreateOptions() with { Authentication = AuthenticationMode.External, IncludeSampleModule = false });
        var full = _generator.CreatePlan(CreateOptions());

        Assert.NotNull(minimal.FindFile("src/Acme.Shop.Infrastructure/Persistence/Configurations/.gitkeep"));
        Assert.Null(full.FindFile("src/Acme.Shop.Infrastructure/Persistence/Configurations/.gitkeep"));

        // Migration klasörü ilk "dotnet ef migrations add" komutuna kadar boştur.
        Assert.NotNull(full.FindFile("src/Acme.Shop.Infrastructure/Persistence/Migrations/.gitkeep"));
    }

    /// <summary>
    /// Visual Studio ilk projeyi başlangıç projesi yapar; sınıf kitaplığı ilk sırada olursa F5
    /// "Çıkış türü sınıf kitaplığı olan bir proje doğrudan başlatılamaz" hatası verir.
    /// </summary>
    [Theory]
    [InlineData(SolutionFormat.Sln)]
    [InlineData(SolutionFormat.Slnx)]
    public void CreatePlan_SolutionFile_ListsApiProjectFirst(SolutionFormat format)
    {
        var plan = _generator.CreatePlan(CreateOptions() with { SolutionFormat = format });

        var solution = plan.FindFile(plan.SolutionFileName)!.Content;
        var firstProjectLine = solution.Split("\r\n").First(line =>
            format == SolutionFormat.Sln
                ? line.StartsWith("Project(", StringComparison.Ordinal)
                : line.TrimStart().StartsWith("<Project ", StringComparison.Ordinal));

        Assert.Contains($"{SolutionName}.Api.csproj", firstProjectLine);
    }

    [Fact]
    public void CreatePlan_SameOptions_IsDeterministic()
    {
        var options = CreateOptions();

        var first = _generator.CreatePlan(options);
        var second = _generator.CreatePlan(options);

        Assert.Equal(first.Files, second.Files);
    }

    [Fact]
    public void CreatePlan_InvalidName_Throws()
    {
        Assert.Throws<ArgumentException>(() => _generator.CreatePlan(CreateOptions() with { SolutionName = "Acme.Result" }));
    }

    [Fact]
    public async Task GenerateAsync_WritesFilesAndSecrets_AndRefusesNonEmptyDirectory()
    {
        var root = Path.Combine(Path.GetTempPath(), "cag-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = CreateOptions() with { OutputDirectory = Path.Combine(root, "out"), UserSecretsRoot = Path.Combine(root, "secrets") };

            var result = await _generator.GenerateAsync(options);

            Assert.True(File.Exists(result.SolutionFilePath));
            Assert.Equal(result.Plan.Files.Count, Directory.EnumerateFiles(result.SolutionDirectory, "*", SearchOption.AllDirectories).Count());
            Assert.Equal(Path.Combine(root, "secrets", options.RandomValues.UserSecretsId.ToString(), "secrets.json"), result.UserSecretsFilePath);

            using var secrets = JsonDocument.Parse(await File.ReadAllTextAsync(result.UserSecretsFilePath!));
            Assert.Equal(options.RandomValues.JwtSigningKey, secrets.RootElement.GetProperty("Jwt:SigningKey").GetString());

            await Assert.ThrowsAsync<IOException>(() => _generator.GenerateAsync(options));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_KeepsExistingSecretValues()
    {
        var root = Path.Combine(Path.GetTempPath(), "cag-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = CreateOptions() with { OutputDirectory = Path.Combine(root, "out"), UserSecretsRoot = Path.Combine(root, "secrets") };
            var secretsFile = Path.Combine(root, "secrets", options.RandomValues.UserSecretsId.ToString(), "secrets.json");
            Directory.CreateDirectory(Path.GetDirectoryName(secretsFile)!);
            await File.WriteAllTextAsync(secretsFile, """{ "Jwt:SigningKey": "kullanicinin-degeri", "Baska:Ayar": "1" }""");

            await _generator.GenerateAsync(options);

            using var secrets = JsonDocument.Parse(await File.ReadAllTextAsync(secretsFile));
            Assert.Equal("kullanicinin-degeri", secrets.RootElement.GetProperty("Jwt:SigningKey").GetString());
            Assert.Equal("1", secrets.RootElement.GetProperty("Baska:Ayar").GetString());
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    [Fact]
    public async Task GenerateAsync_WithoutSecrets_DoesNotCreateSecretsFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "cag-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = CreateOptions() with
            {
                OutputDirectory = Path.Combine(root, "out"),
                UserSecretsRoot = Path.Combine(root, "secrets"),
                Authentication = AuthenticationMode.External,
                Database = DatabaseProvider.Sqlite
            };

            var result = await _generator.GenerateAsync(options);

            Assert.Null(result.UserSecretsFilePath);
            Assert.False(Directory.Exists(Path.Combine(root, "secrets")));
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }

    private static GeneratorOptions CreateOptions() => new()
    {
        SolutionName = SolutionName,
        OutputDirectory = Path.GetTempPath(),
        RandomValues = new ProjectRandomValues(Convert.ToBase64String(new byte[64]).Replace('A', 'k'), 5001, 7001, Guid.Parse("8f7c2a8e-58a4-4f3b-9d61-2f0f4b8a1c11"))
    };
}
