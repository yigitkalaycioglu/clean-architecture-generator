using System.Text;
using CleanArchitectureGenerator.Engine.Solutions;
using CleanArchitectureGenerator.Engine.Templating;
using CleanArchitectureGenerator.Engine.Validation;

namespace CleanArchitectureGenerator.Engine;

/// <summary>
/// Şablonlardan Clean Architecture çözümünü üretir. <see cref="CreatePlan"/> diske dokunmadan
/// tüm dosyaları bellekte hazırlar; <see cref="GenerateAsync"/> planı diske yazar.
/// </summary>
public sealed class SolutionGenerator(ITemplateSource templateSource)
{
    private const string PlaceholderFileName = ".gitkeep";
    private const string FolderTreeToken = "__FolderTree__";

    private static readonly UTF8Encoding Utf8WithoutBom = new(encoderShouldEmitUTF8Identifier: false);
    private static readonly UTF8Encoding Utf8WithBom = new(encoderShouldEmitUTF8Identifier: true);

    private static readonly string[] SolutionItemCandidates =
        [".editorconfig", "Directory.Build.props", "Directory.Packages.props", "nuget.config", "README.md", "SECURITY.md"];

    public SolutionGenerator()
        : this(EmbeddedTemplateSource.Default)
    {
    }

    public ITemplateSource TemplateSource => templateSource;

    public GenerationPlan CreatePlan(GeneratorOptions options)
    {
        ValidateOptions(options);

        var context = TemplateContext.Create(options);
        var excludedDirectories = ProjectCatalog.All
            .Where(project => !project.IsIncluded(options))
            .Select(project => project.TemplateDirectory + "/")
            .ToList();

        var files = new List<PlannedFile>();
        foreach (var template in templateSource.GetTemplates())
        {
            if (excludedDirectories.Any(directory => template.RelativePath.StartsWith(directory, StringComparison.Ordinal)))
            {
                continue;
            }

            var outputPath = MapPath(template.RelativePath, context);
            if (outputPath.EndsWith("/" + PlaceholderFileName, StringComparison.Ordinal))
            {
                files.Add(new PlannedFile(outputPath, string.Empty));
                continue;
            }

            var content = TemplateProcessor.Process(template.Content, template.RelativePath, context);
            if (content.Length > 0)
            {
                files.Add(new PlannedFile(outputPath, content));
            }
        }

        RemoveRedundantPlaceholders(files);

        var projects = ProjectCatalog.All
            .Where(project => project.IsIncluded(options))
            .Select(project => project.ToPlannedProject(options.SolutionName))
            .ToList();

        var missingProject = projects.FirstOrDefault(project => files.All(file => file.RelativePath != project.RelativePath));
        if (missingProject is not null)
        {
            throw new TemplateException($"Şablonlarda proje dosyası bulunamadı: {missingProject.RelativePath}");
        }

        var solutionItems = SolutionItemCandidates
            .Where(item => files.Any(file => file.RelativePath == item))
            .ToList();
        var startupProject = projects.Single(project => project.Layer == ProjectCatalog.StartupLayer);
        var solutionFileName = context.Tokens["SolutionFile"];
        files.Add(new PlannedFile(
            solutionFileName,
            SolutionFileWriter.Write(options.SolutionFormat, options.SolutionName, projects, solutionItems, startupProject)));

        files.Sort((left, right) => StringComparer.OrdinalIgnoreCase.Compare(left.RelativePath, right.RelativePath));
        InsertFolderTree(files, options.SolutionName);

        return new GenerationPlan(options.SolutionName, solutionFileName, projects, files);
    }

