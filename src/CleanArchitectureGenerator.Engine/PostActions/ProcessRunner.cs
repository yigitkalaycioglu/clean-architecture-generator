using System.ComponentModel;
using System.Diagnostics;
using System.Text;

namespace CleanArchitectureGenerator.Engine.PostActions;

/// <summary>
/// Üretim sonrası komutları (git, dotnet) çalıştırır ve çıktılarını satır satır iletir.
/// </summary>
public static class ProcessRunner
{
    /// <returns>Çıkış kodu. Program bulunamazsa -1.</returns>
    public static async Task<int> RunAsync(string fileName, IEnumerable<string> arguments, string workingDirectory, Action<string>? onOutput = null, CancellationToken cancellationToken = default)
    {
        var startInfo = new ProcessStartInfo(fileName)
        {
            WorkingDirectory = workingDirectory,
            RedirectStandardOutput = true,
            RedirectStandardError = true,
            UseShellExecute = false,
            CreateNoWindow = true,
            StandardOutputEncoding = Encoding.UTF8,
            StandardErrorEncoding = Encoding.UTF8
        };

        foreach (var argument in arguments)
        {
            startInfo.ArgumentList.Add(argument);
        }

        // dotnet çıktısı arayüzle aynı dilde (Türkçe) olsun, ilk çalıştırma ve logo mesajları gösterilmesin.
        startInfo.Environment["DOTNET_CLI_UI_LANGUAGE"] = "tr";
        startInfo.Environment["DOTNET_NOLOGO"] = "1";
        startInfo.Environment["DOTNET_SKIP_FIRST_TIME_EXPERIENCE"] = "1";

        using var process = new Process { StartInfo = startInfo, EnableRaisingEvents = true };
        var exited = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Exited += (_, _) => exited.TrySetResult();
        process.OutputDataReceived += (_, e) => Forward(e.Data);
        process.ErrorDataReceived += (_, e) => Forward(e.Data);

        try
        {
            process.Start();
        }
        catch (Win32Exception)
        {
            onOutput?.Invoke($"'{fileName}' bulunamadı. Kurulu ve PATH üzerinde olduğundan emin olun.");
            return -1;
        }

        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        try
        {
            await exited.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            process.Kill(entireProcessTree: true);
            throw;
        }

        // Kalan çıktı satırları için kısa bir süre beklenir. Süreç arka planda başka süreçler başlattıysa
        // (ör. kalıcı MSBuild düğümleri) bunlar çıktı borusunu açık tutabilir; bu yüzden süresiz beklenmez.
        using var drainTimeout = new CancellationTokenSource(TimeSpan.FromSeconds(3));
        try
        {
            await process.WaitForExitAsync(drainTimeout.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
        }

        return process.ExitCode;

        void Forward(string? line)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                onOutput?.Invoke(line);
            }
        }
    }
}

/// <summary>Çözüm üretildikten sonra isteğe bağlı olarak çalıştırılan adımlar.</summary>
public static class PostGenerationActions
{
    public static async Task<bool> InitializeGitRepositoryAsync(string solutionDirectory, Action<string>? onOutput = null, CancellationToken cancellationToken = default)
    {
        // "-b main" git 2.28+ ister; eski sürümlerde düz "git init" denenir.
        var exitCode = await ProcessRunner.RunAsync("git", ["init", "-b", "main"], solutionDirectory, onOutput, cancellationToken).ConfigureAwait(false);
        if (exitCode > 0)
        {
            exitCode = await ProcessRunner.RunAsync("git", ["init"], solutionDirectory, onOutput, cancellationToken).ConfigureAwait(false);
        }

        return exitCode == 0;
    }

    public static async Task<bool> BuildSolutionAsync(string solutionFilePath, Action<string>? onOutput = null, CancellationToken cancellationToken = default)
    {
        // --disable-build-servers: derleme bitince arkada MSBuild/derleyici süreci kalmaz; bunlar hem çıktı
        // borusunu açık tutar hem de üretilen klasördeki dosyaları kilitleyebilir.
        var exitCode = await ProcessRunner.RunAsync(
            "dotnet",
            ["build", solutionFilePath, "--nologo", "-v", "minimal", "--disable-build-servers"],
            Path.GetDirectoryName(solutionFilePath)!,
            onOutput,
            cancellationToken).ConfigureAwait(false);

        return exitCode == 0;
    }
}
