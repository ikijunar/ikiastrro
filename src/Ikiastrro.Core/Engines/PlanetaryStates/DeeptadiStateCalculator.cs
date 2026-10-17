namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>
/// Dīptādi Avastha (PVR sec 15.4.3) — "state related to attitude and mood," 9 states, read the way
/// Jagannatha Hora prints them in its "Mood (D-1)" column (decision 012, golden:
/// <c>AvasthaJhoraMoodGoldenTests</c>, 4 reference charts, 36 planet cells):
///   - Deepta (exalted) and Swastha (own sign / moolatrikona) are placement flags.
///   - Mudita / Saanta / Deena / Duhkhita are the planet's compound (Panchadha) relationship to its
///     sign lord and are kept <i>alongside</i> Deepta/Swastha — an exalted Sun in Aries (lord Mars:
///     natural friend, temporary enemy) is Deepta and Deena. A planet in its own sign has no
///     separate sign lord, so gets no relationship tier.
///   - Vikala: joined by malefic planets (plural: at least two natural malefics in the sign).
///   - Khala: the sign is owned by a natural malefic (Sun, Mars, Saturn).
///   - Kopita: joined closely by the Sun — the Sun within <see cref="KopitaOrbDegrees"/>.
/// "Malefic" here is the fixed classical set (<see cref="ClassicalMalefics"/>); JHora does not make
/// the Moon or Mercury conditional for these states. Every match is kept (the book orders none).
///
/// Works for any chart type: it only needs dignity, the sign-lord relationship, conjunctions and the
/// Sun's real separation, all of which ChartAnalyzer already produces.
/// </summary>
public static class DeeptadiStateCalculator
{
    /// <summary>Kopita's "joined closely" orb. JHora flags 3.8°, 3.9°, 4.3° and 1.2° from the Sun and
    /// not 6.4°, so the true cut-off lies in [4.3°, 6.4°); 5° is the round value inside it.</summary>
    public const decimal KopitaOrbDegrees = 5m;

    public static List<byte> For(
        string? dignityStatus,
        string? relationToSignLord,
        string? signLordPlanet,
        IReadOnlyList<string> conjunctPlanets,
        decimal? distanceFromSunDegrees,
        PlanetaryStateRuleSet rules) =>
        Evaluate(dignityStatus, relationToSignLord, signLordPlanet, conjunctPlanets, distanceFromSunDegrees, rules)
            .Select(m => m.Id).ToList();

    /// <summary>The same states as <see cref="For"/>, each with the inputs that produced it.</summary>
    public static List<StateReason> Explain(
        string? dignityStatus,
        string? relationToSignLord,
        string? signLordPlanet,
        IReadOnlyList<string> conjunctPlanets,
        decimal? distanceFromSunDegrees,
        PlanetaryStateRuleSet rules) =>
        Evaluate(dignityStatus, relationToSignLord, signLordPlanet, conjunctPlanets, distanceFromSunDegrees, rules)
            .Select(m => m.Reason).ToList();

    private static List<(byte Id, StateReason Reason)> Evaluate(
        string? dignityStatus,
        string? relationToSignLord,
        string? signLordPlanet,
        IReadOnlyList<string> conjunctPlanets,
        decimal? distanceFromSunDegrees,
        PlanetaryStateRuleSet rules)
    {
        const string system = "Deeptadi";
        var found = new List<(byte Id, StateReason Reason)>();

        // Placement tier: exaltation / own sign.
        var isPlacementTier = dignityStatus is "Exalted" or "Own Sign" or "Moolatrikona";
        if (isPlacementTier && rules.DeeptadiByDignity.TryGetValue(dignityStatus!, out var placement))
            found.Add((placement.AvasthaStateId, new StateReason(system, placement.StateName,
                $"Dignity is {dignityStatus} (placement tier: exaltation or own sign)")));

        // Relationship tier to the sign lord. Nodes have no Naisargika table, so for them (relation
        // null) the engine's own dignity label stands in, as before.
        var relationKey = relationToSignLord ?? (isPlacementTier ? null : dignityStatus);
        if (relationKey is not null && rules.DeeptadiByDignity.TryGetValue(relationKey, out var tier)
            && found.All(f => f.Id != tier.AvasthaStateId))
            found.Add((tier.AvasthaStateId, new StateReason(system, tier.StateName,
                relationToSignLord is not null
                    ? $"Compound relationship to its sign lord {signLordPlanet} is {relationToSignLord}"
                    : $"No sign-lord relationship available (node); dignity {dignityStatus} stands in")));

        var malefics = conjunctPlanets.Where(ClassicalMalefics.Contains).ToList();
        if (malefics.Count >= 2)
            found.Add((rules.DeeptadiStatesByName["Vikala"].Id, new StateReason(system, "Vikala",
                $"Joined by {malefics.Count} natural malefics: {string.Join(", ", malefics)}")));

        if (signLordPlanet is not null && ClassicalMalefics.Contains(signLordPlanet))
            found.Add((rules.DeeptadiStatesByName["Khala"].Id, new StateReason(system, "Khala",
                $"Its sign is owned by {signLordPlanet}, a natural malefic")));

        if (distanceFromSunDegrees is { } d && d <= KopitaOrbDegrees)
            found.Add((rules.DeeptadiStatesByName["Kopita"].Id, new StateReason(system, "Kopita",
                $"{d:0.0}° from the Sun (within the {KopitaOrbDegrees:0}° orb)")));

        return found;
    }
}
