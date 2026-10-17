using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dignity;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.Relationships;
using Ikiastrro.Core.Models;

using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>
/// Builds the tbl_Fact_PlanetaryState rows for one chart from the ChartKeyDetail list that
/// ChartAnalyzer.Compute already produced (it carries Sign / DegreesInSignDecimal /
/// DignityStatus per planet). ChartResultId is stamped by the caller after the parent ChartResult
/// row exists — same contract as ChartAnalyzer.
///
/// Ascendant is excluded (no avastha for a house-circle point). The age state is emitted only for
/// D1 (needs a continuous within-sign degree); the wakefulness state is emitted for every chart type.
/// Dīptādi/Lajjitādi are also emitted for every chart type — like Wakefulness, they only need
/// DignityStatus + conjunctions/aspects, all chart-type-agnostic.
/// </summary>
public static class PlanetaryStateComputer
{
    public static List<PlanetaryStateFact> Compute(
        ChartAnalysisInput input,
        IReadOnlyList<ChartKeyDetail> keyDetails,
        PlanetaryStateRuleSet rules,
        double? janmaGhatis = null,
        IReadOnlyList<ChartConjunction>? conjunctions = null,
        IReadOnlyList<ChartAspect>? aspects = null)
    {
        var isRasiChart = input.ChartType == "D1";
        var facts = new List<PlanetaryStateFact>();

        // Dīptādi/Lajjitādi shared inputs: natural-malefic predicate (reuses ArgalaCalculator's
        // already-sourced conditional Moon/Mercury reading), each planet's own sign (for
        // DignityEngine.EvaluatePairRelationship), and conjunct/aspecting-planet lookups built once
        // from the already-computed conjunction/aspect rows — not recomputed here.
        var grahaDetails = keyDetails.Where(k => k.PointKind == "Graha" && k.Planet != "Ascendant").ToList();
        var occupancy = ArgalaFactBuilder.BuildOccupancy(grahaDetails);
        bool IsNaturalMalefic(string planet) => ArgalaCalculator.IsNaturalMalefic(Enum.Parse<PlanetName>(planet), occupancy);
        var signByPlanet = grahaDetails
            .Where(k => Enum.TryParse<ZodiacName>(k.Sign, out _))
            .ToDictionary(k => k.Planet, k => Enum.Parse<ZodiacName>(k.Sign));

        var conjunctionsByPlanet = (conjunctions ?? Array.Empty<ChartConjunction>())
            .SelectMany(c => new[] { (Planet: c.Planet1, Other: c.Planet2), (Planet: c.Planet2, Other: c.Planet1) })
            .GroupBy(x => x.Planet)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(x => x.Other).ToList());
        var aspectingByTarget = (aspects ?? Array.Empty<ChartAspect>())
            .GroupBy(a => a.AspectedTarget)
            .ToDictionary(g => g.Key, g => (IReadOnlyList<string>)g.Select(a => a.AspectingPlanet).ToList());

        // Sayanaadi's chart-wide (not per-planet) inputs: M = Moon's nakshatra, L = Lagna's rasi,
        // G = the ghati running at birth. Resolved once; only used when isRasiChart.
        // kd.Nakshatra stores AstroMath's canonical display name ("Ashwini", "Uttara Phalguni", ...)
        // - NOT the ConstellationName enum member spelling (old VedAstro form, e.g. "Aswini",
        // "Uttara") - see ConstellationName's own doc comment. Look up against the canonical list.
        static int? NakshatraNumber(string? name)
        {
            if (name is null) return null;
            var index = AstroMath.NakshatraCanonicalNames.ToList().IndexOf(name);
            return index < 0 ? null : index + 1;
        }
        var moonNakshatraNumber = isRasiChart
            ? NakshatraNumber(keyDetails.FirstOrDefault(k => k.Planet == "Moon")?.Nakshatra)
            : null;
        var lagnaRasiNumber = isRasiChart ? (int)input.AscendantSign + 1 : (int?)null;
        var ghati = janmaGhatis is { } jg ? PostureStateCalculator.GhatiRunning(jg) : (int?)null;

