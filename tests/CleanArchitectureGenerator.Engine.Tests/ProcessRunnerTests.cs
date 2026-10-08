using System.Collections.Concurrent;
using System.Diagnostics;
using CleanArchitectureGenerator.Engine.PostActions;

namespace CleanArchitectureGenerator.Engine.Tests;

public class ProcessRunnerTests
{
    /// <summary>
    /// dotnet build'in başlattığı kalıcı MSBuild düğümleri çıktı borusunu miras alıp açık tutar.
    /// Çalıştırıcı, süreç çıktıktan sonra boru kapanana kadar süresiz beklememeli.
    /// </summary>
    [Fact]
    public async Task RunAsync_ChildKeepsOutputPipeOpen_ReturnsShortlyAfterExit()
    {
        if (!OperatingSystem.IsWindows())
        {
            return;
        }

        var lines = new ConcurrentQueue<string>();
        var stopwatch = Stopwatch.StartNew();

        // cmd hemen çıkar; "start /b" ile başlattığı ping ise ~15 sn boyunca çıktı borusunu açık tutar.
        var exitCode = await ProcessRunner.RunAsync(
            "cmd.exe", ["/c", "start /b ping -n 15 127.0.0.1 >nul & echo bitti"], Path.GetTempPath(), lines.Enqueue);

        Assert.Equal(0, exitCode);
        Assert.Contains("bitti", lines);
        Assert.True(stopwatch.Elapsed < TimeSpan.FromSeconds(10), $"RunAsync {stopwatch.Elapsed.TotalSeconds:0.0} sn sürdü.");
    }

    [Fact]
    public async Task RunAsync_MissingExecutable_ReturnsMinusOne()
    {
        var lines = new ConcurrentQueue<string>();

        var exitCode = await ProcessRunner.RunAsync("bu-program-yok-cag-12345", [], Path.GetTempPath(), lines.Enqueue);

        Assert.Equal(-1, exitCode);
        Assert.Single(lines);
    }
}
