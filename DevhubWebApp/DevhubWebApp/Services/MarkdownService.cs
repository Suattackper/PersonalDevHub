using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools.Markdown;
using Markdig;

namespace DevhubWebApp.Services;

/// <summary>
/// Markdig-backed <see cref="IMarkdownService"/>. The pipeline is built once and reused: building
/// it per keystroke would dominate the cost of rendering a preview.
/// </summary>
public sealed class MarkdownService : IMarkdownService
{
    private readonly MarkdownPipeline _pipeline;

    public MarkdownService()
        => _pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UseEmojiAndSmiley()
            // The preview renders text the user may have pasted from anywhere. Disabling raw HTML
            // means a document can style itself but can never inject script into the application.
            .DisableHtml()
            .Build();

    public string ToHtml(string? markdown)
        => string.IsNullOrEmpty(markdown) ? string.Empty : Markdown.ToHtml(markdown, _pipeline);

    public string ToPlainText(string? markdown)
        => string.IsNullOrEmpty(markdown) ? string.Empty : Markdown.ToPlainText(markdown, _pipeline);

    public MarkdownStatistics Analyse(string? markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
        {
            return MarkdownStatistics.Empty;
        }

        var plain = ToPlainText(markdown);
        var words = CountWords(plain);

        return new MarkdownStatistics
        {
            Words = words,
            Characters = markdown.Length,
            CharactersWithoutSpaces = markdown.Count(character => !char.IsWhiteSpace(character)),
            Lines = markdown.AsSpan().Count('\n') + 1,
            ReadingTime = EstimateReadingTime(words)
        };
    }

    /// <summary>Counts whitespace-separated runs; punctuation stays attached to its word.</summary>
    private static int CountWords(string text)
    {
        var count = 0;
        var inWord = false;

        foreach (var character in text)
        {
            if (char.IsWhiteSpace(character))
            {
                inWord = false;
                continue;
            }

            if (!inWord)
            {
                inWord = true;
                count++;
            }
        }

        return count;
    }

    /// <summary>
    /// Exact reading time at <see cref="AppConstants.ReadingWordsPerMinute"/>, the figure commonly
    /// used for prose. Rounding belongs to the display layer, not to the measurement.
    /// </summary>
    private static TimeSpan EstimateReadingTime(int words)
        => TimeSpan.FromMinutes(words / (double)AppConstants.ReadingWordsPerMinute);
}
