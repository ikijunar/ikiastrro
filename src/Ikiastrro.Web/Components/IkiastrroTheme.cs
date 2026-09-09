using MudBlazor;

namespace Ikiastrro.Web.Components;

/// <summary>
/// App-wide MudBlazor theme wired to the Iki-Astrro brand palette
/// (<c>docs/ui/brand.md</c>). The warm canvas is used for both Background and
/// Surface so panels, menus and dropdowns sit on the same colour as the rest
/// of the app (and as the Ganesha artwork) — elevation shadow does the
/// separating, not a white fill.
///
/// Hex values are duplicated from <c>wwwroot/css/tokens.css</c> because a
/// <see cref="MudTheme"/> is C# and cannot read CSS custom properties. Keep the
/// two in sync; the CSS tokens remain the source of truth for everything else.
/// </summary>
public static class IkiastrroTheme
{
    private const string Canvas = "#FAF5EA";   // --brand-canvas
    private const string Midnight = "#0F2041"; // --brand-midnight
    private const string Sunset = "#F47A24";   // --brand-sunset
    private const string Line = "#D9D1C7";     // --brand-line
    private const string Muted = "#53698D";    // --brand-muted

    public static readonly MudTheme Value = new()
    {
        PaletteLight = new PaletteLight
        {
            Primary = Sunset,
            PrimaryContrastText = Midnight,
            Secondary = Midnight,
            Background = Canvas,
            BackgroundGray = Canvas,
            Surface = Canvas,
            AppbarBackground = Sunset,
            AppbarText = Midnight,
            DrawerBackground = Canvas,
            DrawerText = Midnight,
            TextPrimary = Midnight,
            TextSecondary = Muted,
            ActionDefault = Midnight,
            Divider = Line,
            LinesDefault = Line,
            LinesInputs = Line,
            TableLines = Line,
        },
    };
}
