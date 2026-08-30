using VedicHoroGen.Core.Astro;
using VedicHoroGen.Core.Models;

namespace VedicHoroGen.Core.Calculators;

/// <summary>
/// Builds every chart type's derived table rows (key-details, house-lordship, conjunctions, aspects)
/// from an already-computed ChartAnalysisInput. ChartResultId/BirthDetailId/Name are left unset here —
/// the caller stamps them after the parent ChartResult row exists (needs its identity value).
///
/// Chart-type-agnostic by design: dignity, house-lordship, conjunctions, and aspects only depend on
/// "which sign is each planet in" + "which sign is the Ascendant in" — not which divisional chart
/// produced those signs. Originally D1-only ("D1KeyDetailsComputer"); generalized 2026-08-24 so D9
/// (and any future D2/D10/...) reuses this same logic instead of a bespoke copy — see ChartKeyDetail's
/// isRasiChart-gated fields for the one place D1 genuinely differs (continuous degree vs. discrete
/// varga sign bucket).
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

        // All classical planets' current signs, needed by ClassicalDignity to locate a sign-lord's own
        // position for Panchadha Maitri (temporary friendship depends on where the lord itself sits).
        var allSigns = input.Planets
            .Where(p => p.Planet != "Ascendant")
            .ToDictionary(p => p.Planet, p => Enum.Parse<ZodiacName>(p.Sign));

        // Computed up front so each keyDetail row below can carry its own AspectingPlanets summary.
        var aspectResults = ClassicalRelationships.FindAspects(input);
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
            var dignity = ClassicalDignity.Evaluate(planet.Planet, sign, isRasiChart ? (double?)degreeInSign : null, allSigns);

            // Nakshatra lord and retrograde status are derived purely from the real longitude every
            // chart type carries (NirayanaLongitudeDegrees) — populated for D1 and D9 alike, unlike
            // Nakshatra/NakshatraPada display fields above which stay D1-only (2026-08-28).
            var nakshatraLordPlanet = AstroMath.GetNakshatraLord(nirayanaLongitude).ToString();
            var nakshatraIndex = AstroMath.GetNakshatraIndexAndFractionElapsed(nirayanaLongitude).NakshatraIndex;
            var nakshatraSubLordPlanet = AstroMath.GetNakshatraSubLord(nirayanaLongitude).ToString();

            // Combustion only applies to 6 of the 9 planets (not Sun/Rahu/Ketu) and not the Ascendant.
            // Same chart-type-relative longitude choice as sunCombustionLongitude above — D1 uses the
            // real longitude, a varga chart uses its own remapped longitude.
            var combustionLongitude = isRasiChart
                ? nirayanaLongitude
                : planet.VargaLongitudeDegrees
                    ?? throw new InvalidOperationException($"Chart data for {planet.Planet} is missing VargaLongitudeDegrees for chart type '{input.ChartType}'.");
            var combustion = ClassicalCombustion.IsApplicable(planet.Planet)
                ? ClassicalCombustion.Evaluate(planet.Planet, combustionLongitude, sunCombustionLongitude, planet.IsRetrograde)
                : null;

            keyDetails.Add(new ChartKeyDetail
            {
                ChartType = input.ChartType,
                Planet = planet.Planet,
                Sign = planet.Sign,
                DegreesInSignDisplay = isRasiChart ? planet.DegreesInSign : null,
                DegreesInSignDecimal = isRasiChart ? Math.Round(degreeInSign, 4) : null,
                NirayanaLongitudeDegrees = nirayanaLongitude,
                Nakshatra = isRasiChart ? planet.Nakshatra : null,
                NakshatraPada = isRasiChart ? planet.NakshatraPada : null,
                NakshatraLordPlanet = nakshatraLordPlanet,
                NakshatraId = (byte)(nakshatraIndex + 1),
                NakshatraPadaId = isRasiChart ? AstroMath.GetOverallPadaIndex(nirayanaLongitude) + 1 : null,
                NakshatraSubLordPlanet = nakshatraSubLordPlanet,
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
                DignityStatus = dignity.DignityStatus,
                AspectingPlanets = aspectingPlanetsByTarget.GetValueOrDefault(planet.Planet)
            });
        }

        // Quick lookup for house-lords: where does each planet actually sit (all 3 reckonings + dignity)?
        var placementByPlanet = keyDetails.Where(k => k.Planet != "Ascendant").ToDictionary(k => k.Planet);

        var houseLords = new List<ChartHouseLord>();
        for (var houseNumber = 1; houseNumber <= 12; houseNumber++)
        {
            var houseSign = ClassicalDignity.GetHouseSign(input.AscendantSign, houseNumber);
            var lordPlanet = ClassicalDignity.GetSignLord(houseSign);
            var lordPlacement = placementByPlanet[lordPlanet];

            houseLords.Add(new ChartHouseLord
            {
                ChartType = input.ChartType,
                HouseNumber = houseNumber,
                HouseSign = houseSign.ToString(),
                LordPlanet = lordPlanet,
                LordPlacedInHouseFromLagna = lordPlacement.HouseNumberFromLagna,
                LordPlacedInHouseFromSun = lordPlacement.HouseNumberFromSun,
                LordPlacedInHouseFromMoon = lordPlacement.HouseNumberFromMoon,
                LordPlacedInSign = lordPlacement.Sign,
                LordDignityStatus = lordPlacement.DignityStatus
            });
        }

        var conjunctions = ClassicalRelationships.FindConjunctions(input)
            .Select(c => new ChartConjunction
            {
                ChartType = input.ChartType,
                Planet1 = c.Planet1,
                Planet2 = c.Planet2,
                Sign = c.Sign,
                HouseNumberFromLagna = c.HouseNumberFromLagna,
                DegreeSeparation = c.DegreeSeparation
            })
            .ToList();

        var aspects = aspectResults
            .Select(a => new ChartAspect
            {
                ChartType = input.ChartType,
                AspectingPlanet = a.AspectingPlanet,
                AspectedTarget = a.AspectedTarget,
                AspectType = a.AspectType
            })
            .ToList();

        return (keyDetails, houseLords, conjunctions, aspects);
    }
}
