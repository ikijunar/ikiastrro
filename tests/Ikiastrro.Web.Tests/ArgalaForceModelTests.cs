using Bunit;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.PlanetaryStates;
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
    public void Table_SwitchingChart_KeepsThePick_AndRebuildsTheOverlayForTheNewVarga()
    {
        var grahas = Chart5();
        ArgalaOverlay? sent = null;
        var cut = RenderTable(grahas, o => sent = o);
        ClickHouse(cut, 4);
        Assert.Equal("D1", sent!.ChartType);

        cut.Render(p => p.Add(x => x.ChartType, "D9").Add(x => x.Facts, ArgalaFacts.Live("D9", "Scorpio", grahas)));

        cut.WaitForAssertion(() => Assert.Equal("D9", sent!.ChartType));
        Assert.Equal(4, sent!.House);
        Assert.Single(cut.FindAll(".at-reading"));
        Assert.Contains("D9", cut.Find(".at-reading-varga").Normalized());
    }

    // --- avasthas on argala ---

    private static StateReason Reason(string system, string state, string why) => new(system, state, why);

    private static ArgalaMeaningContext WithStates(params (string Planet, PlanetStateInfo State)[] states) =>
        new("D1", null, null, null, states.ToDictionary(x => x.Planet, x => x.State));

    [Fact]
    public void PlanetStateInfo_ReadsDeliveryFromJagradadi_AndKeepsOnlyTheDamagingStates()
    {
        var info = PlanetStateInfo.From([
            Reason("Jagradadi", "Sushupti", "Dignity is Enemy"),
            Reason("Lajjitadi", "Lajjita", "In the 5th house, joined by Saturn"),
            Reason("Lajjitadi", "Garvita", "Dignity is Exalted"),
            Reason("Deeptadi", "Kopita", "3.8 degrees from the Sun")]);

        Assert.Equal(Delivery.Negligible, info.Delivery);
        Assert.Equal("Sushupti", info.Alertness);
        Assert.Equal(["Lajjita"], info.Damaging.Select(d => d.State));
        Assert.Equal(Delivery.Unknown, PlanetStateInfo.From([]).Delivery);
        Assert.Equal(Delivery.Full, PlanetStateInfo.From([Reason("Jagradadi", "Jagrat", "Dignity is Own Sign")]).Delivery);
        Assert.Equal(Delivery.Medium, PlanetStateInfo.From([Reason("Jagradadi", "Swapna", "Dignity is Friend")]).Delivery);
    }

    [Fact]
    public void Reading_FlagsAWinThatRestsOnASleepingPlanet()
    {
        // House 1 of Chart 5 is won 3-2: a one-planet margin, so every argala planet is deciding. Mercury is asleep.
        var ctx = WithStates(("Mercury", PlanetStateInfo.From([Reason("Jagradadi", "Sushupti", "Dignity is Enemy")])));

        var force = For(1, Chart5(), ctx);

        var caveat = Assert.Single(force.Reading.Caveats);
        Assert.Contains("rests on Mercury", caveat);
        Assert.Contains("asleep", caveat);
        var cause = Assert.Single(force.Reading.Causes, c => c.StartsWith("Mercury"));
        Assert.Contains("asleep: its results are negligible", cause);
        Assert.Contains("Dignity is Enemy", cause);
    }

    [Fact]
    public void Reading_NoCaveat_WhenTheWinIsNotAOnePlanetMargin()
    {
        // House 11 is won 4-2, so no single planet decides it, whatever Mercury's state.
        var ctx = WithStates(("Mercury", PlanetStateInfo.From([Reason("Jagradadi", "Sushupti", "Dignity is Enemy")])));

        var force = For(11, Chart5(), ctx);

        Assert.Empty(force.Reading.Caveats);
        Assert.Empty(ArgalaForceModel.DecidingPlanets(force));
    }

    [Fact]
    public void Reading_FlagsADamagedPlanet_AndNamesPvrHouseResult()
    {
        // Aries lagna, Jupiter in Leo (house 5): the 2nd from house 4 (Cancer) is Leo, so Jupiter causes argala on house 4
        // from the 5th house, which is where Lajjita's PVR result applies.
        var grahas = Chart(ZodiacName.Aries, (PlanetName.Jupiter, ZodiacName.Leo));
        var force = For(4, grahas, WithStates(("Jupiter", PlanetStateInfo.From([
            Reason("Lajjitadi", "Lajjita", "In the 5th house, joined by Saturn")]))));

        var cause = Assert.Single(force.Reading.Causes);
        Assert.Contains("Lajjita (In the 5th house, joined by Saturn)", cause);
        Assert.Contains("losses related to progeny", cause);
        Assert.Contains(force.Reading.Caveats, c => c.Contains("Lajjita"));
    }

    [Fact]
    public void Overlay_CarriesEachPlanetsTag_AndTheLanesStrongestDelivery()
    {
        var ctx = WithStates(
            ("Mercury", PlanetStateInfo.From([Reason("Jagradadi", "Sushupti", "Dignity is Enemy")])),
            ("Ketu", PlanetStateInfo.From([Reason("Jagradadi", "Swapna", "Dignity is Friend")])));
        var force = For(1, Chart5(), ctx);

        var overlay = ArgalaForceModel.Overlay("D1", force, null);

        var second = overlay.Links.Single(l => l.ArgalaSign == ZodiacName.Sagittarius);          // Mercury, the 2nd from Scorpio
        Assert.Equal(Delivery.Negligible, second.Delivery);
        var tag = Assert.Single(second.ArgalaPlanets);
        Assert.Equal("Su", tag.Tag);
        Assert.True(tag.Deciding);
        Assert.Contains("asleep", tag.Title);
        var fourth = overlay.Links.Single(l => l.ArgalaSign == ZodiacName.Aquarius);             // Ketu
        Assert.Equal(Delivery.Medium, fourth.Delivery);
        Assert.Equal("Sw", fourth.ArgalaPlanets[0].Tag);
    }

    [Fact]
    public void LaneDelivery_IsTheBestPlanetsDelivery()
    {
        ForcePlanet P(PlanetName n, Delivery d) =>
            new(n, Ikiastrro.Core.LifeMatters.Promise.Direction.Neutral, 0, new PlanetStateInfo(d, null, null, []));

        Assert.Equal(Delivery.Full, ArgalaForceModel.LaneDelivery([P(PlanetName.Sun, Delivery.Negligible), P(PlanetName.Moon, Delivery.Full)]));
        Assert.Equal(Delivery.Medium, ArgalaForceModel.LaneDelivery([P(PlanetName.Sun, Delivery.Unknown), P(PlanetName.Moon, Delivery.Medium)]));
        Assert.Equal(Delivery.Unknown, ArgalaForceModel.LaneDelivery([P(PlanetName.Sun, Delivery.Unknown)]));
    }

    [Fact]
    public void Avastha_Table_ShowsTheDefinitions_AndWhyEachStateWasAssigned()
    {
        var states = new Dictionary<byte, PlanetaryStateRow>
        {
            [1] = new(1, "Jagradadi", "Sushupti", 3, null), [2] = new(2, "Deeptadi", "Duhkhita", 6, null),
        };
        var rows = new List<PlanetaryStateFact>
        {
            new() { Planet = "Mars", WakefulnessStateId = 1, DeeptadiStateIds = [2] }
        };
        var reasons = new Dictionary<string, IReadOnlyList<StateReason>>
        {
            ["Mars"] = [Reason("Jagradadi", "Sushupti", "Dignity is Enemy"),
                        Reason("Deeptadi", "Duhkhita", "Compound relationship to its sign lord Venus is Enemy")]
        };

        var cut = Render<PlanetaryStateTable>(p => p
            .Add(x => x.Rows, rows).Add(x => x.StateNames, states).Add(x => x.Reasons, reasons));

        var text = cut.Markup.Normalized();
        Assert.Contains("What each avastha set looks at", text);
        Assert.Contains("Friendship with the sign lord", text);
        Assert.Contains("Not part of any avastha", text);
        Assert.Contains("Dignity is Enemy", text);
        Assert.Contains("Compound relationship to its sign lord Venus is Enemy", text);
        Assert.Equal(5, cut.FindAll(".pst-defs-table tbody tr").Count);
    }
}
