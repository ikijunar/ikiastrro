using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Yoga;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Web.Tests;

public sealed class MahabhagyaYogaEvaluatorTests
{
    [Fact]
    public void Male_Day_All_Odd_Is_Present()
        => Assert.True(Evaluate(YogaSubjectSex.Male, false, ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Libra).Present);

    [Fact]
    public void Female_Night_All_Even_Is_Present()
        => Assert.True(Evaluate(YogaSubjectSex.Female, true, ZodiacName.Taurus, ZodiacName.Cancer, ZodiacName.Pisces).Present);

    [Theory]
    [InlineData(YogaSubjectSex.Male, true)]
    [InlineData(YogaSubjectSex.Female, false)]
    public void Opposite_Day_Night_Branch_Is_Not_Present(YogaSubjectSex sex, bool isNight)
        => Assert.False(Evaluate(sex, isNight, ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Libra).Present);

    [Fact]
    public void Unspecified_Sex_Is_Unevaluated_Not_Absent()
    {
        var result = Evaluate(YogaSubjectSex.Unspecified, false, ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Libra);
        Assert.Null(result.Present);
        Assert.Equal("NOT_EVALUATED", result.EvaluationStatus);
        Assert.Contains("no inference", result.Notes);
    }

    [Fact]
    public void Mixed_Parity_Is_Not_Present()
        => Assert.False(Evaluate(YogaSubjectSex.Male, false, ZodiacName.Aries, ZodiacName.Leo, ZodiacName.Virgo).Present);

    private static MahabhagyaEvaluation Evaluate(YogaSubjectSex sex, bool night,
        ZodiacName ascendant, ZodiacName sun, ZodiacName moon)
    {
        var chart = new ChartAnalysisInput("D1", ascendant, new List<PlanetPosition>
        {
            new() { Planet = "Sun", Sign = sun.ToString(), HouseNumber = 1 },
            new() { Planet = "Moon", Sign = moon.ToString(), HouseNumber = 1 }
        });
        return MahabhagyaYogaEvaluator.Evaluate(chart, night, sex);
    }
}
