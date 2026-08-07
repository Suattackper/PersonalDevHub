using MudBlazor;

namespace DevhubWebApp.Themes;

/// <summary>
/// The application palette and typography. Colors follow a VS Code "Dark Modern" /
/// GitHub Dark feel: deep neutral surfaces, a single blue accent, restrained borders.
/// </summary>
public static class DevHubTheme
{
    public const string SidebarWidth = "260px";
    public const string SidebarCollapsedWidth = "68px";
    public const string TopBarHeight = "56px";

    public static MudTheme Create() => new()
    {
        PaletteDark = new PaletteDark
        {
            Black = "#0b0d10",
            Primary = "#4f9cf9",
            PrimaryContrastText = "#0b0d10",
            Secondary = "#a371f7",
            Tertiary = "#3fb950",
            Info = "#4f9cf9",
            Success = "#3fb950",
            Warning = "#d29922",
            Error = "#f85149",
            Background = "#0d1117",
            BackgroundGray = "#010409",
            Surface = "#161b22",
            DrawerBackground = "#0d1117",
            DrawerText = "#c9d1d9",
            DrawerIcon = "#8b949e",
            AppbarBackground = "#161b22",
            AppbarText = "#e6edf3",
            TextPrimary = "#e6edf3",
            TextSecondary = "#8b949e",
            TextDisabled = "#484f58",
            ActionDefault = "#8b949e",
            ActionDisabled = "#484f58",
            Divider = "#30363d",
            DividerLight = "#21262d",
            TableLines = "#30363d",
            LinesDefault = "#30363d",
            LinesInputs = "#30363d",
            HoverOpacity = 0.08,
            OverlayDark = "rgba(1,4,9,0.72)"
        },
        PaletteLight = new PaletteLight
        {
            Primary = "#0969da",
            Secondary = "#8250df",
            Tertiary = "#1a7f37",
            Info = "#0969da",
            Success = "#1a7f37",
            Warning = "#9a6700",
            Error = "#cf222e",
            Background = "#ffffff",
            BackgroundGray = "#f6f8fa",
            Surface = "#f6f8fa",
            DrawerBackground = "#f6f8fa",
            DrawerText = "#1f2328",
            DrawerIcon = "#57606a",
            AppbarBackground = "#ffffff",
            AppbarText = "#1f2328",
            TextPrimary = "#1f2328",
            TextSecondary = "#57606a",
            Divider = "#d0d7de",
            DividerLight = "#eaeef2",
            LinesDefault = "#d0d7de",
            LinesInputs = "#d0d7de"
        },
        LayoutProperties = new LayoutProperties
        {
            DefaultBorderRadius = "8px",
            DrawerWidthLeft = SidebarWidth,
            DrawerMiniWidthLeft = SidebarCollapsedWidth,
            AppbarHeight = TopBarHeight
        },
        Typography = new Typography
        {
            Default = new DefaultTypography
            {
                FontFamily = ["Inter", "Segoe UI", "system-ui", "-apple-system", "sans-serif"],
                FontSize = "0.875rem",
                LineHeight = "1.5"
            },
            H5 = new H5Typography { FontSize = "1.35rem", FontWeight = "600" },
            H6 = new H6Typography { FontSize = "1.05rem", FontWeight = "600" },
            Subtitle1 = new Subtitle1Typography { FontSize = "0.9rem", FontWeight = "600" },
            Subtitle2 = new Subtitle2Typography { FontSize = "0.8rem", FontWeight = "600" },
            Body2 = new Body2Typography { FontSize = "0.8125rem" },
            Caption = new CaptionTypography { FontSize = "0.75rem" },
            Button = new ButtonTypography { FontWeight = "600", TextTransform = "none" }
        }
    };
}
