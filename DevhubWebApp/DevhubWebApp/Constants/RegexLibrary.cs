using DevhubWebApp.Models.Tools.Regex;

namespace DevhubWebApp.Constants;

/// <summary>
/// Ready-made patterns and the syntax cheatsheet. Kept as data so the tester's UI stays a plain
/// list rendering — adding a pattern is a one-line change here and nowhere else.
/// </summary>
public static class RegexLibrary
{
    /// <summary>Common patterns offered in the tool.</summary>
    public static IReadOnlyList<RegexTemplate> Templates { get; } =
    [
        new()
        {
            Name = "Email",
            Pattern = @"[\w.+-]+@[\w-]+\.[\w.-]+",
            Description = "A pragmatic email match — deliberately not the full RFC 5322 grammar.",
            Sample = "contact devhub@example.com or sales@sub.example.co.uk"
        },
        new()
        {
            Name = "URL",
            Pattern = @"https?://[^\s""'<>]+",
            Description = "HTTP and HTTPS URLs up to the first whitespace or quote.",
            Sample = "See https://example.com/docs?page=2 and http://localhost:5199"
        },
        new()
        {
            Name = "IPv4 address",
            Pattern = @"\b(?:(?:25[0-5]|2[0-4]\d|1?\d?\d)\.){3}(?:25[0-5]|2[0-4]\d|1?\d?\d)\b",
            Description = "Dotted-quad addresses, with each octet range-checked.",
            Sample = "hosts 192.168.1.10, 10.0.0.255 and not 999.1.1.1"
        },
        new()
        {
            Name = "ISO date",
            Pattern = @"\d{4}-(?:0[1-9]|1[0-2])-(?:0[1-9]|[12]\d|3[01])",
            Description = "Dates in yyyy-MM-dd form.",
            Sample = "released 2024-11-30, planned 2025-01-15"
        },
        new()
        {
            Name = "GUID",
            Pattern = @"[0-9a-fA-F]{8}-(?:[0-9a-fA-F]{4}-){3}[0-9a-fA-F]{12}",
            Description = "Canonical hyphenated UUID.",
            Sample = "id 3f2a1b4c-5d6e-4f80-9a1b-2c3d4e5f6071"
        },
        new()
        {
            Name = "Hex colour",
            Pattern = @"#(?:[0-9a-fA-F]{3}|[0-9a-fA-F]{6})\b",
            Description = "Three or six digit CSS colours.",
            Sample = "colors #fff, #0d1117 and #4f9cf9"
        },
        new()
        {
            Name = "HTML tag",
            Pattern = @"</?([a-zA-Z][\w-]*)(?:\s[^>]*)?>",
            Description = "Opening or closing tags, capturing the tag name.",
            Sample = "<div class=\"x\">text</div><br />"
        },
        new()
        {
            Name = "Duplicate word",
            Pattern = @"\b(\w+)\s+\1\b",
            Description = "A word repeated immediately, using a backreference.",
            Sample = "this this is a a typical typo"
        },
        new()
        {
            Name = "Leading whitespace",
            Pattern = @"^[ \t]+",
            Description = "Indentation at the start of each line — pair it with the multiline flag.",
            Sample = "    indented\n\tтab indented"
        },
        new()
        {
            Name = "Named capture",
            Pattern = @"(?<key>\w+)=(?<value>[^;]+)",
            Description = "Key/value pairs with named groups.",
            Sample = "host=localhost;port=5199;debug=true"
        }
    ];

    /// <summary>Syntax reference shown beside the editor.</summary>
    public static IReadOnlyList<RegexCheatsheetEntry> Cheatsheet { get; } =
    [
        new() { Token = ".", Meaning = "Any character except newline", Category = "Character classes" },
        new() { Token = @"\d \D", Meaning = "Digit / not a digit", Category = "Character classes" },
        new() { Token = @"\w \W", Meaning = "Word character / not a word character", Category = "Character classes" },
        new() { Token = @"\s \S", Meaning = "Whitespace / not whitespace", Category = "Character classes" },
        new() { Token = "[abc]", Meaning = "Any one of a, b or c", Category = "Character classes" },
        new() { Token = "[^abc]", Meaning = "Any character except a, b or c", Category = "Character classes" },
        new() { Token = "[a-z]", Meaning = "Any character in the range", Category = "Character classes" },

        new() { Token = "*", Meaning = "Zero or more", Category = "Quantifiers" },
        new() { Token = "+", Meaning = "One or more", Category = "Quantifiers" },
        new() { Token = "?", Meaning = "Zero or one", Category = "Quantifiers" },
        new() { Token = "{n} {n,} {n,m}", Meaning = "Exactly n / at least n / between n and m", Category = "Quantifiers" },
        new() { Token = "*? +? ??", Meaning = "Lazy versions — match as little as possible", Category = "Quantifiers" },

        new() { Token = "^ $", Meaning = "Start / end of input, or of a line with the multiline flag", Category = "Anchors" },
        new() { Token = @"\b \B", Meaning = "Word boundary / not a word boundary", Category = "Anchors" },
        new() { Token = @"\A \z", Meaning = "Start / end of input, regardless of flags", Category = "Anchors" },

        new() { Token = "(…)", Meaning = "Capturing group", Category = "Groups" },
        new() { Token = "(?:…)", Meaning = "Non-capturing group", Category = "Groups" },
        new() { Token = "(?<name>…)", Meaning = "Named capturing group", Category = "Groups" },
        new() { Token = @"\1 \k<name>", Meaning = "Backreference to an earlier group", Category = "Groups" },
        new() { Token = "a|b", Meaning = "Either a or b", Category = "Groups" },

        new() { Token = "(?=…)", Meaning = "Positive lookahead", Category = "Lookaround" },
        new() { Token = "(?!…)", Meaning = "Negative lookahead", Category = "Lookaround" },
        new() { Token = "(?<=…)", Meaning = "Positive lookbehind", Category = "Lookaround" },
        new() { Token = "(?<!…)", Meaning = "Negative lookbehind", Category = "Lookaround" },

        new() { Token = "$1 ${name}", Meaning = "Insert a captured group in the replacement", Category = "Replacement" },
        new() { Token = "$&", Meaning = "Insert the whole match", Category = "Replacement" },
        new() { Token = "$$", Meaning = "A literal dollar sign", Category = "Replacement" }
    ];
}
