using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaLordshipTests
{
    [Fact]
    public void Vanchana_Forms_When_Lagna_Lord_Joins_Saturn()
    {
        var chart = Chart(ZodiacName.Aries, P("Mars", "Taurus", 2), P("Saturn", "Taurus", 2));
        Assert.True(Raman(chart, 11).Present);
    }

    [Fact]
    public void Vanchana_Forms_From_Malefic_In_Lagna_And_Gulika_In_Trine()
    {
        var chart = Chart(ZodiacName.Aries, P("Sun", "Aries", 1)) with
        {
            SpecialPoints = new[] { P("Gulika", "Leo", 5) }
        };
        Assert.True(Raman(chart, 11).Present);
    }

    [Fact]
    public void Kahala_First_Alternative_Uses_Mutual_Kendra_And_Strong_Lagna_Lord()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Moon", "Cancer", 4, 95), P("Jupiter", "Libra", 7, 195), P("Mars", "Aries", 1, 5));
        Assert.True(Raman(chart, 15).Present);
    }

    [Fact]
    public void Kahala_Does_Not_Treat_One_Planet_As_Mutual_Kendra_With_Itself()
    {
        var chart = Chart(ZodiacName.Aquarius,
            P("Venus", "Taurus", 4, 45), P("Saturn", "Aquarius", 1, 315));
        Assert.False(Raman(chart, 15).Present);
    }

    [Fact]
    public void Kahala_Second_Alternative_Accepts_Strong_Fourth_Lord_Aspected_By_Tenth_Lord()
    {
        var chart = Chart(ZodiacName.Aries,
            P("Moon", "Cancer", 4, 95), P("Saturn", "Libra", 7, 195));
        Assert.True(Raman(chart, 15).Present);
    }

    private static SourceAttributedYogaResult Raman(ChartAnalysisInput chart, int number)
        => new VerifiedSourceYogaEngine().Detect(chart).Single(x => x.SourceVariantCode == $"RAMAN_300_{number:000}");

    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets)
        => new("D1", ascendant, planets.ToList());

    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
