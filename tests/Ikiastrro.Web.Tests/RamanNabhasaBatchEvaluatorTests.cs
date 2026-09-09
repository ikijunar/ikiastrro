using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanNabhasaBatchEvaluatorTests
{
    [Fact]
    public void Yupa_Requires_Seven_Classical_Planets_Across_All_First_Four_Houses()
    {
        var chart = ClassicalChart(1, 1, 2, 2, 3, 3, 4);
        Assert.True(Row(RamanNabhasaBatchEvaluator.Evaluate(chart), "RAMAN_300_071").Present);
    }

    [Fact]
    public void Four_House_Pattern_Does_Not_Match_If_One_Named_House_Is_Empty()
    {
        var chart = ClassicalChart(1, 1, 2, 2, 3, 3, 3);
        Assert.False(Row(RamanNabhasaBatchEvaluator.Evaluate(chart), "RAMAN_300_071").Present);
    }

    [Fact]
    public void Danda_And_Chapa_Wrap_Across_Twelfth_House()
    {
        Assert.True(Row(RamanNabhasaBatchEvaluator.Evaluate(ClassicalChart(10, 10, 11, 11, 12, 12, 1)), "RAMAN_300_074").Present);
        Assert.True(Row(RamanNabhasaBatchEvaluator.Evaluate(ClassicalChart(10, 11, 12, 1, 2, 3, 4)), "RAMAN_300_078").Present);
    }

    [Fact]
    public void Ardha_Chandra_Preserves_Eight_Starting_House_Variants()
    {
        var rows = RamanNabhasaBatchEvaluator.Evaluate(ClassicalChart(12, 1, 2, 3, 4, 5, 6))
            .Where(x => x.SourceVariantCode.StartsWith("RAMAN_300_079")).ToList();
        Assert.Equal(8, rows.Count);
        Assert.True(Row(rows, "RAMAN_300_079_H12").Present);
        Assert.Single(rows, x => x.Present == true);
    }

    [Fact]
    public void Chandra_Requires_All_Six_Odd_Houses_To_Be_Occupied()
    {
        var chart = ClassicalChart(1, 1, 3, 5, 7, 9, 11);
        Assert.True(Row(RamanNabhasaBatchEvaluator.Evaluate(chart), "RAMAN_300_080").Present);
    }

    [Fact]
    public void Nodes_Do_Not_Affect_Seven_Planet_Distribution()
    {
        var chart = ClassicalChart(1, 1, 2, 2, 3, 3, 4);
        chart.Planets.Add(P("Rahu", 9));
        chart.Planets.Add(P("Ketu", 3));
        Assert.True(Row(RamanNabhasaBatchEvaluator.Evaluate(chart), "RAMAN_300_071").Present);
    }

    private static ContextualYogaResult Row(IEnumerable<ContextualYogaResult> rows, string variant)
        => rows.Single(x => x.SourceVariantCode == variant);
    private static ChartAnalysisInput ClassicalChart(params int[] houses)
    {
        var names = new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" };
        return new("D1", ZodiacName.Aries, names.Zip(houses, P).ToList());
    }
    private static PlanetPosition P(string planet, int house)
        => new() { Planet = planet, HouseNumber = house, Sign = ((ZodiacName)((house - 1) % 12)).ToString() };
}