    public async Task<GenerationResult> GenerateAsync(GeneratorOptions options, IProgress<GenerationProgress>? progress = null, CancellationToken cancellationToken = default)
    {
        var plan = CreatePlan(options);
        var rootDirectory = Path.GetFullPath(options.SolutionDirectory);

        if (Directory.Exists(rootDirectory) && Directory.EnumerateFileSystemEntries(rootDirectory).Any() && !options.OverwriteExisting)
        {
            throw new IOException($"Hedef klasör boş değil: {rootDirectory}");
        }

        Directory.CreateDirectory(rootDirectory);
        var rootWithSeparator = rootDirectory.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;

        for (var index = 0; index < plan.Files.Count; index++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var file = plan.Files[index];
            var fullPath = Path.GetFullPath(Path.Combine(rootDirectory, file.RelativePath.Replace('/', Path.DirectorySeparatorChar)));
            if (!fullPath.StartsWith(rootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException($"Şablon çözüm klasörünün dışına yazmaya çalışıyor: {file.RelativePath}");
            }

            Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
            var encoding = file.RelativePath.EndsWith(".sln", StringComparison.OrdinalIgnoreCase) ? Utf8WithBom : Utf8WithoutBom;
            await File.WriteAllTextAsync(fullPath, file.Content, encoding, cancellationToken).ConfigureAwait(false);

            progress?.Report(new GenerationProgress(index + 1, plan.Files.Count, file.RelativePath));
        }

        // Gizli değerler (JWT imzalama anahtarı vb.) çözüm klasörüne değil, depo dışındaki user-secrets'a yazılır.
        var userSecretsFile = await UserSecretsWriter.WriteAsync(
            options.UserSecretsRoot ?? UserSecretsWriter.DefaultRoot,
            options.RandomValues.UserSecretsId,
            TemplateContext.CreateDevelopmentSecrets(options),
            cancellationToken).ConfigureAwait(false);

        return new GenerationResult(rootDirectory, Path.Combine(rootDirectory, plan.SolutionFileName), plan, userSecretsFile);
    }

    private static void ValidateOptions(GeneratorOptions options)
    {
        var nameValidation = SolutionNameValidator.Validate(options.SolutionName);
        if (!nameValidation.IsValid)
        {
            throw new ArgumentException(nameValidation.Error, nameof(options));
        }

        if (!Enum.IsDefined(options.Authentication) || !Enum.IsDefined(options.Database) || !Enum.IsDefined(options.SolutionFormat))
        {
            throw new ArgumentException("Geçersiz seçenek değeri.", nameof(options));
        }
    }

    /// <summary>
    /// Şablon yolunu çıktı yoluna çevirir: token'lar değiştirilir, "dot_" ile başlayan
    /// klasör ve dosya adları "." ile başlar (dot_gitignore -> .gitignore) ve ".template"
    /// son eki silinir (X.csproj.template -> X.csproj). Son ek, IDE'lerin şablonları gerçek
    /// proje sanıp derlemesini önler.
    /// </summary>
    private static string MapPath(string templatePath, TemplateContext context)
    {
        const string TemplateSuffix = ".template";
        var segments = TemplateProcessor.ReplaceTokens(templatePath, context.Tokens).Split('/');
        for (var index = 0; index < segments.Length; index++)
        {
            if (segments[index].StartsWith("dot_", StringComparison.Ordinal))
            {
                segments[index] = "." + segments[index]["dot_".Length..];
            }
        }

        if (segments[^1].EndsWith(TemplateSuffix, StringComparison.Ordinal))
        {
            segments[^1] = segments[^1][..^TemplateSuffix.Length];
        }

        return string.Join('/', segments);
    }

    /// <summary>
    /// .gitkeep yalnızca boş kalacak klasörlerin Git'te tutulması içindir;
    /// klasörde başka dosya üretildiyse kaldırılır.
    /// </summary>
    private static void RemoveRedundantPlaceholders(List<PlannedFile> files)
    {
        var contentFiles = files.Where(file => file.FileName != PlaceholderFileName).ToList();
        var redundantPlaceholders = files
            .Where(file => file.FileName == PlaceholderFileName)
            .Where(placeholder =>
            {
                var directory = placeholder.RelativePath[..^PlaceholderFileName.Length];
                return contentFiles.Any(file => file.RelativePath.StartsWith(directory, StringComparison.OrdinalIgnoreCase));
            })
            .ToHashSet();

        files.RemoveAll(redundantPlaceholders.Contains);
    }

    /// <summary>README içindeki __FolderTree__ yerine üretilen klasör ağacını yazar.</summary>
    private static void InsertFolderTree(List<PlannedFile> files, string solutionName)
    {
        var readmeIndex = files.FindIndex(file => file.RelativePath == "README.md");
        if (readmeIndex < 0 || !files[readmeIndex].Content.Contains(FolderTreeToken, StringComparison.Ordinal))
        {
            return;
        }

        var directories = new GenerationPlan(solutionName, string.Empty, [], files).Directories;
        var tree = FolderTreeRenderer.Render(solutionName, directories);
        files[readmeIndex] = files[readmeIndex] with { Content = files[readmeIndex].Content.Replace(FolderTreeToken, tree, StringComparison.Ordinal) };
    }
}
