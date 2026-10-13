using Bunit;
using Ikiastrro.Core.Engines.PlanetNature;
using Ikiastrro.Core.Models;
using Ikiastrro.Web.Components.Charts;
using Ikiastrro.Web.Components.Workspace;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>Astro Facts "Benefic / malefic by school": one column per school, a dash where a school says nothing, and the
/// functional columns recounted from each chart's own Lagna.</summary>
public sealed class PlanetBeneficMaleficTableTests : BunitContext
{
    private static ChartKeyDetail G(string planet, string sign, int house, double lon, string? dignity = null, string? signLord = null, string? nakLord = null, string? nak = null) => new()
    {
        PointKind = "Graha", Planet = planet, Sign = sign, HouseNumberFromLagna = house, NirayanaLongitudeDegrees = lon,
        DignityStatus = dignity, SignLordPlanet = signLord, NakshatraLordPlanet = nakLord, Nakshatra = nak,
    };

    /// <summary>Taurus Lagna. Jupiter in Virgo (6th) owns the 8th and 11th; Venus owns the Lagna and the 6th; Saturn is yogakaraka.</summary>
    private static LoadedChart D1(string type = "D1", string lagna = "Taurus") => new(type, type, type, null, lagna, "Taurus", null,
        [
            G("Ascendant", lagna, 1, 40),
            G("Sun", "Taurus", 1, 45, "Neutral", "Venus", "Sun", "Krittika"), G("Moon", "Cancer", 3, 100),
            G("Mars", "Leo", 4, 130), G("Mercury", "Gemini", 2, 70),
            G("Jupiter", "Virgo", 5, 170, "Enemy", "Mercury", "Moon", "Hasta"), G("Venus", "Libra", 6, 200),
            G("Saturn", "Capricornus", 9, 280, "Own", "Saturn", "Sun", "Uttara Ashadha"), G("Rahu", "Aries", 12, 10), G("Ketu", "Libra", 6, 190),
        ], [], [], [], [], [], null, null, "test");

    private static SchoolRuleBook Book()
    {
        SchoolRule R(string school, string topic, string cls, string? planet, int house, string result) =>
            new(school, topic, cls, planet, house, result, null, "SRC_TEST");
        var schools = new[]
        {
            new BeneficMaleficSchool("PVR", "PVR book", "PVR", "FUNCTIONAL", "SRC_PVR_INTEGRATED", 1, null),
            new BeneficMaleficSchool("BPHS", "BPHS book", "BPHS", "NATURAL", "SRC_BPHS", 2, null),
            new BeneficMaleficSchool("SARAVALI", "Saravali book", "Saravali", "NATURAL", "SRC_SARAVALI", 4, null),
        };
        var rules = new List<SchoolRule>
        {
            R("BPHS", "LORDSHIP", "ANY", null, 8, "NEUTRAL"), R("BPHS", "LORDSHIP", "ANY", null, 11, "MALEFIC"),
            R("PVR", "PLACEMENT", "FUNCTIONAL_MALEFIC", null, 6, "GOOD"),
            R("SARAVALI", "PLACEMENT", "NATURAL_BENEFIC", null, 6, "CHALLENGING"),
            R("SARAVALI", "PLACEMENT", "NATURAL_BENEFIC", null, 5, "GOOD"),
        };
        return new SchoolRuleBook(schools, rules);
    }

    private IRenderedComponent<PlanetBeneficMaleficTable> RenderIt(LoadedChart chart, IReadOnlyDictionary<string, LoadedChart>? charts = null) =>
        Render<PlanetBeneficMaleficTable>(p => p.Add(x => x.Chart, chart).Add(x => x.Charts, charts).Add(x => x.Rules, Book()).Add(x => x.MoonWaxing, true));

    [Fact]
    public void Dasha_column_marks_running_lords_and_flags_adverse_ones()
    {
        // Mars Mahadasha, Sun Antardasha on 1 Jun 2020. Taurus Lagna: Mars rules the 7th and 12th (Phaladeepika 20.24).
        var ad = new Ikiastrro.Core.Engines.Dasha.DashaPeriodRecord { Lord = "Sun", LevelNumber = 2, StartDate = new DateTime(2020, 1, 1), EndDate = new DateTime(2021, 1, 1) };
        var md = new Ikiastrro.Core.Engines.Dasha.DashaPeriodRecord { Lord = "Mars", LevelNumber = 1, StartDate = new DateTime(2015, 1, 1), EndDate = new DateTime(2022, 1, 1) };
        md.Children.Add(ad);
        var cut = Render<PlanetBeneficMaleficTable>(p => p.Add(x => x.Chart, D1()).Add(x => x.Rules, Book()).Add(x => x.MoonWaxing, true)
            .Add(x => x.DashaPeriods, new[] { md }).Add(x => x.AsOf, new DateTime(2020, 6, 1)));

        Assert.Contains("Dasha period", cut.FindAll("thead th").Select(h => h.TextContent.Trim()));
        var rows = cut.FindAll("tbody tr");
        var mars = rows.Single(r => r.QuerySelector("b")!.TextContent == "Mars").Normalized();
        Assert.Contains("MD · until Jan 2022", mars);
        Assert.Contains("Adverse flag", mars);
        Assert.Contains("Lord of the 12th: its dasa brings misery (Phaladeepika 20.24)", mars);
        Assert.Contains("AD · until Jan 2021", rows.Single(r => r.QuerySelector("b")!.TextContent == "Sun").Normalized());
        Assert.DoesNotContain(" until ", rows.Single(r => r.QuerySelector("b")!.TextContent == "Moon").Normalized());   // not running
    }

