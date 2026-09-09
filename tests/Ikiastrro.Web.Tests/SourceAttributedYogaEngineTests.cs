using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class SourceAttributedYogaEngineTests
{
    [Fact]
    public void Detect_Preserves_Raman_And_Pvr_As_Separate_Variants()
    {
        var results = new SourceAttributedYogaEngine().Detect(Chart(
            P("Moon", "Aries", 1), P("Jupiter", "Cancer", 4)));
        var variants = results.Where(x => x.YogaCode == "YOGA_GAJAKESARI").ToList();
        Assert.Equal(2, variants.Count);
        Assert.All(variants, x => Assert.True(x.Present));
        Assert.Contains(variants, x => x.SourceRefCode == "SRC_RAMAN_300_COMBINATIONS" && x.SourceVariantCode == "RAMAN_300_001");
        Assert.Contains(variants, x => x.SourceRefCode == "SRC_PVR_INTEGRATED" && x.SourceLocator.Contains("§11.6"));
    }

    [Theory]
    [InlineData(9, false, true)]
    [InlineData(11, true, true)]
    public void BudhaAditya_Uses_Source_Specific_Combustion_Qualification(double separation, bool raman, bool pvr)
    {
        var results = new SourceAttributedYogaEngine().Detect(Chart(
            P("Sun", "Aries", 1, 5), P("Mercury", "Aries", 1, 5 + separation)));
        Assert.Equal(raman, results.Single(x => x.SourceVariantCode == "RAMAN_300_024").Present);
        Assert.Equal(pvr, results.Single(x => x.SourceVariantCode == "PVR_CH11_BUDHA_ADITYA").Present);
    }

    [Fact]
    public void Mahapurusha_Requires_Kendra_And_Own_Or_Exalted_Sign()
    {
        var engine = new SourceAttributedYogaEngine();
        Assert.True(engine.Detect(Chart(P("Mars", "Capricornus", 10))).Single(x => x.SourceVariantCode == "RAMAN_300_022").Present);
        Assert.False(engine.Detect(Chart(P("Mars", "Capricornus", 3))).Single(x => x.SourceVariantCode == "RAMAN_300_022").Present);
    }

    [Fact]
    public void Nodes_Do_Not_Create_Solar_Or_Lunar_Flanking_Yogas()
    {
        var results = new SourceAttributedYogaEngine().Detect(Chart(
            P("Sun", "Aries", 1), P("Moon", "Cancer", 4), P("Rahu", "Taurus", 2), P("Ketu", "Gemini", 3)));
        Assert.All(results.Where(x => x.YogaCode is "YOGA_VESI" or "YOGA_SUNAPHA"), x => Assert.False(x.Present));
    }

    private static ChartAnalysisInput Chart(params PlanetPosition[] planets)
        => new("D1", ZodiacName.Aries, planets.ToList());

    private static PlanetPosition P(string planet, string sign, int house, double? longitude = null)
        => new() { Planet = planet, Sign = sign, HouseNumber = house, NirayanaLongitudeDegrees = longitude };
}
