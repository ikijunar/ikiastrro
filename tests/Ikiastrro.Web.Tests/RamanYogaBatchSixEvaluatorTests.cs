using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaBatchSixEvaluatorTests
{
    [Fact]
    public void Batch_Contains_61_Through_70()
    {
        var rows = RamanYogaBatchSixEvaluator.Evaluate(Chart(ZodiacName.Aries));
        Assert.Equal(10, rows.Count);
        Assert.Equal(Enumerable.Range(61, 10).Select(n => $"RAMAN_300_{n:000}"), rows.Select(x => x.SourceVariantCode));
    }

    [Fact]
    public void Vishnu_Garuda_And_Gola_Report_Missing_Context()
    {
        var rows = RamanYogaBatchSixEvaluator.Evaluate(Chart(ZodiacName.Aries));
        foreach (var number in new[] { 62, 66, 68 })
        {
            var row = Row(rows, number);
            Assert.Null(row.Present);
            Assert.Equal("NOT_EVALUATED", row.EvaluationStatus);
        }
    }

    [Fact]
    public void Ravi_Requires_Sun_In_Tenth_And_Tenth_Lord_With_Saturn_In_Third()
    {
        var chart = Chart(ZodiacName.Cancer, P("Sun", "Aries", 10), P("Mars", "Virgo", 3), P("Saturn", "Virgo", 3));
        Assert.True(Row(RamanYogaBatchSixEvaluator.Evaluate(chart), 65).Present);
    }

    [Fact]
    public void Gola_Evaluates_When_D9_And_Full_Moon_Context_Are_Available()
    {
        var d1 = Chart(ZodiacName.Aries, P("Moon", "Sagittarius", 9), P("Jupiter", "Sagittarius", 9), P("Venus", "Sagittarius", 9));
        var d9 = Chart(ZodiacName.Gemini, P("Mercury", "Gemini", 1));
        Assert.True(Row(RamanYogaBatchSixEvaluator.Evaluate(d1, d9, isFullMoon: true), 68).Present);
    }

    [Fact]
    public void Thrilochana_Uses_Mutual_Trines()
    {
        var chart = Chart(ZodiacName.Taurus, P("Sun", "Aries", 12), P("Moon", "Leo", 4), P("Mars", "Sagittarius", 8));
        Assert.True(Row(RamanYogaBatchSixEvaluator.Evaluate(chart), 69).Present);
    }

    private static ContextualYogaResult Row(IReadOnlyList<ContextualYogaResult> rows, int number) =>
        rows.Single(x => x.SourceVariantCode == $"RAMAN_300_{number:000}");
    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets) => new("D1", ascendant, planets.ToList());
    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null) =>
        new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
