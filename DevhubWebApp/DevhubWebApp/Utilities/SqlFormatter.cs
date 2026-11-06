using System.Text;

namespace DevhubWebApp.Utilities;

/// <summary>A conservative SQL pretty-printer that never rewrites strings or comments.</summary>
public static class SqlFormatter
{
    private static readonly HashSet<string> Keywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "DISTINCT", "FROM", "WHERE", "INSERT", "INTO", "VALUES", "UPDATE", "SET",
        "DELETE", "JOIN", "INNER", "LEFT", "RIGHT", "FULL", "OUTER", "CROSS", "ON", "AS",
        "AND", "OR", "NOT", "NULL", "IS", "IN", "EXISTS", "BETWEEN", "LIKE", "CASE", "WHEN",
        "THEN", "ELSE", "END", "GROUP", "BY", "HAVING", "ORDER", "ASC", "DESC", "LIMIT",
        "OFFSET", "UNION", "ALL", "WITH", "CREATE", "ALTER", "DROP", "TABLE", "RETURNING",
        "COUNT", "SUM", "AVG", "MIN", "MAX"
    };

    private static readonly HashSet<string> NewLineBefore = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "FROM", "WHERE", "INSERT", "UPDATE", "DELETE", "VALUES", "SET", "JOIN",
        "INNER", "LEFT", "RIGHT", "FULL", "CROSS", "ON", "GROUP", "ORDER", "HAVING", "LIMIT", "OFFSET", "UNION",
        "RETURNING"
    };

    private static readonly HashSet<string> JoinModifiers = new(StringComparer.OrdinalIgnoreCase)
    {
        "INNER", "LEFT", "RIGHT", "FULL", "CROSS"
    };

    private static readonly HashSet<string> StatementStarters = new(StringComparer.OrdinalIgnoreCase)
    {
        "SELECT", "INSERT", "UPDATE", "DELETE", "WITH", "CREATE", "ALTER", "DROP"
    };

    /// <summary>Checks structural errors identifiable without choosing a SQL dialect.</summary>
    public static string Validate(string? sql)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }
        var tokens = Tokenize(sql);
        var firstWord = tokens.FirstOrDefault(token => token.Kind == SqlTokenKind.Word)?.Text;
        if (firstWord is null || !StatementStarters.Contains(firstWord))
        {
            return "Input does not begin with a supported SQL statement.";
        }
        var depth = 0;
        foreach (var token in tokens)
        {
            if (token.Kind == SqlTokenKind.Unclosed)
            {
                return token.Text.StartsWith("/*", StringComparison.Ordinal)
                    ? "Unterminated block comment."
                    : "Unterminated SQL string or quoted identifier.";
            }
            if (token.Kind is SqlTokenKind.Literal or SqlTokenKind.LineComment or SqlTokenKind.BlockComment)
            {
                continue;
            }
            if (token.Text == "(")
            {
                depth++;
            }
            if (token.Text == ")" && --depth < 0)
            {
                return "Unexpected closing parenthesis.";
            }
        }
        if (depth > 0)
        {
            return "One or more closing parentheses are missing.";
        }
        return string.Empty;
    }

    public static string Format(string? sql, bool uppercaseKeywords = true)
    {
        if (string.IsNullOrWhiteSpace(sql))
        {
            return string.Empty;
        }

        var tokens = Tokenize(sql);
        var output = new StringBuilder(sql.Length + 32);
        var indent = 0;
        var lineStart = true;

        void NewLine()
        {
            while (output.Length > 0 && output[^1] == ' ')
            {
                output.Length--;
            }
            if (output.Length > 0 && output[^1] != '\n')
            {
                output.AppendLine();
            }
            lineStart = true;
        }

        void Write(string value)
        {
            if (lineStart)
            {
                output.Append(' ', Math.Max(0, indent) * 4);
                lineStart = false;
            }
            output.Append(value);
        }

        for (var index = 0; index < tokens.Count; index++)
        {
            var token = tokens[index];
            var upper = token.Kind == SqlTokenKind.Word ? token.Text.ToUpperInvariant() : token.Text;
            var text = token.Kind == SqlTokenKind.Word && uppercaseKeywords && Keywords.Contains(token.Text)
                ? upper
                : token.Text;

            if (token.Kind is SqlTokenKind.LineComment or SqlTokenKind.BlockComment)
            {
                if (!lineStart)
                {
                    Write(" ");
                }
                Write(text.TrimEnd());
                NewLine();
                continue;
            }

            if (token.Text == ")")
            {
                indent = Math.Max(0, indent - 1);
                if (output.Length > 0 && output[^1] == ' ')
                {
                    output.Length--;
                }
                Write(")");
                continue;
            }

            var compound = upper is "BY" && index > 0 && tokens[index - 1].Text.Equals("GROUP", StringComparison.OrdinalIgnoreCase)
                || upper is "BY" && index > 0 && tokens[index - 1].Text.Equals("ORDER", StringComparison.OrdinalIgnoreCase)
                || upper is "JOIN" && index > 0 && JoinModifiers.Contains(tokens[index - 1].Text);

            if (token.Kind == SqlTokenKind.Word && NewLineBefore.Contains(upper) && !compound)
            {
                NewLine();
            }
            else if (upper is "AND" or "OR")
            {
                NewLine();
            }

            if (token.Text == "(")
            {
                if (!lineStart && output[^1] == ' ')
                {
                    output.Length--;
                }
                Write("(");
                indent++;
                if (index + 1 < tokens.Count && tokens[index + 1].Text.Equals("SELECT", StringComparison.OrdinalIgnoreCase))
                {
                    NewLine();
                }
                continue;
            }

            if (token.Text == ",")
            {
                if (output.Length > 0 && output[^1] == ' ')
                {
                    output.Length--;
                }
                Write(",");
                NewLine();
                continue;
            }

            if (token.Text == ";")
            {
                if (output.Length > 0 && output[^1] == ' ')
                {
                    output.Length--;
                }
                Write(";");
                NewLine();
                continue;
            }

            if (!lineStart && output.Length > 0 && output[^1] is not (' ' or '\n' or '(' or '.') && token.Text != ".")
            {
                Write(" ");
            }
            if (token.Text == "." && output.Length > 0 && output[^1] == ' ')
            {
                output.Length--;
            }
            Write(text);
        }

        return output.ToString().Trim();
    }

    private static List<SqlToken> Tokenize(string sql)
    {
        var tokens = new List<SqlToken>();
        for (var i = 0; i < sql.Length;)
        {
            if (char.IsWhiteSpace(sql[i]))
            {
                i++;
                continue;
            }
            var start = i;
            if (sql[i] is '\'' or '"' or '`' or '[')
            {
                var close = sql[i] == '[' ? ']' : sql[i++];
                var closed = false;
                if (sql[start] == '[')
                {
                    i++;
                }
                while (i < sql.Length)
                {
                    if (sql[i] == close)
                    {
                        i++;
                        if (i < sql.Length && sql[i] == close)
                        {
                            i++;
                            continue;
                        }
                        closed = true;
                        break;
                    }
                    if (sql[i] == '\\' && i + 1 < sql.Length)
                    {
                        i += 2;
                    }
                    else
                    {
                        i++;
                    }
                }
                tokens.Add(new(closed ? SqlTokenKind.Literal : SqlTokenKind.Unclosed, sql[start..i]));
                continue;
            }
            if (sql[i] == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                i += 2;
                while (i < sql.Length && sql[i] is not ('\r' or '\n'))
                {
                    i++;
                }
                tokens.Add(new(SqlTokenKind.LineComment, sql[start..i]));
                continue;
            }
            if (sql[i] == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                {
                    i++;
                }
                var closed = i + 1 < sql.Length;
                i = Math.Min(sql.Length, i + 2);
                tokens.Add(new(closed ? SqlTokenKind.BlockComment : SqlTokenKind.Unclosed, sql[start..i]));
                continue;
            }
            if (char.IsLetterOrDigit(sql[i]) || sql[i] is '_' or '@' or '$')
            {
                i++;
                while (i < sql.Length && (char.IsLetterOrDigit(sql[i]) || sql[i] is '_' or '@' or '$'))
                {
                    i++;
                }
                tokens.Add(new(SqlTokenKind.Word, sql[start..i]));
                continue;
            }
            tokens.Add(new(SqlTokenKind.Symbol, sql[i++].ToString()));
        }
        return tokens;
    }

    private enum SqlTokenKind { Word, Literal, Symbol, LineComment, BlockComment, Unclosed }

    private sealed record SqlToken(SqlTokenKind Kind, string Text);
}
