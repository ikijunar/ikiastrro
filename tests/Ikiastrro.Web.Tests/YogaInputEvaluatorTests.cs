using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class YogaInputEvaluatorTests
{
    [Theory]
    [InlineData(0, 0, false, false)]
    [InlineData(350, 10, true, false)]
    [InlineData(0, 167.999, true, false)]
    [InlineData(0, 168, true, true)]
    [InlineData(0, 179.999, true, true)]
    [InlineData(0, 180, false, false)]
    [InlineData(0, 359, false, false)]
    public void PhaseBoundaries(double sun, double moon, bool waxing, bool full)
    {
        var phase = LunarPhase.Calculate(sun, moon)!;
        Assert.Equal(waxing, phase.IsWaxing);
        Assert.Equal(full, phase.IsFullMoon);
    }

    [Fact]
    public void InvalidPhaseIsMissing()
    {
        Assert.Null(LunarPhase.Calculate(null, 180));
        Assert.Null(LunarPhase.Calculate(double.NaN, 180));
    }

    [Fact]
    public void MissingInputsAreReportedTogether()
    {
        var rows = YogaInputEvaluator.Evaluate(new(), [], null);
        Assert.Equal(5, rows.Count);
        var garuda = rows.Single(x => x.Result.SourceVariantCode == "RAMAN_300_066");
        Assert.Equal(new[] { "CHART_D1", "CHART_D9", "BIRTH_DAY_NIGHT", "MOON_WAXING" }, garuda.MissingRequirementCodes);
        Assert.All(rows, r => { Assert.Null(r.Result.Present); Assert.Equal("NOT_EVALUATED", r.Result.EvaluationStatus); });
    }

    [Theory]
    [InlineData("Male", false, ZodiacName.Aries, true)]
    [InlineData("Female", true, ZodiacName.Taurus, true)]
    [InlineData("Male", true, ZodiacName.Aries, false)]
    [InlineData("Female", false, ZodiacName.Taurus, false)]
    public void SexAndDayNightReachMahabhagya(string sex, bool night, ZodiacName sign, bool present)
    {
        var row = YogaInputEvaluator.Evaluate(new() { Sex = sex }, [Chart("D1", sign)], Times(night))[0];
        Assert.Equal(present, row.Result.Present);
        Assert.Empty(row.MissingRequirementCodes);
    }

    [Fact]
    public void UnspecifiedSexDoesNotDefaultToMale()
    {
        var row = YogaInputEvaluator.Evaluate(new(), [Chart("D1", ZodiacName.Aries)], Times(false))[0];
        Assert.Null(row.Result.Present);
        Assert.Contains("SUBJECT_SEX", row.MissingRequirementCodes);
    }

    [Fact]
    public void CompleteInputsEvaluateAllFiveVariants()
    {
        var rows = YogaInputEvaluator.Evaluate(new() { Sex = "Male" },
            [Chart("D1", ZodiacName.Aries), Chart("D9", ZodiacName.Aries)], Times(false));
        Assert.All(rows, r => { Assert.Equal("EVALUATED", r.Result.EvaluationStatus); Assert.Empty(r.MissingRequirementCodes); });
    }

    [Fact]
    public void MissingLongitudeGatesBothExactDegreeVariants()
    {
        var chart = Chart("D1", ZodiacName.Aries);
        chart.Planets.First().NirayanaLongitudeDegrees = null;
        var rows = YogaInputEvaluator.Evaluate(new() { Sex = "Male" }, [chart], Times(false));
        foreach (var r in rows.Where(r => r.Result.SourceVariantCode is "RAMAN_300_058" or "RAMAN_300_059"))
        {
            Assert.Null(r.Result.Present);
            Assert.Contains("EXACT_LONGITUDE", r.MissingRequirementCodes);
        }
    }

    private static ChartAnalysisInput Chart(string type, ZodiacName sign) => new(type, sign,
        new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" }.Select(p =>
            new PlanetPosition { Planet = p, Sign = sign.ToString(), HouseNumber = 1,
                NirayanaLongitudeDegrees = (int)sign * 30 + 10 }).ToList());
    private static SunTimes Times(bool night) => new(
        new DateTimeOffset(2026, 9, 9, 6, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 9, 18, 0, 0, TimeSpan.Zero),
        new DateTimeOffset(2026, 9, 10, 6, 0, 0, TimeSpan.Zero), night);
}
