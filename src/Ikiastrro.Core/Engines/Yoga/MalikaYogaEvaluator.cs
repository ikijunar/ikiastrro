using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public sealed record MalikaYogaResult(
    string YogaCode,
    string DisplayName,
    int StartingHouse,
    bool Present,
    string OutcomeNatureCode,
    string SourceRefCode,
    string SourceVariantCode,
    string SourceLocator);

/// <summary>Raman combinations 32–43: seven classical planets in seven distinct contiguous houses.</summary>
public static class MalikaYogaEvaluator
{
    private static readonly string[] Names =
    {
        "Lagna Malika", "Dhana Malika", "Vikrama Malika", "Sukha Malika",
        "Putra Malika", "Satru Malika", "Kalatra Malika", "Randhra Malika",
        "Bhagya Malika", "Karma Malika", "Labha Malika", "Vyaya Malika"
    };

    private static readonly string[] Codes =
    {
        "YOGA_MALIKA_LAGNA", "YOGA_MALIKA_DHANA", "YOGA_MALIKA_VIKRAMA", "YOGA_MALIKA_SUKHA",
        "YOGA_MALIKA_PUTRA", "YOGA_MALIKA_SATRU", "YOGA_MALIKA_KALATRA", "YOGA_MALIKA_RANDHRA",
        "YOGA_MALIKA_BHAGYA", "YOGA_MALIKA_KARMA", "YOGA_MALIKA_LABHA", "YOGA_MALIKA_VYAYA"
    };

    public static IReadOnlyList<MalikaYogaResult> Evaluate(ChartAnalysisInput chart)
    {
        var houses = chart.Planets
            .Where(x => Enum.TryParse<PlanetName>(x.Planet, out var planet)
                && planet is not PlanetName.Rahu and not PlanetName.Ketu)
            .Select(x => x.HouseNumber)
            .ToList();
        var hasSevenClassicalPlanets = houses.Count == 7;
        var distinct = houses.ToHashSet();

        return Enumerable.Range(1, 12).Select(start =>
        {
            var required = Enumerable.Range(0, 7).Select(offset => ((start + offset - 1) % 12) + 1).ToHashSet();
            var present = hasSevenClassicalPlanets && distinct.Count == 7 && distinct.SetEquals(required);
            var index = start - 1;
            return new MalikaYogaResult(Codes[index], Names[index], start, present, Nature(start),
                "SRC_RAMAN_300_COMBINATIONS", $"RAMAN_300_{start + 31:000}",
                $"combinations 32-43; {Names[index]}; printed p.65; scan pp.77-78");
        }).ToList();
    }

    private static string Nature(int startingHouse) => startingHouse switch
    {
        6 or 8 => "INAUSPICIOUS",
        3 or 7 or 11 => "MIXED",
        _ => "AUSPICIOUS"
    };
}
