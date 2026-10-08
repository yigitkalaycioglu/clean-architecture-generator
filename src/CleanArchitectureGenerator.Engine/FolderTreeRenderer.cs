using System.Text;

namespace CleanArchitectureGenerator.Engine;

/// <summary>
/// Klasör listesini README'de gösterilecek ağaç metnine çevirir:
/// <code>
/// Firma.Proje/
/// ├── src/
/// │   └── Firma.Proje.Core/
/// └── tests/
/// </code>
/// </summary>
public static class FolderTreeRenderer
{
    public static string Render(string rootName, IEnumerable<string> directories)
    {
        var root = new Node(rootName);
        foreach (var directory in directories)
        {
            var node = root;
            foreach (var segment in directory.Split('/'))
            {
                node = node.GetOrAddChild(segment);
            }
        }

        var builder = new StringBuilder();
        builder.Append(rootName).Append("/\r\n");
        AppendChildren(builder, root, string.Empty);
        return builder.ToString().TrimEnd();
    }

    private static void AppendChildren(StringBuilder builder, Node node, string indent)
    {
        for (var index = 0; index < node.Children.Count; index++)
        {
            var child = node.Children[index];
            var isLast = index == node.Children.Count - 1;
            builder.Append(indent).Append(isLast ? "└── " : "├── ").Append(child.Name).Append("/\r\n");
            AppendChildren(builder, child, indent + (isLast ? "    " : "│   "));
        }
    }

    private sealed class Node(string name)
    {
        public string Name { get; } = name;

        public List<Node> Children { get; } = [];

        public Node GetOrAddChild(string name)
        {
            var child = Children.FirstOrDefault(node => string.Equals(node.Name, name, StringComparison.OrdinalIgnoreCase));
            if (child is null)
            {
                child = new Node(name);
                Children.Add(child);
            }

            return child;
        }
    }
}
