namespace DevhubWebApp.Models.Tools.Lorem;

/// <summary>Auto-saved state of the Lorem Ipsum generator.</summary>
public sealed class LoremWorkspace
{
    public LoremUnit Unit { get; set; } = LoremUnit.Paragraphs;

    public int Count { get; set; } = 3;

    public bool StartWithLoremIpsum { get; set; } = true;

    public LoremFormat Format { get; set; } = LoremFormat.PlainText;

    /// <summary>When set, the same seed reproduces the same passage.</summary>
    public int? Seed { get; set; }

    /// <summary>Projects the persisted options onto the generator's contract.</summary>
    public LoremOptions ToOptions() => new()
    {
        Unit = Unit,
        Count = Count,
        StartWithLoremIpsum = StartWithLoremIpsum,
        Format = Format,
        Seed = Seed
    };
}