        foreach (var kd in keyDetails)
        {
            // Ascendant + the special points (AL / A2–A12 / HL / Gulika / Maandi) are not grahas —
            // no avastha for a house-circle or reference point.
            if (kd.Planet == "Ascendant" || kd.PointKind != "Graha")
                continue;

            var fact = new PlanetaryStateFact
            {
                Planet = kd.Planet,
                PlanetId = (byte?)Ikiastrro.Core.Engines.Astronomy.AstroIds.PlanetIdOrNull(kd.Planet),
                RuleSetId = rules.RuleSetId,
            };

            // Wakefulness state — every chart type, from dignity.
            var wake = WakefulnessStateCalculator.For(kd.DignityStatus, rules.WakefulnessByDignity);
            fact.WakefulnessStateId = wake?.AvasthaStateId;
            if (wake is not null)
                fact.Reasons.Add(new StateReason("Jagradadi", wake.StateName, $"Dignity is {kd.DignityStatus}"));

            // Age state — D1 only, from within-sign degree.
            if (isRasiChart && kd.DegreesInSignDecimal is { } degree
                && Enum.TryParse<ZodiacName>(kd.Sign, out var sign))
            {
                var age = AgeStateCalculator.For(sign, degree, rules.AgeBands);
                fact.AgeStateId = age.AvasthaStateId;
                fact.AgeEffectFraction = age.EffectFraction;
                fact.Reasons.Add(new StateReason("Baaladi", age.StateName,
                    $"{degree:0.0}° in {sign}, {(AgeStateCalculator.IsOddSign(sign) ? "an odd" : "an even")} sign, falls in the {age.StateName} band ({age.EffectFraction:0.###} of its results)"));
            }

            // Posture (Sayanaadi) state — D1 only, needs the within-sign degree (for the
            // navamsa index), the planet's own nakshatra, and the chart-wide M/G/L above.
            if (isRasiChart && kd.DegreesInSignDecimal is { } degreeForNavamsa
                && moonNakshatraNumber is { } m && lagnaRasiNumber is { } l && ghati is { } g
                && NakshatraNumber(kd.Nakshatra) is { } c
                && fact.PlanetId is { } planetIndex)
            {
                var navamsa = PostureStateCalculator.NavamsaIndex(degreeForNavamsa);
                var index = PostureStateCalculator.ComputeIndex(c, planetIndex, navamsa, m, g, l);
                var posture = PostureStateCalculator.For(index, rules.PostureStatesBySequence);
                fact.PostureStateId = posture?.Id;
                if (posture is not null)
                    fact.Reasons.Add(new StateReason("Sayanadi", posture.StateName,
                        $"((C {c} × P {planetIndex} × A {navamsa}) + M {m} + G {g} + L {l}) mod 12 = {index}"));
            }

            // Dīptādi + Lajjitādi — every chart type, from dignity + this chart's own
            // conjunctions/aspects (already computed by ChartAnalyzer/RelationshipEngine).
            var conjunctWith = conjunctionsByPlanet.GetValueOrDefault(kd.Planet, Array.Empty<string>());
            var aspectedBy = aspectingByTarget.GetValueOrDefault(kd.Planet, Array.Empty<string>());

            // Relationship of the planet to its sign lord (null for nodes / a planet in its own sign)
            // and its real angular separation from the Sun (null for the Sun itself).
            string? relationToSignLord = null;
            if (kd.SignLordPlanet is { } signLord && signLord != kd.Planet
                && signByPlanet.TryGetValue(kd.Planet, out var planetSign)
                && signByPlanet.TryGetValue(signLord, out var lordSign))
                relationToSignLord = DignityEngine.EvaluatePairRelationship(kd.Planet, signLord, planetSign, lordSign);

            var sunDetail = grahaDetails.FirstOrDefault(k => k.Planet == "Sun");
            decimal? distanceFromSun = kd.Planet != "Sun" && sunDetail is not null
                ? CombustionEngine.AngularSeparation(kd.NirayanaLongitudeDegrees, sunDetail.NirayanaLongitudeDegrees)
                : null;

            fact.DeeptadiStateIds = DeeptadiStateCalculator.For(
                kd.DignityStatus, relationToSignLord, kd.SignLordPlanet, conjunctWith, distanceFromSun, rules);
            fact.Reasons.AddRange(DeeptadiStateCalculator.Explain(
                kd.DignityStatus, relationToSignLord, kd.SignLordPlanet, conjunctWith, distanceFromSun, rules));

            if (signByPlanet.TryGetValue(kd.Planet, out var ownSign))
            {
                string? RelationshipOf(string other) =>
                    signByPlanet.TryGetValue(other, out var otherSign)
                        ? DignityEngine.EvaluatePairRelationship(kd.Planet, other, ownSign, otherSign)
                        : null;

                fact.LajjitadiStateIds = LajjitadiStateCalculator.For(
                    kd.Sign, kd.DignityStatus, kd.HouseNumberFromLagna, conjunctWith, aspectedBy,
                    IsNaturalMalefic, RelationshipOf, rules);
                fact.Reasons.AddRange(LajjitadiStateCalculator.Explain(
                    kd.Sign, kd.DignityStatus, kd.HouseNumberFromLagna, conjunctWith, aspectedBy,
                    IsNaturalMalefic, RelationshipOf, rules));
            }

            facts.Add(fact);
        }

        return facts;
    }
}
