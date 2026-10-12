using Bunit;
using Ikiastrro.Web.Components.Charts;
using Xunit;

namespace Ikiastrro.Web.Tests;

public sealed class PolarGridLagnaSelectTests : BunitContext
{
    [Fact]
    public void RendersOrderedAdditiveLagnaChoicesWithoutKarakasOrDetailTable()
    {
        var cut = Render<PolarGridLagnaSelect>(p => p
            .Add(x => x.Sectors, Sectors)
            .Add(x => x.ReferencePoints, References)
            .Add(x => x.ChartCode, "D1")
            .Add(x => x.AscendantSign, "Aries"));

        // sign ring + planet ring + one ring per lagna (5), 12 sectors each
        Assert.Equal(84, cut.FindAll(".pgls-sector").Count);
        Assert.Equal(
            new[] { "SIGN LAGNA", "ARUDHA LAGNA", "HORA LAGNA", "SREE LAGNA", "GHATI LAGNA" },
            cut.FindAll(".pgls-check span").Select(x => x.TextContent.Trim()).ToArray());
        // all five lagnas are selected by default
        Assert.Equal(5, cut.FindAll(".pgls-check.is-active").Count);
        Assert.All(cut.FindAll(".pgls-check input"), i => Assert.True(i.HasAttribute("checked")));
        Assert.DoesNotContain("Naisargika", cut.Markup);
        Assert.DoesNotContain("Chara", cut.Markup);
        Assert.Empty(cut.FindAll("table"));
        Assert.Empty(cut.FindAll("aside"));
    }

    [Fact]
    public void TogglesReferencesOnTheWheelAndRendersOneSouthIndianGrid()
    {
        var polar = Render<PolarGridLagnaSelect>(p => p
            .Add(x => x.Sectors, Sectors)
            .Add(x => x.ReferencePoints, References)
            .Add(x => x.ChartCode, "D1"));

        polar.FindAll(".pgls-check input")[4].Change(false);
        Assert.Equal(4, polar.FindAll(".pgls-check.is-active").Count);
        Assert.Equal(4, polar.FindAll(".pgls-check input[checked]").Count);
        Assert.Contains("SI · AR · HO · SR", polar.Markup); // wheel aria-label lists the active lagnas
        Assert.Equal(48, polar.FindAll(".pgls-house").Count);
        Assert.Contains("ref-lagna", polar.FindAll(".pgls-check")[0].ClassList);
        Assert.Contains("ref-arudha", polar.FindAll(".pgls-check")[1].ClassList);

        var south = Render<PolarGridLagnaSelect>(p => p
            .Add(x => x.Sectors, Sectors)
            .Add(x => x.ReferencePoints, References)
            .Add(x => x.ChartCode, "D9")
            .Add(x => x.View, "south"));

        // the grid view is a single South-Indian grid driven by the "Grid display" extras, not per-lagna checkboxes
        Assert.Single(south.FindAll(".pgls-south-grid"));
        Assert.Single(south.FindAll(".chart"));
        Assert.NotEmpty(south.FindAll(".pgls-extras .pgls-check"));
        Assert.Empty(south.FindAll(".pgls-polar"));
    }

    private static readonly IReadOnlyList<PolarGridLagnaSelect.Sector> Sectors =
        new[] { "Aries", "Taurus", "Gemini", "Cancer", "Leo", "Virgo", "Libra", "Scorpio", "Sagittarius", "Capricornus", "Aquarius", "Pisces" }
            .Select((sign, index) => new PolarGridLagnaSelect.Sector(index + 1, sign)).ToList();

    private static readonly IReadOnlyList<PolarGridLagnaSelect.ReferencePoint> References =
    [
        new("LAGNA", "Sign Lagna", "Aries"),
        new("ARUDHA_LAGNA", "Arudha Lagna", "Pisces"),
        new("HORA_LAGNA", "Hora Lagna", "Taurus"),
        new("SREE_LAGNA", "Sree Lagna", "Gemini"),
        new("GHATI_LAGNA", "Ghati Lagna", "Cancer")
    ];
}
