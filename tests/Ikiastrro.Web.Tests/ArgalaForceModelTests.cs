using Bunit;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;
using Ikiastrro.Data.Statistics;
using Ikiastrro.Web.Components.Charts;
using Microsoft.AspNetCore.Components;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>The unified Argala table's logic: winners and lane pairing checked against PVR's Exercise 16 / Chart 5
/// (Lagna Scorpio), the reading's content, the overlay for the left chart, and the table's click behaviour.</summary>
public sealed class ArgalaForceModelTests : BunitContext
{
    private static ChartKeyDetail Graha(string planet, ZodiacName sign) => new()
    {
        PointKind = "Graha", Planet = planet, Sign = sign.ToString(), NirayanaLongitudeDegrees = (int)sign * 30 + 10
    };

    private static List<ChartKeyDetail> Chart(ZodiacName lagna, params (PlanetName Planet, ZodiacName Sign)[] placements) =>
        new[] { Graha("Ascendant", lagna) }.Concat(placements.Select(p => Graha(p.Planet.ToString(), p.Sign))).ToList();

    // PVR Exercise 16, Chart 5 (docs/research/domain/argala-virodhargala-drishti-lifematters.md §4).
    private static List<ChartKeyDetail> Chart5() => Chart(ZodiacName.Scorpio,
        (PlanetName.Mars, ZodiacName.Scorpio), (PlanetName.Saturn, ZodiacName.Scorpio),
        (PlanetName.Mercury, ZodiacName.Sagittarius), (PlanetName.Venus, ZodiacName.Capricornus),
        (PlanetName.Ketu, ZodiacName.Aquarius), (PlanetName.Moon, ZodiacName.Aries),
        (PlanetName.Sun, ZodiacName.Taurus), (PlanetName.Rahu, ZodiacName.Leo), (PlanetName.Jupiter, ZodiacName.Virgo));

    private static HouseForce For(int house, List<ChartKeyDetail> grahas, ArgalaMeaningContext? ctx = null)
    {
        var lagna = grahas.First(g => g.Planet == "Ascendant").Sign;
        return ArgalaForceModel.Build(house, ArgalaFacts.Live("D1", lagna, grahas), grahas, lagna, ctx);
    }

    private static string[] Names(IEnumerable<ForcePlanet> planets) => planets.Select(p => p.Planet.ToString()).ToArray();

    [Fact]
    public void Chart5_House1_PairsEachArgalaWithItsVirodhargala_AndArgalaWins()
    {
        var force = For(1, Chart5());

        Assert.Equal([2, 4, 11, 5], force.Lanes.Select(l => l.ArgalaOffset));
        Assert.Equal([12, 10, 3, 9], force.Lanes.Select(l => l.ObstructionOffset));
        Assert.Equal(["Mercury"], Names(force.Lanes[0].Argala));
        Assert.Empty(force.Lanes[0].Virodhargala);
        Assert.Equal(["Rahu"], Names(force.Lanes[1].Virodhargala));
        Assert.Equal(["Venus"], Names(force.Lanes[2].Virodhargala));
        Assert.Equal(ArgalaVerdict.Holds, force.Lanes[0].Verdict);
        Assert.Equal(ArgalaVerdict.Contested, force.Lanes[1].Verdict);
        Assert.Null(force.Lanes[3].Verdict);
        Assert.Equal((3, 2), (force.ArgalaForce, force.VirodhargalaForce));
        Assert.Equal(ArgalaOutcome.ArgalaWins, force.Outcome);
        Assert.False(force.DecidedByDignity);
    }

    [Fact]
    public void Chart5_House11_MaleficThird_MovesToAnExceptionLaneOnTheArgalaSide()
    {
        var force = For(11, Chart5());

        var exception = Assert.Single(force.Lanes, l => l.ArgalaOffset == 3);
        Assert.Equal(["Mars", "Saturn"], Names(exception.Argala));
        Assert.Empty(exception.Virodhargala);
        Assert.True(exception.ExceptionApplied);
        Assert.Equal((4, 2), (force.ArgalaForce, force.VirodhargalaForce));
        Assert.Equal(ArgalaOutcome.ArgalaWins, force.Outcome);
    }

    [Fact]
    public void Chart5_House4_CountsAntiZodiacally_BecauseKetuHoldsTheSign()
    {
        var force = For(4, Chart5());

        Assert.True(force.CountedAntiZodiacally);
        var second = force.Lanes.Single(l => l.ArgalaOffset == 2);
        Assert.Equal(3, second.ArgalaHouse);                        // Capricorn, Venus — not house 5
        Assert.Equal(["Venus"], Names(second.Argala));
        Assert.Equal(3, ArgalaForceModel.HouseFrom(4, 2, reverse: true));
        Assert.Equal(5, ArgalaForceModel.HouseFrom(4, 2, reverse: false));
    }

