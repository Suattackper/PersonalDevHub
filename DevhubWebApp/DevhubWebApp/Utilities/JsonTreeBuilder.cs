using System.Globalization;
using System.Text.Json;
using DevhubWebApp.Models.Tools.Json;

namespace DevhubWebApp.Utilities;

/// <summary>
/// Projects a JSON document into a display tree. Pure and bounded: very large documents are
/// truncated per level rather than freezing the UI with hundreds of thousands of nodes.
/// </summary>
public static class JsonTreeBuilder
{
    /// <summary>
    /// Maximum children materialised per container before a summary node is appended. Each node
    /// becomes a component, and rendering a few hundred of them is already noticeable on
    /// WebAssembly — the editor remains the place to read the whole document.
    /// </summary>
    public const int MaxChildrenPerNode = 100;

    /// <summary>Builds the tree, or returns <c>null</c> when the document does not parse.</summary>
    public static JsonTreeNode? Build(string? json)
    {
        if (string.IsNullOrWhiteSpace(json))
        {
            return null;
        }

        try
        {
            using var document = JsonDocument.Parse(json);
            return Build(document.RootElement);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    /// <summary>Builds the tree from an already-parsed document, avoiding a second parse.</summary>
    public static JsonTreeNode Build(JsonElement root) => BuildNode("root", "$", root);

    private static JsonTreeNode BuildNode(string name, string path, JsonElement element) => new()
    {
        Name = name,
        Path = path,
        Kind = element.ValueKind,
        Value = Describe(element),
        Children = BuildChildren(path, element)
    };

    private static IReadOnlyList<JsonTreeNode> BuildChildren(string path, JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                var properties = new List<JsonTreeNode>();
                foreach (var property in element.EnumerateObject())
                {
                    if (properties.Count == MaxChildrenPerNode)
                    {
                        properties.Add(Truncated(path));
                        break;
                    }

                    properties.Add(BuildNode(property.Name, $"{path}.{property.Name}", property.Value));
                }

                return properties;

            case JsonValueKind.Array:
                var items = new List<JsonTreeNode>();
                var index = 0;
                foreach (var item in element.EnumerateArray())
                {
                    if (items.Count == MaxChildrenPerNode)
                    {
                        items.Add(Truncated(path));
                        break;
                    }

                    items.Add(BuildNode($"[{index}]", $"{path}[{index}]", item));
                    index++;
                }

                return items;

            default:
                return Array.Empty<JsonTreeNode>();
        }
    }

    private static JsonTreeNode Truncated(string path) => new()
    {
        Name = "…",
        Path = path,
        Kind = JsonValueKind.Undefined,
        Value = $"more than {MaxChildrenPerNode} entries — open the editor to see them all"
    };

    private static string Describe(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.Object => $"{{ {CountLabel(element.EnumerateObject().Count(), "property", "properties")} }}",
        JsonValueKind.Array => $"[ {CountLabel(element.GetArrayLength(), "item", "items")} ]",
        JsonValueKind.String => $"\"{element.GetString()}\"",
        JsonValueKind.Number => element.GetRawText(),
        JsonValueKind.True => "true",
        JsonValueKind.False => "false",
        JsonValueKind.Null => "null",
        _ => string.Empty
    };

    private static string CountLabel(int count, string singular, string plural)
        => $"{count.ToString("N0", CultureInfo.InvariantCulture)} {(count == 1 ? singular : plural)}";
}
