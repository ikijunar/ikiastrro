using Bunit;
using Microsoft.AspNetCore.Components;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Data;
using Ikiastrro.Web.Components.Charts;
using Xunit;

namespace Ikiastrro.Web.Tests;

public sealed class YogaEvaluationTableTests : BunitContext
{
    [Fact]
    public void RendersNotesOnlyWhenPresent()
    {
        var rows = new[]
        {
            new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_BUDHA_ADITYA", true, "EVALUATED", "SUN",
                "Sun and Mercury conjunct (same sign)", "Present, but Mercury is combust."),
            new YogaEvaluationRow("SRC_RAMAN_300_COMBINATIONS", "YOGA_GAJAKESARI", true, "EVALUATED", "MOON",
                "Jupiter in a kendra from Moon", null),
        };

        var cut = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows));

        Assert.Contains("Present, but Mercury is combust.", cut.Markup);
        Assert.Single(cut.FindAll(".yg-notes"));
    }

    [Fact]
    public void ShowsBasisCausingPlanetsAndTheirShadbala()
    {
        var rows = new[]
        {
            new YogaEvaluationRow("SRC_RAMAN_300_COMBINATIONS", "YOGA_AYATNA_DHANA_LABHA", true, "EVALUATED", "LAGNA",
                "Lagna lord and 2nd lord exchange houses", null),
        };
        var lords = new Dictionary<int, PlanetName> { [1] = PlanetName.Mars, [2] = PlanetName.Venus };
        var shadbala = new[]
        {
            new ShadbalaSummaryRow("Mars", 0, 0, 0, 0, 0, 0, 0, 0, 6.5m, 5m, 130m),
            new ShadbalaSummaryRow("Venus", 0, 0, 0, 0, 0, 0, 0, 0, 3m, 5.5m, 55m),
        };

        var cut = Render<YogaEvaluationTable>(p => p
            .Add(x => x.Rows, rows)
            .Add(x => x.HouseLords, lords)
            .Add(x => x.Shadbala, shadbala));

        Assert.Contains("Lagna based", cut.Markup);
        Assert.Equal(new[] { "Mars", "Venus" }, cut.FindAll(".yg-pstrength b").Select(b => b.TextContent));
        Assert.Contains("yg-tier-strong", cut.Markup);
        Assert.Contains("yg-tier-weak", cut.Markup);
        Assert.Contains("130%", cut.Markup);
    }

    [Fact]
    public void ListsAYogaOnceWithEveryCataloguedVariantInTheVariantsColumn()
    {
        var rows = new[]
        {
            new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_RUCHAKA", true, "EVALUATED", "LAGNA", "Mars own/exalted in a kendra", null,
                SourceVariantCode: "PVR_CH11_RUCHAKA", SourceLocator: "ch.11 11.3.1", VariantDisplayName: "Ruchaka Yoga"),
            new YogaEvaluationRow("SRC_RAMAN_300_COMBINATIONS", "YOGA_RUCHAKA", true, "EVALUATED", "LAGNA", "Mars own/exalted in a kendra", null,
                SourceVariantCode: "RAMAN_300_022", SourceLocator: "combination 22; printed p.30"),
        };

        var cut = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows));

        Assert.Single(cut.FindAll("tbody tr:not(.yg-group)"));
        var variants = cut.Find(".yg-variants").Normalized();
        Assert.Contains("Ruchaka Yoga", variants);
        Assert.Contains("combination 22", variants);
        Assert.Contains("Pvr Integrated", variants);
        Assert.Contains("1 present of", cut.Find(".yg-card-head p").Normalized());
    }

    [Fact]
    public void GroupsRowsUnderTheirSetAndDropsTheVargaInterpretationSourceAndFindFields()
    {
        var rows = new[]
        {
            new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_RUCHAKA", true, "EVALUATED", "LAGNA", "Mars own in a kendra", null, SourceVariantCode: "V1", YogaSetName: "Mahapurusha Yogas"),
            new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_BHADRA", true, "EVALUATED", "LAGNA", "Mercury own in a kendra", null, SourceVariantCode: "V2", YogaSetName: "Mahapurusha Yogas"),
            new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_ADHI", true, "EVALUATED", "MOON", "Benefics 6/7/8 from Moon", null, SourceVariantCode: "V3", YogaSetName: "Moon Yogas"),
        };

        var cut = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows));

        Assert.Equal(new[] { "Mahapurusha Yogas 2", "Moon Yogas 1" }, cut.FindAll(".yg-group th").Select(t => t.Normalized()));
        var headers = cut.FindAll("thead th").Select(t => t.Normalized()).ToList();
        Assert.DoesNotContain(headers, h => h.StartsWith("Varga") || h.StartsWith("Interpretation") || h.StartsWith("Source") || h.StartsWith("Set"));
        Assert.Contains(headers, h => h.StartsWith("Variants"));
        Assert.Empty(cut.FindAll("input[type=search]"));
    }

    [Fact]
    public void VargaSelectorMarksTheSelectedChartAndRaisesTheChange()
    {
        var rows = new[] { new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_RUCHAKA", true, "EVALUATED", "LAGNA", "Mars own in a kendra", null, ChartType: "D9") };
        string? picked = null;
        var cut = Render<YogaEvaluationTable>(p => p
            .Add(x => x.Rows, rows)
            .Add(x => x.Varga, "D9")
            .Add(x => x.Vargas, new[] { "D1", "D2", "D3", "D9", "D12", "D30" })
            .Add(x => x.OnVargaChanged, EventCallback.Factory.Create<string>(this, v => picked = v)));

        var buttons = cut.FindAll(".yg-varga");
        Assert.Equal(6, buttons.Count);
        Assert.Equal("D9 Navamsa", cut.Find(".yg-varga.is-on").Normalized());
        Assert.Contains("Ṣaḍbala is the natal (D1) strength", cut.Find(".yg-varga-note").Normalized());

        buttons.Single(b => b.TextContent.Contains("D12")).Click();
        Assert.Equal("D12", picked);
    }

    [Fact]
    public void HidesTheVargaSelectorWithASingleChart()
    {
        var rows = new[] { new YogaEvaluationRow("SRC_PVR_INTEGRATED", "YOGA_RUCHAKA", true, "EVALUATED", "LAGNA", "Mars own in a kendra", null, ChartType: "D30") };
        var withSelector = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows).Add(x => x.Varga, "D30")
            .Add(x => x.Vargas, new[] { "D1", "D30" }));
        Assert.Equal(2, withSelector.FindAll(".yg-varga").Count);

        var plain = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows));
        Assert.Empty(plain.FindAll(".yg-varga"));
    }

    [Fact]
    public void VargaPlacementsDriveTheCausingPlanetsAndNaturalShadbalaStays()
    {
        // Mars is the lagna lord in the selected varga; the Ṣaḍbala column still reads the natal strength.
        var rows = new[] { new YogaEvaluationRow("SRC_RAMAN_300_COMBINATIONS", "YOGA_X", true, "EVALUATED", "LAGNA",
            "Lagna lord and 2nd lord exchange houses", null, ChartType: "D9") };
        var cut = Render<YogaEvaluationTable>(p => p.Add(x => x.Rows, rows).Add(x => x.Varga, "D9")
            .Add(x => x.Vargas, new[] { "D1", "D9" })
            .Add(x => x.HouseLords, new Dictionary<int, PlanetName> { [1] = PlanetName.Mars, [2] = PlanetName.Venus })
            .Add(x => x.Shadbala, new[] { new ShadbalaSummaryRow("Mars", 0, 0, 0, 0, 0, 0, 0, 0, 6.5m, 5m, 130m) }));
        Assert.Equal(new[] { "Mars", "Venus" }, cut.FindAll(".yg-pstrength b").Select(b => b.TextContent));
        Assert.Contains("130%", cut.Markup);
    }
}
