namespace DevhubWebApp.Models.Tools.Barcode;

/// <summary>Auto-saved state of the barcode generator.</summary>
public sealed class BarcodeWorkspace
{
    public Symbology Symbology { get; set; } = Symbology.Code128;

    public string Data { get; set; } = string.Empty;

    /// <summary>Width of a single module in pixels; the main size control.</summary>
    public int ModuleWidth { get; set; } = 2;

    public int Height { get; set; } = 90;

    public int Margin { get; set; } = 12;

    public int FontSize { get; set; } = 14;

    /// <summary>Print the human-readable text under the bars.</summary>
    public bool ShowText { get; set; } = true;

    public string Foreground { get; set; } = "#000000";

    public string Background { get; set; } = "#ffffff";
}
