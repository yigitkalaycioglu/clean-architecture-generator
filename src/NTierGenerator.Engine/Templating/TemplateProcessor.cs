using System.Text.RegularExpressions;

namespace NTierGenerator.Engine.Templating;

/// <summary>
/// Bir şablon dosyasını işler:
/// <list type="number">
/// <item>Koşullu blokları uygular. Yönerge satırları dosya türüne göre yorum olarak yazılır ve çıktıdan silinir:
/// <c>//#if Auth</c>, <c>&lt;!--#if Auth--&gt;</c>, <c>@*#if Auth*@</c>; ayrıca <c>#elif</c>, <c>#else</c>, <c>#endif</c>.</item>
/// <item><c>__Name__</c> biçimindeki token'ları değerleriyle değiştirir (büyük harfle başlayan bilinen token'lar).</item>
/// <item>Satır sonlarını CRLF yapar, art arda boş satırları teke indirir.</item>
/// </list>
/// </summary>
internal static partial class TemplateProcessor
{
    [GeneratedRegex(@"^\s*(?://|<!--|@\*)#(?<keyword>if|elif|else|endif)\b(?<expression>.*?)(?:-->|\*@)?\s*$")]
    private static partial Regex DirectiveRegex();

    [GeneratedRegex("__(?<name>[A-Z][A-Za-z0-9]*)__")]
    private static partial Regex TokenRegex();

    public static string Process(string template, string templatePath, TemplateContext context)
    {
        var lines = ApplyConditionals(template, templatePath, context);
        var text = ReplaceTokens(string.Join('\n', lines), context.Tokens);
        return Tidy(text, templatePath.EndsWith(".cs", StringComparison.OrdinalIgnoreCase));
    }

    public static string ReplaceTokens(string text, IReadOnlyDictionary<string, string> tokens) =>
        TokenRegex().Replace(text, match => tokens.TryGetValue(match.Groups["name"].Value, out var value) ? value : match.Value);

    private static List<string> ApplyConditionals(string template, string templatePath, TemplateContext context)
    {
        var output = new List<string>();
        var frames = new Stack<ConditionFrame>();
        var isActive = true;
        var lineNumber = 0;

        foreach (var line in template.Replace("\r\n", "\n").Split('\n'))
        {
            lineNumber++;
            var directive = DirectiveRegex().Match(line);
            if (!directive.Success)
            {
                if (isActive)
                {
                    output.Add(line);
                }

                continue;
            }

            var expression = directive.Groups["expression"].Value.Trim();
            switch (directive.Groups["keyword"].Value)
            {
                case "if":
                {
                    var condition = Evaluate(expression, templatePath, lineNumber, context);
                    frames.Push(new ConditionFrame(isActive, condition));
                    isActive = isActive && condition;
                    break;
                }
                case "elif":
                {
                    var frame = PeekFrame(frames, "#elif", templatePath, lineNumber);
                    var condition = !frame.BranchTaken && Evaluate(expression, templatePath, lineNumber, context);
                    frame.BranchTaken |= condition;
                    isActive = frame.ParentActive && condition;
                    break;
                }
                case "else":
                {
                    var frame = PeekFrame(frames, "#else", templatePath, lineNumber);
                    isActive = frame.ParentActive && !frame.BranchTaken;
                    frame.BranchTaken = true;
                    break;
                }
                default:
                    isActive = PeekFrame(frames, "#endif", templatePath, lineNumber).ParentActive;
                    frames.Pop();
                    break;
            }
        }

        if (frames.Count > 0)
        {
            throw new TemplateException($"{templatePath}: {frames.Count} adet #if bloğu #endif ile kapatılmamış.");
        }

        return output;
    }

    private static bool Evaluate(string expression, string templatePath, int lineNumber, TemplateContext context)
    {
        try
        {
            return ConditionEvaluator.Evaluate(expression, context.Symbols, TemplateContext.KnownSymbols);
        }
        catch (TemplateException exception)
        {
            throw new TemplateException($"{templatePath}:{lineNumber}: {exception.Message}");
        }
    }

    private static ConditionFrame PeekFrame(Stack<ConditionFrame> frames, string keyword, string templatePath, int lineNumber) =>
        frames.Count > 0
            ? frames.Peek()
            : throw new TemplateException($"{templatePath}:{lineNumber}: {keyword} için eşleşen #if yok.");

    /// <summary>
    /// Koşullu bloklar silindiğinde kalan fazla boş satırları temizler. C# dosyalarında
    /// '{' satırından sonraki ve '}' satırından önceki boş satırlar da kaldırılır.
    /// </summary>
    private static string Tidy(string text, bool isCSharp)
    {
        var result = new List<string>();
        foreach (var line in text.Split('\n'))
        {
            var isBlank = string.IsNullOrWhiteSpace(line);
            if (isBlank && result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            {
                continue;
            }

            if (isCSharp && isBlank && result.Count > 0 && result[^1].TrimEnd().EndsWith('{'))
            {
                continue;
            }

            if (isCSharp && line.Trim() is "}" or "};" && result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
            {
                result.RemoveAt(result.Count - 1);
            }

            result.Add(line);
        }

        // Dosya başındaki boş satırlar silinir, dosya tek bir satır sonuyla biter.
        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[0]))
        {
            result.RemoveAt(0);
        }

        while (result.Count > 0 && string.IsNullOrWhiteSpace(result[^1]))
        {
            result.RemoveAt(result.Count - 1);
        }

        return result.Count == 0 ? string.Empty : string.Join("\r\n", result) + "\r\n";
    }

    private sealed class ConditionFrame(bool parentActive, bool branchTaken)
    {
        public bool ParentActive { get; } = parentActive;

        public bool BranchTaken { get; set; } = branchTaken;
    }
}
