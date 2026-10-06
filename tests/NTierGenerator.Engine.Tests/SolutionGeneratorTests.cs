using System.Text.Json;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace NTierGenerator.Engine.Tests;

public partial class SolutionGeneratorTests
{
    private const string SolutionName = "Acme.Shop";

    private readonly SolutionGenerator _generator = new();

    [GeneratedRegex("__[A-Z][A-Za-z0-9]*__")]
    private static partial Regex LeftoverTokenRegex();

    [GeneratedRegex(@"^\s*(//|<!--|@\*)#(if|elif|else|endif)\b", RegexOptions.Multiline)]
    private static partial Regex LeftoverDirectiveRegex();

    /// <summary>Geçerli tüm seçenek kombinasyonları (en az bir sunum katmanı), her veritabanı ve çözüm biçimiyle.</summary>
    public static TheoryData<GeneratorOptions> AllOptionCombinations()
    {
        var data = new TheoryData<GeneratorOptions>();
        foreach (var database in Enum.GetValues<DatabaseProvider>())
        {
            for (var flags = 0; flags < 64; flags++)
            {
                bool Flag(int bit) => (flags & (1 << bit)) != 0;
                if (!Flag(0) && !Flag(1))
                {
                    continue;
                }

                data.Add(new GeneratorOptions
                {
                    SolutionName = SolutionName,
                    OutputDirectory = Path.GetTempPath(),
                    Database = database,
                    SolutionFormat = Flag(5) ? SolutionFormat.Slnx : SolutionFormat.Sln,
                    IncludeWebApi = Flag(0),
                    IncludeWebUi = Flag(1),
                    IncludeAuthentication = Flag(2),
                    IncludeSampleModule = Flag(3),
                    IncludeTests = Flag(4)
                });
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
            else if (file.RelativePath.EndsWith(".csproj", StringComparison.Ordinal) || file.RelativePath.EndsWith(".props", StringComparison.Ordinal))
            {
                XDocument.Parse(file.Content);
            }
            else if (file.RelativePath.EndsWith(".cs", StringComparison.Ordinal) && !file.RelativePath.EndsWith("Program.cs", StringComparison.Ordinal))
            {
                Assert.Contains($"namespace {SolutionName}.", file.Content);
            }
        }

        var expectedLayers = new List<string> { "Core", "Entities", "DataAccess", "Business" };
        if (options.IncludeWebApi) expectedLayers.Add("WebAPI");
        if (options.IncludeWebUi) expectedLayers.Add("WebUI");
        if (options.IncludeTests) expectedLayers.Add("Tests");
        Assert.Equal(expectedLayers.Order(), plan.Projects.Select(project => project.Layer).Order());

        var solutionFile = plan.FindFile(plan.SolutionFileName);
        Assert.NotNull(solutionFile);
        foreach (var project in plan.Projects)
        {
            Assert.NotNull(plan.FindFile(project.RelativePath));
            var pathInSolution = options.SolutionFormat == SolutionFormat.Sln ? project.RelativePath.Replace('/', '\\') : project.RelativePath;
            Assert.Contains(pathInSolution, solutionFile.Content);
        }

        var usesAuth = options.IncludeAuthentication && options.IncludeWebApi;
        Assert.Equal(usesAuth, plan.Files.Any(file => file.Content.Contains("SecuredOperation", StringComparison.Ordinal)));
        Assert.Equal(options.IncludeSampleModule, plan.FindFile("src/Acme.Shop.Entities/Concrete/Product.cs") is not null);
    }

    [Fact]
    public void CreatePlan_DefaultOptions_ContainsGoldStandardStructure()
    {
        var plan = _generator.CreatePlan(new GeneratorOptions { SolutionName = SolutionName, OutputDirectory = Path.GetTempPath() });

        string[] expectedFiles =
        [
            "Acme.Shop.sln",
            "Directory.Build.props",
            "Directory.Packages.props",
            ".editorconfig",
            ".gitignore",
            ".config/dotnet-tools.json",
            "src/Acme.Shop.Core/Utilities/Results/IDataResult.cs",
            "src/Acme.Shop.Core/DataAccess/EntityFramework/EfEntityRepositoryBase.cs",
            "src/Acme.Shop.Core/Aspects/Autofac/Validation/ValidationAspect.cs",
            "src/Acme.Shop.DataAccess/Concrete/EntityFramework/Contexts/ShopDbContext.cs",
            "src/Acme.Shop.Business/DependencyResolvers/Autofac/AutofacBusinessModule.cs",
            "src/Acme.Shop.Business/BusinessAspects/Autofac/SecuredOperation.cs",
            "src/Acme.Shop.WebAPI/Controllers/ProductsController.cs",
            "tests/Acme.Shop.Tests/Business/ProductManagerTests.cs"
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
    public void CreatePlan_WithoutSampleModule_KeepsEmptyFoldersWithGitKeep()
    {
        var plan = _generator.CreatePlan(new GeneratorOptions
        {
            SolutionName = SolutionName,
            OutputDirectory = Path.GetTempPath(),
            IncludeSampleModule = false,
            IncludeAuthentication = false
        });

        Assert.NotNull(plan.FindFile("src/Acme.Shop.Entities/Concrete/.gitkeep"));
        Assert.NotNull(plan.FindFile("src/Acme.Shop.Business/Abstract/.gitkeep"));
        Assert.NotNull(plan.FindFile("src/Acme.Shop.WebAPI/Controllers/.gitkeep"));

        // Örnek modül ya da Auth açıkken aynı klasörlerde gerçek dosya olduğu için .gitkeep üretilmez.
        var fullPlan = _generator.CreatePlan(new GeneratorOptions { SolutionName = SolutionName, OutputDirectory = Path.GetTempPath() });
        Assert.DoesNotContain(fullPlan.Files, file => file.FileName == ".gitkeep");
    }

    /// <summary>
    /// Visual Studio ilk projeyi başlangıç projesi yapar; sınıf kitaplığı ilk sırada olursa F5
    /// "Çıkış türü sınıf kitaplığı olan bir proje doğrudan başlatılamaz" hatası verir.
    /// </summary>
    [Theory]
    [InlineData(true, true, SolutionFormat.Sln, "WebAPI")]
    [InlineData(true, false, SolutionFormat.Sln, "WebAPI")]
    [InlineData(false, true, SolutionFormat.Sln, "WebUI")]
    [InlineData(true, true, SolutionFormat.Slnx, "WebAPI")]
    [InlineData(false, true, SolutionFormat.Slnx, "WebUI")]
    public void CreatePlan_SolutionFile_ListsWebProjectFirst(bool webApi, bool webUi, SolutionFormat format, string expectedLayer)
    {
        var plan = _generator.CreatePlan(new GeneratorOptions
        {
            SolutionName = SolutionName,
            OutputDirectory = Path.GetTempPath(),
            IncludeWebApi = webApi,
            IncludeWebUi = webUi,
            SolutionFormat = format
        });

        var solution = plan.FindFile(plan.SolutionFileName)!.Content;
        var firstProjectLine = solution.Split("\r\n").First(line =>
            format == SolutionFormat.Sln
                ? line.StartsWith("Project(", StringComparison.Ordinal)
                : line.TrimStart().StartsWith("<Project ", StringComparison.Ordinal));

        Assert.Contains($"{SolutionName}.{expectedLayer}.csproj", firstProjectLine);
    }

    [Fact]
    public void CreatePlan_SameOptions_IsDeterministic()
    {
        var options = new GeneratorOptions { SolutionName = SolutionName, OutputDirectory = Path.GetTempPath() };

        var first = _generator.CreatePlan(options);
        var second = _generator.CreatePlan(options);

        Assert.Equal(first.Files, second.Files);
    }

    [Fact]
    public void CreatePlan_WithoutPresentationLayer_Throws()
    {
        var options = new GeneratorOptions
        {
            SolutionName = SolutionName,
            OutputDirectory = Path.GetTempPath(),
            IncludeWebApi = false,
            IncludeWebUi = false
        };

        Assert.Throws<ArgumentException>(() => _generator.CreatePlan(options));
    }

    [Fact]
    public async Task GenerateAsync_WritesFilesAndRefusesNonEmptyDirectory()
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ntier-tests-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new GeneratorOptions { SolutionName = SolutionName, OutputDirectory = outputDirectory };

            var result = await _generator.GenerateAsync(options);

            Assert.True(File.Exists(result.SolutionFilePath));
            Assert.Equal(result.Plan.Files.Count, Directory.EnumerateFiles(result.SolutionDirectory, "*", SearchOption.AllDirectories).Count());
            await Assert.ThrowsAsync<IOException>(() => _generator.GenerateAsync(options));
        }
        finally
        {
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}
