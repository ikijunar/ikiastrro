using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Dasha;
using Ikiastrro.Core.Engines.PlanetNature;
using Ikiastrro.Core.Models;
using Xunit;

namespace Ikiastrro.Yoga.Tests;

/// <summary>Planet benefic/malefic by school (docs/research/domain/planet-benefic-malefic-perspectives.md). No DB: the rule
/// book below mirrors the rows migration 180 seeds for the cases these tests exercise. Expected verdicts are worked by hand
/// from the books (PVR Table 30; BPHS ch.34; Raman HTJH ch.1; Saravali ch.30 v.86-87).</summary>
public class PlanetBeneficMaleficReaderTests
{
    private static SchoolRule R(string school, string topic, string cls, string? planet, int house, string result, string src) =>
        new(school, topic, cls, planet, house, result, null, src);

    private static readonly SchoolRuleBook Book = BuildBook();

    private static SchoolRuleBook BuildBook()
    {
        var rules = new List<SchoolRule>();
        // PVR placement (functional)
        foreach (var h in new[] { 1, 4, 5, 7, 9, 10 })
        {
            rules.Add(R("PVR", "PLACEMENT", "FUNCTIONAL_BENEFIC", null, h, "GOOD", "SRC_PVR_INTEGRATED"));
            rules.Add(R("PVR", "PLACEMENT", "FUNCTIONAL_MALEFIC", null, h, "CHALLENGING", "SRC_PVR_INTEGRATED"));
        }
        foreach (var h in new[] { 3, 6, 8, 12 })
            rules.Add(R("PVR", "PLACEMENT", "FUNCTIONAL_MALEFIC", null, h, "GOOD", "SRC_PVR_INTEGRATED"));
        // Saravali placement (natural)
        foreach (var h in Enumerable.Range(1, 12))
        {
            var dus = h is 6 or 8 or 12;
            rules.Add(R("SARAVALI", "PLACEMENT", "NATURAL_MALEFIC", null, h, dus ? "GOOD" : "CHALLENGING", "SRC_SARAVALI"));
            rules.Add(R("SARAVALI", "PLACEMENT", "NATURAL_BENEFIC", null, h, dus ? "CHALLENGING" : "GOOD", "SRC_SARAVALI"));
        }
        foreach (var h in new[] { 3, 6, 11 })
            rules.Add(R("BPHS", "PLACEMENT", "NATURAL_MALEFIC", null, h, "GOOD", "SRC_BPHS"));
        // BPHS lordship
        var bphs = new Dictionary<int, string> { [1] = "BENEFIC", [2] = "NEUTRAL", [3] = "MALEFIC", [4] = "NEUTRAL", [5] = "BENEFIC", [6] = "MALEFIC",
            [7] = "NEUTRAL", [8] = "NEUTRAL", [9] = "BENEFIC", [10] = "NEUTRAL", [11] = "MALEFIC", [12] = "NEUTRAL" };
        foreach (var (h, res) in bphs) rules.Add(R("BPHS", "LORDSHIP", "ANY", null, h, res, "SRC_BPHS"));
        // Raman lordship
        rules.Add(R("RAMAN", "LORDSHIP", "ANY", null, 1, "BENEFIC", "SRC_RAMAN_HTJH"));
        rules.Add(R("RAMAN", "LORDSHIP", "ANY", "Moon", 1, "NEUTRAL", "SRC_RAMAN_HTJH"));
        foreach (var h in new[] { 5, 9 }) rules.Add(R("RAMAN", "LORDSHIP", "ANY", null, h, "BENEFIC", "SRC_RAMAN_HTJH"));
        foreach (var h in new[] { 3, 6, 11 }) rules.Add(R("RAMAN", "LORDSHIP", "ANY", null, h, "MALEFIC", "SRC_RAMAN_HTJH"));
        foreach (var h in new[] { 4, 7, 10 })
        {
            rules.Add(R("RAMAN", "LORDSHIP", "NATURAL_BENEFIC", null, h, "MALEFIC", "SRC_RAMAN_HTJH"));
            rules.Add(R("RAMAN", "LORDSHIP", "NATURAL_MALEFIC", null, h, "BENEFIC", "SRC_RAMAN_HTJH"));
        }
        foreach (var p in new[] { "Sun", "Moon" })
            foreach (var h in new[] { 2, 8, 12 })
                rules.Add(R("RAMAN", "LORDSHIP", "ANY", p, h, "NEUTRAL", "SRC_RAMAN_HTJH"));

        var schools = new[]
        {
            new BeneficMaleficSchool("PVR", "PVR", "PVR", "FUNCTIONAL", "SRC_PVR_INTEGRATED", 1, null),
            new BeneficMaleficSchool("BPHS", "BPHS", "BPHS", "NATURAL", "SRC_BPHS", 2, null),
            new BeneficMaleficSchool("RAMAN", "Raman", "Raman", "NATURAL", "SRC_RAMAN_HTJH", 3, null),
            new BeneficMaleficSchool("SARAVALI", "Saravali", "Saravali", "NATURAL", "SRC_SARAVALI", 4, null),
        };
        return new SchoolRuleBook(schools, rules);
    }

