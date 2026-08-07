using DevhubWebApp.Models.Tools;
using MudBlazor;

namespace DevhubWebApp.Constants;

/// <summary>
/// The category catalogue. Tools reference a category by id through <c>[Tool]</c>; adding a
/// category means adding one entry here and nothing else — navigation renders whatever it finds.
/// </summary>
public static class ToolCategories
{
    public const string FormattersId = "formatters";
    public const string EncodersId = "encoders";
    public const string GeneratorsId = "generators";
    public const string ConvertersId = "converters";
    public const string TextId = "text";
    public const string WebId = "web";

    private static readonly IReadOnlyList<ToolCategory> Catalogue =
    [
        new()
        {
            Id = FormattersId,
            Label = "Formatters",
            Icon = Icons.Material.Filled.FormatAlignLeft,
            Order = 10,
            Description = "Pretty-print and normalise structured documents."
        },
        new()
        {
            Id = EncodersId,
            Label = "Encoders & Crypto",
            Icon = Icons.Material.Filled.Lock,
            Order = 20,
            Description = "Encode, decode and digest payloads."
        },
        new()
        {
            Id = ConvertersId,
            Label = "Converters",
            Icon = Icons.Material.Filled.SwapHoriz,
            Order = 30,
            Description = "Translate values between representations."
        },
        new()
        {
            Id = GeneratorsId,
            Label = "Generators",
            Icon = Icons.Material.Filled.AutoAwesome,
            Order = 40,
            Description = "Produce identifiers, secrets and placeholder content."
        },
        new()
        {
            Id = TextId,
            Label = "Text",
            Icon = Icons.Material.Filled.Subject,
            Order = 50,
            Description = "Compare, transform and author text."
        },
        new()
        {
            Id = WebId,
            Label = "Web",
            Icon = Icons.Material.Filled.Public,
            Order = 60,
            Description = "Look up and dissect web protocol details."
        }
    ];

    /// <summary>All categories in navigation order.</summary>
    public static IReadOnlyList<ToolCategory> All => Catalogue;

    /// <summary>Resolves a category by id, or <c>null</c> when it is not registered.</summary>
    public static ToolCategory? Find(string id)
        => Catalogue.FirstOrDefault(category => string.Equals(category.Id, id, StringComparison.OrdinalIgnoreCase));
}
