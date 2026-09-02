using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Relationships;

public record ConjunctionResult(string Planet1, string Planet2, string Sign, int HouseNumberFromLagna, decimal? DegreeSeparation);
public record AspectResult(string AspectingPlanet, string AspectedTarget, string AspectType);

/// <summary>
/// Conjunctions (Graha Yuti) and aspects (Graha Drishti) — computed purely from this project's own
/// already-computed ChartAnalysisInput, same rationale as DignityEngine: pure classical rules
/// applied to data this project's own pipeline produced, not re-derived via some engine's own
/// relationship helpers. Chart-type-agnostic: works identically for D1, D9, and any future divisional
/// chart, since "same sign" and "house offset" only need Sign/HouseNumber, not which chart produced them.
/// </summary>
public static class RelationshipEngine
{
    /// <summary>
    /// House-offsets (1 = same sign) each graha casts a full aspect on, counted from its own position.
    /// The 7th is universal to all 7 classical grahas. Mars/Jupiter/Saturn add their classical specials.
    /// Rahu/Ketu use the Jupiter-style convention (5th/7th/9th) per rammyps's decision (2026-08-24) —
    /// their aspect rule is genuinely disputed across texts, same as their exaltation/debilitation was.
    /// </summary>
    private static readonly Dictionary<string, int[]> AspectOffsets = new()
    {
        ["Sun"] = new[] { 7 },
        ["Moon"] = new[] { 7 },
        ["Mars"] = new[] { 4, 7, 8 },
        ["Mercury"] = new[] { 7 },
        ["Jupiter"] = new[] { 5, 7, 9 },
        ["Venus"] = new[] { 7 },
        ["Saturn"] = new[] { 3, 7, 10 },
        ["Rahu"] = new[] { 5, 7, 9 },
        ["Ketu"] = new[] { 5, 7, 9 }
    };

    private static string OffsetLabel(int offset) => offset switch
    {
        3 => "3rd", 4 => "4th", 5 => "5th", 7 => "7th", 8 => "8th", 9 => "9th", 10 => "10th",
        _ => $"{offset}th"
    };

    /// <summary>
    /// Conjunctions: pairs of the 9 grahas sharing the same sign in this chart. Ascendant excluded
    /// (not a graha). DegreeSeparation is only computed for D1 — see ConjunctionResult/ChartConjunction
    /// for why a varga chart's "same sign" can't be scored the same way.
    /// </summary>
    public static List<ConjunctionResult> FindConjunctions(ChartAnalysisInput input)
    {
        var isRasiChart = input.ChartType == "D1";
        var grahas = input.Planets.Where(p => p.Planet != "Ascendant").ToList();
        var results = new List<ConjunctionResult>();

        for (var i = 0; i < grahas.Count; i++)
        {
            for (var j = i + 1; j < grahas.Count; j++)
            {
                if (grahas[i].Sign != grahas[j].Sign) continue;

                decimal? separation = null;
                if (isRasiChart)
                {
                    var lon1 = grahas[i].NirayanaLongitudeDegrees!.Value;
                    var lon2 = grahas[j].NirayanaLongitudeDegrees!.Value;
                    var rawSeparation = (decimal)Math.Abs(lon1 - lon2);
                    if (rawSeparation > 180) rawSeparation = 360 - rawSeparation;
                    separation = Math.Round(rawSeparation, 4);
                }

                results.Add(new ConjunctionResult(
                    grahas[i].Planet, grahas[j].Planet, grahas[i].Sign, grahas[i].HouseNumber, separation));
            }
        }

        return results;
    }

    /// <summary>Aspects: directional, planet-to-(planet-or-Ascendant), per the classical house-offset rules above.</summary>
    public static List<AspectResult> FindAspects(ChartAnalysisInput input)
    {
        var results = new List<AspectResult>();

        foreach (var aspecting in input.Planets.Where(p => p.Planet != "Ascendant"))
        {
            var aspectingSign = Enum.Parse<ZodiacName>(aspecting.Sign);

            foreach (var offset in AspectOffsets[aspecting.Planet])
            {
                var aspectedSign = HouseEngine.GetHouseSign(aspectingSign, offset);

                foreach (var target in input.Planets)
                {
                    if (target.Planet == aspecting.Planet) continue;
                    if (target.Sign != aspectedSign.ToString()) continue;

                    results.Add(new AspectResult(aspecting.Planet, target.Planet, OffsetLabel(offset)));
                }
            }
        }

        return results;
    }

    /// <summary>
    /// tbl_Chart_Conjunctions rows for this chart. Lifted verbatim from ChartAnalyzer
    /// (2026-09-02 engine reorg) — the pair is canonicalized so Planet1Id &lt; Planet2Id.
    /// </summary>
    public static List<ChartConjunction> BuildConjunctionRows(ChartAnalysisInput input) =>
        FindConjunctions(input)
            .Select(c =>
            {
                // Canonicalize the pair so Planet1Id < Planet2Id, keeping the name pair aligned to the id pair.
                var idA = AstroIds.PlanetId(Enum.Parse<PlanetName>(c.Planet1));
                var idB = AstroIds.PlanetId(Enum.Parse<PlanetName>(c.Planet2));
                var (lowName, lowId, highName, highId) = idA <= idB
                    ? (c.Planet1, idA, c.Planet2, idB)
                    : (c.Planet2, idB, c.Planet1, idA);
                return new ChartConjunction
                {
                    Planet1 = lowName,
                    Planet1Id = lowId,
                    Planet2 = highName,
                    Planet2Id = highId,
                    Sign = c.Sign,
                    SignId = AstroIds.SignId(Enum.Parse<ZodiacName>(c.Sign)),
                    HouseNumberFromLagna = c.HouseNumberFromLagna,
                    DegreeSeparation = c.DegreeSeparation
                };
            })
            .ToList();

    /// <summary>
    /// tbl_Chart_Aspects rows for an already-computed FindAspects result. Lifted verbatim from
    /// ChartAnalyzer (2026-09-02 engine reorg) — the composer computes aspectResults once and
    /// reuses it for both the per-planet AspectingPlanets summary and these rows.
    /// </summary>
    public static List<ChartAspect> BuildAspectRows(IReadOnlyList<AspectResult> aspectResults) =>
        aspectResults
            .Select(a => new ChartAspect
            {
                AspectingPlanet = a.AspectingPlanet,
                AspectingPlanetId = AstroIds.PlanetId(Enum.Parse<PlanetName>(a.AspectingPlanet)),
                AspectedTarget = a.AspectedTarget,
                AspectedTargetType = a.AspectedTarget == "Ascendant" ? "Ascendant" : "Planet",
                AspectedPlanetId = AstroIds.PlanetIdOrNull(a.AspectedTarget),
                AspectType = a.AspectType
            })
            .ToList();
}
