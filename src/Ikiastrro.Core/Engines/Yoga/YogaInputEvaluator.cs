using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public sealed record LunarPhase(double ElongationDegrees, bool IsWaxing, bool IsFullMoon,
    string PolicyCode = "PURNIMA_TITHI_168_INCLUSIVE_180_EXCLUSIVE_V1")
{
    // Explicit operational interpretation: full Moon means the Purnima tithi.
    // This policy is not a claim that Raman specifies a numerical orb.
    public static LunarPhase? Calculate(double? sun, double? moon)
    {
        if (sun is null || moon is null || !double.IsFinite(sun.Value) || !double.IsFinite(moon.Value))
            return null;
        var angle = ((moon.Value - sun.Value) % 360 + 360) % 360;
        return new(angle, angle > 0 && angle < 180, angle >= 168 && angle < 180);
    }
}

public sealed record YogaInputEvaluation(
    ContextualYogaResult Result, IReadOnlyList<string> MissingRequirementCodes,
    LunarPhase? LunarPhase, bool? IsNightBirth, string? Sex,
    string SunriseMethodCode = "SWISSEPH_DISC_CENTER_NO_REFRACTION");

/// <summary>Connects the seven migration-51 inputs to their five source variants.</summary>
public static class YogaInputEvaluator
{
    public static IReadOnlyList<YogaInputEvaluation> Evaluate(
        BirthDetails birth, IReadOnlyList<ChartAnalysisInput> charts, SunTimes? sunTimes)
    {
        var d1 = charts.FirstOrDefault(c => c.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        var d9 = charts.FirstOrDefault(c => c.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));
        double? Longitude(string planet) => d1?.Planets.FirstOrDefault(p => p.Planet == planet)?.NirayanaLongitudeDegrees;
        var phase = LunarPhase.Calculate(Longitude("Sun"), Longitude("Moon"));
        bool? night = sunTimes is not null && sunTimes.Sunrise < sunTimes.Sunset
            && sunTimes.Sunset < sunTimes.NextSunrise ? sunTimes.IsNightBirth : null;
        var sex = birth.Sex switch { "Male" => YogaSubjectSex.Male, "Female" => YogaSubjectSex.Female, _ => YogaSubjectSex.Unspecified };
        var results = new List<YogaInputEvaluation>();
        foreach (var (number, code, page) in new[] {
            (25, "YOGA_MAHABHAGYA", 67), (58, "YOGA_JAYA", 97),
            (59, "YOGA_VIDYUT", 99), (66, "YOGA_GARUDA", 109), (68, "YOGA_GOLA", 112) })
        {
            var missing = new List<string>();
            if (d1 is null) missing.Add("CHART_D1");
            if (number is 66 or 68 && d9 is null) missing.Add("CHART_D9");
            if (number is 25 or 66 && night is null) missing.Add("BIRTH_DAY_NIGHT");
            if (number == 25 && sex == YogaSubjectSex.Unspecified) missing.Add("SUBJECT_SEX");
            if (number == 66 && phase is null) missing.Add("MOON_WAXING");
            if (number == 68 && phase is null) missing.Add("MOON_FULL");
            if (d1 is not null)
            {
                // All grahas used by these compound predicates must be supplied.
                foreach (var planet in new[] { "Sun", "Moon", "Mars", "Mercury", "Jupiter", "Venus", "Saturn" })
                    if (!d1.Planets.Any(p => p.Planet == planet)) missing.Add("PLANET_" + planet.ToUpperInvariant());
                if (number is 58 or 59 && d1.Planets.Any(p =>
                    p.Planet is "Sun" or "Moon" or "Mars" or "Mercury" or "Jupiter" or "Venus" or "Saturn"
                    && (p.NirayanaLongitudeDegrees is null || !double.IsFinite(p.NirayanaLongitudeDegrees.Value))))
                    missing.Add("EXACT_LONGITUDE");
            }
            if (number is 66 or 68 && d9 is not null)
            {
                var planet = number == 66 ? "Moon" : "Mercury";
                if (!d9.Planets.Any(p => p.Planet == planet)) missing.Add("D9_" + planet.ToUpperInvariant());
            }
            ContextualYogaResult row;
            if (missing.Count > 0)
                row = new(code, null, "NOT_EVALUATED", "SRC_RAMAN_300_COMBINATIONS",
                    $"RAMAN_300_{number:000}", $"combination {number}; printed p.{page - 12}; scan p.{page}",
                    "Missing inputs: " + string.Join(", ", missing));
            else if (number == 25)
            {
                var r = MahabhagyaYogaEvaluator.Evaluate(d1!, night!.Value, sex);
                row = new(code, r.Present, r.EvaluationStatus, r.SourceRefCode, r.SourceVariantCode, r.SourceLocator, r.Notes);
            }
            else if (number is 58 or 59)
                row = RamanYogaBatchFiveEvaluator.Evaluate(d1!, d9).Single(r => r.SourceVariantCode == $"RAMAN_300_{number:000}");
            else
                row = RamanYogaBatchSixEvaluator.Evaluate(d1!, d9, night is null ? null : !night,
                    phase?.IsWaxing, phase?.IsFullMoon).Single(r => r.SourceVariantCode == $"RAMAN_300_{number:000}");
            results.Add(new(row, missing, phase, night, birth.Sex));
        }
        return results;
    }
}