    private static ChartKeyDetail G(string planet, ZodiacName sign, int house, double lon = 0) => new()
    {
        Planet = planet, Sign = sign.ToString(), PointKind = "Graha", HouseNumberFromLagna = house,
        NirayanaLongitudeDegrees = lon, SignLordPlanet = null,
    };

    private static SchoolFunctionalReading Fn(IReadOnlyList<SchoolFunctionalReading> l, string school) => l.Single(r => r.SchoolCode == school);

    private static IReadOnlyList<SchoolFunctionalReading> Functional(ZodiacName lagna, PlanetName planet, int house, NaturalNature nat = NaturalNature.Benefic)
    {
        var n = new NaturalNatureReading(planet.ToString(), nat, "", null);
        return SchoolFunctionalNature.ReadAll(lagna, planet, n, house, true, Book);
    }

    [Fact]
    public void Jupiter_for_taurus_lagna_is_malefic_in_pvr_and_bphs_and_raman()
    {
        // 8th and 11th lord. PVR Table 30: malefic. BPHS ch.34: the 8th with the 11th is "extremely harmful". Raman: 11th lord malefic.
        var f = Functional(ZodiacName.Taurus, PlanetName.Jupiter, 3);
        Assert.Equal(SchoolFunctional.Malefic, Fn(f, "PVR").Verdict);
        Assert.Equal(SchoolFunctional.Malefic, Fn(f, "BPHS").Verdict);
        Assert.Equal(SchoolFunctional.Malefic, Fn(f, "RAMAN").Verdict);
    }

    [Fact]
    public void Natural_benefic_owning_two_kendras_is_malefic_in_pvr_and_raman_but_neutral_in_bphs()
    {
        // Gemini Lagna, Jupiter owns the 7th and 10th: the schools' core disagreement.
        var f = Functional(ZodiacName.Gemini, PlanetName.Jupiter, 3);
        Assert.Equal(SchoolFunctional.Malefic, Fn(f, "PVR").Verdict);
        Assert.Equal(SchoolFunctional.Neutral, Fn(f, "BPHS").Verdict);
        Assert.Equal(SchoolFunctional.Malefic, Fn(f, "RAMAN").Verdict);
    }

    [Fact]
    public void Raman_kendradhipati_effect_ceases_in_the_kendra_it_owns()
    {
        // Gemini Lagna, Jupiter in the 7th (Sagittarius): the 7th lordship stops acting malefic; the 10th still does -> mixed.
        var f = Functional(ZodiacName.Gemini, PlanetName.Jupiter, 7);
        var r = Fn(f, "RAMAN");
        Assert.Equal(SchoolFunctional.Mixed, r.Verdict);
        Assert.Contains("ceases", r.Why);
    }

    [Theory]
    [InlineData(ZodiacName.Taurus, PlanetName.Saturn)]      // 9th and 10th
    [InlineData(ZodiacName.Libra, PlanetName.Saturn)]       // 4th and 5th
    [InlineData(ZodiacName.Cancer, PlanetName.Mars)]        // 5th and 10th
    [InlineData(ZodiacName.Aquarius, PlanetName.Venus)]     // 4th and 9th
    public void Kendra_and_kona_lord_is_yogakaraka_in_every_school(ZodiacName lagna, PlanetName planet)
    {
        var f = Functional(lagna, planet, 1, NaturalNature.Malefic);
        Assert.All(f, r => Assert.Equal(SchoolFunctional.Yogakaraka, r.Verdict));
    }

    [Fact]
    public void Sun_and_moon_do_not_suffer_eighth_lordship()
    {
        // Sagittarius Lagna: the Moon rules Cancer, the 8th.
        var f = Functional(ZodiacName.Sagittarius, PlanetName.Moon, 1);
        Assert.Equal(SchoolFunctional.Neutral, Fn(f, "PVR").Verdict);
        Assert.Equal(SchoolFunctional.Neutral, Fn(f, "BPHS").Verdict);
        Assert.Equal(SchoolFunctional.Neutral, Fn(f, "RAMAN").Verdict);
    }

