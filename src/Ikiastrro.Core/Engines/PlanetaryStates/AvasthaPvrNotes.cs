namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>
/// What each avastha state means, in P.V.R. Narasimha Rao's words, <i>Vedic Astrology: An Integrated Approach</i>
/// sec.15.4.1-15.4.3, printed pp.190-192 (<c>SRC_PVR_INTEGRATED</c>, checked against the raw extract), together
/// with the house-specific results he names there. Pure text; no I/O. The Śayanādi (15.4.4) results are per
/// planet and live in <c>tbl_Rule_PostureStateInterpretation</c>, not here.
/// <list type="bullet">
/// <item>Bālādi (age, 15.4.1): Saisava a quarter of its results, Kumaara half, Yuva all, Vriddha some, Mrita none.</item>
/// <item>Jāgradādi (alertness, 15.4.2): awake gives full results, dreaming medium, asleep negligible.</item>
/// <item>Dīptādi (attitude and mood, 15.4.3): nine states, then six more (Lajjita onward).</item>
/// </list>
/// State names are the project's spellings from <c>tbl_Dim_PlanetaryState</c> (PVR's Saisava is Baala here,
/// Kumaara is Kumara, Jaagrita is Jagrat).
/// </summary>
public static class AvasthaPvrNotes
{
    public const string Source = "PVR §15.4";

    /// <summary>PVR's caution after the age table (15.4.1): age is one factor among several.</summary>
    public const string AgeCaution =
        "PVR: do not be carried away with the age states; a planet can give good results irrespective of its " +
        "age-related state, as age is just one of the factors deciding its effectiveness.";

    private static readonly Dictionary<(string System, string State), string> Meanings = new()
    {
        [("Baaladi", "Baala")] = "child (Saisava): gives one quarter of its results",
        [("Baaladi", "Kumara")] = "adolescent (Kumaara): gives half of its results",
        [("Baaladi", "Yuva")] = "youth: gives all of its results",
        [("Baaladi", "Vriddha")] = "old: gives some of its results and is not very effective",
        [("Baaladi", "Mrita")] = "dead: gives none of its results",

        [("Jagradadi", "Jagrat")] = "awake, in an exaltation or own rasi: gives full results",
        [("Jagradadi", "Swapna")] = "dreaming, in a neutral or friendly planet's rasi: gives medium results",
        [("Jagradadi", "Sushupti")] = "asleep, in its debilitation rasi or an enemy's: its results are negligible",

        [("Deeptadi", "Deepta")] = "bright: in its exaltation rasi",
        [("Deeptadi", "Swastha")] = "doing well, contented, comfortable, natural: in its own rasi",
        [("Deeptadi", "Mudita")] = "delighted: in a good friend's rasi",
        [("Deeptadi", "Saanta")] = "peaceful: in a friend's rasi",
        [("Deeptadi", "Deena")] = "sad, depressed: in a neutral planet's rasi",
        [("Deeptadi", "Duhkhita")] = "distressed, miserable: in an enemy's rasi",
        [("Deeptadi", "Vikala")] = "crippled, confused: joined by malefic planets",
        [("Deeptadi", "Khala")] = "mischievous, scheming: in a malefic planet's rasi",
        [("Deeptadi", "Kopita")] = "angry: joined closely by the Sun",

        [("Lajjitadi", "Lajjita")] = "ashamed: in the 5th house joined by the Sun, Mars, Saturn, Rahu or Ketu",
        [("Lajjitadi", "Garvita")] = "proud: in its exaltation or moolatrikona rasi",
        [("Lajjitadi", "Kshudhita")] = "hungry: in an enemy's rasi, or conjoined or aspected by enemies, or conjoined by Saturn",
        [("Lajjitadi", "Trishita")] = "thirsty: in a watery rasi and aspected by enemies without the aspect of benefics",
        [("Lajjitadi", "Mudita")] = "delighted: in a friend's sign, conjoined or aspected by friends and conjoined by Jupiter",
        [("Lajjitadi", "Kshobhita")] = "shaken, agitated: conjoined by the Sun and aspected by malefics or enemies",
    };

    /// <summary>One avastha set: what PVR says it looks at and how this app reads it. <see cref="Uses"/> is the list of
    /// inputs the set depends on, <see cref="AppReading"/> where the app's reading is narrower than, or follows a
    /// source other than, the PVR wording, and <see cref="Scope"/> which charts it is computed for.</summary>
    public sealed record AvasthaSystemInfo(
        string System, string Title, string Section, IReadOnlyList<string> Uses, string AppReading, string Scope);

