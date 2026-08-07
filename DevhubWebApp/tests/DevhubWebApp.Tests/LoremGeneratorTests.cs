using DevhubWebApp.Models.Tools.Lorem;
using DevhubWebApp.Utilities;
using Xunit;

namespace DevhubWebApp.Tests;

/// <summary>Generation, formatting and determinism rules of the placeholder text generator.</summary>
public class LoremGeneratorTests
{
    [Fact]
    public void Generate_ProducesTheRequestedNumberOfWords()
    {
        var text = LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Words, Count = 12 });

        Assert.Equal(12, text.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Generate_ProducesTheRequestedNumberOfSentences()
    {
        var text = LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Sentences, Count = 4, Seed = 7 });

        Assert.Equal(4, text.Count(character => character == '.'));
    }

    [Fact]
    public void Generate_ProducesTheRequestedNumberOfParagraphs()
    {
        var text = LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Paragraphs, Count = 3, Seed = 7 });

        var paragraphs = text.Split("\n\n", StringSplitOptions.RemoveEmptyEntries)
            .Where(block => !string.IsNullOrWhiteSpace(block))
            .ToList();

        Assert.Equal(3, paragraphs.Count);
    }

    [Fact]
    public void StartWithLoremIpsum_ControlsTheOpening()
    {
        var withClassic = LoremGenerator.Generate(new LoremOptions { Count = 1, Seed = 1 });
        var withoutClassic = LoremGenerator.Generate(new LoremOptions { Count = 1, Seed = 1, StartWithLoremIpsum = false });

        Assert.StartsWith(LoremGenerator.ClassicOpening, withClassic, StringComparison.Ordinal);
        Assert.DoesNotContain(LoremGenerator.ClassicOpening, withoutClassic, StringComparison.Ordinal);
    }

    /// <summary>The seed is what makes generated text usable in a test or a screenshot.</summary>
    [Fact]
    public void Generate_IsDeterministicForAGivenSeed()
    {
        var options = new LoremOptions { Count = 4, Seed = 42, StartWithLoremIpsum = false };

        Assert.Equal(LoremGenerator.Generate(options), LoremGenerator.Generate(options));
    }

    [Fact]
    public void Generate_DiffersBetweenSeeds()
    {
        var first = LoremGenerator.Generate(new LoremOptions { Count = 4, Seed = 1, StartWithLoremIpsum = false });
        var second = LoremGenerator.Generate(new LoremOptions { Count = 4, Seed = 2, StartWithLoremIpsum = false });

        Assert.NotEqual(first, second);
    }

    [Fact]
    public void HtmlFormat_WrapsEveryParagraph()
    {
        var html = LoremGenerator.Generate(new LoremOptions { Count = 2, Seed = 3, Format = LoremFormat.Html });

        Assert.Equal(2, html.Split("<p>", StringSplitOptions.RemoveEmptyEntries).Length);
        Assert.Contains("</p>", html, StringComparison.Ordinal);
    }

    [Fact]
    public void MarkdownFormat_SeparatesParagraphsWithABlankLine()
    {
        var markdown = LoremGenerator.Generate(new LoremOptions { Count = 2, Seed = 3, Format = LoremFormat.Markdown });

        Assert.Contains("\n\n", markdown.ReplaceLineEndings("\n"), StringComparison.Ordinal);
        Assert.DoesNotContain("<p>", markdown, StringComparison.Ordinal);
    }

    [Fact]
    public void Generate_ClampsTheCountToTheSupportedRange()
    {
        Assert.NotEmpty(LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Words, Count = 0 }));

        var capped = LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Words, Count = LoremGenerator.MaxCount + 100 });

        Assert.Equal(LoremGenerator.MaxCount, capped.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length);
    }

    [Fact]
    public void Generate_CapitalisesSentencesAndEndsThemWithAPeriod()
    {
        var text = LoremGenerator.Generate(new LoremOptions { Unit = LoremUnit.Sentences, Count = 2, Seed = 5, StartWithLoremIpsum = false });

        Assert.True(char.IsUpper(text[0]));
        Assert.EndsWith(".", text, StringComparison.Ordinal);
    }

    [Fact]
    public void Measure_CountsWordsAndCharacters()
    {
        var (words, characters) = LoremGenerator.Measure("one two three");

        Assert.Equal(3, words);
        Assert.Equal(13, characters);
    }

    [Fact]
    public void Measure_ReturnsZeroForBlankInput()
        => Assert.Equal((0, 0), LoremGenerator.Measure("   "));
}