    [Fact]
    public void Bphs_two_house_lord_gives_the_stronger_houses_result()
    {
        // Libra Lagna, Jupiter owns the 3rd and 6th; the 6th is stronger in BPHS's order -> malefic.
        var r = Fn(Functional(ZodiacName.Libra, PlanetName.Jupiter, 1), "BPHS");
        Assert.Equal(SchoolFunctional.Malefic, r.Verdict);
        Assert.Contains("H6 is the stronger house", r.Why);
    }

    [Fact]
    public void Nodes_have_no_functional_nature()
    {
        var f = Functional(ZodiacName.Aries, PlanetName.Rahu, 1, NaturalNature.Malefic);
        Assert.All(f, r => Assert.Equal(SchoolFunctional.NotStated, r.Verdict));
    }

    [Fact]
    public void Schools_can_disagree_on_one_placement()
    {
        // Taurus Lagna, Venus (functional malefic, natural benefic) in the 6th: PVR good, Saravali challenging.
        var nat = new NaturalNatureReading("Venus", NaturalNature.Benefic, "", null);
        var pvr = SchoolFunctionalNature.Pvr(ZodiacName.Taurus, PlanetName.Venus, true);
        Assert.Equal(SchoolFunctional.Malefic, pvr.Verdict);
        var p = SchoolPlacement.ReadAll(6, nat, pvr, Book);
        Assert.Equal(PlacementEffect.Good, p.Single(x => x.SchoolCode == "PVR").Effect);
        Assert.Equal(PlacementEffect.Challenging, p.Single(x => x.SchoolCode == "SARAVALI").Effect);
        Assert.Equal(PlacementEffect.NotStated, p.Single(x => x.SchoolCode == "BPHS").Effect);   // BPHS rule is for malefics only
    }

    [Fact]
    public void Functional_neutral_gets_no_pvr_placement_verdict()
    {
        var nat = new NaturalNatureReading("Moon", NaturalNature.Malefic, "", null);
        var pvr = SchoolFunctionalNature.Pvr(ZodiacName.Sagittarius, PlanetName.Moon, true);   // neutral
        var p = SchoolPlacement.ReadAll(4, nat, pvr, Book);
        Assert.Equal(PlacementEffect.NotStated, p.Single(x => x.SchoolCode == "PVR").Effect);
    }

    [Fact]
    public void Mercury_alone_is_benefic_and_with_a_malefic_majority_is_malefic()
    {
        var alone = NaturalNatureReader.Read(new[] { G("Mercury", ZodiacName.Gemini, 1), G("Sun", ZodiacName.Leo, 5) }, true);
        Assert.Equal(NaturalNature.Benefic, alone["Mercury"].Nature);

        var withMars = NaturalNatureReader.Read(new[] { G("Mercury", ZodiacName.Aries, 1), G("Mars", ZodiacName.Aries, 1) }, true);
        Assert.Equal(NaturalNature.Malefic, withMars["Mercury"].Nature);
    }

    [Fact]
    public void Mercury_with_one_benefic_and_one_malefic_is_mixed_in_pvr_and_malefic_in_bphs()
    {
        var chart = new[] { G("Mercury", ZodiacName.Aries, 1), G("Mars", ZodiacName.Aries, 1), G("Venus", ZodiacName.Aries, 1) };
        var r = NaturalNatureReader.Read(chart, true)["Mercury"];
        Assert.Equal(NaturalNature.Mixed, r.Nature);
        Assert.Contains("Mars", r.BphsDiffers);
    }

    [Fact]
    public void Waning_moon_is_malefic_and_a_conjunct_benefic_is_reported_for_bphs()
    {
        var chart = new[] { G("Moon", ZodiacName.Taurus, 1, 40), G("Jupiter", ZodiacName.Taurus, 1, 41) };
        var r = NaturalNatureReader.Read(chart, moonWaxing: false)["Moon"];
        Assert.Equal(NaturalNature.Malefic, r.Nature);
        Assert.Contains("Jupiter", r.BphsDiffers);
    }

