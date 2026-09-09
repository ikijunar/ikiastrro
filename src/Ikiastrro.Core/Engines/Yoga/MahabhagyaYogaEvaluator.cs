using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public enum YogaSubjectSex
{
    Unspecified,
    Male,
    Female
}

public sealed record MahabhagyaEvaluation(
    bool? Present,
    string EvaluationStatus,
    string SourceRefCode,
    string SourceVariantCode,
    string SourceLocator,
    string Notes);

/// <summary>
/// Raman combination 25 exactly as stated: male + day + three odd signs,
/// or female + night + three even signs. No sex-neutralized extension.
/// </summary>
public static class MahabhagyaYogaEvaluator
{
    public static MahabhagyaEvaluation Evaluate(
        ChartAnalysisInput chart,
        bool isNightBirth,
        YogaSubjectSex subjectSex)
    {
        const string note = "Raman requires male/day with Sun, Moon and Lagna in odd signs, or female/night with all three in even signs.";
        if (subjectSex == YogaSubjectSex.Unspecified)
            return Result(null, "NOT_EVALUATED", note + " Subject sex was not supplied; no inference was made.");

        var sun = chart.Planets.SingleOrDefault(x => x.Planet == PlanetName.Sun.ToString());
        var moon = chart.Planets.SingleOrDefault(x => x.Planet == PlanetName.Moon.ToString());
        if (sun is null || moon is null)
            return Result(null, "NOT_EVALUATED", note + " Sun or Moon placement was unavailable.");

        var signs = new[] { chart.AscendantSign, Enum.Parse<ZodiacName>(sun.Sign), Enum.Parse<ZodiacName>(moon.Sign) };
        var allOdd = signs.All(IsOdd);
        var allEven = signs.All(x => !IsOdd(x));
        var present = subjectSex switch
        {
            YogaSubjectSex.Male => !isNightBirth && allOdd,
            YogaSubjectSex.Female => isNightBirth && allEven,
            _ => false
        };
        return Result(present, "EVALUATED", note);
    }

    private static bool IsOdd(ZodiacName sign) => ((int)sign + 1) % 2 == 1;

    private static MahabhagyaEvaluation Result(bool? present, string status, string notes)
        => new(present, status, "SRC_RAMAN_300_COMBINATIONS", "RAMAN_300_025",
            "combination 25; printed p.55; scan p.67", notes);
}
