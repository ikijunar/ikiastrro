namespace Ikiastrro.Web.Components.Shared;

/// <summary>One plain-language note for an Astro Facts section: what the table is, how to read it, and
/// where the method comes from. <see cref="Refs"/> name books and sections the app's own code and rule
/// tables already cite (docs/research/sources.md); where a reading is the project's own wording the
/// entry says so rather than borrowing a book's authority.</summary>
public sealed record SectionGuideEntry(string What, string HowToRead, params string[] Refs);

/// <summary>Layman notes for every Astro Facts section, keyed by section id (see AstroFacts.NatalSection)
/// or by tab key. Shown collapsed under each heading by <c>SectionGuide</c>.</summary>
public static class AstroFactsGuides
{
    private const string Pvr = "P.V.R. Narasimha Rao, Vedic Astrology: An Integrated Approach";
    private const string Bphs = "Parāśara, Brihat Parashara Hora Shastra";
    private const string RamanHtjh = "B.V. Raman, How to Judge a Horoscope";
    private const string Raman300 = "B.V. Raman, Three Hundred Important Combinations";
    private const string RamanBalas = "B.V. Raman, Graha and Bhava Balas";
    private const string Jhora = "Jagannatha Hora (P.V.R. Narasimha Rao's software) — formulae reproduced and checked against its own output for the reference chart; a classical page is not yet cited";
    private const string Synthesis = "ikiastrro project synthesis — the app's own wording, not a classical text";

    public static SectionGuideEntry? For(string key) => Entries.GetValueOrDefault(key);