    [Fact]
    public void Reader_builds_a_row_per_graha_with_sign_and_nakshatra_lords()
    {
        var grahas = new List<ChartKeyDetail>
        {
            new() { Planet = "Sun", Sign = "Aries", PointKind = "Graha", HouseNumberFromLagna = 1, DignityStatus = "Exalted",
                    SignLordPlanet = "Mars", NakshatraLordPlanet = "Venus", Nakshatra = "Bharani", NakshatraPada = 2, NakshatraSubLordPlanet = "Moon" },
            new() { Planet = "Mars", Sign = "Leo", PointKind = "Graha", HouseNumberFromLagna = 5, SignLordPlanet = "Sun", NakshatraLordPlanet = "Ketu" },
            new() { Planet = "Venus", Sign = "Taurus", PointKind = "Graha", HouseNumberFromLagna = 2, SignLordPlanet = "Venus", NakshatraLordPlanet = "Mars" },
        };
        var rows = PlanetBeneficMaleficReader.Read(grahas, ZodiacName.Aries, true, Book);
        var sun = rows.Single(r => r.Planet == "Sun");
        Assert.Equal(3, rows.Count);
        Assert.Equal("Mars", sun.SignInfo.Lord!.Planet);
        Assert.Equal(NaturalNature.Malefic, sun.SignInfo.Lord.Natural);
        Assert.Equal("Venus", sun.NakshatraInfo.StarLord!.Planet);
        Assert.Equal(NaturalNature.Benefic, sun.NakshatraInfo.StarLord.Natural);
        Assert.True(rows.Single(r => r.Planet == "Venus").SignInfo.IsOwnLord);
    }

    [Fact]
    public void Final_house_verdict_counts_conjunct_and_aspecting_planets()
    {
        // Taurus Lagna. Jupiter and Venus (both functional malefics) sit together in Gemini, the 2nd; Mars in Scorpio
        // aspects it (8th sight). Mercury, the 2nd lord, is a functional benefic.
        var chart = new List<ChartKeyDetail>
        {
            G("Ascendant", ZodiacName.Taurus, 1, 40),
            G("Sun", ZodiacName.Leo, 4, 130), G("Moon", ZodiacName.Cancer, 3, 100), G("Mars", ZodiacName.Scorpio, 7, 220),
            G("Mercury", ZodiacName.Virgo, 5, 170), G("Jupiter", ZodiacName.Gemini, 2, 70), G("Venus", ZodiacName.Gemini, 2, 72),
            G("Saturn", ZodiacName.Pisces, 11, 340), G("Rahu", ZodiacName.Aquarius, 10, 310), G("Ketu", ZodiacName.Leo, 4, 130),
        };
        var rows = PlanetBeneficMaleficReader.Read(chart, ZodiacName.Taurus, true, Book);
        var jupiter = rows.Single(r => r.Planet == "Jupiter");
        Assert.NotNull(jupiter.HouseVerdict);
        Assert.Equal(2, jupiter.HouseVerdict!.HouseNumber);
        Assert.Contains("Venus", jupiter.HouseVerdict.MaleficOccupants);          // the conjunction
        Assert.Contains("Jupiter", jupiter.HouseVerdict.MaleficOccupants);        // the planet itself is an occupant
        Assert.Contains("Mars", jupiter.HouseVerdict.MaleficAspectors);           // Mars in Scorpio aspects the 2nd (8th from itself)
        Assert.Equal(Ikiastrro.Core.Engines.Houses.HouseBeneficMaleficVerdict.Malefic, jupiter.HouseVerdict.Verdict);
    }

    private static List<ChartKeyDetail> TaurusChart()
    {
        var chart = new List<ChartKeyDetail>
        {
            G("Ascendant", ZodiacName.Taurus, 1, 40),
            G("Sun", ZodiacName.Leo, 4, 130), G("Moon", ZodiacName.Cancer, 3, 100), G("Mars", ZodiacName.Scorpio, 7, 220),
            G("Mercury", ZodiacName.Virgo, 5, 170), G("Jupiter", ZodiacName.Gemini, 2, 70), G("Venus", ZodiacName.Gemini, 2, 72),
            G("Saturn", ZodiacName.Pisces, 11, 340), G("Rahu", ZodiacName.Aquarius, 10, 310), G("Ketu", ZodiacName.Leo, 4, 130),
        };
        chart.Single(k => k.Planet == "Mars").DignityStatus = "Own Sign";
        chart.Single(k => k.Planet == "Jupiter").DignityStatus = "Enemy";
        return chart;
    }

