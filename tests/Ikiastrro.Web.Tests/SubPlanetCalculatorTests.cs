using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Karakas;
using Xunit;

namespace Ikiastrro.Web.Tests;

public class SubPlanetCalculatorTests
{
    // Independent fixture transcribed from SRC_PVR_INTEGRATED Tables 9/10 and section 4.3.
    private static SubPlanetRuleSet Rules()
    {
        string?[] cycle = ["Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn", null];
        int[] nightStarts = [4, 5, 6, 0, 1, 2, 3];
        var parts = new List<SubPlanetPartRule>();
        for (var day = 0; day < 7; day++)
        for (var part = 0; part < 8; part++)
        {
            parts.Add(new("DAY", day, part + 1, cycle[(day + part) % 8]));
            parts.Add(new("NIGHT", day, part + 1, cycle[(nightStarts[day] + part) % 8]));
        }
        return new(1,
            [new("Dhuma", 1, "ADD", null, 133.333333),
             new("Vyatipata", 2, "COMPLEMENT_360", "Dhuma", null),
             new("Parivesha", 3, "ADD", "Vyatipata", 180),
             new("Indrachapa", 4, "COMPLEMENT_360", "Parivesha", null),
             new("Upaketu", 5, "ADD", "Indrachapa", 16.666667)],
            [new("Kaala", "Sun", .5, 8), new("Mrityu", "Mars", .5, 8),
             new("Ardhaprahara", "Mercury", .5, 8), new("Yamaghantaka", "Jupiter", .5, 8),
             new("Gulika", "Saturn", .5, 8), new("Maandi", "Saturn", 0, 8)], parts);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(30)]
    [InlineData(226.666667)]
    [InlineData(359.999999)]
    public void SunChainWrapsAndPreservesIdentities(double longitude)
    {
        var start = new DateTimeOffset(2026, 9, 6, 6, 0, 0, TimeSpan.Zero);
        var result = SubPlanetCalculator.Compute(new(start, start.AddHours(12), start.AddDays(1), false),
            longitude, Rules(), _ => 25).ToDictionary(p => p.Code, p => p.NirayanaLongitudeDegrees);
        Assert.Equal(11, result.Count);
        Assert.All(result.Values, value => Assert.True(value >= 0 && value < 360));
        Assert.Equal(longitude, (result["Upaketu"] + 30) % 360, 6);
        Assert.True(Math.Abs((result["Dhuma"] + result["Vyatipata"]) % 360) < 0.000001);
        Assert.Equal(180, Math.Abs(result["Parivesha"] - result["Vyatipata"]), 6);
    }

    [Fact]
    public void EveryWeekdayAndArcUsesTheCorrectSaturnPartAndFraction()
    {
        int[] daySaturn = [6, 5, 4, 3, 2, 1, 0];
        int[] nightSaturn = [2, 1, 0, 6, 5, 4, 3];
        for (var weekday = 0; weekday < 7; weekday++)
        foreach (var night in new[] { false, true })
        {
            var sunrise = new DateTimeOffset(2026, 9, 6, 6, 0, 0, TimeSpan.FromHours(5.5)).AddDays(weekday);
            var sun = new SunTimes(sunrise, sunrise.AddHours(10), sunrise.AddDays(1), night);
            var start = night ? sun.Sunset : sun.Sunrise;
            var duration = night ? 14d : 10d;
            var result = SubPlanetCalculator.Compute(sun, 10, Rules(), t => (t - start).TotalHours)
                .ToDictionary(p => p.Code, p => p.NirayanaLongitudeDegrees);
            var index = night ? nightSaturn[weekday] : daySaturn[weekday];
            Assert.Equal(duration * index / 8, result["Maandi"], 8);
            Assert.Equal(duration * (index + .5) / 8, result["Gulika"], 8);
        }
    }

    [Fact]
    public void RejectsIncompleteRulesBeforeCallingAstronomy()
    {
        var start = DateTimeOffset.UtcNow;
        var bad = Rules() with { Parts = Rules().Parts.Skip(1).ToArray() };
        Assert.Throws<InvalidOperationException>(() => SubPlanetCalculator.Compute(
            new(start, start.AddHours(12), start.AddDays(1), false), 1, bad,
            _ => throw new Exception("Astronomy must not run")));
    }

    [Fact]
    public void RejectsBrokenChainAndNonFiniteResults()
    {
        var start = DateTimeOffset.UtcNow;
        var sun = new SunTimes(start, start.AddHours(12), start.AddDays(1), false);
        var rules = Rules();
        var bad = rules with { SunRules = rules.SunRules.Select(r => r.Code == "Dhuma" ? r with { InputCode = "missing" } : r).ToArray() };
        Assert.Throws<InvalidOperationException>(() => SubPlanetCalculator.Compute(sun, 1, bad, _ => 1));
        Assert.Throws<InvalidOperationException>(() => SubPlanetCalculator.Compute(sun, 1, rules, _ => double.NaN));
    }
}