    private static readonly Dictionary<string, SectionGuideEntry> Entries = new()
    {
        // ---- Natal › General
        ["af-panchanga"] = new(
            "The five \"limbs\" of the day you were born: weekday, lunar day (tithi), the Moon's star (nakshatra), yoga and karana, with sunrise and sunset.",
            "The nakshatra is your birth star and sets where the dasha (life-period) sequence starts. The tithi tells whether the Moon was waxing or waning. Together they describe the quality of the birth moment.",
            Pvr),
        ["af-positions"] = new(
            "Where each planet stood in the sky at birth: its sign, degree, nakshatra and house counted from the Lagna (rising sign). Rahu and Ketu are the Moon's nodes, not physical planets.",
            "The planet is the \"who\", the sign is the \"how\", the house is the \"where in life\". A planet marked retrograde (R) appears to move backwards, which tradition reads as turned inward or stronger in effort. Positions use the Lahiri sidereal zodiac.",
            Pvr, "Swiss Ephemeris (astronomical positions, Lahiri ayanāṁśa, true node)"),
        ["af-sign-nakshatra"] = new(
            "For each planet, what its sign and nakshatra together say, among the 36 valid sign-and-star pairs.",
            "A star is 13°20′ wide, so it sits inside one sign or crosses one border; the pair gives a finer flavour than the sign alone. Expand a row for the narrative.",
            Synthesis),

        // ---- Natal › Special points
        ["af-upagrahas"] = new(
            "Shadow points calculated from the Sun's day or night portions (Gulika, Māndi and others). They are not planets.",
            "Traditionally read as hidden troubles or obstacles in the house they fall in, mostly when they sit with something important.",
            Pvr),
        ["af-arudha-padas"] = new(
            "The Ārūḍha pada of each house: the sign where that house's matters appear in the world, as against what is really there.",
            "Think \"image versus reality\". The Ārūḍha Lagna (AL) is how you are seen by others; the other padas show how each area of life is perceived.",
            Pvr),
        ["af-graha-arudhas"] = new(
            "The same \"image\" idea worked out for each planet.",
            "Shows how a planet's affairs are perceived by the world, as against what the planet actually does in the chart.",
            Pvr),
        ["af-yogi"] = new(
            "The Yogi and Avayogi points (a lucky and an unlucky point worked from the Sun and Moon), the Bhṛgu Bindu and the Varṇada lagnas.",
            "The Yogi planet is thought to support you when it is strong; the Avayogi to obstruct. They are used as supporting indications, not main verdicts.",
            Pvr),
        ["af-nava-tara"] = new(
            "The nine tārās: the 27 stars grouped in nine sets counted from your birth star (and from the Lagna's star), each set friendly or unfriendly.",
            "Used for judging a day or a planet: stars in friendly sets (Sampat, Kṣema, Sādhana, Mitra, Parama Mitra) favour; Vipat, Pratyak and Naidhana disfavour.",
            Pvr),

        ["af-special-tara"] = new(
            "Eleven more star-sets counted from your birth star (and from the Lagna's star): Janma, Karma, Sāmudāyika, Sāṅghātika, Jāti, Naidhana, Deśa, Abhiṣeka, Ādhāna, Vainaśika and Mānasa. Unlike the nine tārās, this count includes Abhijit, the small 28th star between U.Āṣāḍhā and Śravaṇa.",
            "Each name is an area of life — Karma is work, Naidhana is death, Abhiṣeka is coronation (rise in status), Mānasa is the mind. Planets transiting or running a dasha through these stars colour that area. Read it as a timing helper, not a verdict.",
            Jhora),
        ["af-latta"] = new(
            "Each planet \"kicks\" one star: Sun the 12th from where it stands, Moon the 22nd back, Mars the 3rd, Mercury the 7th back, Jupiter the 6th, Venus the 5th back, Saturn the 8th, and the nodes the 9th back. A planet sitting in that star is said to be struck.",
            "Look at the last column: a planet named there is the one being kicked. A kick from a malefic is read as trouble for the struck planet's matters; from a benefic it is milder. The traditional lists are short, so absence of a kick is normal.",
            Jhora),
        ["af-special-tithis"] = new(
            "Thirty-six special tithis worked from the Moon–Sun gap at birth: multiply the gap by 1, 2, 3 … 36 and read the lunar day each lands on. The first twelve are named Janma (birth), Dhana (wealth), Bhrātṛ (siblings), Mātṛ (mother), Putra (children), Śatru (enemies), Kalatra (spouse), Mṛtyu (death), Bhāgya (fortune), Karma (work), Lābha (gains), Vyaya (loss); the next two rounds repeat them.",
            "Each row gives the lunar day, whether the Moon is waxing (bright) or waning (dark), how much of it is left, its planetary lord and its goddess (Nityā devī). A tithi whose lord is a friend of the matter it names is read as supportive.",
            Jhora),
        ["af-sphutas"] = new(
            "Sensitive points built by adding multiples of the Lagna, Moon, Sun and Gulika. Prāṇa (life force), Deha (body) and Mṛtyu (death) are the group read for health and longevity; the Tri-, Catus- and Pañca-sphuṭas add the Moon, Sun and Rahu in turn. Tithi, Yoga and Rāhu-tithi sphuṭas are sums and differences of Sun, Moon and Rahu.",
            "A sphuṭa is a point in a sign, like a planet. Its sign and nakshatra, the house it falls in and who rules it are what is read; a malefic or the 6th, 8th or 12th house is the usual warning. The \"Made from\" column shows the arithmetic. \"Gulika\" in those formulas is the point this app lists as Māndi — JHora and this app swap the two names, and JHora's Gulika is the one the formulas use.",
            Jhora),
        ["af-sahams"] = new(
            "Thirty-six sensitive points, each standing for one matter (fortune, marriage, children, profession, death and so on). Each is worked as one planet's distance from another, laid off again from a third point, usually the Lagna. By night most formulas run in reverse.",
            "Find the matter you care about, then read the sign, house and sign lord: a saham in a kendra or trikona house with a strong lord is favourable, one in the 6th, 8th or 12th is the reverse. Sahams are mainly used to time events when a planet transits over them.",
            Pvr, Jhora),
        ["af-malicious"] = new(
            "Two \"malicious\" divisions used in longevity reading: the 64th navamsa and the 22nd drekkana counted from the Lagna's and from the Moon's own navamsa and drekkana. Their lords are the planets said to bring danger in their periods.",
            "The 64th navamsa is the same in every method; the 22nd drekkana depends on which of the five D3 schemes you follow, so all five are shown. Where several schemes agree on a planet, that planet carries more weight.",
            Bphs, Jhora),

        // ---- Natal › About planets
        ["af-moon"] = new(
            "The Moon's state: waxing or waning, phase and strength, because the Moon stands for your mind and emotions.",
            "A bright, waxing Moon is generally taken as supportive; a dim, waning Moon as more sensitive. Used alongside the Moon's sign and house.",
            Pvr),
        ["af-dignity"] = new(
            "How comfortable each planet is in its sign: exalted, own sign, friend, neutral, enemy or debilitated, plus the planet's natural role (kāraka).",
            "Think of each planet as a person: in its own or an exalted sign it is at home and delivers well; debilitated or in an enemy's sign it struggles. The exaltation bar shows how close it is to its peak degree.",
            Pvr, Bphs),
        ["af-benefic-malefic"] = new(
            "Whether each planet acts as a friend or a foe in this chart, read by what it is (natural), what its house ownership makes of it (functional) and how it fares in the house it sits in — one column per school, because the schools do not agree.",
            "A planet's natural nature is fixed (Jupiter and Venus benefic, Sun, Mars and Saturn malefic, the Moon by waxing or waning, Mercury by its company). Its functional nature depends on the Lagna: a natural benefic that owns a quadrant is neutral in BPHS but malefic for PVR and Raman. Hover any cell for the rule and its source. A dash means the school says nothing, not that the planet is neutral. In a varga the table recounts the houses from that varga's own Lagna.",
            Pvr, Bphs, RamanHtjh, "Kalyāṇa Varma, Saravali (ch.30 v.86-87)", "Mantreśvara, Phaladeepika (ch.6 notes)", "docs/research/domain/planet-benefic-malefic-perspectives.md"),
        ["af-motion"] = new(
            "Speed and direction of each planet, whether it is too close to the Sun (combust), and whether two planets are in a \"planetary war\".",
            "A combust planet is overpowered by the Sun and its results are dimmed. In a planetary war the planet that loses gives weaker results. Retrograde planets turn their energy inward.",
            Pvr, "Combustion orbs per Brihat Parashara Hora Shastra"),
        ["af-mrityu-pushkara"] = new(
            "Two kinds of special degrees: Mṛtyu bhāga (a \"death-like\" degree that weakens) and Puṣkara (a nourishing degree that supports).",
            "\"In\" means the planet is inside that one-degree window. Mṛtyu bhāga is a caution; Puṣkara is a quiet blessing. Treat both as small modifiers.",
            Pvr),
        ["af-rudra-maheswara"] = new(
            "Rudra, the Trishoola signs and Maheswara: reference points tradition uses to judge the timing of difficult events.",
            "They are shown as facts. The book itself says they are not infallible, so use them only as background to the main dasha reading.",
            Pvr + ", sec. 14.3"),
        ["af-states"] = new(
            "The planetary states (avasthas): each planet's age (child to old), alertness (awake, dreaming, asleep), posture and shine.",
            "A planet in a lively state (youthful, awake) is thought to deliver its results; one in a weak state (very young or old, asleep) delivers less. A modifier on top of dignity and strength.",
            Pvr, "Bālādi and Jāgradādi states per Brihat Parashara Hora Shastra"),
        ["af-sayanadi"] = new(
            "How each planet's Śayanādi state (its \"posture\") is calculated, so every input is visible.",
            "The formula combines the planet's star, its index, its navāṃśa, the Moon's star, the birth ghaṭī and the Lagna sign, then takes the remainder after dividing by 12. You do not need to follow it; it is shown so the result can be checked.",
            Pvr + ", sec. 15.4.4, Table 36"),

        // ---- Natal › About houses
        ["af-house-lords"] = new(
            "For each house: its sign, which planet rules it (the lord), where that lord sits, and how dignified the lord is.",
            "A house's results are carried by its lord. A lord in a good house and a good sign supports that area of life; a lord in a difficult house (6, 8, 12) or weak sign strains it. Placement is read from the Lagna and from the Moon.",
            Pvr, RamanHtjh),
        ["af-house-findings"] = new(
            "Classical statements about what happens when a house's lord sits in each other house, with a baseline and, where the source gives one, a well-placed and an afflicted version.",
            "Read the baseline first; the well-placed or afflicted line applies when the lord is actually strong or weak. These are first-pass statements, not yet cross-checked against a second author.",
            RamanHtjh),
        ["af-house-verdicts"] = new(
            "What helps or hurts each house, in two readings side by side: Raman's count of the lord's nature, occupants and aspects, and the Narasimha Rao step-by-step of planets that help or hinder.",
            "Where the two agree, the house is clearly supported or pressured. Where they differ, treat the house as mixed.",
            RamanHtjh, Pvr),
        ["af-argala"] = new(
            "Argala is \"intervention\": planets in the 2nd, 4th, 11th (and 5th) from a house push its results; planets in the 12th, 10th, 3rd (and 9th) can obstruct that push (virodhargala).",
            "Per house, see who pushes and who blocks; the Net column tells which wins. A house with strong helpers and few blockers is carried well.",
            Pvr + ", sec. 10.5–10.6"),
        ["af-stronger-rasi"] = new(
            "Which of two signs is stronger, and how that decides the starting sign and direction of the Narayana dasha.",
            "Narayana dasha is a sign-based life-period system. The stronger of the Lagna and the 7th sign is its starting point; the order then runs forward or backward by a fixed rule.",
            Pvr + ", sec. 15.5.2 and 18.2.1"),

        // ---- Natal › Signs & planet aspects
        ["af-graha-drishti"] = new(
            "How strongly each planet looks at each other body (aspects), as a percentage.",
            "Every planet fully aspects the 7th house from itself; Mars also 4th and 8th, Jupiter 5th and 9th, Saturn 3rd and 10th. A higher percentage means a stronger influence. Pick a focus body to see who looks at it.",
            "Brihat Parashara Hora Shastra ch. 26 (graha dṛṣṭi)"),
        ["af-rasi-drishti"] = new(
            "Sign aspects (Jaimini style): which signs look at which, regardless of the planets in them.",
            "Movable signs aspect fixed signs except the neighbour; fixed aspect movable except the neighbour; dual signs aspect each other. A planet in a sign receives these sign aspects.",
            Pvr),
        ["af-dispositors"] = new(
            "Each planet is \"hosted\" by the lord of the sign it sits in (its dispositor). Following the hosts from planet to host to host shows where each chain finally ends.",
            "The last planet in a chain, in its own sign, is the final dispositor. A single final dispositor means the whole chart answers to one planet; a mutual reception means two planets share the final say. The plain-words box under the table reads your own chart.",
            "Method: following sign lords, as implemented in ikiastrro", Synthesis),
        ["af-conjunctions"] = new(
            "Groups of three or more planets sitting in the same sign.",
            "A crowd of planets in one sign concentrates energy on that house's matters, for better or worse depending on who is in it and how strong they are. Check combustion in the Motion table when the Sun is part of the group.",
            Pvr),

        // ---- Transit / Strength / other tabs
        ["transit"] = new(
            "Where the planets are now (or on a date you pick) compared with your birth chart, with the Gochara (transit) reading.",
            "Slow planets (Saturn, Jupiter, Rahu, Ketu) matter most. Transits are read from the natal Moon: Saturn over the 12th, 1st and 2nd from the Moon is Sade Sati. The Gochara net tier summarises whether the transit is helping or pressing.",
            Pvr),
        ["strength-planet"] = new(
            "Ṣaḍbala, the \"six strengths\" of each planet, shown in rūpas. The marker shows the minimum the planet needs.",
            "A bar past the marker means the planet is strong enough to deliver its results; short of it means it struggles. Strong, Moderate and Weak bands compare the planet with its own required minimum. Expand a row for the six components.",
            Pvr, "Brihat Parashara Hora Shastra ch. 27 (Ṣaḍbala)", RamanBalas),
        ["strength-house"] = new(
            "Bhāva Bala: how strong each house is, from its lord's strength, the planets that look at it and its own position.",
            "A strong house gives its matters (e.g. home, career, marriage) with less struggle. Compare houses with each other rather than against a fixed number.",
            Pvr, RamanBalas),
        ["strength-ashtavarga"] = new(
            "Aṣṭakavarga: a point score of each sign by each planet. The Sarvāṣṭaka total (SAV) adds all planets' points per sign.",
            "A sign with many points (about 31 or more) is favourable when planets transit it; few points (under about 25) is unfavourable. Each planet's own table (BAV) shows where that planet transits best.",
            "Brihat Parashara Hora Shastra, Aṣṭakavarga chapter", Pvr),
        ["karakas"] = new(
            "One chart for every special reference point: the special lagnas (Hora, Ghaṭi, Śrī, Bhāva, Indu, Prāṇapada and others) and the special points — upagrahas, Bhṛgu Bindu and Varṇada lagnas, the 14 sphuṭas and the 36 sahams — laid on the sign you pick, as a polar wheel or a South Indian grid. The Jaimini karakas (the planet ranked highest by degree is the Ātmakāraka, the \"soul\" planet) are in the table below.",
            "Use the four dropdowns under the chart picker — Lagnas, Upagrahas, Sphuṭas, Sahams. Each opens a checklist with a Find box, All / None and a preset (for example Key 10 sahams); the number on the button is how many are on. In the Grid a coloured chip in a sign is a point sitting there (grey upagraha, teal Bhṛgu Bindu/Varṇada, blue sphuṭa, brown saham) and \"+n\" means more — hover or tap a sign to list them all with degree and house. In the Wheel each sign's points sit in their own ring inside the planets, and the lagna rings show house numbers counted from each lagna. On a divisional chart the points keep their rāśi (D1) degree and take that chart's sign.",
            Pvr, Jhora),
        ["yogas"] = new(
            "Yogas are named planetary combinations, each with a classical meaning. The table lists those present in your chart.",
            "\"Based on\" shows the reference point (Sun, Moon or Lagna). \"Planets\" are those that make the yoga and the strength column shows how strong each is. Good yogas help most when their planets are strong; weak planets give a faint result.",
            Raman300, Pvr),
        ["vargas"] = new(
            "The divisional charts (vargas): the same birth divided into finer slices (D9 for marriage and dharma, D10 for career and so on). Aṁśabala counts the vargas where a planet is well placed.",
            "A planet that is strong in many divisional charts is reliable; one that is strong only in the main chart is less so. Vimśopaka scores this out of 20. Vargottama means the planet is in the same sign in D1 and D9, a sign of strength.",
            Pvr, "Brihat Parashara Hora Shastra (Varga Viveka)"),
        ["all-charts"] = new(
            "Every divisional chart on one screen.",
            "Use it to scan quickly; open a single chart on the Natal tab for detail.",
            Pvr),
        ["d1-promise"] = new(
            "For each life matter (career, marriage, children and so on), what the main birth chart promises, read from the relevant houses, lords and karakas.",
            "A promise is potential, not a guarantee: it says what the chart supports. Timing comes from the dasha tabs. Argala is read pair by pair, with good or bad influences listed.",
            Pvr, Synthesis),
        ["phalita"] = new(
            "Phalita dasas: the sign-based life-period systems (Narayana, Lagna Kendradi Rasi, Sudasa, Drigdasa) and Ashtottari, each with the period running now, the dated table, and what the book reads in it.",
            "Pick a system and a date. The running period shows which sign is \"switched on\" now; each rule is shown as holding or not holding in this chart. These are facts from the book's own rules, to be used alongside the main dasha, not in place of it.",
            Pvr + ", Part 2 (rāśi dasas)"),
        ["dasha-matters"] = new(
            "Which life matters are active in each dasha period, and how the planet running the period is placed.",
            "A period's results follow the planet running it: its house, lordship, strength and the matters it rules. Check the current and next period first.",
            Pvr + ", sec. 16.5.1"),
    };
}
