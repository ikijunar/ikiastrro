using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.Nakshatras;

/// <summary>
/// Thin wrapper over the AstroMath nakshatra helpers, so "which engine owns nakshatra
/// linkage" has a named home. Extracted verbatim from ChartAnalyzer (2026-09-02).
/// </summary>
public static class NakshatraEngine
{
    public readonly record struct NakshatraDerivation(
        string LordPlanet, int NakshatraIndex, string SubLordPlanet, int OverallPadaIndex);

    public static NakshatraDerivation ForLongitude(double nirayanaLongitude) => new(
        LordPlanet:      AstroMath.GetNakshatraLord(nirayanaLongitude).ToString(),
        NakshatraIndex:  AstroMath.GetNakshatraIndexAndFractionElapsed(nirayanaLongitude).NakshatraIndex,
        SubLordPlanet:   AstroMath.GetNakshatraSubLord(nirayanaLongitude).ToString(),
        OverallPadaIndex: AstroMath.GetOverallPadaIndex(nirayanaLongitude));
}
