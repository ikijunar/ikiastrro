using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanContextYogaEvaluatorTests
{
    [Fact]
    public void Missing_D9_Leaves_Gauri_And_Bharathi_Unevaluated()
    {
        var rows = RamanContextYogaEvaluator.Evaluate(Chart(ZodiacName.Aries));
        Assert.All(new[] { Row(rows, 28), Row(rows, 29) }, x =>
        {
            Assert.Null(x.Present);
            Assert.Equal("NOT_EVALUATED", x.EvaluationStatus);
        });
    }

    [Fact]
    public void Lakshmi_Uses_Lagna_Based_Kendra_Or_Trine()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Mars", "Aries", 1, 5), P("Jupiter", "Sagittarius", 9, 250));
        Assert.True(Row(RamanContextYogaEvaluator.Evaluate(chart), 27).Present);
    }

    [Fact]
    public void Chapa_Detects_Lagna_Based_Fourth_Tenth_Exchange()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Mars", "Capricornus", 10, 280),
            P("Moon", "Capricornus", 10, 285), P("Saturn", "Cancer", 4, 100));
        Assert.True(Row(RamanContextYogaEvaluator.Evaluate(chart), 30).Present);
    }

    [Fact]
    public void Sreenatha_Detects_Seventh_Lord_Exalted_In_Lagna_Tenth()
    {
        var chart = Chart(ZodiacName.Sagittarius,
            P("Mercury", "Virgo", 10, 160), P("Sun", "Virgo", 10, 165));
        Assert.True(Row(RamanContextYogaEvaluator.Evaluate(chart), 31).Present);
    }

    [Fact]
    public void Batch_Contains_Exactly_26_Through_31()
    {
        var numbers = RamanContextYogaEvaluator.Evaluate(Chart(ZodiacName.Aries))
            .Select(x => int.Parse(x.SourceVariantCode[10..])).ToArray();
        Assert.Equal(new[] { 26, 27, 28, 29, 30, 31 }, numbers);
    }

    private static ContextualYogaResult Row(IReadOnlyList<ContextualYogaResult> rows, int number)
        => rows.Single(x => x.SourceVariantCode == $"RAMAN_300_{number:000}");
    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets)
        => new("D1", ascendant, planets.ToList());
    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
