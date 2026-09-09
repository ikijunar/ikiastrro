using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class YogaClassificationTests
{
    [Fact]
    public void Classification_Separates_Family_Nature_And_Source_Strength()
    {
        var row = new SourceAttributedYogaResult("YOGA_ADHI", "Chandra", true, false, null,
            "SRC_RAMAN_300_COMBINATIONS", "RAMAN_300_007", "combination 7");
        var classification = YogaClassificationCatalog.For(row);
        Assert.Equal("CHANDRA", classification.FormationFamilyCode);
        Assert.Equal("AUSPICIOUS", classification.OutcomeNatureCode);
        Assert.Equal("MAJOR", classification.SourceStrengthClassCode);
        Assert.Equal("RAMAN_COMBINATION_007", classification.SourceCategoryCode);
        Assert.Equal("BVR-300", classification.SourceCorpusCode);
    }

    [Fact]
    public void Source_Unspecified_Is_Not_Replaced_With_Invented_Ranking()
    {
        var row = new SourceAttributedYogaResult("YOGA_VESI", "Ravi", true, false, null,
            "SRC_PVR_INTEGRATED", "PVR_CH11_VESI", "ch.11 §11.2.1");
        Assert.Equal("SOURCE_UNSPECIFIED", YogaClassificationCatalog.For(row).SourceStrengthClassCode);
    }

    [Fact]
    public void Same_Yoga_Can_Have_Source_Specific_Category()
    {
        var raman = new SourceAttributedYogaResult("YOGA_GAJAKESARI", "Chandra", true, false, null,
            "SRC_RAMAN_300_COMBINATIONS", "RAMAN_300_001", "combination 1");
        var pvr = raman with { SourceRefCode = "SRC_PVR_INTEGRATED", SourceVariantCode = "PVR_CH11_GAJAKESARI" };
        Assert.Equal("RAMAN_COMBINATION_001", YogaClassificationCatalog.For(raman).SourceCategoryCode);
        Assert.Equal("PVR_CHANDRA_YOGAS", YogaClassificationCatalog.For(pvr).SourceCategoryCode);
        Assert.Equal("BVR-300", YogaClassificationCatalog.For(raman).SourceCorpusCode);
        Assert.Equal("PVR-SPECIFIC", YogaClassificationCatalog.For(pvr).SourceCorpusCode);
    }

    [Fact]
    public void Non_Raman_And_Non_Pvr_Source_Is_Classified_As_Others()
    {
        var row = new SourceAttributedYogaResult("YOGA_EXAMPLE", "Other", false, false, null,
            "SRC_BPHS", "BPHS_EXAMPLE", "chapter example");
        var classification = YogaClassificationCatalog.For(row);
        Assert.Equal("OTHERS", classification.SourceCorpusCode);
        Assert.Equal("OTHER_SOURCE", classification.SourceCategoryCode);
    }

    [Fact]
    public void Chart_Strength_State_Is_Separate_From_Static_Source_Class()
    {
        var chart = new ChartAnalysisInput("D1", ZodiacName.Aries, new List<PlanetPosition>
        {
            new() { Planet = "Mars", Sign = "Capricornus", HouseNumber = 10 }
        });
        var result = new ClassifiedSourceYogaEngine().Detect(chart)
            .Single(x => x.Detection.SourceVariantCode == "RAMAN_300_022");
        Assert.Equal("MAJOR", result.Classification.SourceStrengthClassCode);
        Assert.Equal("QUALIFIED", result.EvaluatedStrengthState);
    }
}
