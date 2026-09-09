using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 71–80: Akriti/Nabhasa distribution yogas.</summary>
public static class RamanNabhasaBatchEvaluator
{
    private static readonly PlanetName[] Classical =
    [
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
        PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn
    ];

    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput d1)
    {
        var rows = new List<ContextualYogaResult>
        {
            Row("YOGA_YUPA", 71, 103, 115, OccupiesCompleteRun(d1, 1, 4)),
            Row("YOGA_ISHU", 72, 103, 115, OccupiesCompleteRun(d1, 4, 4)),
            Row("YOGA_SAKTI", 73, 103, 115, OccupiesCompleteRun(d1, 7, 4)),
            Row("YOGA_DANDA", 74, 103, 115, OccupiesCompleteRun(d1, 10, 4)),
            Row("YOGA_NAV", 75, 105, 117, OccupiesCompleteRun(d1, 1, 7)),
            Row("YOGA_KUTA", 76, 105, 117, OccupiesCompleteRun(d1, 4, 7)),
            Row("YOGA_CHHATRA", 77, 105, 117, OccupiesCompleteRun(d1, 7, 7)),
            Row("YOGA_CHAPA", 78, 105, 117, OccupiesCompleteRun(d1, 10, 7))
        };

        foreach (var start in new[] { 2, 3, 5, 6, 8, 9, 11, 12 })
            rows.Add(Variant("YOGA_ARDHA_CHANDRA", $"RAMAN_300_079_H{start:00}", 106, 118,
                OccupiesCompleteRun(d1, start, 7), $"One of Raman's eight Panapara/Apoklima starting-house forms; starts at house {start}."));

        rows.Add(Row("YOGA_CHANDRA", 80, 107, 119, OccupiesCompleteSet(d1, [1, 3, 5, 7, 9, 11])));
        return rows;
    }

    private static bool OccupiesCompleteRun(ChartAnalysisInput chart, int start, int length)
        => OccupiesCompleteSet(chart, Enumerable.Range(0, length).Select(offset => Wrap(start + offset)).ToArray());

    private static bool OccupiesCompleteSet(ChartAnalysisInput chart, IReadOnlyCollection<int> houses)
    {
        var positions = Classical.Select(planet => Find(chart, planet)).ToArray();
        if (positions.Any(position => position is null)) return false;
        var occupied = positions.Select(position => position!.HouseNumber).ToArray();
        return occupied.All(houses.Contains) && houses.All(occupied.Contains);
    }

    private static int Wrap(int house) => ((house - 1) % 12) + 1;
    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet)
        => chart.Planets.SingleOrDefault(position => position.Planet == planet.ToString());
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present)
        => Variant(code, $"RAMAN_300_{number:000}", printed, scan, present, null);
    private static ContextualYogaResult Variant(string code, string variant, int printed, int scan, bool present, string? notes)
        => new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", variant,
            $"combination {variant.Substring(10, 3)}; printed p.{printed}; scan p.{scan}", notes);
}
