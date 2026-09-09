using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Strength;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;
using Xunit;

namespace Ikiastrro.Web.Tests;

public class ShadbalaCalculatorTests
{
    [Fact]
    public void CalculatesAllSevenClassicalPlanetsAndSixFamilies()
    {
        var chart = Chart("D1", new[]
        {
            Planet("Sun", "Aries", 10, 1), Planet("Moon", "Taurus", 3, 4),
            Planet("Mars", "Capricornus", 28, 10), Planet("Mercury", "Virgo", 15, 1),
            Planet("Jupiter", "Cancer", 5, 1), Planet("Venus", "Pisces", 27, 4),
            Planet("Saturn", "Libra", 20, 7)
        });
        var positions = Positions();
        var sun = new SunTimes(DateTimeOffset.UtcNow.AddHours(-6), DateTimeOffset.UtcNow.AddHours(6), DateTimeOffset.UtcNow.AddDays(1), false);

        var results = ShadbalaCalculator.Calculate(new[] { chart }, positions, sun);

        Assert.Equal(7, results.Count);
        Assert.All(results, result =>
        {
            Assert.True(result.ShadbalaVirupas > 0);
            Assert.True(result.ShadbalaRupas > 0);
            Assert.Equal(6, result.Components.Select(c => c.BalaCode).Distinct().Count());
        });
        Assert.Equal(60, results.Single(r => r.Planet == "Sun").Components.Single(c => c.SubComponentCode == "UCHCHA_BALA").ValueVirupas, 3);
    }

    [Fact]
    public void ExcludesNodesFromProductionShadbala()
    {
        var chart = Chart("D1", new[]
        {
            Planet("Sun", "Aries", 10, 1), Planet("Rahu", "Cancer", 2, 4), Planet("Ketu", "Capricornus", 2, 10)
        });
        var results = ShadbalaCalculator.Calculate(new[] { chart }, Positions(),
            new SunTimes(DateTimeOffset.UtcNow.AddHours(-6), DateTimeOffset.UtcNow.AddHours(6), DateTimeOffset.UtcNow.AddDays(1), false));

        Assert.DoesNotContain(results, r => r.Planet is "Rahu" or "Ketu");
    }

    [Theory]
    [InlineData(PlanetName.Moon, ZodiacName.Taurus, 2, "EXALTED", 4)]
    [InlineData(PlanetName.Moon, ZodiacName.Taurus, 3, "MOOLATRIKONA", 3)]
    [InlineData(PlanetName.Mercury, ZodiacName.Virgo, 14, "EXALTED", 4)]
    [InlineData(PlanetName.Mercury, ZodiacName.Virgo, 17, "MOOLATRIKONA", 3)]
    [InlineData(PlanetName.Mars, ZodiacName.Capricornus, 20, "EXALTED", 4)]
    [InlineData(PlanetName.Jupiter, ZodiacName.Cancer, 5, "EXALTED", 4)]
    [InlineData(PlanetName.Venus, ZodiacName.Pisces, 27, "EXALTED", 4)]
    [InlineData(PlanetName.Saturn, ZodiacName.Libra, 20, "EXALTED", 4)]
    [InlineData(PlanetName.Rahu, ZodiacName.Gemini, 12, "EXALTED", 4)]
    [InlineData(PlanetName.Ketu, ZodiacName.Sagittarius, 12, "EXALTED", 4)]
    public void UsesPvrDignitySegmentsAndScores(PlanetName planet, ZodiacName sign, double degree,
        string expectedCode, int expectedScore)
    {
        var result = PvrDignityEvaluator.Evaluate(planet, sign, degree,
            new Dictionary<string, ZodiacName> { [planet.ToString()] = sign });

        Assert.Equal(expectedCode, result.DignityTypeCode);
        Assert.Equal(expectedScore, result.DignityScore);
    }

    [Theory]
    [InlineData("MOOLATRIKONA", null, 45.0)]
    [InlineData("OWN", null, 30.0)]
    [InlineData("NEUTRAL", "ADHIMITRA", 22.5)]
    [InlineData("NEUTRAL", "MITRA", 15.0)]
    [InlineData("NEUTRAL", "SAMA", 7.5)]
    [InlineData("NEUTRAL", "SHATRU", 3.75)]
    [InlineData("NEUTRAL", "ADHISHATRU", 1.875)]
    public void UsesRamanSaptavargajaPointLadder(string dignity, string? relationship, double points)
    {
        Assert.Equal(points, PvrDignityEvaluator.SaptavargajaPoints(dignity, relationship), 3);
    }

    [Fact]
    public void CalculatesTwelveBhavaBalaRowsWithThreeComponents()
    {
        var chart = Chart("D1", new[]
        {
            Planet("Sun", "Aries", 10, 1), Planet("Moon", "Taurus", 3, 2),
            Planet("Mars", "Capricornus", 28, 10), Planet("Mercury", "Virgo", 15, 6),
            Planet("Jupiter", "Cancer", 5, 4), Planet("Venus", "Pisces", 27, 12),
            Planet("Saturn", "Libra", 20, 7)
        });
        var strengths = ShadbalaCalculator.Calculate(new[] { chart }, Positions(),
            new SunTimes(DateTimeOffset.UtcNow.AddHours(-6), DateTimeOffset.UtcNow.AddHours(6), DateTimeOffset.UtcNow.AddDays(1), false));

        var results = BhavaBalaCalculator.Calculate(chart, strengths);

        Assert.Equal(12, results.Count);
        Assert.All(results, house => Assert.Equal(3, house.Components.Count));
        Assert.Equal(30, results[0].Components.Single(c => c.ComponentCode == "BHAVA_DIG_BALA").ValueVirupas);
        Assert.Equal(results[0].Components.Sum(c => c.ValueVirupas), results[0].BhavaBalaVirupas, 3);
        Assert.Equal(results[0].BhavaBalaVirupas / 60.0, results[0].BhavaBalaRupas, 3);
    }

    [Fact]
    public void DetectsVargottamaFromD1AndD9Signs()
    {
        var d1 = Chart("D1", new[] { Planet("Sun", "Aries", 10, 1), Planet("Moon", "Taurus", 3, 2) });
        var d9 = Chart("D9", new[] { Planet("Sun", "Aries", 1, 1), Planet("Moon", "Scorpio", 2, 8) });

        var results = VargottamaDetector.Calculate(new[] { d1, d9 });

        Assert.True(results.Single(x => x.Planet == "Sun").IsVargottama);
        Assert.False(results.Single(x => x.Planet == "Moon").IsVargottama);
        Assert.Equal("Aries", results.Single(x => x.Planet == "Sun").D1Sign);
    }

    private static ChartAnalysisInput Chart(string type, IEnumerable<PlanetPosition> planets) =>
        new(type, ZodiacName.Aries, planets.ToList());

    private static PlanetPosition Planet(string name, string sign, double degree, int house) => new()
    {
        Planet = name,
        Sign = sign,
        HouseNumber = house,
        NirayanaLongitudeDegrees = ((int)Enum.Parse<ZodiacName>(sign) * 30) + degree,
        IsRetrograde = false
    };

    private static SiderealPositions Positions()
    {
        var longs = PlanetNames.All9.ToDictionary(p => p, _ => 10d);
        var lats = PlanetNames.All9.ToDictionary(p => p, _ => 0d);
        var speeds = PlanetNames.All9.ToDictionary(p => p, _ => 1d);
        longs[PlanetName.Sun] = 10;
        longs[PlanetName.Moon] = 40;
        return new SiderealPositions(0, longs, lats, speeds, 24, 12);
    }
}
