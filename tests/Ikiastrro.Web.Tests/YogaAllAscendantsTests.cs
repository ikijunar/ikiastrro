using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class YogaAllAscendantsTests
{
    public static IEnumerable<object[]> AllAscendants()
    {
        foreach (var ascendant in Enum.GetValues<ZodiacName>())
        {
            var marsHouse = Distance(ascendant, ZodiacName.Capricornus);
            yield return new object[] { ascendant, marsHouse is 1 or 4 or 7 or 10 };
        }
    }

    [Theory]
    [MemberData(nameof(AllAscendants))]
    public void Ruchaka_Works_For_Every_Ascendant(ZodiacName ascendant, bool expected)
    {
        var chart = Chart(ascendant, Placement(ascendant, PlanetName.Mars, ZodiacName.Capricornus));
        var result = new SourceAttributedYogaEngine().Detect(chart)
            .Single(x => x.SourceVariantCode == "RAMAN_300_022");
        Assert.Equal(expected, result.Present);
    }

    [Theory]
    [MemberData(nameof(AllAscendants))]
    public void Vesi_Wraps_From_Pisces_To_Aries_For_Every_Ascendant(ZodiacName ascendant, bool _)
    {
        var chart = Chart(ascendant,
            Placement(ascendant, PlanetName.Sun, ZodiacName.Pisces),
            Placement(ascendant, PlanetName.Mercury, ZodiacName.Aries));
        Assert.All(new SourceAttributedYogaEngine().Detect(chart).Where(x => x.YogaCode == "YOGA_VESI"),
            x => Assert.True(x.Present));
    }

    [Fact]
    public void Ruchaka_Exalted_Mars_Is_Kendra_Only_For_Four_Ascendants()
    {
        var present = Enum.GetValues<ZodiacName>().Count(ascendant =>
        {
            var chart = Chart(ascendant, Placement(ascendant, PlanetName.Mars, ZodiacName.Capricornus));
            return new SourceAttributedYogaEngine().Detect(chart)
                .Single(x => x.SourceVariantCode == "RAMAN_300_022").Present;
        });
        Assert.Equal(4, present);
    }

    private static ChartAnalysisInput Chart(ZodiacName ascendant, params PlanetPosition[] planets)
        => new("D1", ascendant, planets.ToList());

    private static PlanetPosition Placement(ZodiacName ascendant, PlanetName planet, ZodiacName sign)
        => new() { Planet = planet.ToString(), Sign = sign.ToString(), HouseNumber = Distance(ascendant, sign) };

    private static int Distance(ZodiacName from, ZodiacName to)
        => (((int)to - (int)from + 12) % 12) + 1;
}
