using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanNabhasaSecondBatchYogaTests
{
    [Fact]
    public void Batch_Tracks_81_Through_100_With_Grouped_Forms()
    {
        var rows = RamanNabhasaSecondBatchEvaluator.Evaluate(ClassicalChart(1, 2, 3, 4, 5, 6, 7));
        Assert.Equal(26, rows.Count);
        Assert.Equal(4, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_081")));
        Assert.Equal(3, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_087")));
        Assert.Equal(2, rows.Count(x => x.SourceVariantCode.StartsWith("RAMAN_300_089")));
    }

    [Fact]
    public void Gada_Supports_Each_Adjacent_Kendra_Pair()
    {
        var rows = RamanNabhasaSecondBatchEvaluator.Evaluate(ClassicalChart(10, 10, 10, 1, 1, 1, 1));
        Assert.True(Row(rows, "RAMAN_300_081_H10").Present);
        Assert.False(Row(rows, "RAMAN_300_082").Present);
    }

    [Fact]
    public void Vajra_Uses_Natural_Benefic_And_Malefic_Groups()
    {
        var chart = Chart(P("Moon", 1), P("Mercury", 7), P("Jupiter", 1), P("Venus", 7),
            P("Sun", 4), P("Mars", 10), P("Saturn", 4));
        Assert.True(Row(RamanNabhasaSecondBatchEvaluator.Evaluate(chart), "RAMAN_300_084").Present);
    }

    [Fact]
    public void Sankhya_Formation_Is_Superseded_By_Another_Nabhasa_Pattern()
    {
        var rows = RamanNabhasaSecondBatchEvaluator.Evaluate(ClassicalChart(1, 2, 3, 4, 5, 6, 7));
        var vallaki = Row(rows, "RAMAN_300_091");
        Assert.False(vallaki.Present);
        Assert.Contains("loses individuality", vallaki.Notes);
    }

    [Theory]
    [InlineData("RAMAN_300_098", 1, 4, 7, 10, 1, 4, 7)]
    [InlineData("RAMAN_300_099", 2, 5, 8, 11, 2, 5, 8)]
    [InlineData("RAMAN_300_100", 3, 6, 9, 12, 3, 6, 9)]
    public void Asraya_Yogas_Require_One_Sign_Modality(string variant, params int[] houses)
    {
        Assert.True(Row(RamanNabhasaSecondBatchEvaluator.Evaluate(ClassicalChart(houses)), variant).Present);
    }

    private static ContextualYogaResult Row(IEnumerable<ContextualYogaResult> rows, string variant)
        => rows.Single(x => x.SourceVariantCode == variant);
    private static ChartAnalysisInput ClassicalChart(params int[] houses)
    {
        var names = new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" };
        return Chart(names.Zip(houses, P).ToArray());
    }
    private static ChartAnalysisInput Chart(params PlanetPosition[] planets)
        => new("D1", ZodiacName.Aries, planets.ToList());
    private static PlanetPosition P(string planet, int house)
        => new() { Planet = planet, HouseNumber = house, Sign = ((ZodiacName)((house - 1) % 12)).ToString() };
}
