using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class MalikaYogaEvaluatorTests
{
    [Theory]
    [InlineData(1, "RAMAN_300_032")]
    [InlineData(5, "RAMAN_300_036")]
    [InlineData(12, "RAMAN_300_043")]
    public void Seven_Contiguous_Houses_Select_The_Correct_Malika(int start, string expectedVariant)
    {
        var planets = Classical.Select((planet, offset) =>
            P(planet, ((start + offset - 1) % 12) + 1)).ToArray();
        var rows = MalikaYogaEvaluator.Evaluate(Chart(planets));
        Assert.Equal(expectedVariant, Assert.Single(rows, x => x.Present).SourceVariantCode);
    }

    [Fact]
    public void Two_Classical_Planets_Sharing_A_House_Prevents_Malika()
    {
        var planets = Classical.Select((planet, offset) => P(planet, offset == 6 ? 6 : offset + 1)).ToArray();
        Assert.DoesNotContain(MalikaYogaEvaluator.Evaluate(Chart(planets)), x => x.Present);
    }

    [Fact]
    public void Rahu_And_Ketu_Do_Not_Create_Or_Invalidate_Malika()
    {
        var planets = Classical.Select((planet, offset) => P(planet, offset + 1))
            .Concat(new[] { P("Rahu", 11), P("Ketu", 5) }).ToArray();
        Assert.Equal("RAMAN_300_032",
            Assert.Single(MalikaYogaEvaluator.Evaluate(Chart(planets)), x => x.Present).SourceVariantCode);
    }

    [Fact]
    public void Evaluator_Returns_All_Twelve_Numbered_Variants()
    {
        var rows = MalikaYogaEvaluator.Evaluate(Chart());
        Assert.Equal(12, rows.Count);
        Assert.Equal(Enumerable.Range(32, 12), rows.Select(x => int.Parse(x.SourceVariantCode[10..])));
    }

    private static readonly string[] Classical = { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" };
    private static ChartAnalysisInput Chart(params PlanetPosition[] planets) => new("D1", ZodiacName.Aries, planets.ToList());
    private static PlanetPosition P(string planet, int house) => new() { Planet = planet, Sign = ((ZodiacName)(house - 1)).ToString(), HouseNumber = house };
}