    [Fact]
    public void Running_dasha_picks_maha_antar_and_pratyantar_for_a_date()
    {
        var pd = new DashaPeriodRecord { Lord = "Venus", LevelNumber = 3, StartDate = new DateTime(2010, 3, 1), EndDate = new DateTime(2010, 4, 1) };
        var ad = new DashaPeriodRecord { Lord = "Sun", LevelNumber = 2, StartDate = new DateTime(2010, 1, 1), EndDate = new DateTime(2011, 1, 1) };
        ad.Children.Add(pd);
        var md = new DashaPeriodRecord { Lord = "Mars", LevelNumber = 1, StartDate = new DateTime(2005, 1, 1), EndDate = new DateTime(2012, 1, 1) };
        md.Children.Add(ad);

        var run = RunningDasha.At(new[] { md }, new DateTime(2010, 3, 15));
        Assert.Equal(new[] { "Mars", "Sun", "Venus" }, run.Take(3).Select(r => r.Planet));
        Assert.Equal(7, run.Count);                        // levels 4-7 follow from the Vimshottari proportions
        Assert.Equal(new[] { "Sookshma", "Prana", "Deha", "Level 7" }, run.Skip(3).Select(r => r.Level));
        for (var i = 1; i < run.Count; i++) Assert.True(run[i].End <= run[i - 1].End && run[i].End > new DateTime(2010, 3, 15), run[i].Level);
        Assert.Equal(new[] { "Mahadasha", "Antardasha", "Pratyantardasha" }, run.Take(3).Select(r => r.Level));
        Assert.Single(RunningDasha.At(new[] { md }, new DateTime(2006, 1, 1)));   // no antardasa stored for that date
        Assert.Empty(RunningDasha.At(new[] { md }, new DateTime(2020, 1, 1)));
    }

    [Fact]
    public void Dasha_facts_flag_the_lord_of_the_twelfth_and_a_planet_in_an_inimical_sign()
    {
        // Taurus Lagna: Mars rules the 7th and 12th (Phaladeepika 20.24: the 12th lord's dasa brings misery);
        // Jupiter sits in an inimical sign (20.27) and rules the 8th and 11th.
        var chart = TaurusChart();
        var rows = PlanetBeneficMaleficReader.Read(chart, ZodiacName.Taurus, true, Book);
        var run = new[] { new RunningLord("Mahadasha", "Mars", new DateTime(2030, 1, 1)), new RunningLord("Antardasha", "Venus", new DateTime(2027, 1, 1)) };
        var read = DashaNatureReader.Read(rows, chart, ZodiacName.Taurus, run);

        var mars = read.Single(r => r.Planet == "Mars");
        Assert.Equal("Mahadasha", mars.Running!.Level);
        Assert.Contains(mars.Facts, f => f.Source == "Phaladeepika 20.24" && f.Adverse && f.Text.Contains("12th"));
        Assert.Contains(mars.Facts, f => f.Source == "Phaladeepika 20.30" && f.Text.StartsWith("In its own sign"));

        var jupiter = read.Single(r => r.Planet == "Jupiter");
        Assert.Null(jupiter.Running);
        Assert.Contains(jupiter.Facts, f => f.Source == "Phaladeepika 20.24" && f.Text.Contains("8th"));
        Assert.Contains(jupiter.Facts, f => f.Source == "Phaladeepika 20.27" && f.Text.Contains("inimical sign"));
        Assert.True(jupiter.HasAdverseFlag);
    }

    [Fact]
    public void Antardasha_lord_is_judged_by_its_house_from_the_mahadasha_lord()
    {
        // Mars in Scorpio, Venus in Gemini: Gemini is the 8th from Scorpio, so Phaladeepika 20.29 reads the antardasa as bad.
        var chart = TaurusChart();
        var rows = PlanetBeneficMaleficReader.Read(chart, ZodiacName.Taurus, true, Book);
        var run = new[] { new RunningLord("Mahadasha", "Mars", new DateTime(2030, 1, 1)), new RunningLord("Antardasha", "Venus", new DateTime(2027, 1, 1)) };
        var venus = DashaNatureReader.Read(rows, chart, ZodiacName.Taurus, run).Single(r => r.Planet == "Venus");
        var fact = venus.Facts.Single(f => f.Source == "Phaladeepika 20.29");
        Assert.True(fact.Adverse);
        Assert.Contains("8th house from Mars", fact.Text);
    }

    [Fact]
    public void Pvr_across_charts_uses_each_charts_own_lagna()
    {
        var m = PlanetBeneficMaleficReader.PvrAcrossCharts(new[] { ("D1", ZodiacName.Taurus), ("D10", ZodiacName.Gemini) }, true);
        Assert.Equal(SchoolFunctional.Yogakaraka, m["D1"][PlanetName.Saturn]);
        Assert.Equal(SchoolFunctional.Malefic, m["D10"][PlanetName.Jupiter]);
        Assert.False(m["D1"].ContainsKey(PlanetName.Rahu));
    }
}
