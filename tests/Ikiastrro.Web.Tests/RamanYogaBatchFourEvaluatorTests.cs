using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaBatchFourEvaluatorTests
{
    [Fact]
    public void Missing_D9_Leaves_Mridanga_Unevaluated()
    {
        var row = Row(RamanYogaBatchFourEvaluator.Evaluate(Chart(ZodiacName.Aries)), "RAMAN_300_046");
        Assert.Null(row.Present);
        Assert.Equal("NOT_EVALUATED", row.EvaluationStatus);
    }

    [Fact]
    public void Gaja_Uses_Seventh_Lord_In_Eleventh_With_Moon_And_Eleventh_Lord_Aspect()
    {
        var chart = Chart(ZodiacName.Capricornus,
            P("Moon", "Scorpio", 11), P("Mars", "Aries", 4));
        Assert.True(Row(RamanYogaBatchFourEvaluator.Evaluate(chart), "RAMAN_300_048").Present);
    }

    [Fact]
    public void Kalanidhi_Preserves_Classical_And_Observed_Variants()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Jupiter", "Taurus", 2), P("Venus", "Taurus", 2));
        var rows = RamanYogaBatchFourEvaluator.Evaluate(chart);
        Assert.True(Row(rows, "RAMAN_300_049_CLASSICAL").Present);
        Assert.True(Row(rows, "RAMAN_300_049_OBSERVED").Present);
    }

    [Fact]
    public void Observed_Kalanidhi_Extends_To_Ninth_But_Classical_Does_Not()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Jupiter", "Sagittarius", 9), P("Mercury", "Sagittarius", 9));
        var rows = RamanYogaBatchFourEvaluator.Evaluate(chart);
        Assert.False(Row(rows, "RAMAN_300_049_CLASSICAL").Present);
        Assert.True(Row(rows, "RAMAN_300_049_OBSERVED").Present);
    }

    [Fact]
    public void Amsavatara_Requires_Movable_Lagna_Kendra_Benefics_And_Exalted_Saturn()
    {
        var chart = Chart(ZodiacName.Cancer,
            P("Venus", "Cancer", 1), P("Jupiter", "Aries", 10), P("Saturn", "Libra", 4, 185));
        Assert.True(Row(RamanYogaBatchFourEvaluator.Evaluate(chart), "RAMAN_300_050").Present);
    }

    [Fact]
    public void Batch_Contains_44_Through_50_With_Two_49_Variants()
    {
        var variants = RamanYogaBatchFourEvaluator.Evaluate(Chart(ZodiacName.Aries)).Select(x => x.SourceVariantCode).ToList();
        Assert.Equal(8, variants.Count);
        Assert.Contains("RAMAN_300_044", variants);
        Assert.Contains("RAMAN_300_050", variants);
        Assert.Equal(2, variants.Count(x => x.StartsWith("RAMAN_300_049")));
    }

    private static ContextualYogaResult Row(IReadOnlyList<ContextualYogaResult> rows, string variant)
        => rows.Single(x => x.SourceVariantCode == variant);
    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets)
        => new("D1", ascendant, planets.ToList());
    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
