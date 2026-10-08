using System.Reflection;
using System.Text;

namespace CleanArchitectureGenerator.Engine.Templating;

/// <param name="RelativePath">Şablon kök klasörüne göre '/' ile ayrılmış yol, örneğin "src/__Name__.Core/__Name__.Core.csproj".</param>
public sealed record TemplateFile(string RelativePath, string Content)
{
    /// <summary>
    /// Şablon projeleri bir IDE tarafından derlenirse oluşan bin/obj içerikleri şablon sayılmaz.
    /// </summary>
    internal static bool IsBuildArtifact(string relativePath) =>
        relativePath.Split('/').Any(segment => segment.Equals("bin", StringComparison.OrdinalIgnoreCase) || segment.Equals("obj", StringComparison.OrdinalIgnoreCase));
}

public interface ITemplateSource
{
    /// <summary>Şablonların nereden okunduğunu açıklayan kısa metin (günlükte gösterilir).</summary>
    string Description { get; }

    IReadOnlyList<TemplateFile> GetTemplates();
}

/// <summary>
/// DLL'e gömülü şablonları okur. Kaynak adları csproj'daki LogicalName ile klasör yolunu korur.
/// </summary>
public sealed class EmbeddedTemplateSource : ITemplateSource
{
    private const string ResourcePrefix = "Templates/";

    private readonly Lazy<IReadOnlyList<TemplateFile>> _templates;

    public EmbeddedTemplateSource(Assembly assembly)
    {
        _templates = new Lazy<IReadOnlyList<TemplateFile>>(() => Load(assembly));
    }

    public static EmbeddedTemplateSource Default { get; } = new(typeof(EmbeddedTemplateSource).Assembly);

    public string Description => "Gömülü şablonlar";

    public IReadOnlyList<TemplateFile> GetTemplates() => _templates.Value;

    private static List<TemplateFile> Load(Assembly assembly)
    {
        var templates = new List<TemplateFile>();
        foreach (var resourceName in assembly.GetManifestResourceNames())
        {
            var normalizedName = resourceName.Replace('\\', '/');
            if (!normalizedName.StartsWith(ResourcePrefix, StringComparison.Ordinal) || TemplateFile.IsBuildArtifact(normalizedName))
            {
                continue;
            }

            using var stream = assembly.GetManifestResourceStream(resourceName)
                ?? throw new InvalidOperationException($"Gömülü şablon okunamadı: {resourceName}");
            using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
            templates.Add(new TemplateFile(normalizedName[ResourcePrefix.Length..], reader.ReadToEnd()));
        }

        return templates.OrderBy(template => template.RelativePath, StringComparer.Ordinal).ToList();
    }
}

/// <summary>
/// Diskteki bir klasörden şablon okur. Uygulamanın yanına "Templates" klasörü konarak
/// şablonlar yeniden derleme yapmadan özelleştirilebilir.
/// </summary>
public sealed class DirectoryTemplateSource(string rootDirectory) : ITemplateSource
{
    public string Description => $"Klasördeki şablonlar: {rootDirectory}";

    public IReadOnlyList<TemplateFile> GetTemplates()
    {
        return Directory.EnumerateFiles(rootDirectory, "*", SearchOption.AllDirectories)
            .Select(path => (FullPath: path, RelativePath: Path.GetRelativePath(rootDirectory, path).Replace('\\', '/')))
            .Where(file => !TemplateFile.IsBuildArtifact(file.RelativePath))
            .Select(file => new TemplateFile(file.RelativePath, File.ReadAllText(file.FullPath, Encoding.UTF8)))
            .OrderBy(template => template.RelativePath, StringComparer.Ordinal)
            .ToList();
    }
}
