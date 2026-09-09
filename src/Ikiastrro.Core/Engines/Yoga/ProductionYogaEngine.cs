using Ikiastrro.Core.Pipeline;

namespace Ikiastrro.Core.Engines.Yoga;

public sealed record UnifiedYogaEvaluation(
    ContextualYogaResult Result,
    IReadOnlyList<string> MissingRequirementCodes);

/// <summary>
/// Production composition root for every source-attributed yoga evaluator currently implemented.
/// One source variant is one result; authorities and textual alternatives are never collapsed.
/// </summary>
public sealed class ProductionYogaEngine : IYogaEngine
{
    private readonly VerifiedSourceYogaEngine _verified = new();

    public IReadOnlyList<YogaResult> Detect(ChartBundle bundle) => DetectDetailed(bundle)
        .Select(x => new YogaResult(x.Result.YogaCode, Category(x.Result),
            x.Result.Present == true, false, x.Result.Notes))
        .ToList();

    public IReadOnlyList<UnifiedYogaEvaluation> DetectDetailed(ChartBundle bundle)
    {
        var d1 = bundle.Charts.FirstOrDefault(x => x.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        if (d1 is null) return [];
        var d9 = bundle.Charts.FirstOrDefault(x => x.ChartType.Equals("D9", StringComparison.OrdinalIgnoreCase));

        var rows = new Dictionary<(string Source, string Variant), UnifiedYogaEvaluation>();
        void Add(ContextualYogaResult result, IReadOnlyList<string>? missing = null)
            => rows[(result.SourceRefCode, result.SourceVariantCode)] =
                new(result, missing ?? MissingCodes(result));

        foreach (var x in _verified.Detect(d1))
            Add(new(x.YogaCode, x.Present, "EVALUATED", x.SourceRefCode,
                x.SourceVariantCode, x.SourceLocator, x.Notes));

        foreach (var x in YogaInputEvaluator.Evaluate(bundle.Birth, bundle.Charts, bundle.SunTimes))
            Add(x.Result, x.MissingRequirementCodes);
        foreach (var x in RamanContextYogaEvaluator.Evaluate(d1, d9)) Add(x);
        foreach (var x in MalikaYogaEvaluator.Evaluate(d1))
            Add(new(x.YogaCode, x.Present, "EVALUATED", x.SourceRefCode,
                x.SourceVariantCode, x.SourceLocator, x.DisplayName));
        foreach (var x in RamanYogaBatchFourEvaluator.Evaluate(d1, d9)) Add(x);
        foreach (var x in RamanYogaBatchFiveEvaluator.Evaluate(d1, d9)) Add(x);
        foreach (var x in RamanYogaBatchSixEvaluator.Evaluate(d1, d9,
                     !bundle.SunTimes.IsNightBirth, Phase(bundle)?.IsWaxing, Phase(bundle)?.IsFullMoon)) Add(x);
        foreach (var x in RamanNabhasaBatchEvaluator.Evaluate(d1)) Add(x);
        foreach (var x in RamanNabhasaSecondBatchEvaluator.Evaluate(d1)) Add(x);
        foreach (var x in RamanYogaBatchSevenEvaluator.Evaluate(bundle)) Add(x);
        foreach (var x in RamanDhanaYogaEvaluator.Evaluate(bundle)) Add(x);
        foreach (var x in RamanDaridraYogaEvaluator.Evaluate(d1)) Add(x);
        foreach (var x in RamanYogaBatchEightEvaluator.Evaluate(bundle)) Add(x);
        foreach (var x in RamanFinalHundredCatalog.Entries()) Add(x);

        // Context-aware rows carry stricter missing-input semantics than their batch counterparts.
        foreach (var x in YogaInputEvaluator.Evaluate(bundle.Birth, bundle.Charts, bundle.SunTimes))
            Add(x.Result, x.MissingRequirementCodes);

        return rows.Values
            .OrderBy(x => EntryNumber(x.Result.SourceVariantCode))
            .ThenBy(x => x.Result.SourceRefCode)
            .ThenBy(x => x.Result.SourceVariantCode)
            .ToList();
    }

    private static LunarPhase? Phase(ChartBundle bundle)
    {
        var d1 = bundle.Charts.FirstOrDefault(x => x.ChartType.Equals("D1", StringComparison.OrdinalIgnoreCase));
        double? Longitude(string planet) => d1?.Planets.FirstOrDefault(x => x.Planet == planet)?.NirayanaLongitudeDegrees;
        return LunarPhase.Calculate(Longitude("Sun"), Longitude("Moon"));
    }

    private static IReadOnlyList<string> MissingCodes(ContextualYogaResult result)
    {
        if (result.EvaluationStatus != "NOT_EVALUATED") return [];
        var note = result.Notes ?? string.Empty;
        if (note.Contains("D9", StringComparison.OrdinalIgnoreCase)) return ["CHART_D9"];
        if (note.Contains("Vaiseshikamsa", StringComparison.OrdinalIgnoreCase)) return ["VAISESHIKAMSA"];
        if (note.Contains("strength", StringComparison.OrdinalIgnoreCase)) return ["YOGA_STRENGTH_POLICY"];
        if (EntryNumber(result.SourceVariantCode) >= 201) return ["PREDICATE_NOT_IMPLEMENTED"];
        return ["REQUIRED_CALCULATION_NOT_IMPLEMENTED"];
    }

    private static int EntryNumber(string variant)
    {
        if (!variant.StartsWith("RAMAN_300_", StringComparison.Ordinal)) return int.MaxValue;
        var digits = new string(variant[10..].TakeWhile(char.IsDigit).ToArray());
        return int.TryParse(digits, out var number) ? number : int.MaxValue;
    }

    private static string Category(ContextualYogaResult result)
        => result.SourceRefCode == "SRC_PVR_INTEGRATED" ? "PVR" : "Raman";
}
