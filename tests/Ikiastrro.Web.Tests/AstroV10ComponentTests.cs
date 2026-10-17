using Bunit;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Lords;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;
using Ikiastrro.Web.Components.AstroV10;
using Ikiastrro.Web.Components.Charts;
using Microsoft.AspNetCore.Components;
using Xunit;
using Direction = Ikiastrro.Core.LifeMatters.Promise.Direction;

namespace Ikiastrro.Web.Tests;

/// <summary>The AF-v10 building blocks: house tile, planet card and lord chain.</summary>
public sealed class AstroV10ComponentTests : BunitContext
{
    private static ChartKeyDetail Graha(string planet, ZodiacName sign) => new()
    {
        PointKind = "Graha", Planet = planet, Sign = sign.ToString(), NirayanaLongitudeDegrees = (int)sign * 30 + 10,
        HouseNumberFromLagna = 6, DignityStatus = "Friend"
    };

    private static HouseForce Force(int house)
    {
        ChartKeyDetail[] grahas =
        [
            Graha("Ascendant", ZodiacName.Scorpio), Graha("Mercury", ZodiacName.Sagittarius), Graha("Ketu", ZodiacName.Aquarius),
            Graha("Jupiter", ZodiacName.Virgo), Graha("Rahu", ZodiacName.Leo), Graha("Venus", ZodiacName.Capricornus)
        ];
        return ArgalaForceModel.Build(house, ArgalaFacts.Live("D1", "Scorpio", grahas), grahas, "Scorpio");
    }

    [Fact]
    public void BhavaTile_ShowsNumber_Sign_Score_AndTheWinnerInWords()
    {
        var cut = Render<BhavaTile>(p => p.Add(x => x.Force, Force(1)));

        Assert.Equal("1", cut.Find(".bt-num").TextContent);
        Assert.Equal("Sco", cut.Find(".bt-sign").TextContent);
        Assert.Equal("Argala", cut.Find(".bt-word").TextContent);            // Mercury, Ketu, Jupiter against Rahu, Venus
        Assert.Contains("3–2", cut.Find(".bt-score").Normalized());
        Assert.Contains("width:60%", cut.Find(".bt-meter-arg").GetAttribute("style"));
    }

    [Fact]
    public void BhavaTile_Click_RaisesThePickedHouse_AndSelectedIsMarked()
    {
        var picked = 0;
        var cut = Render<BhavaTile>(p => p
            .Add(x => x.Force, Force(1)).Add(x => x.Selected, true)
            .Add(x => x.OnPick, EventCallback.Factory.Create<int>(this, h => picked = h)));

        cut.Find(".bt").Click();

        Assert.Equal(1, picked);
        Assert.Equal("true", cut.Find(".bt").GetAttribute("aria-pressed"));
        Assert.Contains("bt-on", cut.Find(".bt").ClassList);
    }

    [Fact]
    public void BhavaTile_AQuietHouse_SaysQuiet()
    {
        ChartKeyDetail[] grahas = [Graha("Ascendant", ZodiacName.Aries), Graha("Sun", ZodiacName.Pisces)];
        var quiet = ArgalaForceModel.Build(7, ArgalaFacts.Live("D1", "Aries", grahas), grahas, "Aries");

        var cut = Render<BhavaTile>(p => p.Add(x => x.Force, quiet));

        Assert.Equal("Quiet", cut.Find(".bt-word").TextContent);
        Assert.Contains("bt-quiet", cut.Find(".bt").ClassList);
    }

    private static PlanetStateInfo State(string state, string reason, params string[] damaging) =>
        PlanetStateInfo.From(new[] { new StateReason("Jagradadi", state, reason) }
            .Concat(damaging.Select(d => new StateReason("Lajjitadi", d, "reason"))));

    [Theory]
    [InlineData("Jagrat", "pc-ring-full", "Jg")]
    [InlineData("Swapna", "pc-ring-medium", "Sw")]
    [InlineData("Sushupti", "pc-ring-negligible", "Su")]
    public void PlanetCard_AlertnessIsARing_AndATag(string state, string ringClass, string code)
    {
        var cut = Render<PlanetCard>(p => p
            .Add(x => x.Row, Graha("Mars", ZodiacName.Sagittarius)).Add(x => x.State, State(state, "Dignity is Friend")));

        Assert.Contains(ringClass, cut.Find(".pc-glyph").ClassList);
        Assert.Equal(code, cut.Find(".pc-tag").TextContent);
    }

