using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaBatchFiveEvaluatorTests
{
    [Fact]
    public void Batch_Preserves_All_Alternatives_And_51_Through_60()
    {
        var rows = RamanYogaBatchFiveEvaluator.Evaluate(Chart(ZodiacName.Aries));
        Assert.Equal(15, rows.Count);
        Assert.Equal(3, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_051")));
        Assert.Equal(2, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_052")));
        Assert.Equal(2, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_053")));
        Assert.Equal(2, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_054")));
        Assert.Contains(rows, x => x.SourceVariantCode == "RAMAN_300_060");
    }

    [Fact]
    public void Missing_D9_Leaves_Only_D9_Dependent_Variants_Unevaluated()
    {
        var rows = RamanYogaBatchFiveEvaluator.Evaluate(Chart(ZodiacName.Taurus));
        Assert.Equal("NOT_EVALUATED", Row(rows, "RAMAN_300_054_NAVAMSA").EvaluationStatus);
        Assert.Equal("NOT_EVALUATED", Row(rows, "RAMAN_300_057").EvaluationStatus);
        Assert.Equal("EVALUATED", Row(rows, "RAMAN_300_054_RASI").EvaluationStatus);
    }

    [Fact]
    public void Matsya_Relaxed_Can_Match_When_Strict_Fourth_And_Eighth_Are_Absent()
    {
        var c = Chart(ZodiacName.Aries, P("Sun", "Aries", 1), P("Saturn", "Sagittarius", 9),
            P("Mars", "Leo", 5), P("Jupiter", "Leo", 5));
        var rows = RamanYogaBatchFiveEvaluator.Evaluate(c);
        Assert.True(Row(rows, "RAMAN_300_053_RELAXED").Present);
        Assert.False(Row(rows, "RAMAN_300_053_STRICT").Present);
    }

    [Fact]
    public void Kusuma_Uses_Stated_Eighth_From_Moon_Definition()
    {
        var c = Chart(ZodiacName.Aries, P("Jupiter", "Aries", 1), P("Moon", "Libra", 7), P("Sun", "Taurus", 2));
        Assert.True(Row(RamanYogaBatchFiveEvaluator.Evaluate(c), "RAMAN_300_052_PRIMARY").Present);
    }

    [Fact]
    public void Jaya_Requires_Longitude_And_Exact_Deep_Exaltation()
    {
        var missing = RamanYogaBatchFiveEvaluator.Evaluate(Chart(ZodiacName.Aries, P("Mercury", "Pisces", 12), P("Saturn", "Libra", 7)));
        Assert.Equal("NOT_EVALUATED", Row(missing, "RAMAN_300_058").EvaluationStatus);

        var exact = Chart(ZodiacName.Aries, P("Mercury", "Pisces", 12, 345), P("Saturn", "Libra", 7, 200));
        Assert.True(Row(RamanYogaBatchFiveEvaluator.Evaluate(exact), "RAMAN_300_058").Present);
    }

    private static ContextualYogaResult Row(IReadOnlyList<ContextualYogaResult> rows, string variant) => rows.Single(x => x.SourceVariantCode == variant);
    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets) => new("D1", ascendant, planets.ToList());
    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null) => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
