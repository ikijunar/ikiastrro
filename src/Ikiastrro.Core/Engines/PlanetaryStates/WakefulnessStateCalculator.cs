namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>
/// Jagradadi Avastha — the "waking state" of a planet, from its dignity in the sign it occupies:
/// Jagrat (awake — own sign / exaltation / moolatrikona), Swapna (dreaming — friend / neutral
/// sign), Sushupti (sleeping — enemy sign / debilitation). Works for any chart type, since it
/// only needs DignityStatus (which ChartAnalyzer already computes for every chart type).
///
/// Data-driven: the DignityStatus -> state map comes from tbl_Rule_WakefulnessState (a given
/// RuleSetId), never hardcoded here.
/// </summary>
public static class WakefulnessStateCalculator
{
    /// <summary>The wakefulness rule row for <paramref name="dignityStatus"/>, or null if the
    /// dignity value is absent or not mapped (e.g. Ascendant, which is excluded upstream anyway).</summary>
    public static WakefulnessStateRuleRow? For(string? dignityStatus, IReadOnlyDictionary<string, WakefulnessStateRuleRow> mapByDignity)
    {
        if (string.IsNullOrWhiteSpace(dignityStatus))
            return null;
        return mapByDignity.GetValueOrDefault(dignityStatus);
    }
}
