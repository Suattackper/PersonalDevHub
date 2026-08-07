namespace DevhubWebApp.Models.Tools.Colors;

/// <summary>Auto-saved state of the colour toolkit.</summary>
public sealed class ColorWorkspace
{
    /// <summary>The working colour, stored as text so any accepted notation round-trips.</summary>
    public string Input { get; set; } = "#4f9cf9";

    /// <summary>Name used for exported variables.</summary>
    public string PaletteName { get; set; } = "brand";

    public PaletteKind Palette { get; set; } = PaletteKind.Scale;

    /// <summary>Background used by the contrast checker.</summary>
    public string ContrastBackground { get; set; } = "#ffffff";

    /// <summary>Colours the user kept, newest first.</summary>
    public List<string> Favorites { get; set; } = [];

    /// <summary>Recently used colours, newest first.</summary>
    public List<string> History { get; set; } = [];
}
