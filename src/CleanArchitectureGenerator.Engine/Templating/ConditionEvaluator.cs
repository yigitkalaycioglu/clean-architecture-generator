namespace CleanArchitectureGenerator.Engine.Templating;

/// <summary>
/// Şablonlardaki koşul ifadelerini değerlendirir: <c>Auth</c>, <c>!Tests</c>, <c>Sample &amp;&amp; Auth</c>,
/// <c>(WebApi || WebUi) &amp;&amp; !Sqlite</c>. Bilinmeyen semboller yazım hatası sayılır ve hata verir.
/// </summary>
internal static class ConditionEvaluator
{
    public static bool Evaluate(string expression, IReadOnlySet<string> activeSymbols, IReadOnlySet<string> knownSymbols)
    {
        var parser = new Parser(Tokenize(expression), expression, activeSymbols, knownSymbols);
        var result = parser.ParseOr();
        parser.ExpectEnd();
        return result;
    }

    private static List<string> Tokenize(string expression)
    {
        var tokens = new List<string>();
        var index = 0;
        while (index < expression.Length)
        {
            var character = expression[index];
            if (char.IsWhiteSpace(character))
            {
                index++;
            }
            else if (character is '(' or ')' or '!')
            {
                tokens.Add(character.ToString());
                index++;
            }
            else if (expression.AsSpan(index).StartsWith("&&") || expression.AsSpan(index).StartsWith("||"))
            {
                tokens.Add(expression.Substring(index, 2));
                index += 2;
            }
            else if (char.IsLetter(character))
            {
                var start = index;
                while (index < expression.Length && char.IsLetterOrDigit(expression[index]))
                {
                    index++;
                }

                tokens.Add(expression[start..index]);
            }
            else
            {
                throw new TemplateException($"Koşul ifadesinde beklenmeyen karakter '{character}': {expression}");
            }
        }

        return tokens;
    }

    private sealed class Parser(List<string> tokens, string expression, IReadOnlySet<string> activeSymbols, IReadOnlySet<string> knownSymbols)
    {
        private int _position;

        private string? Current => _position < tokens.Count ? tokens[_position] : null;

        public bool ParseOr()
        {
            var value = ParseAnd();
            while (Current == "||")
            {
                _position++;
                var right = ParseAnd();
                value = value || right;
            }

            return value;
        }

        public void ExpectEnd()
        {
            if (Current is not null)
            {
                throw new TemplateException($"Koşul ifadesinde beklenmeyen '{Current}': {expression}");
            }
        }

        private bool ParseAnd()
        {
            var value = ParseUnary();
            while (Current == "&&")
            {
                _position++;
                var right = ParseUnary();
                value = value && right;
            }

            return value;
        }

        private bool ParseUnary()
        {
            if (Current == "!")
            {
                _position++;
                return !ParseUnary();
            }

            if (Current == "(")
            {
                _position++;
                var value = ParseOr();
                if (Current != ")")
                {
                    throw new TemplateException($"Koşul ifadesinde ')' eksik: {expression}");
                }

                _position++;
                return value;
            }

            var symbol = Current ?? throw new TemplateException($"Koşul ifadesi eksik: '{expression}'");
            if (!knownSymbols.Contains(symbol))
            {
                throw new TemplateException($"Bilinmeyen şablon sembolü '{symbol}'. Geçerli semboller: {string.Join(", ", knownSymbols)}");
            }

            _position++;
            return activeSymbols.Contains(symbol);
        }
    }
}
