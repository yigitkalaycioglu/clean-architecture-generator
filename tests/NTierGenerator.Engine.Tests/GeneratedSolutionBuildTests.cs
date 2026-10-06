using NTierGenerator.Engine.PostActions;
using Xunit.Abstractions;

namespace NTierGenerator.Engine.Tests;

/// <summary>
/// Üretilen çözümü gerçekten derler ve testlerini çalıştırır. NuGet erişimi gerektirdiği ve
/// birkaç dakika sürdüğü için varsayılan olarak atlanır; çalıştırmak için:
/// <code>$env:NTIER_BUILD_TESTS = "1"; dotnet test</code>
/// </summary>
public class GeneratedSolutionBuildTests(ITestOutputHelper output)
{
    [BuildTheory]
    [InlineData("Full", DatabaseProvider.SqlServer, true, true, true, true, true)]
    [InlineData("Minimal", DatabaseProvider.Sqlite, true, false, false, false, false)]
    [InlineData("MvcOnly", DatabaseProvider.PostgreSql, false, true, false, true, true)]
    [InlineData("ApiAuthWithoutSample", DatabaseProvider.SqlServer, true, false, true, false, true)]
    [InlineData("ApiSampleWithoutAuth", DatabaseProvider.Sqlite, true, false, false, true, true)]
    public async Task GeneratedSolution_BuildsWithoutErrorsAndTestsPass(
        string scenario, DatabaseProvider database, bool webApi, bool webUi, bool auth, bool sample, bool tests)
    {
        var outputDirectory = Path.Combine(Path.GetTempPath(), "ntier-build-" + Guid.NewGuid().ToString("N"));
        try
        {
            var options = new GeneratorOptions
            {
                SolutionName = "Acme." + scenario,
                OutputDirectory = outputDirectory,
                Database = database,
                IncludeWebApi = webApi,
                IncludeWebUi = webUi,
                IncludeAuthentication = auth,
                IncludeSampleModule = sample,
                IncludeTests = tests
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
            if (Directory.Exists(outputDirectory))
            {
                Directory.Delete(outputDirectory, recursive: true);
            }
        }
    }
}

public sealed class BuildTheoryAttribute : TheoryAttribute
{
    public BuildTheoryAttribute()
    {
        if (Environment.GetEnvironmentVariable("NTIER_BUILD_TESTS") != "1")
        {
            Skip = "Derleme testleri için NTIER_BUILD_TESTS=1 ayarlayın.";
        }
    }
}
