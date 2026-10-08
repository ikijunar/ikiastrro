using Bunit;
using Ikiastrro.Core.Models;
using Ikiastrro.Web.Components.Charts;
using Ikiastrro.Web.Components.Workspace;
using Xunit;

namespace Ikiastrro.Web.Tests;

/// <summary>Astro Facts additions: the Vaiseshikamsa table (Vargas step) and the Moon nakshatra attributes card (About planets).</summary>
public sealed class VaiseshikamsaAndNakshatraAttributesTests : BunitContext
{
    private static readonly string[] AllCharts =
        ["D1", "D2", "D3", "D4", "D7", "D9", "D10", "D12", "D16", "D20", "D24", "D27", "D30", "D40", "D45", "D60"];

    private static readonly string[] Grahas = ["Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn"];

    private static ChartKeyDetail G(string planet, string sign) => new()
        { PointKind = "Graha", Planet = planet, Sign = sign, NirayanaLongitudeDegrees = 15 };

    /// <summary>Every graha in Gemini except the Sun, which is in <paramref name="sunSign"/>.</summary>
    private static LoadedChart Chart(string type, string sunSign) => new(type, type, type, null, "Aries", "Gemini", null,
        Grahas.Select(p => G(p, p == "Sun" ? sunSign : "Gemini")).Append(G("Ascendant", "Aries")).ToList(),
        [], [], [], [], [], null, null, "test");

    [Fact]
    public void Counts_own_or_exalted_for_JHora_and_no_longer_shows_the_Raman_column()
    {
        // Sun owns Leo in D1-D3, is exalted in Aries in D7 and D9, and is in Gemini everywhere else.
        var signs = new Dictionary<string, string> { ["D1"] = "Leo", ["D2"] = "Leo", ["D3"] = "Leo", ["D7"] = "Aries", ["D9"] = "Aries" };
        var charts = AllCharts.ToDictionary(t => t, t => Chart(t, signs.GetValueOrDefault(t, "Gemini")));

        var cut = Render<VaiseshikamsaTable>(p => p.Add(x => x.Charts, charts));

        var sun = cut.FindAll("tbody tr")[0].Normalized();
        Assert.Contains("Sun", sun);
        Assert.DoesNotContain("Uttamamsa", sun);                      // the Raman own-sign ladder is gone
        Assert.DoesNotContain("Raman", cut.Markup);
        Assert.Contains("5 5-Simhasana D1 D2 D3 D7 D9", sun);         // JHora Dasa Varga: own or exalted
        Assert.Contains("5 5-Kanduka D1 D2 D3 D7 D9", sun);           // JHora Shodasa Varga
        Assert.Equal(7, cut.FindAll("tbody tr").Count);
        Assert.Contains("0 —", cut.FindAll("tbody tr")[1].Normalized());   // Moon is in Gemini everywhere: no tier
    }

    [Fact]
    public void Names_the_missing_vargas_instead_of_guessing()
    {
        var charts = new Dictionary<string, LoadedChart> { ["D1"] = Chart("D1", "Leo") };

        var cut = Render<VaiseshikamsaTable>(p => p.Add(x => x.Charts, charts));

        Assert.Empty(cut.FindAll("table"));
        Assert.Contains("missing D2", cut.Markup.Normalized());
    }

    [Fact]
    public void Moon_nakshatra_card_shows_the_matching_attributes()
    {
        var anuradha = new NakshatraReference(17, "Anuradha", 213.33m, 226.67m, 7, 17, "Mitra", "Lotus", "Sattva", "Deva",
            "Deer", "Female", "Pitta", "Brahmanas", "Fire", "East", 8, false);

        var cut = Render<MoonNakshatraAttributes>(p => p.Add(x => x.Nakshatra, anuradha));
        var text = cut.Markup.Normalized();

        Assert.Contains("Moon nakṣatra attributes — Anuradha", text);
        Assert.Contains("Gaṇa Deva", text);
        Assert.Contains("Yoni Deer (female)", text);
        Assert.Contains("Nāḍī Pitta", text);
        Assert.Contains("Varṇa Brahmanas", text);
    }

    [Fact]
    public void Moon_nakshatra_card_is_empty_without_a_nakshatra()
    {
        var cut = Render<MoonNakshatraAttributes>();
        Assert.Empty(cut.FindAll("section"));
    }
}
