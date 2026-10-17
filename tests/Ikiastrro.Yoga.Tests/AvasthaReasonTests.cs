using Ikiastrro.Core.Engines.PlanetaryStates;
using Xunit;

namespace Ikiastrro.Yoga.Tests;

/// <summary>Each avastha state comes with the inputs that produced it, from the same calculation that picks the state
/// (Explain and For share one Evaluate), so a reason can never disagree with the result.</summary>
public class AvasthaReasonTests
{
    private static readonly PlanetaryStateRuleSet Rules = BuildRules();

    private static PlanetaryStateRuleSet BuildRules()
    {
        byte id = 1;
        PlanetaryStateRow Row(string system, string name) => new(id++, system, name, 1, null);
        var dee = new[] { "Deepta", "Swastha", "Mudita", "Saanta", "Deena", "Duhkhita", "Vikala", "Khala", "Kopita" }
            .Select(n => Row("Deeptadi", n)).ToDictionary(r => r.StateName);
        var laj = new[] { "Lajjita", "Garvita", "Kshudhita", "Trishita", "Mudita", "Kshobhita" }
            .Select(n => Row("Lajjitadi", n)).ToDictionary(r => r.StateName);
        byte rid = 1;
        DeeptadiStateRuleRow Rule(string dignity, string state) => new(rid++, 1, dignity, dee[state].Id, state);
        var byDignity = new[]
        {
            Rule("Exalted", "Deepta"), Rule("Moolatrikona", "Swastha"), Rule("Own Sign", "Swastha"),
            Rule("Great Friend", "Mudita"), Rule("Friend", "Saanta"), Rule("Neutral", "Deena"),
            Rule("Enemy", "Duhkhita"), Rule("Great Enemy", "Duhkhita"), Rule("Debilitated", "Duhkhita"),
        }.ToDictionary(r => r.DignityStatus);
        return new PlanetaryStateRuleSet(1, new List<AgeStateRuleRow>(), new Dictionary<string, WakefulnessStateRuleRow>(),
            new Dictionary<byte, PlanetaryStateRow>(), byDignity, dee, laj);
    }

    private static string NameOf(IEnumerable<PlanetaryStateRow> rows, byte id) => rows.Single(r => r.Id == id).StateName;

    [Fact]
    public void Deeptadi_ReasonsNameTheSameStatesAsTheIds_AndSayWhy()
    {
        // Sun exalted in Aries (lord Mars): Deepta by placement, Deena by a neutral compound tie to Mars, Khala (Mars
        // is a malefic), Kopita at 3.8° from itself is not applicable — pass 3.8° to prove the orb reason.
        string[] conj = ["Saturn", "Rahu"];
        var ids = DeeptadiStateCalculator.For("Exalted", "Neutral", "Mars", conj, 3.8m, Rules);
        var reasons = DeeptadiStateCalculator.Explain("Exalted", "Neutral", "Mars", conj, 3.8m, Rules);

        var all = Rules.DeeptadiStatesByName.Values;
        Assert.Equal(ids.Select(i => NameOf(all, i)), reasons.Select(r => r.State));
        Assert.Equal(["Deepta", "Deena", "Vikala", "Khala", "Kopita"], reasons.Select(r => r.State));
        Assert.Contains("Neutral", reasons[1].Reason);
        Assert.Contains("Mars", reasons[1].Reason);
        Assert.Contains("Saturn, Rahu", reasons[2].Reason);
        Assert.Contains("3.8°", reasons[4].Reason);
        Assert.All(reasons, r => Assert.Equal("Deeptadi", r.System));
    }

    [Fact]
    public void Deeptadi_NoStates_NoReasons()
    {
        Assert.Empty(DeeptadiStateCalculator.Explain(null, null, null, [], null, Rules));
    }

    [Fact]
    public void Lajjitadi_ReasonsNameTheSameStatesAsTheIds_AndListTheInvolvedPlanets()
    {
        var ids = LajjitadiStateCalculator.For("Cancer", "Enemy", 5, ["Saturn", "Jupiter"], ["Mars"],
            p => p is "Mars" or "Saturn", p => p == "Mars" ? "Enemy" : p == "Jupiter" ? "Friend" : "Neutral", Rules);
        var reasons = LajjitadiStateCalculator.Explain("Cancer", "Enemy", 5, ["Saturn", "Jupiter"], ["Mars"],
            p => p is "Mars" or "Saturn", p => p == "Mars" ? "Enemy" : p == "Jupiter" ? "Friend" : "Neutral", Rules);

        var all = Rules.LajjitadiStatesByName.Values;
        Assert.Equal(ids.Select(i => NameOf(all, i)), reasons.Select(r => r.State));
        Assert.Equal(["Lajjita", "Kshudhita", "Trishita", "Mudita"], reasons.Select(r => r.State));
        Assert.Contains("5th house", reasons[0].Reason);
        Assert.Contains("Saturn", reasons[0].Reason);
        Assert.Contains("enemy's sign", reasons[1].Reason);
        Assert.Contains("aspected by enemies Mars", reasons[1].Reason);
        Assert.Contains("watery sign (Cancer)", reasons[2].Reason);
        Assert.Contains("joined by Jupiter", reasons[3].Reason);
    }

    [Fact]
    public void Systems_ListAllFive_AndSayWhatNoAvasthaCovers()
    {
        Assert.Equal(["Baaladi", "Jagradadi", "Deeptadi", "Lajjitadi", "Sayanadi"], AvasthaPvrNotes.Systems.Select(s => s.System));
        Assert.All(AvasthaPvrNotes.Systems, s => Assert.NotEmpty(s.Uses));
        Assert.Contains(AvasthaPvrNotes.NotCoveredByAvasthas, x => x.StartsWith("Functional nature"));
        Assert.Contains(AvasthaPvrNotes.NotCoveredByAvasthas, x => x.StartsWith("Argala"));
    }
}
