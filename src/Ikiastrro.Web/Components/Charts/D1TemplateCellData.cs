namespace Ikiastrro.Web.Components.Charts;

/// <summary>One planet's line inside a D1TemplateGrid cell — glyph, whole degrees within the sign,
/// and its retrograde/combust flags (rendered as "(..)" parens and a 🔥, matching SouthIndianGrid's
/// existing direction-suffix/combust-icon convention). <paramref name="Key"/> is the lowercase
/// planet name (e.g. "sun") used to look up that planet's identity color
/// (--tmpl-planet-<paramref name="Key"/>, 2026-09-05) — same key tokens.css's own --planet-* set
/// already uses. <paramref name="DignityScore"/> (2026-09-05) is
/// ChartViewModel.DignityScore(k.DignityStatus) — a signed -4..+4 strength score printed as
/// "Dg(+N)"/"Dg(-N)" right after the combust flame.</summary>
public sealed record TemplatePlanetLine(string Glyph, string Key, int DegreesInSign, bool IsRetrograde, bool IsCombust, int DignityScore);

/// <summary>One karaka/special-point line for D1TemplateGrid's Ras(L)/Hor(L) header row — a short
/// code (a CharaKaraka like "Ak", or a special-point code like "HL"/"AL"/"A7") flanked by its house
/// count from Lagna and from the natal Moon — the same two-axis gold/silver idea SouthIndianGrid
/// already uses for planet house badges, generalized here to karakas and special points.</summary>
public sealed record TemplatePointLine(string Code, int HouseFromLagna, int HouseFromMoon);
