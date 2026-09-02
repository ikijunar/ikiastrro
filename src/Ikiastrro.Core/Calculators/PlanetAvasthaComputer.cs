using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;

using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Calculators;

/// <summary>
/// Builds the tbl_Fact_PlanetAvastha rows for one chart from the ChartKeyDetail list that
/// ChartAnalyzer.Compute already produced (it carries Sign / DegreesInSignDecimal /
/// DignityStatus per planet). ChartResultId is stamped by the caller after the parent ChartResult
/// row exists — same contract as ChartAnalyzer.
///
/// Ascendant is excluded (no avastha for a house-circle point). Baaladi is emitted only for D1
/// (needs a continuous within-sign degree); Jagradadi is emitted for every chart type.
/// </summary>
public static class PlanetAvasthaComputer
{
    public static List<PlanetAvasthaFact> Compute(
        ChartAnalysisInput input,
        IReadOnlyList<ChartKeyDetail> keyDetails,
        AvasthaRuleSet rules)
    {
        var isRasiChart = input.ChartType == "D1";
        var facts = new List<PlanetAvasthaFact>();

        foreach (var kd in keyDetails)
        {
            // Ascendant + the special points (AL / A2–A12 / HL / Gulika / Maandi) are not grahas —
            // no avastha for a house-circle or reference point.
            if (kd.Planet == "Ascendant" || kd.PointKind != "Graha")
                continue;

            var fact = new PlanetAvasthaFact
            {
                Planet = kd.Planet,
                PlanetId = (byte?)Ikiastrro.Core.Engines.Astronomy.AstroIds.PlanetIdOrNull(kd.Planet),
                RuleSetId = rules.RuleSetId,
            };

            // Jagradadi — every chart type, from dignity.
            var jag = JagradadiAvastha.For(kd.DignityStatus, rules.JagradadiByDignity);
            fact.JagradadiStateId = jag?.AvasthaStateId;

            // Baaladi — D1 only, from within-sign degree.
            if (isRasiChart && kd.DegreesInSignDecimal is { } degree
                && Enum.TryParse<ZodiacName>(kd.Sign, out var sign))
            {
                var baaladi = BaaladiAvastha.For(sign, degree, rules.Baaladi);
                fact.BaaladiStateId = baaladi.AvasthaStateId;
                fact.BaaladiEffectFraction = baaladi.EffectFraction;
            }

            facts.Add(fact);
        }

        return facts;
    }
}