    [Fact]
    public void PlanetCard_FlagsADamagingState_AndShowsNature_AndHouse()
    {
        var nature = new LordNature("Mars", true, Direction.Obstructive, [3, 8], 6, "Friend");
        var cut = Render<PlanetCard>(p => p
            .Add(x => x.Row, Graha("Mars", ZodiacName.Sagittarius)).Add(x => x.Nature, nature)
            .Add(x => x.State, State("Swapna", "Dignity is Friend", "Kshudhita")));

        var text = cut.Markup.Normalized();
        Assert.Contains("Kshudhita", cut.Find(".pc-warn").Normalized());
        Assert.Contains("malefic · L3,8", text);
        Assert.Contains("H6", text);
        Assert.Contains("pc-bad", cut.Find(".pc").ClassList);
    }

    [Fact]
    public void PlanetCard_Click_RaisesThePlanet()
    {
        string? picked = null;
        var cut = Render<PlanetCard>(p => p
            .Add(x => x.Row, Graha("Venus", ZodiacName.Libra))
            .Add(x => x.OnPick, EventCallback.Factory.Create<string>(this, s => picked = s)));

        cut.Find(".pc").Click();

        Assert.Equal("Venus", picked);
    }

    [Fact]
    public void LordChain_ListsSignStarAndPadaLord_WithEachOnesNature_AndRaisesAPickedLord()
    {
        var triad = new PlanetLordTriad("Sun", "Mars", "Saturn", "Venus", ZodiacName.Taurus, "Mercury");
        var natures = new Dictionary<string, LordNature>
        {
            ["Mars"] = new("Mars", true, Direction.Obstructive, [3, 8], 6, "Own Sign"),
            ["Saturn"] = new("Saturn", true, Direction.Neutral, [7], 3, null),
            ["Venus"] = new("Venus", false, Direction.Supportive, [2, 9], 4, "Friend"),
        };
        string? picked = null;
        var cut = Render<LordChain>(p => p
            .Add(x => x.Triad, triad).Add(x => x.Natures, natures)
            .Add(x => x.OnLord, EventCallback.Factory.Create<string>(this, s => picked = s)));

        var nodes = cut.FindAll(".lc-node");
        Assert.Equal(3, nodes.Count);
        Assert.Equal(["Sign lord", "Star lord", "Pada lord"], nodes.Select(n => n.QuerySelector(".lc-role")!.TextContent));
        Assert.Contains("mal L3,8", nodes[0].Normalized());
        Assert.Contains("ben L2,9", nodes[2].Normalized());
        Assert.Contains("lord of house 3 and 8", nodes[0].GetAttribute("title"));
        Assert.Contains("lc-bad", nodes[0].ClassList);
        Assert.Contains("lc-good", nodes[2].ClassList);

        nodes[1].Click();
        Assert.Equal("Saturn", picked);
    }

    [Fact]
    public void LordChain_APlanetThatIsItsOwnSignLord_ReadsItself()
    {
        var triad = new PlanetLordTriad("Mars", "Mars", "Ketu", "Venus", ZodiacName.Taurus, null);

        var cut = Render<LordChain>(p => p.Add(x => x.Triad, triad));

        Assert.Equal("itself", cut.FindAll(".lc-name")[0].TextContent);
    }

    [Fact]
    public void LordNature_ReadsHousesOwnedAndStoodIn_FromTheChart()
    {
        var chart = new Ikiastrro.Web.Components.Workspace.LoadedChart(
            "D1", "RASI", "", null, "Aries", null, null,
            [Graha("Ascendant", ZodiacName.Aries), Graha("Mars", ZodiacName.Aries)],
            [new ChartHouseLord { HouseNumber = 1, LordPlanet = "Mars" }, new ChartHouseLord { HouseNumber = 8, LordPlanet = "Mars" }],
            [], [], [], [], null, null, "test");

        var natures = LordNature.For(chart);

        Assert.Equal([1, 8], natures["Mars"].HousesOwned);
        Assert.Equal(6, natures["Mars"].HouseStood);
        Assert.True(natures["Mars"].NaturalMalefic);
    }
}
