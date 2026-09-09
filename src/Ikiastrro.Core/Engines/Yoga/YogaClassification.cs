namespace Ikiastrro.Core.Engines.Yoga;

public sealed record YogaClassification(
    string FormationFamilyCode,
    string OutcomeNatureCode,
    string SourceStrengthClassCode,
    string SourceCategoryCode,
    string SourceCorpusCode);

public sealed record ClassifiedYogaResult(
    SourceAttributedYogaResult Detection,
    YogaClassification Classification,
    string EvaluatedStrengthState);

/// <summary>
/// Static, source-aware taxonomy. Per-chart strength is deliberately not stored
/// here; <see cref="ClassifiedSourceYogaEngine"/> derives its state from a detection.
/// </summary>
public static class YogaClassificationCatalog
{
    public static YogaClassification For(SourceAttributedYogaResult result)
    {
        var family = result.YogaCode switch
        {
            "YOGA_VESI" or "YOGA_VASI" or "YOGA_UBHAYACHARI" or "YOGA_BUDHA_ADITYA" => "RAVI",
            "YOGA_SUNAPHA" or "YOGA_ANAPHA" or "YOGA_DURADHARA" or "YOGA_KEMADRUMA"
                or "YOGA_CHANDRA_MANGALA" or "YOGA_ADHI" or "YOGA_GAJAKESARI"
                or "YOGA_SAKATA_LUNAR" => "CHANDRA",
            "YOGA_HAMSA" or "YOGA_MALAVYA" or "YOGA_SASA" or "YOGA_RUCHAKA" or "YOGA_BHADRA" => "MAHAPURUSHA",
            "YOGA_VASUMATHI" => "DHANA",
            "YOGA_RAJALAKSHANA" => "RAJA",
            "YOGA_CHATUSSAGARA" => "KENDRA_DISTRIBUTION",
            "YOGA_AMALA" or "YOGA_PARVATA" => "OTHER_POPULAR",
            "YOGA_VANCHANA_CHORA_BHEETHI" => "ARISHTA",
            "YOGA_KAHALA" => "RAJA",
            _ => "OTHER"
        };

        var nature = result.YogaCode switch
        {
            "YOGA_KEMADRUMA" or "YOGA_SAKATA_LUNAR" => "INAUSPICIOUS",
            "YOGA_CHANDRA_MANGALA" or "YOGA_SASA" or "YOGA_KAHALA" => "MIXED",
            "YOGA_VANCHANA_CHORA_BHEETHI" => "INAUSPICIOUS",
            _ => "AUSPICIOUS"
        };

        var sourceStrength = result.YogaCode switch
        {
            "YOGA_ADHI" or "YOGA_HAMSA" or "YOGA_MALAVYA" or "YOGA_SASA"
                or "YOGA_RUCHAKA" or "YOGA_BHADRA" => "MAJOR",
            _ => "SOURCE_UNSPECIFIED"
        };

        var sourceCategory = result.SourceRefCode == "SRC_PVR_INTEGRATED"
            ? family switch
            {
                "RAVI" => "PVR_RAVI_YOGAS",
                "CHANDRA" => "PVR_CHANDRA_YOGAS",
                "MAHAPURUSHA" => "PVR_PANCHA_MAHAPURUSHA",
                _ => "PVR_OTHER_POPULAR"
            }
            : result.SourceRefCode == "SRC_RAMAN_300_COMBINATIONS"
                ? $"RAMAN_COMBINATION_{EntryNumber(result.SourceVariantCode):000}"
                : "OTHER_SOURCE";

        return new(family, nature, sourceStrength, sourceCategory,
            YogaSourceCorpusCatalog.For(result.SourceRefCode, result.SourceVariantCode));
    }

    private static int EntryNumber(string variant)
        => variant.StartsWith("RAMAN_300_") && int.TryParse(variant[10..], out var number) ? number : 0;
}

public static class YogaSourceCorpusCatalog
{
    public const string Bvr300 = "BVR-300";
    public const string PvrSpecific = "PVR-SPECIFIC";
    public const string Others = "OTHERS";

    public static string For(string sourceRefCode, string sourceVariantCode)
    {
        if (sourceRefCode == "SRC_RAMAN_300_COMBINATIONS" || sourceVariantCode.StartsWith("RAMAN_300_"))
            return Bvr300;
        if (sourceRefCode == "SRC_PVR_INTEGRATED" || sourceVariantCode.StartsWith("PVR_"))
            return PvrSpecific;
        return Others;
    }
}

public sealed class ClassifiedSourceYogaEngine
{
    private readonly VerifiedSourceYogaEngine _engine = new();

    public IReadOnlyList<ClassifiedYogaResult> Detect(Ikiastrro.Core.Pipeline.ChartAnalysisInput chart)
        => _engine.Detect(chart).Select(result => new ClassifiedYogaResult(
            result,
            YogaClassificationCatalog.For(result),
            StrengthState(result))).ToList();

    private static string StrengthState(SourceAttributedYogaResult result)
    {
        if (!result.Present) return "NOT_PRESENT";
        if (result.Cancelled) return "CANCELLED";
        if (result.Notes?.Contains("reduces benefic power", StringComparison.OrdinalIgnoreCase) == true)
            return "REDUCED";
        return result.SourceStrengthClassCode() == "SOURCE_UNSPECIFIED" ? "FORMATION_CONFIRMED" : "QUALIFIED";
    }
}

internal static class YogaClassificationExtensions
{
    public static string SourceStrengthClassCode(this SourceAttributedYogaResult result)
        => YogaClassificationCatalog.For(result).SourceStrengthClassCode;
}