    /// <summary>The five sets in PVR order, with the inputs each uses (rammyps, 2026-10-17). The one-line reason
    /// for each planet's state comes from <see cref="StateReason"/>; this is the set-level definition.</summary>
    public static readonly IReadOnlyList<AvasthaSystemInfo> Systems =
    [
        new("Baaladi", "Age", "15.4.1",
            ["Degree within the sign", "Odd or even sign"],
            "Five degree bands per sign parity (tbl_Rule_AgeState), each with the share of results PVR gives it.",
            "D1 only: a varga sign has no continuous degree."),
        new("Jagradadi", "Alertness", "15.4.2",
            ["Dignity of the sign it occupies"],
            "Dignity maps to awake, dreaming or asleep (tbl_Rule_WakefulnessState); moolatrikona counts with own sign, great enemy and debilitation with enemy.",
            "Every chart."),
        new("Deeptadi", "Attitude and mood: nine states", "15.4.3",
            ["Exaltation or own sign", "Friendship with the sign lord", "Conjunction with malefics", "Sign lord is a malefic", "Closeness to the Sun"],
            "Follows Jagannatha Hora's Mood column (decision 012): Deepta or Swastha kept alongside the sign-lord friendship tier; Vikala needs two or more natural malefics; Kopita uses a 5° orb; malefics are the fixed set Sun, Mars, Saturn, Rahu, Ketu.",
            "Every chart."),
        new("Lajjitadi", "Attitude and mood: six more states", "15.4.3",
            ["House position (Lajjita: 5th)", "Conjunctions", "Aspects received", "Friendship with the planets joining or aspecting it", "Watery sign", "Exaltation or moolatrikona"],
            "Friend and enemy are the directional Panchadha friendship of this planet to the other. Every match is kept; PVR orders none. Only Lajjita and Garvita can be checked against Jagannatha Hora.",
            "Every chart."),
        new("Sayanadi", "Activity: twelve states", "15.4.4",
            ["Own nakshatra", "Planet index", "Navamsa within its sign", "Moon's nakshatra", "Birth ghati", "Lagna sign"],
            "PVR's formula exactly ((C × P × A) + M + G + L) mod 12; matches Jagannatha Hora. PVR calls this the most important of the states.",
            "D1 only: needs the exact degree and the birth ghati."),
    ];

    /// <summary>What no avastha set looks at. These sit beside the states, never inside them, so the reading shows
    /// each once and does not count a dignity, aspect or conjunction twice.</summary>
    public static readonly IReadOnlyList<string> NotCoveredByAvasthas =
    [
        "Functional nature for the lagna (benefic or malefic by the houses a planet owns)",
        "Houses owned and the house stood in, beyond Lajjita's 5th",
        "Retrogression, and combustion as a test in its own right (Kopita covers closeness to the Sun only)",
        "Strength measures: Ṣaḍbala, Aṣṭakavarga, Vimśopaka",
        "Aspects the planet casts (avasthas read only the aspects it receives)",
        "Argala position, pairing and counts",
    ];

    /// <summary>PVR's meaning of one state, or null when the book gives none for it (the Śayanādi states).</summary>
    public static string? Meaning(string avasthaSystem, string stateName) =>
        Meanings.GetValueOrDefault((avasthaSystem, stateName));

    /// <summary>
    /// The results PVR names for a planet in these states in a given house (15.4.3, last paragraph): a planet in
    /// Lajjita in the 5th may bring losses related to progeny; one in Kshobhita in the 7th may bring loss of
    /// spouse; and a house holding planets in Kshudhita or Kshobhita has its significations affected.
    /// </summary>
    public static IReadOnlyList<string> HouseResults(IEnumerable<string> lajjitadiStateNames, int house)
    {
        var states = lajjitadiStateNames.ToHashSet(StringComparer.OrdinalIgnoreCase);
        var results = new List<string>();
        if (house == 5 && states.Contains("Lajjita")) results.Add("Lajjita in the 5th: there may be losses related to progeny.");
        if (house == 7 && states.Contains("Kshobhita")) results.Add("Kshobhita in the 7th: there may be loss of spouse.");
        if (states.Contains("Kshudhita") || states.Contains("Kshobhita"))
            results.Add($"Kshudhita or Kshobhita in house {house}: the significations of that house are affected.");
        return results;
    }
}
