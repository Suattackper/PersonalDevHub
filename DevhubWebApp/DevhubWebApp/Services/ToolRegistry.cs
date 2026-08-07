using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Services;

/// <summary>
/// Validates and orders whatever <see cref="IToolDiscovery"/> found. Ordering is computed once at
/// construction, so navigation, search and the dashboard all share one consistent sequence.
/// </summary>
public sealed class ToolRegistry : IToolRegistry
{
    private readonly Dictionary<string, ToolDescriptor> _byId;
    private readonly ILookup<string, ToolDescriptor> _byCategory;

    public ToolRegistry(IToolDiscovery discovery)
    {
        ArgumentNullException.ThrowIfNull(discovery);

        Tools = Order(discovery.Discover());
        _byId = BuildIndex(Tools);
        _byCategory = Tools.ToLookup(tool => tool.Category.Id, StringComparer.OrdinalIgnoreCase);

        Categories = Tools
            .Select(tool => tool.Category)
            .Distinct()
            .OrderBy(category => category.Order)
            .ThenBy(category => category.Label, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    public IReadOnlyList<ToolDescriptor> Tools { get; }

    public IReadOnlyList<ToolCategory> Categories { get; }

    public ToolDescriptor? Find(string? toolId)
        => string.IsNullOrWhiteSpace(toolId) ? null : _byId.GetValueOrDefault(toolId);

    public IReadOnlyList<ToolDescriptor> Search(string? term)
        => Tools.Where(tool => tool.Matches(term)).ToList();

    public IReadOnlyList<ToolDescriptor> InCategory(string categoryId)
        => _byCategory[categoryId].ToList();

    /// <summary>Experimental tools sort last so the catalogue leads with stable modules.</summary>
    private static List<ToolDescriptor> Order(IEnumerable<ToolDescriptor> tools)
        => tools
            .OrderBy(tool => tool.Category.Order)
            .ThenBy(tool => tool.IsExperimental)
            .ThenBy(tool => tool.Order)
            .ThenBy(tool => tool.Name, StringComparer.OrdinalIgnoreCase)
            .ToList();

    /// <summary>A duplicate id would make one of the tools unreachable, so it fails at start-up.</summary>
    private static Dictionary<string, ToolDescriptor> BuildIndex(IEnumerable<ToolDescriptor> tools)
    {
        var index = new Dictionary<string, ToolDescriptor>(StringComparer.OrdinalIgnoreCase);

        foreach (var tool in tools)
        {
            if (!index.TryAdd(tool.Id, tool))
            {
                throw new InvalidOperationException(
                    $"Duplicate tool id '{tool.Id}' declared by {tool.ComponentType.Name} and {index[tool.Id].ComponentType.Name}.");
            }
        }

        return index;
    }
}
