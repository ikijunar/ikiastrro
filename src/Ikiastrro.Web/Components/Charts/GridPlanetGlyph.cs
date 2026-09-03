namespace Ikiastrro.Web.Components.Charts;

/// <summary>
/// One planet glyph to render inside a <see cref="SouthIndianGrid"/> cell — e.g. "Su" with the
/// exalted-dignity dot color, or "As" (the Ascendant) with no dot, since it has no dignity of
/// its own. <paramref name="DignityToken"/> is the `--dignity-{token}` CSS variable suffix
/// (see <c>ChartViewModel.DignityToken</c>) — null means "don't render a dot."
///
/// <paramref name="IsRetrograde"/>/<paramref name="IsCombust"/> (2026-08-28) render as a "(D)"/"(R)"
/// direction suffix and a flame icon respectively. Always known (non-nullable) for every glyph this
/// record is built for — the Ascendant never gets a GridPlanetGlyph at all (BuildPlanetsBySign
/// filters it out before this is constructed), so there's no "no retrograde concept" case to model here.
/// </summary>
public sealed record GridPlanetGlyph(string PlanetName, string? DignityToken, bool IsRetrograde, bool IsCombust);