    [Fact]
    public void Without_a_dasha_tree_there_is_no_dasha_column()
    {
        Assert.DoesNotContain("Dasha period", RenderIt(D1()).FindAll("thead th").Select(h => h.TextContent.Trim()));
    }

    [Fact]
    public void Shows_one_column_per_school_and_one_row_per_graha()
    {
        var cut = RenderIt(D1());
        var heads = cut.FindAll("thead th").Select(h => h.TextContent.Trim()).ToList();
        Assert.Contains("PVR", heads);
        Assert.Contains("BPHS", heads);
        Assert.Contains("House (final)", heads);
        Assert.DoesNotContain("Saravali", heads);                       // per-school placement columns were replaced by the final-house verdict
        Assert.Equal(9, cut.FindAll("tbody tr").Count);
    }

    [Fact]
    public void Jupiter_row_shows_each_schools_own_reading()
    {
        // Taurus Lagna: PVR malefic; BPHS: 8th with 11th is extremely harmful; Jupiter sits in Virgo, the 5th.
        var cut = RenderIt(D1());
        var row = cut.FindAll("tbody tr").Single(r => r.QuerySelector("b")!.TextContent == "Jupiter");
        var cells = row.QuerySelectorAll("td").Select(c => c.TextContent.Trim()).ToList();
        Assert.Equal("Benefic", cells[1]);                  // natural
        Assert.Equal("Malefic", cells[2]);                  // PVR functional
        Assert.Equal("Malefic", cells[3]);                  // BPHS functional (8th + 11th)
        Assert.Matches("^(Benefic|Malefic|Mixed|Neutral)", cells[4]);   // final house verdict
        Assert.Contains("Lord Mercury", cells[4]);                      // Virgo, the 5th, is ruled by Mercury
        Assert.Contains("Lord Mercury", row.Normalized());
    }

    [Fact]
    public void Final_house_names_the_conjunctions()
    {
        // Venus and Ketu share Libra, the 6th from a Taurus Lagna.
        var venus = RenderIt(D1()).FindAll("tbody tr").Single(r => r.QuerySelector("b")!.TextContent == "Venus");
        Assert.Contains("With Ketu (malefic)", venus.Normalized());
    }

    [Fact]
    public void A_school_with_no_rule_shows_a_dash_not_a_verdict()
    {
        var cut = RenderIt(D1());
        var rahu = cut.FindAll("tbody tr").Single(r => r.QuerySelector("b")!.TextContent == "Rahu");
        Assert.Equal("—", rahu.QuerySelectorAll("td")[2].TextContent.Trim());   // PVR Table 30 lists the seven planets only
    }

    [Fact]
    public void Across_vargas_grid_recounts_functional_nature_from_each_charts_own_lagna()
    {
        var charts = new Dictionary<string, LoadedChart> { ["D1"] = D1(), ["D10"] = D1("D10", "Gemini") };
        var cut = RenderIt(D1(), charts);
        Assert.Contains("PVR functional nature across the vargas", cut.Markup.Normalized());
        var jupiter = cut.FindAll(".pbm-across tbody tr").Single(r => r.QuerySelector("b")!.TextContent == "Jupiter");
        var letters = jupiter.QuerySelectorAll("td").Skip(1).Select(c => c.TextContent.Trim()).ToList();
        Assert.Equal(new[] { "M", "M" }, letters);          // Jupiter is a functional malefic for both Taurus and Gemini Lagna
        var saturn = cut.FindAll(".pbm-across tbody tr").Single(r => r.QuerySelector("b")!.TextContent == "Saturn");
        Assert.Equal("Y", saturn.QuerySelectorAll("td").Skip(1).First().TextContent.Trim());   // yogakaraka for Taurus
    }

    [Fact]
    public void Single_chart_has_no_across_vargas_grid()
    {
        var cut = RenderIt(D1(), new Dictionary<string, LoadedChart> { ["D1"] = D1() });
        Assert.Empty(cut.FindAll(".pbm-across"));
    }

    [Fact]
    public void Incomplete_chart_says_so_instead_of_rendering_a_table()
    {
        var thin = D1() with { KeyDetails = D1().KeyDetails.Take(4).ToList() };
        var cut = RenderIt(thin);
        Assert.Empty(cut.FindAll("table"));
        Assert.Contains("Needs a complete chart", cut.Markup.Normalized());
    }
}
