namespace DevhubWebApp.Constants;

/// <summary>Central route table. Pages declare the matching literal in their @page directive.</summary>
public static class AppRoutes
{
    public const string Dashboard = "/";
    public const string Settings = "/settings";
    public const string History = "/history";
    public const string About = "/about";
    public const string ImageTools = "/image-tools";

    /// <summary>Design-system gallery. Development aid, not linked from the product navigation.</summary>
    public const string Playground = "/playground";

    private const string ToolRoot = "/tools";

    /// <summary>Builds the canonical route for a tool id.</summary>
    public static string Tool(string toolId) => $"{ToolRoot}/{toolId}";
}
