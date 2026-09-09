using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class RamanYogaBatchTwoTests
{
    [Fact]
    public void Chatussagara_Requires_All_Four_Kendras_Occupied()
    {
        Assert.True(Raman(8, P("Sun", "Aries", 1), P("Moon", "Cancer", 4), P("Mars", "Libra", 7), P("Jupiter", "Capricornus", 10)).Present);
        Assert.False(Raman(8, P("Sun", "Aries", 1), P("Moon", "Cancer", 4), P("Mars", "Libra", 7)).Present);
    }

    [Fact]
    public void Vasumathi_Accepts_A_Benefic_In_Upachaya_From_Moon()
    {
        Assert.True(Raman(9, P("Moon", "Cancer", 4), P("Jupiter", "Virgo", 6)).Present);
    }

    [Fact]
    public void Rajalakshana_Requires_All_Four_Benefics_In_Kendras()
    {
        Assert.True(Raman(10, P("Moon", "Aries", 1), P("Mercury", "Cancer", 4), P("Jupiter", "Libra", 7), P("Venus", "Capricornus", 10)).Present);
        Assert.False(Raman(10, P("Moon", "Aries", 1), P("Mercury", "Cancer", 4), P("Jupiter", "Libra", 7), P("Venus", "Pisces", 12)).Present);
    }

    [Fact]
    public void Lunar_Sakata_Is_Not_Conflated_With_Nabhasa_Sakata()
    {
        var result = Raman(12, P("Jupiter", "Aries", 1), P("Moon", "Virgo", 6));
        Assert.True(result.Present);
        Assert.Equal("YOGA_SAKATA_LUNAR", result.YogaCode);
        Assert.Contains("Distinct", result.Notes);
    }

    [Fact]
    public void Pvr_Amala_Rejects_Malefic_Sharing_Tenth_While_Raman_Formation_Remains()
    {
        var rows = Detect(P("Jupiter", "Capricornus", 10), P("Mars", "Capricornus", 10));
        Assert.True(rows.Single(x => x.SourceVariantCode == "RAMAN_300_013").Present);
        Assert.False(rows.Single(x => x.SourceVariantCode == "PVR_CH11_AMALA").Present);
    }

    [Fact]
    public void Parvata_Preserves_Raman_Sixth_Versus_Pvr_Seventh_Difference()
    {
        var rows = Detect(P("Jupiter", "Aries", 1), P("Mars", "Libra", 7));
        Assert.True(rows.Single(x => x.SourceVariantCode == "RAMAN_300_014").Present);
        Assert.False(rows.Single(x => x.SourceVariantCode == "PVR_CH11_PARVATA").Present);
    }

    private static SourceAttributedYogaResult Raman(int number, params PlanetPosition[] planets)
        => Detect(planets).Single(x => x.SourceVariantCode == $"RAMAN_300_{number:000}");

    private static IReadOnlyList<SourceAttributedYogaResult> Detect(params PlanetPosition[] planets)
        => new VerifiedSourceYogaEngine().Detect(new ChartAnalysisInput("D1", ZodiacName.Aries, planets.ToList()));

    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
