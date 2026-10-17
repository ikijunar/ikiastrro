using Ikiastrro.Core.Engines.Astronomy;

namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>
/// The unnamed 6-state avastha group PVR gives right after Dīptādi (sec 15.4.3, "6 additional states
/// ... related to attitude and mood") — conventionally called Lajjitādi elsewhere in the jyotish
/// literature, though PVR itself doesn't name the group. All 6 conditions are independent (no shared
/// axis like Dīptādi's dignity tiers) and the book gives no precedence, so every match is kept —
/// several can hold for one planet at once (e.g. Garvita and Mudita together).
///
///   - Lajjita:   in the 5th house (from Lagna) and conjunct Sun/Mars/Saturn/Rahu/Ketu.
///   - Garvita:   exaltation or moolatrikona sign.
///   - Kshudhita: enemy's sign, or conjunct/aspected by an enemy planet, or conjunct Saturn.
///   - Trishita:  a watery sign (Cancer/Scorpio/Pisces — PVR sec 2.2, not the differently-sourced
///                6-sign set RamanYogaBatchSevenEvaluator uses for an unrelated yoga), aspected by an
///                enemy planet, with no benefic aspect.
///   - Mudita:    friend's sign, or conjunct/aspected by a friend planet, or conjunct Jupiter. Same
///                name as one of Dīptādi's 6 states — the book reuses it; tbl_Dim_PlanetaryState's
///                UNIQUE(AvasthaSystem, StateName) already allows this.
///   - Kshobhita: conjunct the Sun and aspected by a malefic or enemy planet.
///
/// "Friend"/"enemy" of an associated (conjunct/aspecting) planet is the directional Panchadha Maitri
/// from this planet's own perspective — DignityEngine.EvaluatePairRelationship, not DignityStatus
/// (which is this planet's relationship to its own sign lord, a different question).
/// </summary>
public static class LajjitadiStateCalculator
{
    private static readonly HashSet<ZodiacName> WaterySigns = new() { ZodiacName.Cancer, ZodiacName.Scorpio, ZodiacName.Pisces };

    private static bool IsEnemyTier(string? tier) => tier is "Enemy" or "Great Enemy";
    private static bool IsFriendTier(string? tier) => tier is "Friend" or "Great Friend";

    public static List<byte> For(
        string sign,
        string? dignityStatus,
        int houseNumberFromLagna,
        IReadOnlyList<string> conjunctPlanets,
        IReadOnlyList<string> aspectingPlanets,
        Func<string, bool> isNaturalMalefic,
        Func<string, string?> relationshipOf,
        PlanetaryStateRuleSet rules) =>
        Evaluate(sign, dignityStatus, houseNumberFromLagna, conjunctPlanets, aspectingPlanets, isNaturalMalefic, relationshipOf, rules)
            .Select(m => m.Id).ToList();

    /// <summary>The same states as <see cref="For"/>, each with the inputs that produced it.</summary>
    public static List<StateReason> Explain(
        string sign,
        string? dignityStatus,
        int houseNumberFromLagna,
        IReadOnlyList<string> conjunctPlanets,
        IReadOnlyList<string> aspectingPlanets,
        Func<string, bool> isNaturalMalefic,
        Func<string, string?> relationshipOf,
        PlanetaryStateRuleSet rules) =>
        Evaluate(sign, dignityStatus, houseNumberFromLagna, conjunctPlanets, aspectingPlanets, isNaturalMalefic, relationshipOf, rules)
            .Select(m => m.Reason).ToList();

    private static List<(byte Id, StateReason Reason)> Evaluate(
        string sign,
        string? dignityStatus,
        int houseNumberFromLagna,
        IReadOnlyList<string> conjunctPlanets,
        IReadOnlyList<string> aspectingPlanets,
        Func<string, bool> isNaturalMalefic,
        Func<string, string?> relationshipOf,
        PlanetaryStateRuleSet rules)
    {
        const string system = "Lajjitadi";
        var found = new List<(byte Id, StateReason Reason)>();
        var states = rules.LajjitadiStatesByName;
        void Add(string state, string reason) => found.Add((states[state].Id, new StateReason(system, state, reason)));
        static string Names(IEnumerable<string> planets) => string.Join(", ", planets);

        // Lajjita — 5th house, conjunct a fixed malefic-leaning graha.
        var lajjitaWith = conjunctPlanets.Where(p => p is "Sun" or "Mars" or "Saturn" or "Rahu" or "Ketu").ToList();
        if (houseNumberFromLagna == 5 && lajjitaWith.Count > 0)
            Add("Lajjita", $"In the 5th house, joined by {Names(lajjitaWith)}");

        // Garvita — exalted or moolatrikona.
        if (dignityStatus is "Exalted" or "Moolatrikona")
            Add("Garvita", $"Dignity is {dignityStatus}");

        // Kshudhita — enemy's sign, or conjunct/aspected by an enemy, or conjunct Saturn.
        var enemyConjunct = conjunctPlanets.Where(p => IsEnemyTier(relationshipOf(p))).ToList();
        var enemyAspect = aspectingPlanets.Where(p => IsEnemyTier(relationshipOf(p))).ToList();
        var kshudhita = new List<string>();
        if (dignityStatus is "Enemy" or "Great Enemy") kshudhita.Add($"in an enemy's sign (dignity {dignityStatus})");
        if (enemyConjunct.Count > 0) kshudhita.Add($"joined by enemies {Names(enemyConjunct)}");
        if (enemyAspect.Count > 0) kshudhita.Add($"aspected by enemies {Names(enemyAspect)}");
        if (conjunctPlanets.Contains("Saturn")) kshudhita.Add("joined by Saturn");
        if (kshudhita.Count > 0) Add("Kshudhita", string.Join("; ", kshudhita));

        // Trishita — watery sign, aspected by an enemy, no benefic aspect.
        if (Enum.TryParse<ZodiacName>(sign, out var signEnum) && WaterySigns.Contains(signEnum)
            && enemyAspect.Count > 0
            && !aspectingPlanets.Any(p => !isNaturalMalefic(p)))
            Add("Trishita", $"In a watery sign ({sign}), aspected by enemies {Names(enemyAspect)}, with no benefic aspect");

        // Mudita — friend's sign, or conjunct/aspected by a friend, or conjunct Jupiter.
        var friendConjunct = conjunctPlanets.Where(p => IsFriendTier(relationshipOf(p))).ToList();
        var friendAspect = aspectingPlanets.Where(p => IsFriendTier(relationshipOf(p))).ToList();
        var mudita = new List<string>();
        if (dignityStatus is "Friend" or "Great Friend") mudita.Add($"in a friend's sign (dignity {dignityStatus})");
        if (friendConjunct.Count > 0) mudita.Add($"joined by friends {Names(friendConjunct)}");
        if (friendAspect.Count > 0) mudita.Add($"aspected by friends {Names(friendAspect)}");
        if (conjunctPlanets.Contains("Jupiter")) mudita.Add("joined by Jupiter");
        if (mudita.Count > 0) Add("Mudita", string.Join("; ", mudita));

        // Kshobhita — conjunct the Sun, aspected by a malefic or an enemy.
        var kshobhitaBy = aspectingPlanets.Where(p => isNaturalMalefic(p) || IsEnemyTier(relationshipOf(p))).ToList();
        if (conjunctPlanets.Contains("Sun") && kshobhitaBy.Count > 0)
            Add("Kshobhita", $"Joined by the Sun and aspected by malefics or enemies {Names(kshobhitaBy)}");

        return found;
    }
}
