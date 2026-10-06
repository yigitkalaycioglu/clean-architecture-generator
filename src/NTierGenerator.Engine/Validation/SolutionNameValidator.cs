using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace NTierGenerator.Engine.Validation;

public readonly record struct NameValidationResult(bool IsValid, string? Error, string? Suggestion = null)
{
    public static NameValidationResult Valid => new(true, null);
}

/// <summary>
/// Çözüm adının hem geçerli bir C# namespace'i hem de geçerli bir Windows klasör adı olmasını denetler.
/// </summary>
public static partial class SolutionNameValidator
{
    public const int MaxLength = 80;

    private const string TurkishCharacters = "çğıöşüÇĞİÖŞÜ";

    private static readonly HashSet<string> CSharpKeywords = new(StringComparer.Ordinal)
    {
        "abstract", "as", "base", "bool", "break", "byte", "case", "catch", "char", "checked", "class", "const",
        "continue", "decimal", "default", "delegate", "do", "double", "else", "enum", "event", "explicit", "extern",
        "false", "finally", "fixed", "float", "for", "foreach", "goto", "if", "implicit", "in", "int", "interface",
        "internal", "is", "lock", "long", "namespace", "new", "null", "object", "operator", "out", "override",
        "params", "private", "protected", "public", "readonly", "ref", "return", "sbyte", "sealed", "short",
        "sizeof", "stackalloc", "static", "string", "struct", "switch", "this", "throw", "true", "try", "typeof",
        "uint", "ulong", "unchecked", "unsafe", "ushort", "using", "virtual", "void", "volatile", "while"
    };

    private static readonly HashSet<string> ReservedDeviceNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "CON", "PRN", "AUX", "NUL",
        "COM1", "COM2", "COM3", "COM4", "COM5", "COM6", "COM7", "COM8", "COM9",
        "LPT1", "LPT2", "LPT3", "LPT4", "LPT5", "LPT6", "LPT7", "LPT8", "LPT9"
    };

    /// <summary>Üretilen kodun kullandığı kütüphanelerin kök namespace'leri; aynı adla başlamak çakışmaya yol açar.</summary>
    private static readonly HashSet<string> ReservedRootNamespaces = new(StringComparer.OrdinalIgnoreCase)
    {
        "System", "Microsoft", "Autofac", "Castle", "FluentValidation", "Moq", "Xunit", "Swashbuckle", "Npgsql"
    };

    [GeneratedRegex("^[A-Za-z_][A-Za-z0-9_]*$")]
    private static partial Regex SegmentRegex();

    [GeneratedRegex("[^A-Za-z0-9_.]+")]
    private static partial Regex InvalidCharactersRegex();

    public static NameValidationResult Validate(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return Invalid("Çözüm adı boş olamaz.");
        }

        if (name.Length > MaxLength)
        {
            return Invalid($"Çözüm adı en fazla {MaxLength} karakter olabilir.");
        }

        if (name.Any(character => TurkishCharacters.Contains(character)))
        {
            return Invalid("Türkçe karakter kullanılamaz.", SuggestName(name));
        }

        if (name.Any(char.IsWhiteSpace))
        {
            return Invalid("Çözüm adında boşluk olamaz.", SuggestName(name));
        }

        var segments = name.Split('.');
        if (segments.Any(segment => segment.Length == 0))
        {
            return Invalid("Ad nokta ile başlayıp bitemez, art arda iki nokta içeremez.");
        }

        foreach (var segment in segments)
        {
            if (!SegmentRegex().IsMatch(segment))
            {
                return Invalid($"'{segment}' geçersiz: harf ya da '_' ile başlamalı, yalnızca İngilizce harf, rakam ve '_' içermeli.", SuggestName(name));
            }

            if (CSharpKeywords.Contains(segment))
            {
                return Invalid($"'{segment}' bir C# anahtar kelimesi, ad olarak kullanılamaz.");
            }
        }

        if (ReservedDeviceNames.Contains(segments[0]))
        {
            return Invalid($"'{segments[0]}' Windows'ta ayrılmış bir ad, klasör adı olarak kullanılamaz.");
        }

        if (ReservedRootNamespaces.Contains(segments[0]))
        {
            return Invalid($"'{segments[0]}' ile başlayan adlar kullanılan kütüphanelerin namespace'leriyle çakışır.");
        }

        return NameValidationResult.Valid;
    }

    /// <summary>
    /// Geçersiz bir girdiden geçerli bir ad önerir: "Mağaza yönetimi" -> "MagazaYonetimi".
    /// Öneri üretilemezse null döner.
    /// </summary>
    public static string? SuggestName(string input)
    {
        var transliterated = new StringBuilder(input.Length);
        foreach (var character in input)
        {
            transliterated.Append(character switch
            {
                'ç' => 'c', 'Ç' => 'C', 'ğ' => 'g', 'Ğ' => 'G', 'ı' => 'i', 'İ' => 'I',
                'ö' => 'o', 'Ö' => 'O', 'ş' => 's', 'Ş' => 'S', 'ü' => 'u', 'Ü' => 'U',
                _ => character
            });
        }

        // Geçersiz karakterlerle ayrılan kelimeler PascalCase olarak birleştirilir.
        var segments = transliterated.ToString()
            .Split('.', StringSplitOptions.RemoveEmptyEntries)
            .Select(segment => string.Concat(InvalidCharactersRegex()
                .Split(segment)
                .Where(word => word.Length > 0)
                .Select(word => char.ToUpper(word[0], CultureInfo.InvariantCulture) + word[1..])))
            .Where(segment => segment.Length > 0)
            .Select(segment => char.IsDigit(segment[0]) ? "_" + segment : segment);

        var suggestion = string.Join('.', segments);
        return suggestion.Length > 0 && suggestion != input && Validate(suggestion).IsValid ? suggestion : null;
    }

    private static NameValidationResult Invalid(string error, string? suggestion = null) => new(false, error, suggestion);
}
