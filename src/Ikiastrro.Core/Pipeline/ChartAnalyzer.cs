using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dignity;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Nakshatras;
using Ikiastrro.Core.Engines.Relationships;
using Ikiastrro.Core.Models;

namespace Ikiastrro.Core.Pipeline;

/// <summary>
/// Builds every chart type's derived table rows (key-details, house-lordship, conjunctions, aspects)
/// from an already-computed ChartAnalysisInput. ChartResultId is left unset here — the caller stamps
/// it after the parent ChartResult row exists (needs its identity value). Person + chart type live
/// only on that parent row now (schema normalization, migration 08).
///
/// Chart-type-agnostic by design: dignity, house-lordship, conjunctions, and aspects only depend on
/// "which sign is each planet in" + "which sign is the Ascendant in" — not which divisional chart
/// produced those signs. Originally D1-only ("D1KeyDetailsComputer"); generalized 2026-08-24 so D9
/// (and any future D2/D10/...) reuses this same logic instead of a bespoke copy — see ChartKeyDetail's
/// isRasiChart-gated fields for the one place D1 genuinely differs (continuous degree vs. discrete
/// varga sign bucket).
///
/// Thin composer since the 2026-09-02 engine reorg: house-lordship, conjunction/aspect row mapping,
/// and the nakshatra derivation moved verbatim to HouseEngine / RelationshipEngine / NakshatraEngine.
/// Only the interwoven keyDetails + special-points loops stay here — they read from several engines
/// at once and have no single owner.
/// </summary>
public static class ChartAnalyzer
{
    public static (List<ChartKeyDetail> KeyDetails, List<ChartHouseLord> HouseLords, List<ChartConjunction> Conjunctions, List<ChartAspect> Aspects)
        Compute(ChartAnalysisInput input)
    {
        var isRasiChart = input.ChartType == "D1";

        var sunSign = Enum.Parse<ZodiacName>(input.Planets.First(p => p.Planet == "Sun").Sign);
        var moonSign = Enum.Parse<ZodiacName>(input.Planets.First(p => p.Planet == "Moon").Sign);
        var sunPlanet = input.Planets.First(p => p.Planet == "Sun");

        // Combustion must be evaluated within the chart type actually being analyzed: D1 uses the
        // Sun's real longitude, but a varga chart (D9, ...) uses the Sun's longitude remapped into
        // that varga's own 0-360° space — otherwise every varga silently reuses the D1 distance
        // instead of computing its own (2026-08-28 fix; see VargaLongitudeDegrees on PlanetPosition).
        var sunCombustionLongitude = isRasiChart
            ? sunPlanet.NirayanaLongitudeDegrees
                ?? throw new InvalidOperationException("Chart data for Sun is missing NirayanaLongitudeDegrees.")
            : sunPlanet.VargaLongitudeDegrees
                ?? throw new InvalidOperationException($"Chart data for Sun is missing VargaLongitudeDegrees for chart type '{input.ChartType}'.");

        // All classical planets' current signs, needed by DignityEngine to locate a sign-lord's own
        // position for Panchadha Maitri (temporary friendship depends on where the lord itself sits).
        var allSigns = input.Planets
            .Where(p => p.Planet != "Ascendant")
            .ToDictionary(p => p.Planet, p => Enum.Parse<ZodiacName>(p.Sign));

        // Computed up front so each keyDetail row below can carry its own AspectingPlanets summary.
        var aspectResults = RelationshipEngine.FindAspects(input);
        var aspectingPlanetsByTarget = aspectResults
            .GroupBy(a => a.AspectedTarget)
            .ToDictionary(g => g.Key, g => string.Join(", ", g.Select(a => $"{a.AspectingPlanet} ({a.AspectType})")));

        var keyDetails = new List<ChartKeyDetail>();
        foreach (var planet in input.Planets)
        {
            var sign = Enum.Parse<ZodiacName>(planet.Sign);
            var nirayanaLongitude = planet.NirayanaLongitudeDegrees
                ?? throw new InvalidOperationException($"Chart data for {planet.Planet} is missing NirayanaLongitudeDegrees.");
            var degreeInSign = (decimal)(nirayanaLongitude % 30);
            var dignity = DignityEngine.Evaluate(planet.Planet, sign, isRasiChart ? (double?)degreeInSign : null, allSigns);

            // The planet's longitude in THIS chart's own 360-degree space. For D1 it IS the real
            // longitude; for a varga it's the (real x N) mod 360 value the computer already stamped.
            var vargaLongitude = isRasiChart
                ? nirayanaLongitude
                : planet.VargaLongitudeDegrees
                    ?? throw new InvalidOperationException($"Chart data for {planet.Planet} is missing VargaLongitudeDegrees for chart type '{input.ChartType}'.");
            var degreeInVargaSign = (decimal)(vargaLongitude % 30);

            // Nakshatra lord and retrograde status are derived purely from the real longitude every
            // chart type carries (NirayanaLongitudeDegrees) — populated for D1 and D9 alike, unlike
            // Nakshatra/NakshatraPada display fields above which stay D1-only (2026-08-28).
            var nak = NakshatraEngine.ForLongitude(nirayanaLongitude);
            var nakshatraLordPlanet = nak.LordPlanet;
            var nakshatraIndex = nak.NakshatraIndex;
            var nakshatraSubLordPlanet = nak.SubLordPlanet;

            // Combustion only applies to 6 of the 9 planets (not Sun/Rahu/Ketu) and not the Ascendant.
            // Same chart-type-relative longitude choice as sunCombustionLongitude above — D1 uses the
            // real longitude, a varga chart uses its own remapped longitude.
            var combustionLongitude = isRasiChart
                ? nirayanaLongitude
                : planet.VargaLongitudeDegrees
                    ?? throw new InvalidOperationException($"Chart data for {planet.Planet} is missing VargaLongitudeDegrees for chart type '{input.ChartType}'.");
            var combustion = CombustionEngine.IsApplicable(planet.Planet)
                ? CombustionEngine.Evaluate(planet.Planet, combustionLongitude, sunCombustionLongitude, planet.IsRetrograde)
                : null;

            keyDetails.Add(new ChartKeyDetail
            {
                Planet = planet.Planet,
                PlanetId = AstroIds.PlanetIdOrNull(planet.Planet),
                Sign = planet.Sign,
                SignId = AstroIds.SignId(sign),
                DegreesInSignDisplay = isRasiChart
                    ? planet.DegreesInSign
                    : AstroMath.FormatDegreesMinutesSeconds(vargaLongitude % 30),
                DegreesInSignDecimal = Math.Round(degreeInVargaSign, 4),
                NirayanaLongitudeDegrees = nirayanaLongitude,
                VargaLongitudeDegrees = vargaLongitude,
                EclipticLatitudeDegrees = planet.EclipticLatitudeDegrees,
                SpeedLongitudeDegPerDay = planet.SpeedLongitudeDegPerDay,
                Nakshatra = isRasiChart ? planet.Nakshatra : null,
                NakshatraPada = isRasiChart ? planet.NakshatraPada : null,
                NakshatraLordPlanet = nakshatraLordPlanet,
                NakshatraLordPlanetId = AstroIds.PlanetIdOrNull(nakshatraLordPlanet),
                NakshatraId = (byte)(nakshatraIndex + 1),
                NakshatraPadaId = isRasiChart ? nak.OverallPadaIndex + 1 : null,
                NakshatraSubLordPlanet = nakshatraSubLordPlanet,
                NakshatraSubLordPlanetId = AstroIds.PlanetIdOrNull(nakshatraSubLordPlanet),
                IsRetrograde = planet.IsRetrograde,
                IsCombust = combustion?.IsCombust,
                DistanceFromSunDegrees = combustion?.DistanceFromSunDegrees,
                CombustionOrbUsedDegrees = combustion?.OrbUsedDegrees,
                HouseNumberFromLagna = planet.HouseNumber,
                HouseNumberFromSun = AstroMath.CountFromSignToSign(sunSign, sign),
                HouseNumberFromMoon = AstroMath.CountFromSignToSign(moonSign, sign),
                OwnSigns = dignity.OwnSigns,
                ExaltationSign = dignity.ExaltationSign,
                DebilitationSign = dignity.DebilitationSign,
                MoolatrikonaSign = dignity.MoolatrikonaSign,
                MoolatrikonaRange = dignity.MoolatrikonaRange,
                SignLordPlanet = dignity.SignLordPlanet,
                SignLordPlanetId = AstroIds.PlanetIdOrNull(dignity.SignLordPlanet),
                DignityStatus = dignity.DignityStatus,
                AspectingPlanets = aspectingPlanetsByTarget.GetValueOrDefault(planet.Planet)
            });
        }

        // Special points (AL, A2..A12, HL, Gulika, Maandi) already projected into this chart's zodiac
        // by SpecialPointProjector. Position-only rows: no dignity/nakshatra/combustion/aspects/karaka
        // (CK_KeyDetails_NonGrahaNulls enforces it).
        foreach (var sp in input.SpecialPoints)
        {
            var spSign = Enum.Parse<ZodiacName>(sp.Sign);
            var spVargaLon = sp.VargaLongitudeDegrees ?? sp.NirayanaLongitudeDegrees ?? 0;
            keyDetails.Add(new ChartKeyDetail
            {
                Planet = sp.Planet,               // "AL", "A2".."A12", later "HL"/"Gulika"/"Maandi"
                PointKind = sp.PointKind,
                Sign = sp.Sign,
                SignId = AstroIds.SignId(spSign),
                NirayanaLongitudeDegrees = sp.NirayanaLongitudeDegrees ?? 0,
                VargaLongitudeDegrees = spVargaLon,
                DegreesInSignDisplay = AstroMath.FormatDegreesMinutesSeconds(spVargaLon % 30),
                DegreesInSignDecimal = Math.Round((decimal)(spVargaLon % 30), 4),
                HouseNumberFromLagna = sp.HouseNumber,
                HouseNumberFromSun = AstroMath.CountFromSignToSign(sunSign, spSign),
                HouseNumberFromMoon = AstroMath.CountFromSignToSign(moonSign, spSign),
                // everything else stays null — CK_KeyDetails_NonGrahaNulls enforces it
            });
        }

        // Quick lookup for house-lords: where does each planet actually sit (all 3 reckonings + dignity)?
        var placementByPlanet = keyDetails
            .Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant")
            .ToDictionary(k => k.Planet);

        var houseLords   = HouseEngine.BuildHouseLords(input, placementByPlanet);
        var conjunctions = RelationshipEngine.BuildConjunctionRows(input);
        var aspects      = RelationshipEngine.BuildAspectRows(aspectResults);

        return (keyDetails, houseLords, conjunctions, aspects);
    }
}
