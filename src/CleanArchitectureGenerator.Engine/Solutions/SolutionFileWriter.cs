using System.Security.Cryptography;
using System.Text;

namespace CleanArchitectureGenerator.Engine.Solutions;

/// <summary>
/// Projelerden çözüm dosyası üretir. GUID'ler addan türetildiği için aynı seçenekler
/// her seferinde aynı çözüm dosyasını üretir.
/// </summary>
internal static class SolutionFileWriter
{
    private const string CSharpProjectTypeGuid = "{FAE04EC0-301F-11D3-BF4B-00C04F79EFBC}";
    private const string SolutionFolderTypeGuid = "{2150E333-8FDC-42A3-9474-1A3956D46DE8}";
    private const string SolutionItemsFolder = "Solution Items";

    private static readonly string[] Configurations = ["Debug|Any CPU", "Release|Any CPU"];

    /// <param name="startupProject">
    /// Visual Studio, kullanıcı ayarı (.vs klasörü) yokken çözüm dosyasındaki ilk projeyi başlangıç projesi yapar.
    /// Bu proje en başa yazılır; böylece çözüm ilk açıldığında F5 bir sınıf kitaplığını değil, web projesini çalıştırır.
    /// </param>
    public static string Write(
        SolutionFormat format,
        string solutionName,
        IReadOnlyList<PlannedProject> projects,
        IReadOnlyList<string> solutionItems,
        PlannedProject startupProject)
    {
        List<PlannedProject> orderedProjects = [startupProject, .. projects.Where(project => project != startupProject)];
        return format == SolutionFormat.Slnx
            ? WriteSlnx(orderedProjects, solutionItems)
            : WriteSln(solutionName, orderedProjects, solutionItems);
    }

    private static string WriteSln(string solutionName, IReadOnlyList<PlannedProject> projects, IReadOnlyList<string> solutionItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine();
        builder.AppendLine("Microsoft Visual Studio Solution File, Format Version 12.00");
        builder.AppendLine("# Visual Studio Version 17");
        builder.AppendLine("VisualStudioVersion = 17.0.31903.59");
        builder.AppendLine("MinimumVisualStudioVersion = 10.0.40219.1");

        // Projeler sanal klasörlerden önce yazılır: dosyadaki ilk Project girdisi başlangıç projesi olur.
        var projectGuids = projects.ToDictionary(project => project, project => CreateGuid(solutionName, project.RelativePath));
        foreach (var project in projects)
        {
            builder.AppendLine($"Project(\"{CSharpProjectTypeGuid}\") = \"{project.Name}\", \"{project.RelativePath.Replace('/', '\\')}\", \"{projectGuids[project]}\"");
            builder.AppendLine("EndProject");
        }

        var folderGuids = projects
            .Select(project => project.SolutionFolder)
            .Distinct(StringComparer.Ordinal)
            .ToDictionary(folder => folder, folder => CreateGuid(solutionName, "folder:" + folder), StringComparer.Ordinal);

        foreach (var (folder, guid) in folderGuids)
        {
            builder.AppendLine($"Project(\"{SolutionFolderTypeGuid}\") = \"{folder}\", \"{folder}\", \"{guid}\"");
            builder.AppendLine("EndProject");
        }

        if (solutionItems.Count > 0)
        {
            var itemsGuid = CreateGuid(solutionName, "folder:" + SolutionItemsFolder);
            builder.AppendLine($"Project(\"{SolutionFolderTypeGuid}\") = \"{SolutionItemsFolder}\", \"{SolutionItemsFolder}\", \"{itemsGuid}\"");
            builder.AppendLine("\tProjectSection(SolutionItems) = preProject");
            foreach (var item in solutionItems)
            {
                var path = item.Replace('/', '\\');
                builder.AppendLine($"\t\t{path} = {path}");
            }

            builder.AppendLine("\tEndProjectSection");
            builder.AppendLine("EndProject");
        }

        builder.AppendLine("Global");
        builder.AppendLine("\tGlobalSection(SolutionConfigurationPlatforms) = preSolution");
        foreach (var configuration in Configurations)
        {
            builder.AppendLine($"\t\t{configuration} = {configuration}");
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(ProjectConfigurationPlatforms) = postSolution");
        foreach (var project in projects)
        {
            foreach (var configuration in Configurations)
            {
                builder.AppendLine($"\t\t{projectGuids[project]}.{configuration}.ActiveCfg = {configuration}");
                builder.AppendLine($"\t\t{projectGuids[project]}.{configuration}.Build.0 = {configuration}");
            }
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(SolutionProperties) = preSolution");
        builder.AppendLine("\t\tHideSolutionNode = FALSE");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(NestedProjects) = preSolution");
        foreach (var project in projects)
        {
            builder.AppendLine($"\t\t{projectGuids[project]} = {folderGuids[project.SolutionFolder]}");
        }

        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("\tGlobalSection(ExtensibilityGlobals) = postSolution");
        builder.AppendLine($"\t\tSolutionGuid = {CreateGuid(solutionName, "solution")}");
        builder.AppendLine("\tEndGlobalSection");
        builder.AppendLine("EndGlobal");

        return builder.ToString().ReplaceLineEndings("\r\n");
    }

    private static string WriteSlnx(IReadOnlyList<PlannedProject> projects, IReadOnlyList<string> solutionItems)
    {
        var builder = new StringBuilder();
        builder.AppendLine("<Solution>");

        if (solutionItems.Count > 0)
        {
            builder.AppendLine($"  <Folder Name=\"/{SolutionItemsFolder}/\">");
            foreach (var item in solutionItems)
            {
                builder.AppendLine($"    <File Path=\"{item}\" />");
            }

            builder.AppendLine("  </Folder>");
        }

        foreach (var group in projects.GroupBy(project => project.SolutionFolder))
        {
            builder.AppendLine($"  <Folder Name=\"/{group.Key}/\">");
            foreach (var project in group)
            {
                builder.AppendLine($"    <Project Path=\"{project.RelativePath}\" />");
            }

            builder.AppendLine("  </Folder>");
        }

        builder.AppendLine("</Solution>");
        return builder.ToString().ReplaceLineEndings("\r\n");
    }

    private static string CreateGuid(string solutionName, string key)
    {
        var hash = SHA256.HashData(Encoding.UTF8.GetBytes($"{solutionName}|{key}"));
        return new Guid(hash.AsSpan(0, 16)).ToString("B").ToUpperInvariant();
    }
}
