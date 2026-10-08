using System.Text.Encodings.Web;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace CleanArchitectureGenerator.Engine;

/// <summary>
/// Geliştirme gizli değerlerini, "dotnet user-secrets" aracının kullandığı secrets.json dosyasına yazar.
/// Dosya kullanıcı profilindedir; çözüm klasörüne ve dolayısıyla Git deposuna hiç girmez.
/// </summary>
internal static class UserSecretsWriter
{
    /// <summary>Windows'ta %APPDATA%\Microsoft\UserSecrets, diğer sistemlerde ~/.microsoft/usersecrets.</summary>
    public static string DefaultRoot => OperatingSystem.IsWindows()
        ? Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "Microsoft", "UserSecrets")
        : Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), ".microsoft", "usersecrets");

    /// <returns>Yazılan dosyanın yolu; yazılacak değer yoksa null.</returns>
    public static async Task<string?> WriteAsync(string root, Guid userSecretsId, IReadOnlyDictionary<string, string> secrets, CancellationToken cancellationToken)
    {
        if (secrets.Count == 0)
        {
            return null;
        }

        var directory = Path.Combine(root, userSecretsId.ToString());
        var path = Path.Combine(directory, "secrets.json");
        Directory.CreateDirectory(directory);

        // Aynı kimlikle daha önce yazılmış değerler korunur; yalnızca eksik anahtarlar eklenir.
        var document = File.Exists(path)
            ? JsonNode.Parse(await File.ReadAllTextAsync(path, cancellationToken).ConfigureAwait(false)) as JsonObject ?? []
            : [];

        foreach (var (key, value) in secrets)
        {
            if (!document.ContainsKey(key))
            {
                document[key] = value;
            }
        }

        await using var stream = new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None);
        await using var writer = new Utf8JsonWriter(stream, new JsonWriterOptions { Indented = true, Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping });
        document.WriteTo(writer);
        await writer.FlushAsync(cancellationToken).ConfigureAwait(false);
        return path;
    }
}
