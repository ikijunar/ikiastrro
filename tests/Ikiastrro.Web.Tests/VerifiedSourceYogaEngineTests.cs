using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class VerifiedSourceYogaEngineTests
{
    [Fact]
    public void PvrGajakesari_Requires_Benefic_Influence()
    {
        var rows = Engine(P("Moon", "Aries", 1), P("Jupiter", "Cancer", 4));
        Assert.True(rows.Single(x => x.SourceVariantCode == "RAMAN_300_001").Present);
        Assert.False(rows.Single(x => x.SourceVariantCode == "PVR_CH11_GAJAKESARI").Present);
    }

    [Fact]
    public void PvrGajakesari_Accepts_Qualified_Jupiter()
    {
        var rows = Engine(P("Moon", "Aries", 1), P("Jupiter", "Cancer", 4), P("Venus", "Cancer", 4));
        Assert.True(rows.Single(x => x.SourceVariantCode == "PVR_CH11_GAJAKESARI").Present);
    }

    [Fact]
    public void PvrGajakesari_Rejects_Debilitated_Jupiter()
    {
        var rows = Engine(P("Moon", "Libra", 7), P("Jupiter", "Capricornus", 10), P("Venus", "Capricornus", 10));
        Assert.True(rows.Single(x => x.SourceVariantCode == "RAMAN_300_001").Present);
        Assert.False(rows.Single(x => x.SourceVariantCode == "PVR_CH11_GAJAKESARI").Present);
    }

    [Fact]
    public void Adhi_Allows_Benefics_In_Any_Of_The_Three_Houses_From_Moon()
    {
        var complete = Engine(P("Moon", "Aries", 1), P("Mercury", "Virgo", 6), P("Jupiter", "Libra", 7), P("Venus", "Scorpio", 8));
        var incomplete = Engine(P("Moon", "Aries", 1), P("Mercury", "Virgo", 6), P("Jupiter", "Libra", 7));
        Assert.All(complete.Where(x => x.YogaCode == "YOGA_ADHI"), x => Assert.True(x.Present));
        Assert.All(incomplete.Where(x => x.YogaCode == "YOGA_ADHI"), x => Assert.True(x.Present));
    }

    [Fact]
    public void Mahapurusha_Uses_Degree_Aware_Existing_Dignity_Evaluator()
    {
        var rows = Engine(P("Mercury", "Virgo", 1, 18));
        Assert.True(rows.Single(x => x.SourceVariantCode == "RAMAN_300_023").Present);
    }

    private static IReadOnlyList<SourceAttributedYogaResult> Engine(params PlanetPosition[] planets)
        => new VerifiedSourceYogaEngine().Detect(new ChartAnalysisInput("D1", ZodiacName.Aries, planets.ToList()));

    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