    [Fact]
    public void Chart5_House7_MoreObstructionThanArgala_VirodhargalaWins()
    {
        var force = For(7, Chart5());

        Assert.Equal((2, 3), (force.ArgalaForce, force.VirodhargalaForce));
        Assert.Equal(ArgalaOutcome.VirodhargalaWins, force.Outcome);
        Assert.StartsWith("Virodhargala wins", force.Reading.Headline);
    }

    [Fact]
    public void NoPlanetsInAnyPosition_IsQuiet()
    {
        var force = For(7, Chart(ZodiacName.Aries, (PlanetName.Sun, ZodiacName.Pisces)));

        Assert.Equal(ArgalaOutcome.Quiet, force.Outcome);
        Assert.Equal(50, ArgalaForceModel.ArgalaShare(force));
    }

    [Fact]
    public void ObstructionWithoutArgala_IsIdle_NotAWinForVirodhargala()
    {
        var force = For(1, Chart(ZodiacName.Aries, (PlanetName.Sun, ZodiacName.Pisces)));

        Assert.Equal(ArgalaOutcome.Idle, force.Outcome);
        Assert.All(force.Lanes, l => Assert.Null(l.Verdict));
    }

    [Fact]
    public void EqualCountAndEqualDignity_IsATie()
    {
        var force = For(1, Chart(ZodiacName.Aries, (PlanetName.Moon, ZodiacName.Taurus), (PlanetName.Sun, ZodiacName.Pisces)));

        Assert.Equal(ArgalaOutcome.Tie, force.Outcome);
        Assert.False(force.DecidedByDignity);
    }

    [Fact]
    public void EqualCount_IsDecidedByDignity_AndSaysSo()
    {
        var grahas = Chart(ZodiacName.Aries, (PlanetName.Moon, ZodiacName.Taurus), (PlanetName.Sun, ZodiacName.Pisces));
        grahas.First(g => g.Planet == "Moon").DignityStatus = "Exalted";

        var force = For(1, grahas);

        Assert.Equal(ArgalaOutcome.ArgalaWins, force.Outcome);
        Assert.True(force.DecidedByDignity);
        Assert.Contains("decided it", force.Reading.Summary);
    }

    [Fact]
    public void Reading_NamesThePlanet_ItsHouse_ItsRole_AndWhatTheHouseStandsFor()
    {
        var ctx = new ArgalaMeaningContext("D1", null,
            new Dictionary<int, string> { [1] = "self", [2] = "wealth", [4] = "home", [11] = "gains" },
            new Dictionary<string, string> { ["Mercury"] = "speech, intellect" });

        var reading = For(1, Chart5(), ctx).Reading;

        // Mercury in house 2 is the 2nd from house 1: PVR sec.10.7's "basic ingredient".
        var mercury = Assert.Single(reading.Causes, c => c.StartsWith("Mercury"));
        Assert.Contains("signifies speech, intellect", mercury);
        Assert.Contains("H2 (wealth)", mercury);
        Assert.Contains("basic ingredient", mercury);
        Assert.Contains("self", reading.Summary);
        Assert.Contains("D1", reading.Varga);
    }

    [Fact]
    public void Reading_InAVarga_SaysWhichChartAndItsDomain()
    {
        var reading = For(1, Chart5(), new ArgalaMeaningContext("D9", "Marriage / Dharma")).Reading;

        Assert.Contains("D9", reading.Varga);
        Assert.Contains("Marriage / Dharma", reading.Varga);
    }

    [Fact]
    public void Reading_WhenVirodhargalaWins_ListsTheObstructingPlanetsFirst()
    {
        var reading = For(7, Chart5()).Reading;

        Assert.Contains("virodhargala", reading.Causes[0]);
    }

    [Fact]
    public void Overlay_OnlyHighlightsTheSelectedLane_AndDimsTheRest()
    {
        var force = For(1, Chart5());

        var all = ArgalaForceModel.Overlay("D1", force, null);
        var lane = ArgalaForceModel.Overlay("D1", force, 4);

        Assert.Equal("D1", all.ChartType);
        Assert.Equal(ZodiacName.Scorpio, all.Target);
        Assert.DoesNotContain(all.Links, l => l.Dimmed);
        Assert.Single(lane.Links, l => !l.Dimmed);
        Assert.Equal(ZodiacName.Aquarius, lane.Links.Single(l => !l.Dimmed).ArgalaSign);   // Ketu, the 4th from Scorpio
    }

