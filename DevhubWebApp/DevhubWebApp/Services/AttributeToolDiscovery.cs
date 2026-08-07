using System.Reflection;
using DevhubWebApp.Constants;
using DevhubWebApp.Interfaces;
using DevhubWebApp.Models.Tools;
using Microsoft.AspNetCore.Components;

namespace DevhubWebApp.Services;

/// <summary>
/// Finds every Razor component annotated with <see cref="ToolAttribute"/> in the given assemblies.
/// Adding a tool is therefore a matter of adding one component file — nothing central changes.
/// </summary>
public sealed class AttributeToolDiscovery : IToolDiscovery
{
    private readonly IReadOnlyList<Assembly> _assemblies;

    /// <summary>Scans the application assembly by default.</summary>
    public AttributeToolDiscovery()
        : this(typeof(AttributeToolDiscovery).Assembly)
    {
    }

    public AttributeToolDiscovery(params Assembly[] assemblies) => _assemblies = assemblies;

    public IReadOnlyList<ToolDescriptor> Discover()
        => _assemblies
            .SelectMany(assembly => assembly.GetTypes())
            .Where(type => typeof(IComponent).IsAssignableFrom(type) && !type.IsAbstract)
            .Select(type => (Type: type, Attribute: type.GetCustomAttribute<ToolAttribute>(inherit: false)))
            .Where(candidate => candidate.Attribute is not null)
            .Select(candidate => Project(candidate.Type, candidate.Attribute!))
            .ToList();

    /// <summary>
    /// Turns the declarative attribute into a descriptor. An unknown category id is a programming
    /// error and fails fast at start-up rather than producing an invisible tool.
    /// </summary>
    private static ToolDescriptor Project(Type componentType, ToolAttribute attribute)
    {
        var category = ToolCategories.Find(attribute.CategoryId)
            ?? throw new InvalidOperationException(
                $"Tool '{attribute.Id}' ({componentType.Name}) declares unknown category '{attribute.CategoryId}'.");

        return new ToolDescriptor
        {
            Id = attribute.Id,
            Name = attribute.Name,
            Description = attribute.Description,
            Icon = attribute.Icon,
            Category = category,
            ComponentType = componentType,
            Keywords = attribute.Keywords,
            Tags = attribute.Tags,
            Version = attribute.Version,
            Order = attribute.Order,
            Color = attribute.Color,
            IsExperimental = attribute.IsExperimental
        };
    }
}
