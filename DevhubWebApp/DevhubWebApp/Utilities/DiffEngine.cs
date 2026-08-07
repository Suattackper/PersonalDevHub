using DevhubWebApp.Models.Tools.Diff;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Line-based diff over the longest common subsequence, the same model Git and editors use.
/// Pure and allocation-conscious: comparison keys are computed once per line rather than on every
/// cell of the LCS table.
/// </summary>
public static class DiffEngine
{
    /// <summary>
    /// Guard on the LCS table. Two documents of 4 000 lines already need 16 million cells; beyond
    /// that the tool falls back to a linear comparison instead of exhausting the browser's memory.
    /// </summary>
    public const int MaxLinesForExactDiff = 4000;

    /// <summary>Compares two documents line by line.</summary>
    public static DiffResult Compare(string? left, string? right, DiffOptions? options = null)
    {
        options ??= DiffOptions.Default;

        var leftLines = Split(left, options);
        var rightLines = Split(right, options);

        if (leftLines.Count == 0 && rightLines.Count == 0)
        {
            return DiffResult.Empty;
        }

        var lines = leftLines.Count > MaxLinesForExactDiff || rightLines.Count > MaxLinesForExactDiff
            ? CompareLinear(leftLines, rightLines, options)
            : CompareExact(leftLines, rightLines, options);

        return new DiffResult { Lines = lines, Statistics = Summarise(lines) };
    }

    /// <summary>Statistics only, for callers that render the diff elsewhere (e.g. Monaco).</summary>
    public static DiffStatistics CompareStatistics(string? left, string? right, DiffOptions? options = null)
        => Compare(left, right, options).Statistics;

    private static List<string> Split(string? text, DiffOptions options)
    {
        if (string.IsNullOrEmpty(text))
        {
            return [];
        }

        var lines = text.ReplaceLineEndings("\n").Split('\n').ToList();

        if (options.IgnoreBlankLines)
        {
            lines.RemoveAll(string.IsNullOrWhiteSpace);
        }

        return lines;
    }

    /// <summary>Normalised form used for equality. The original text is what gets displayed.</summary>
    private static string Key(string line, DiffOptions options)
    {
        var key = options.IgnoreWhitespace
            ? string.Join(' ', line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries))
            : line;

        return options.IgnoreCase ? key.ToLowerInvariant() : key;
    }

    private static List<DiffLine> CompareExact(List<string> left, List<string> right, DiffOptions options)
    {
        var leftKeys = left.Select(line => Key(line, options)).ToArray();
        var rightKeys = right.Select(line => Key(line, options)).ToArray();

        var table = BuildLcsTable(leftKeys, rightKeys);
        var lines = new List<DiffLine>(left.Count + right.Count);

        var leftIndex = 0;
        var rightIndex = 0;

        while (leftIndex < left.Count && rightIndex < right.Count)
        {
            if (string.Equals(leftKeys[leftIndex], rightKeys[rightIndex], StringComparison.Ordinal))
            {
                lines.Add(Unchanged(right[rightIndex], leftIndex, rightIndex));
                leftIndex++;
                rightIndex++;
            }
            else if (table[leftIndex + 1, rightIndex] >= table[leftIndex, rightIndex + 1])
            {
                lines.Add(Removed(left[leftIndex], leftIndex));
                leftIndex++;
            }
            else
            {
                lines.Add(Added(right[rightIndex], rightIndex));
                rightIndex++;
            }
        }

        while (leftIndex < left.Count)
        {
            lines.Add(Removed(left[leftIndex], leftIndex));
            leftIndex++;
        }

        while (rightIndex < right.Count)
        {
            lines.Add(Added(right[rightIndex], rightIndex));
            rightIndex++;
        }

        return lines;
    }

    /// <summary>
    /// Fallback for very large inputs: compares position by position. It reports every difference,
    /// but does not align around inserted blocks the way the exact algorithm does.
    /// </summary>
    private static List<DiffLine> CompareLinear(List<string> left, List<string> right, DiffOptions options)
    {
        var lines = new List<DiffLine>(Math.Max(left.Count, right.Count));

        for (var index = 0; index < Math.Max(left.Count, right.Count); index++)
        {
            var leftLine = index < left.Count ? left[index] : null;
            var rightLine = index < right.Count ? right[index] : null;

            if (leftLine is not null && rightLine is not null
                && string.Equals(Key(leftLine, options), Key(rightLine, options), StringComparison.Ordinal))
            {
                lines.Add(Unchanged(rightLine, index, index));
                continue;
            }

            if (leftLine is not null)
            {
                lines.Add(Removed(leftLine, index));
            }

            if (rightLine is not null)
            {
                lines.Add(Added(rightLine, index));
            }
        }

        return lines;
    }

    /// <summary>Standard LCS length table, filled from the end so the walk above can read it forward.</summary>
    private static int[,] BuildLcsTable(string[] left, string[] right)
    {
        var table = new int[left.Length + 1, right.Length + 1];

        for (var i = left.Length - 1; i >= 0; i--)
        {
            for (var j = right.Length - 1; j >= 0; j--)
            {
                table[i, j] = string.Equals(left[i], right[j], StringComparison.Ordinal)
                    ? table[i + 1, j + 1] + 1
                    : Math.Max(table[i + 1, j], table[i, j + 1]);
            }
        }

        return table;
    }

    private static DiffStatistics Summarise(List<DiffLine> lines) => new()
    {
        Added = lines.Count(line => line.Kind == DiffLineKind.Added),
        Removed = lines.Count(line => line.Kind == DiffLineKind.Removed),
        Unchanged = lines.Count(line => line.Kind == DiffLineKind.Unchanged)
    };

    private static DiffLine Unchanged(string text, int leftIndex, int rightIndex) => new()
    {
        Kind = DiffLineKind.Unchanged,
        Text = text,
        LeftLineNumber = leftIndex + 1,
        RightLineNumber = rightIndex + 1
    };

    private static DiffLine Added(string text, int rightIndex) => new()
    {
        Kind = DiffLineKind.Added,
        Text = text,
        RightLineNumber = rightIndex + 1
    };

    private static DiffLine Removed(string text, int leftIndex) => new()
    {
        Kind = DiffLineKind.Removed,
        Text = text,
        LeftLineNumber = leftIndex + 1
    };
}
