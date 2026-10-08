using CleanArchitectureGenerator.Engine.PostActions;
using Xunit.Abstractions;

namespace CleanArchitectureGenerator.Engine.Tests;

/// <summary>
/// Üretilen çözümü gerçekten derler ve testlerini çalıştırır. NuGet erişimi gerektirdiği ve
/// birkaç dakika sürdüğü için varsayılan olarak atlanır; çalıştırmak için:
/// <code>$env:CAG_BUILD_TESTS = "1"; dotnet test</code>
/// </summary>
public class GeneratedSolutionBuildTests(ITestOutputHelper output)
{
    [BuildTheory]
    [InlineData("LocalFull", DatabaseProvider.SqlServer, AuthenticationMode.Local, true, true, SolutionFormat.Sln)]
    [InlineData("ExternalFull", DatabaseProvider.PostgreSql, AuthenticationMode.External, true, true, SolutionFormat.Sln)]
    [InlineData("LocalMinimal", DatabaseProvider.Sqlite, AuthenticationMode.Local, false, true, SolutionFormat.Slnx)]
    [InlineData("ExternalMinimal", DatabaseProvider.SqlServer, AuthenticationMode.External, false, false, SolutionFormat.Sln)]
    [InlineData("LocalWithoutTests", DatabaseProvider.Sqlite, AuthenticationMode.Local, true, false, SolutionFormat.Sln)]
    public async Task GeneratedSolution_BuildsWithoutWarningsAndTestsPass(
        string scenario, DatabaseProvider database, AuthenticationMode authentication, bool sample, bool tests, SolutionFormat format)
    {
        // Kısa yol: derleme çıktıları Windows'un 260 karakter sınırına takılmasın.
        var root = Path.Combine(Path.GetTempPath(), "cag-" + Guid.NewGuid().ToString("N")[..8]);
        try
        {
            var options = new GeneratorOptions
            {
                SolutionName = "Acme." + scenario,
                OutputDirectory = root,
                Database = database,
                Authentication = authentication,
                IncludeSampleModule = sample,
                IncludeTests = tests,
                SolutionFormat = format,
                UserSecretsRoot = Path.Combine(root, "secrets")
            };

            var result = await new SolutionGenerator().GenerateAsync(options);

            var buildOutput = new List<string>();
            var buildSucceeded = await PostGenerationActions.BuildSolutionAsync(result.SolutionFilePath, line =>
            {
                buildOutput.Add(line);
                output.WriteLine(line);
            });

            Assert.True(buildSucceeded, $"{scenario}: dotnet build başarısız.");
            Assert.DoesNotContain(buildOutput, line => line.Contains(": warning ", StringComparison.Ordinal));

            if (tests)
            {
                var exitCode = await ProcessRunner.RunAsync(
                    "dotnet", ["test", result.SolutionFilePath, "--no-build", "--nologo", "--disable-build-servers"], result.SolutionDirectory, output.WriteLine);
                Assert.Equal(0, exitCode);
            }
        }
        finally
        {
            if (Directory.Exists(root))
            {
                Directory.Delete(root, recursive: true);
            }
        }
    }
}

public sealed class BuildTheoryAttribute : TheoryAttribute
{
    public BuildTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("CAG_BUILD_TESTS") != "1")
        {
            Skip = "Derleme testleri için CAG_BUILD_TESTS=1 ayarlayın.";
        }
    }
}
