using MudBlazor;

namespace DevhubWebApp.Models.Tools;

/// <summary>
/// Declares a Razor component as a DevHub tool. This attribute is the single source of tool
/// metadata: the registry discovers it by reflection, so no central list has to be edited when a
/// tool is added, removed or renamed.
/// </summary>
/// <example>
/// <code>
/// @attribute [Tool(ToolIds.Json, "JSON", ToolCategories.FormattersId,
///                  "Pretty-print, minify and validate JSON.", Icons.Material.Filled.DataObject,
///                  Order = 10, Keywords = new[] { "format", "minify" })]
/// </code>
/// </example>
[AttributeUsage(AttributeTargets.Class, Inherited = false, AllowMultiple = false)]
public sealed class ToolAttribute : Attribute
{
    /// <param name="id">Stable identifier. Persisted in storage and used in the route.</param>
    /// <param name="name">Display name.</param>
    /// <param name="categoryId">Id of a category registered in <c>ToolCategories</c>.</param>
    /// <param name="description">One-sentence summary shown in cards and search results.</param>
    /// <param name="icon">MudBlazor icon path.</param>
    public ToolAttribute(string id, string name, string categoryId, string description, string icon)
    {
        Id = id;
        Name = name;
        CategoryId = categoryId;
        Description = description;
        Icon = icon;
    }

    /// <summary>Stable identifier. Never change it once released.</summary>
    public string Id { get; }

    /// <summary>Display name.</summary>
    public string Name { get; }

    /// <summary>Category id resolved against the category catalogue at start-up.</summary>
    public string CategoryId { get; }

    /// <summary>One-sentence summary.</summary>
    public string Description { get; }

    /// <summary>MudBlazor icon path.</summary>
    public string Icon { get; }

    /// <summary>Extra search terms, e.g. "guid" for the UUID tool.</summary>
    public string[] Keywords { get; set; } = Array.Empty<string>();

    /// <summary>Free-form labels shown on the tool card, e.g. "offline", "rfc-4122".</summary>
    public string[] Tags { get; set; } = Array.Empty<string>();

    /// <summary>Module version, surfaced in the tool header.</summary>
    public string Version { get; set; } = "1.0.0";

    /// <summary>Sort position inside its category. Lower values come first.</summary>
    public int Order { get; set; } = 100;

    /// <summary>Accent color used by the tool's icon and highlights.</summary>
    public Color Color { get; set; } = Color.Primary;

    /// <summary>Marks the tool as experimental; the UI badges it and sorts it last.</summary>
    public bool IsExperimental { get; set; }
}
