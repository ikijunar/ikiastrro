using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

/// <summary>Raman combinations 81–100: remaining Akriti, Sankhya and Asraya Nabhasa yogas.</summary>
public static class RamanNabhasaSecondBatchEvaluator
{
    private static readonly PlanetName[] Classical =
    [
        PlanetName.Sun, PlanetName.Moon, PlanetName.Mars, PlanetName.Mercury,
        PlanetName.Jupiter, PlanetName.Venus, PlanetName.Saturn
    ];
    private static readonly PlanetName[] Benefics =
        [PlanetName.Moon, PlanetName.Mercury, PlanetName.Jupiter, PlanetName.Venus];
    private static readonly PlanetName[] Malefics =
        [PlanetName.Sun, PlanetName.Mars, PlanetName.Saturn];

    public static IReadOnlyList<ContextualYogaResult> Evaluate(ChartAnalysisInput d1)
    {
        var rows = new List<ContextualYogaResult>();
        foreach (var start in new[] { 1, 4, 7, 10 })
            rows.Add(Variant("YOGA_GADA", $"RAMAN_300_081_H{start:00}", 107, 119,
                OccupiesCompleteSet(d1, [start, Wrap(start + 3)]), $"Adjacent-kendra form beginning at house {start}."));

        rows.Add(Row("YOGA_SAKATA", 82, 107, 119, OccupiesCompleteSet(d1, [1, 7])));
        rows.Add(Row("YOGA_VIHAGA", 83, 107, 119, OccupiesCompleteSet(d1, [4, 10])));
        rows.Add(Row("YOGA_VAJRA", 84, 110, 122, Grouped(d1, Benefics, [1, 7]) && Grouped(d1, Malefics, [4, 10])));
        rows.Add(Row("YOGA_YAVA", 85, 110, 122, Grouped(d1, Malefics, [1, 7]) && Grouped(d1, Benefics, [4, 10])));
        rows.Add(Row("YOGA_SRINGHATAKA", 86, 111, 123, OccupiesCompleteSet(d1, [1, 5, 9])));
        foreach (var start in new[] { 2, 3, 4 })
            rows.Add(Variant("YOGA_HALA", $"RAMAN_300_087_H{start:00}", 111, 123,
                OccupiesCompleteSet(d1, [start, start + 4, start + 8]), $"Trinal-house form beginning at house {start}."));
        rows.Add(Row("YOGA_KAMALA", 88, 113, 125, OccupiesCompleteSet(d1, [1, 4, 7, 10])));
        rows.Add(Variant("YOGA_VAPEE", "RAMAN_300_089_PANAPARA", 113, 125,
            OccupiesCompleteSet(d1, [2, 5, 8, 11]), "Planets confined to the four Panapara houses."));
        rows.Add(Variant("YOGA_VAPEE", "RAMAN_300_089_APOKLIMA", 113, 125,
            OccupiesCompleteSet(d1, [3, 6, 9, 12]), "Planets confined to the four Apoklima houses."));
        rows.Add(Row("YOGA_SAMUDRA", 90, 114, 126, OccupiesCompleteSet(d1, [2, 4, 6, 8, 10, 12])));

        var anotherNabhasaPattern = rows.Any(x => x.Present == true)
            || RamanNabhasaBatchEvaluator.Evaluate(d1).Any(x => x.Present == true);
        AddSankhya(rows, d1, 91, "YOGA_VALLAKI", 7, 115, 127, anotherNabhasaPattern);
        AddSankhya(rows, d1, 92, "YOGA_DAMNI", 6, 117, 129, anotherNabhasaPattern);
        AddSankhya(rows, d1, 93, "YOGA_PASA", 5, 118, 130, anotherNabhasaPattern);
        AddSankhya(rows, d1, 94, "YOGA_KEDARA", 4, 120, 132, anotherNabhasaPattern);
        AddSankhya(rows, d1, 95, "YOGA_SULA", 3, 121, 133, anotherNabhasaPattern);
        AddSankhya(rows, d1, 96, "YOGA_YUGA", 2, 121, 133, anotherNabhasaPattern);
        AddSankhya(rows, d1, 97, "YOGA_GOLA_SANKHYA", 1, 121, 133, anotherNabhasaPattern);

        rows.Add(Row("YOGA_RAJJU", 98, 123, 135, OccupiesModality(d1, 0)));
        rows.Add(Row("YOGA_MUSALA", 99, 123, 135, OccupiesModality(d1, 1)));
        rows.Add(Row("YOGA_NALA", 100, 123, 135, OccupiesModality(d1, 2)));
        return rows;
    }

    private static void AddSankhya(List<ContextualYogaResult> rows, ChartAnalysisInput chart,
        int number, string code, int signCount, int printed, int scan, bool superseded)
    {
        var formation = Positions(chart) is { } positions && positions.Select(x => x.Sign).Distinct().Count() == signCount;
        rows.Add(new(code, formation && !superseded, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS",
            $"RAMAN_300_{number:000}", $"combination {number}; printed p.{printed}; scan p.{scan}",
            formation && superseded ? "Numerical formation exists but Raman says Sankhya loses individuality when another Nabhasa yoga is present." : null));
    }

    private static bool Grouped(ChartAnalysisInput chart, IEnumerable<PlanetName> planets, IReadOnlyCollection<int> houses)
    {
        var positions = planets.Select(p => Find(chart, p)).ToArray();
        return positions.All(x => x is not null) && positions.All(x => houses.Contains(x!.HouseNumber))
            && houses.All(h => positions.Any(x => x!.HouseNumber == h));
    }
    private static bool OccupiesCompleteSet(ChartAnalysisInput chart, IReadOnlyCollection<int> houses)
    {
        var positions = Positions(chart); if (positions is null) return false;
        var occupied = positions.Select(x => x.HouseNumber).ToArray();
        return occupied.All(houses.Contains) && houses.All(occupied.Contains);
    }
    private static bool OccupiesModality(ChartAnalysisInput chart, int remainder)
    {
        var positions = Positions(chart); if (positions is null) return false;
        return positions.All(x => ((int)Enum.Parse<ZodiacName>(x.Sign)) % 3 == remainder);
    }
    private static PlanetPosition[]? Positions(ChartAnalysisInput chart)
    {
        var positions = Classical.Select(p => Find(chart, p)).ToArray();
        return positions.Any(x => x is null) ? null : positions.Select(x => x!).ToArray();
    }
    private static int Wrap(int house) => ((house - 1) % 12) + 1;
    private static PlanetPosition? Find(ChartAnalysisInput chart, PlanetName planet)
        => chart.Planets.SingleOrDefault(x => x.Planet == planet.ToString());
    private static ContextualYogaResult Row(string code, int number, int printed, int scan, bool present)
        => Variant(code, $"RAMAN_300_{number:000}", printed, scan, present, null);
    private static ContextualYogaResult Variant(string code, string variant, int printed, int scan, bool present, string? notes)
        => new(code, present, "EVALUATED", "SRC_RAMAN_300_COMBINATIONS", variant,
            $"combination {variant.Substring(10, 3)}; printed p.{printed}; scan p.{scan}", notes);
}
