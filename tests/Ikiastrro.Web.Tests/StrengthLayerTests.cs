using Bunit;
using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Data;
using Ikiastrro.Web.Components.AstroV10;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>AF-v10's strength layer: planet strength, Ashtakavarga and yogas folded into the planet and house picks.</summary>
public sealed class StrengthLayerTests : BunitContext
{
    private static ShadbalaSummaryRow Shad(string planet, decimal rupas, decimal? pct) =>
        new(planet, 0, 0, 0, 0, 0, 0, 0, rupas * 60, rupas, 5m, pct, 40m, 20m);

    private static ChartKeyDetail Graha(string planet, ZodiacName sign, double longitude = 100) => new()
    {
        PointKind = "Graha", Planet = planet, Sign = sign.ToString(), NirayanaLongitudeDegrees = longitude, HouseNumberFromLagna = 1
    };

    private static AshtakavargaRow Bav(string recipient, int sign, int bindus, string chart = "D1") =>
        new(chart, recipient, (byte)sign, (byte)sign, (byte)bindus, null);

    [Fact]
    public void Planets_ReadShadbalaTier_AndOwnBindusInTheSignItStandsIn()
    {
        var result = StrengthLayer.Planets(
            [Shad("Jupiter", 7m, 120m), Shad("Saturn", 3m, 60m)],
            [Bav("JUPITER", 4, 6), Bav("JUPITER", 5, 2)], "D1",
            [Graha("Jupiter", ZodiacName.Cancer), Graha("Saturn", ZodiacName.Libra), Graha("Rahu", ZodiacName.Leo)]);

        Assert.Equal(StrengthTier.Strong, result["Jupiter"].Tier);
        Assert.Equal(6, result["Jupiter"].OwnBindus);              // Cancer = sign 4
        Assert.Equal(StrengthTier.Strong, result["Jupiter"].BindusTier);
        Assert.Equal(StrengthTier.Weak, result["Saturn"].Tier);
        Assert.Null(result["Saturn"].OwnBindus);                    // no Saturn row for Libra
        Assert.Equal(StrengthTier.None, result["Rahu"].Tier);       // Rahu has no Shadbala
        Assert.Null(result["Rahu"].OwnBindus);
    }

    [Fact]
    public void Planets_IgnoreAshtakavargaRowsOfAnotherChart()
    {
        var result = StrengthLayer.Planets([], [Bav("SUN", 1, 7, "D9")], "D1", [Graha("Sun", ZodiacName.Aries)]);
        Assert.Null(result["Sun"].OwnBindus);
    }

    [Fact]
    public void Houses_CountSignsFromTheLagna_AndBandSavByTheSharedScale()
    {
        // Lagna Scorpio (sign 8): house 1 = Scorpio, house 6 = Aries.
        AshtakavargaRow[] rows =
        [
            new("D1", "SUN", 8, 1, 4, 33), new("D1", "SUN", 1, 6, 3, 22), new("D1", "SUN", 2, 7, 3, 27)
        ];
        BhavaBalaSummaryRow[] bhava = [new(1, "Scorpio", "Mars", 1, "Scorpio", null, 480m, 8.0m), new(6, "Aries", "Mars", 1, "Scorpio", null, 240m, 4.0m)];

        var houses = StrengthLayer.Houses(rows, bhava, "D1", "Scorpio");

        Assert.Equal(12, houses.Count);
        Assert.Equal(33, houses[0].SavBindus);
        Assert.Equal(StrengthTier.Strong, houses[0].SavTier);
        Assert.Equal(StrengthTier.Strong, houses[0].BhavaTier);
        Assert.Equal(22, houses[5].SavBindus);
        Assert.Equal(StrengthTier.Weak, houses[5].SavTier);
        Assert.Equal(StrengthTier.Weak, houses[5].BhavaTier);
        Assert.Equal(StrengthTier.Moderate, houses[6].SavTier);     // 27 is between 25 and 31
        Assert.Null(houses[1].SavBindus);                           // no row for Sagittarius
    }

    [Fact]
    public void Houses_BhavaBalaIsNatalOnly()
    {
        var houses = StrengthLayer.Houses([], [new(1, "Aries", "Mars", 1, "Aries", null, 480m, 8m)], "D9", "Aries");
        Assert.All(houses, h => Assert.Null(h.BhavaRupas));
    }

