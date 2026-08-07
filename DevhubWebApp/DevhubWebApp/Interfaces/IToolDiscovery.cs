using DevhubWebApp.Models.Tools;

namespace DevhubWebApp.Interfaces;

/// <summary>
/// Produces the raw tool catalogue. Implementations decide *where* tools come from — today by
/// scanning the assembly for <c>[Tool]</c> components — while the registry owns validation,
/// ordering and lookups.
/// </summary>
public interface IToolDiscovery
{
    /// <summary>Discovers every declared tool. Called once during registry construction.</summary>
    IReadOnlyList<ToolDescriptor> Discover();
}