    [Fact]
    public void LaneOf_MapsEachObstructionColumnToTheArgalaItObstructs()
    {
        Assert.Equal([2, 4, 11, 5], new[] { 12, 10, 3, 9 }.Select(o => ArgalaForceModel.LaneOf(o, thirdIsException: false)));
        Assert.Equal(3, ArgalaForceModel.LaneOf(3, thirdIsException: true));
        Assert.Equal(11, ArgalaForceModel.LaneOf(11, thirdIsException: false));
    }

    // --- the unified table ---

    private IRenderedComponent<ArgalaTable> RenderTable(List<ChartKeyDetail> grahas, Action<ArgalaOverlay?> onOverlay, string chartType = "D1")
    {
        var lagna = grahas.First(g => g.Planet == "Ascendant").Sign;
        return Render<ArgalaTable>(p => p
            .Add(x => x.Grahas, grahas).Add(x => x.AscendantSign, lagna)
            .Add(x => x.Facts, ArgalaFacts.Live(chartType, lagna, grahas)).Add(x => x.ChartType, chartType)
            .Add(x => x.OnOverlayChanged, EventCallback.Factory.Create<ArgalaOverlay?>(this, onOverlay)));
    }

    private static void ClickHouse(IRenderedComponent<ArgalaTable> cut, int house) =>
        cut.FindAll(".at-pick").Single(b => b.TextContent.Trim().TrimEnd('*') == house.ToString()).Click();

    [Fact]
    public void Table_ShowsAHintUntilAHouseIsPicked()
    {
        var cut = RenderTable(Chart5(), _ => { });

        Assert.Empty(cut.FindAll(".at-reading"));
        Assert.Contains("Click a house", cut.Markup.Normalized());
    }

    [Fact]
    public void Table_PickingAHouse_ShowsItsReading_AndSendsItsOverlayToTheChart()
    {
        ArgalaOverlay? sent = null;
        var cut = RenderTable(Chart5(), o => sent = o);

        ClickHouse(cut, 11);

        Assert.Equal(11, sent!.House);
        Assert.Equal(ZodiacName.Virgo, sent.Target);
        Assert.Contains("ARGALA WINS", cut.Find(".at-reading-badge").Normalized());
        Assert.Contains("House 11", cut.Find(".at-reading-title").Normalized());
        Assert.NotEmpty(cut.FindAll(".at-reading-list li"));
        Assert.Single(cut.FindAll(".at-row-on"));
    }

    [Fact]
    public void Table_PickingAPlanetCell_SelectsThatLaneOnly_AndPickingItAgainReturnsToTheWholeHouse()
    {
        ArgalaOverlay? sent = null;
        var cut = RenderTable(Chart5(), o => sent = o);

        ClickHouse(cut, 1);
        Assert.DoesNotContain(sent!.Links, l => l.Dimmed);

        var row = cut.FindAll(".at-row-on").Single();
        row.QuerySelectorAll(".at-cell")[1].Click();                // the 4th-from column
        Assert.Single(sent!.Links, l => !l.Dimmed);
        Assert.Equal(2, cut.FindAll(".at-cell-on").Count);        // the 4th-from cell and its paired 10th-from cell

        cut.FindAll(".at-row-on").Single().QuerySelectorAll(".at-cell")[1].Click();
        Assert.DoesNotContain(sent!.Links, l => l.Dimmed);
        Assert.Empty(cut.FindAll(".at-cell-on"));
    }

    [Fact]
    public void Table_PickingTheSameHouseAgain_ClearsIt()
    {
        ArgalaOverlay? sent = null;
        var cut = RenderTable(Chart5(), o => sent = o);

        ClickHouse(cut, 4);
        ClickHouse(cut, 4);

        Assert.Null(sent);
        Assert.Empty(cut.FindAll(".at-reading"));
    }

    [Fact]
    public void Table_SwitchingChart_DropsThePick()
    {
        var grahas = Chart5();
        var cut = RenderTable(grahas, _ => { });
        ClickHouse(cut, 4);

        cut.Render(p => p.Add(x => x.ChartType, "D9").Add(x => x.Facts, ArgalaFacts.Live("D9", "Scorpio", grahas)));

        Assert.Empty(cut.FindAll(".at-reading"));
    }
}