    [Fact]
    public void Yogas_AreListedOncePerCode_GoodFirst_WithTheWeakestPlanetsPercent()
    {
        YogaEvaluationRow Row(string code, bool present, string nature, string source, string rule) =>
            new(source, code, present, "EVALUATED", "COMBINATION", rule, null, OutcomeNatureCode: nature);
        YogaEvaluationRow[] rows =
        [
            Row("YOGA_KEMADRUMA", true, "INAUSPICIOUS", "SRC_A", "Moon alone"),
            Row("YOGA_GAJAKESARI", true, "AUSPICIOUS", "SRC_B", "Jupiter in a kendra from the Moon"),
            Row("YOGA_GAJAKESARI", true, "AUSPICIOUS", "SRC_A", "Jupiter in a kendra from the Moon"),
            Row("YOGA_ABSENT", false, "AUSPICIOUS", "SRC_A", "Jupiter")
        ];
        var placements = new Dictionary<PlanetName, YogaPlacement> { [PlanetName.Jupiter] = new(1, 1, 1), [PlanetName.Moon] = new(4, 4, 1) };

        var yogas = StrengthLayer.Yogas(rows, new Dictionary<int, PlanetName>(), placements, [Shad("Jupiter", 7m, 130m)]);

        Assert.Equal(["YOGA_GAJAKESARI", "YOGA_KEMADRUMA"], yogas.Select(y => y.Code));
        Assert.Equal("good", yogas[0].Nature);
        Assert.Equal("Gajakesari", yogas[0].Name);
        Assert.Equal("challenging", yogas[1].Nature);
        Assert.Contains("Jupiter", yogas[0].Planets);
    }

    [Theory]
    [InlineData(150, "full strength")]
    [InlineData(90, "moderate")]
    [InlineData(50, "weak planet")]
    public void Delivery_FollowsTheWeakestPlanet(int percent, string expected) =>
        Assert.Contains(expected, StrengthLayer.Delivery(new YogaChipFact("Y", "Y", "good", null, ["Sun"], percent, null, null)));

    [Fact]
    public void Nakshatra_IsReadOffTheTrueLongitude()
    {
        var place = NakshatraPlace.Of(Graha("Moon", ZodiacName.Aries, 5));   // 0-13.33 is the first nakshatra, 3.33 per pada
        Assert.Equal(ConstellationName.Aswini.ToString(), place.Name);
        Assert.Equal(2, place.Pada);
    }

    [Fact]
    public void YogaChip_ShowsNameNature_AndMarksAWeakPlanetYogaDashed()
    {
        var cut = Render<YogaChip>(p => p.Add(x => x.Fact, new YogaChipFact("YOGA_X", "Xyz", "good", null, ["Sun", "Moon"], 60m, null, null)));
        Assert.Equal("Xyz", cut.Find(".yc-name").TextContent);
        Assert.Contains("good", cut.Find(".yc-meta").TextContent);
        Assert.Contains("yc-weak", cut.Find("button").ClassList);
    }

    [Fact]
    public void PlanetCard_ShowsNakshatra_StrengthPercent_AndBindus()
    {
        var strength = new PlanetStrengthFacts("Jupiter", 7m, 130m, StrengthTier.Strong, 40m, 20m, 6, StrengthTier.Strong);
        var cut = Render<PlanetCard>(p => p.Add(x => x.Row, Graha("Jupiter", ZodiacName.Cancer, 100)).Add(x => x.Strength, strength).Add(x => x.Layer, AfLayer.Strength));
        Assert.Contains("pada", cut.Find(".pc-nak").TextContent);
        Assert.Equal("130%", cut.Find(".pc-pct").TextContent);
        Assert.Contains("6", cut.Find(".pc-bav-strong").TextContent);
    }

    [Fact]
    public void BhavaTile_ShowsSavAndBhavaBala_WhenGiven()
    {
        var strength = new HouseStrength(1, 33, StrengthTier.Strong, 8.0m, StrengthTier.Strong);
        var grahas = new[] { Graha("Ascendant", ZodiacName.Scorpio) };
        var force = Ikiastrro.Web.Components.Charts.ArgalaForceModel.Build(1, ArgalaFacts.Live("D1", "Scorpio", grahas), grahas, "Scorpio");
        var cut = Render<BhavaTile>(p => p.Add(x => x.Force, force).Add(x => x.Strength, strength).Add(x => x.Layer, AfLayer.Strength));
        Assert.Contains("33", cut.Find(".bt-str").TextContent);
        Assert.Contains("8.0", cut.Find(".bt-str").TextContent);
    }

    [Fact]
    public void Tiles_AndCards_CarryOneFactPerLayer()
    {
        var grahas = new[] { Graha("Ascendant", ZodiacName.Scorpio) };
        var force = Ikiastrro.Web.Components.Charts.ArgalaForceModel.Build(1, ArgalaFacts.Live("D1", "Scorpio", grahas), grahas, "Scorpio");
        var tile = Render<BhavaTile>(p => p.Add(x => x.Force, force).Add(x => x.Layer, AfLayer.Strength));
        Assert.Empty(tile.FindAll(".bt-meter"));                    // the argala meter belongs to the Argala layer only
        var yogas = Render<BhavaTile>(p => p.Add(x => x.Force, force).Add(x => x.Layer, AfLayer.Yogas).Add(x => x.YogaCount, 3));
        Assert.Equal("3 yogas", yogas.Find(".bt-word").TextContent);

        var card = Render<PlanetCard>(p => p.Add(x => x.Row, Graha("Sun", ZodiacName.Aries)).Add(x => x.Layer, AfLayer.Yogas).Add(x => x.YogaCount, 1));
        Assert.Equal("1 yoga", card.Find(".pc-nature").TextContent);
        Assert.Empty(card.FindAll(".pc-nak"));
    }
}
