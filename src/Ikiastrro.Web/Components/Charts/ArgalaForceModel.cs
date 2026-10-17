using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;
using Ikiastrro.Core.Engines.PlanetaryStates;
using Ikiastrro.Core.LifeMatters.Promise;
using Ikiastrro.Core.Models;
using Ikiastrro.Core.Presentation;
using Ikiastrro.Data;
using Ikiastrro.Data.Statistics;

namespace Ikiastrro.Web.Components.Charts;

/// <summary>Who wins the house.</summary>
public enum ArgalaOutcome
{
    /// <summary>No planet causes argala or virodhargala on the house.</summary>
    Quiet,
    /// <summary>Virodhargala planets stand ready but there is no argala to obstruct.</summary>
    Idle,
    ArgalaWins,
    VirodhargalaWins,
    /// <summary>Equal planet count and equal dignity total — sec.10.7 gives no further tie-break.</summary>
    Tie
}

/// <summary>How much of its results a planet can deliver, from its Jagradadi state (PVR sec.15.4.2): awake gives
/// full results, dreaming medium, asleep negligible. Unknown when no state is available for the chart.</summary>
public enum Delivery { Full, Medium, Negligible, Unknown }

/// <summary>A planet's avasthas as the argala reading uses them: <see cref="Delivery"/> from Jagradadi, and the
/// states PVR says damage the matters of the house the planet stands in (Lajjita, Kshudhita, Kshobhita, sec.15.4.3),
/// each with its reason. Only these two ideas are used, so a dignity or conjunction already inside a state is not
/// counted a second time.</summary>
public sealed record PlanetStateInfo(
    Delivery Delivery, string? Alertness, string? AlertnessReason,
    IReadOnlyList<StateReason> Damaging)
{
    public static readonly PlanetStateInfo None = new(Delivery.Unknown, null, null, []);

    public static PlanetStateInfo From(IEnumerable<StateReason> reasons)
    {
        var list = reasons.ToList();
        var alert = list.FirstOrDefault(r => r.System == "Jagradadi");
        var delivery = alert?.State switch
        {
            "Jagrat" => Delivery.Full, "Swapna" => Delivery.Medium, "Sushupti" => Delivery.Negligible, _ => Delivery.Unknown
        };
        var damaging = list.Where(r => r.System == "Lajjitadi" && r.State is "Lajjita" or "Kshudhita" or "Kshobhita").ToList();
        return new PlanetStateInfo(delivery, alert?.State, alert?.Reason, damaging);
    }
}

/// <summary>One planet as a unit of force: nature is its functional nature for the lagna (the same reading
/// subhaargala / paapaargala uses), dignity only matters for the tie-break, <see cref="State"/> says how much
/// it can deliver.</summary>
public sealed record ForcePlanet(PlanetName Planet, Direction Nature, int Dignity, PlanetStateInfo State);

/// <summary>One argala-vs-virodhargala duel: planets in the <see cref="ArgalaOffset"/>th from the house push,
/// planets in the paired <see cref="ObstructionOffset"/>th push back. <see cref="Verdict"/> is the lane's own
/// count comparison (the one Key Inference shows); null when the lane has no argala to obstruct.</summary>
public sealed record ForceLane(
    int ArgalaOffset, int ObstructionOffset, string Label,
    int ArgalaHouse, int ObstructionHouse, ZodiacName ArgalaSign, ZodiacName ObstructionSign,
    IReadOnlyList<ForcePlanet> Argala, IReadOnlyList<ForcePlanet> Virodhargala,
    bool ExceptionApplied, ArgalaVerdict? Verdict)
{
    public bool IsActive => Argala.Count > 0 || Virodhargala.Count > 0;
}

/// <summary>What the contest means, in PVR sec.10.7's terms: the matter (house, in this varga), who wins, which
/// planets and which houses' matters cause the win, and the lane-by-lane reasoning.
/// <see cref="Headline"/> is the one-line result, <see cref="Summary"/> the plain-language meaning,
/// <see cref="Varga"/> says which chart is being read, <see cref="Causes"/> names the planets and houses behind the
/// result (the winning side first), <see cref="Lanes"/> reads each pair and <see cref="Caveats"/> flags a win that
/// rests on a planet which is asleep (Sushupti) or damaged (Lajjita, Kshudhita, Kshobhita).</summary>
public sealed record ArgalaReading(
    string Headline, string Summary, string Varga,
    IReadOnlyList<string> Causes, IReadOnlyList<string> Lanes, IReadOnlyList<string> Caveats);

/// <summary>Reference text the reading draws on, all optional: the chart being read (code and its Table 11 domain
/// such as "Marriage / Dharma"), each house's matters (PVR ch.7) and each planet's significations (PVR ch.8
/// naisargika karakatwa).</summary>
public sealed record ArgalaMeaningContext(
    string ChartType = "D1", string? Domain = null,
    IReadOnlyDictionary<int, string>? HouseMatters = null,
    IReadOnlyDictionary<string, string>? PlanetMatters = null,
    IReadOnlyDictionary<string, PlanetStateInfo>? PlanetStates = null)
{
    public static readonly ArgalaMeaningContext Empty = new();
}

/// <summary>Everything the unified Argala table needs for one house.</summary>
public sealed record HouseForce(
    int House, ZodiacName Sign, bool CountedAntiZodiacally,
    IReadOnlyList<ForceLane> Lanes,
    int ArgalaForce, int VirodhargalaForce, int ArgalaDignity, int VirodhargalaDignity,
    ArgalaOutcome Outcome, bool DecidedByDignity, Direction ArgalaNature,
    ArgalaReading Reading);

/// <summary>One argala (and its obstruction) as the page's South-Indian chart draws it: an arrow from the argala
/// sign to the target, and a dashed marker on the obstructing sign.</summary>
public sealed record ArgalaOverlayLink(
    ZodiacName ArgalaSign, ZodiacName? ObstructionSign, bool HasArgala, Direction Nature, ArgalaVerdict? Verdict, bool Dimmed,
    Delivery Delivery, IReadOnlyList<OverlayPlanet> ArgalaPlanets, IReadOnlyList<OverlayPlanet> ObstructionPlanets);

/// <summary>A planet's tag on the chart: its alertness code (Jg, Sw, Su), whether a damaging state applies
/// (<see cref="Warn"/>), whether the result rests on it (<see cref="Deciding"/>), and the full wording for the tooltip.</summary>
public sealed record OverlayPlanet(string Planet, string Tag, bool Warn, bool Deciding, string Title);

/// <summary>What the Argala table asks the page's South-Indian chart to show for the selected house.
/// <see cref="ChartType"/> is the chart the facts were read from, so the page can drop an overlay left over from
/// another varga.</summary>
public sealed record ArgalaOverlay(string ChartType, ZodiacName Target, int House, IReadOnlyList<ArgalaOverlayLink> Links);

/// <summary>
/// Builds what the unified Argala &amp; Virodhargala table shows for each house: its four duels (2nd↔12th, 4th↔10th,
/// 11th↔3rd, 5th↔9th, plus the sec.10.6 malefic-3rd exception lane), the overall winner, the reading, and the
/// overlay for the page's chart. Pure and DB-free, so the logic is unit-testable without rendering.
///
/// Sourcing: the facts and the pairing are PVR sec.10.5-10.6; the overall winner is sec.10.7's own method
/// (more planets win, equal count → compare strengths) via <see cref="ArgalaCalculator.Compare"/>, i.e. the
/// same verdict as the table's Net column. Each lane's verdict is the count comparison Key Inference already
/// shows. "Force" is one unit per planet; dignity only breaks a tie. The reading follows sec.10.7's "guess the
/// meaning from the signs, houses and planets involved" using each position's role (sec.10.7 "Use of Argala"),
/// the houses' matters, the planets' significations and the chart's domain; the wording is this project's own
/// (PROJECT_SYNTHESIS).
/// </summary>
public static class ArgalaForceModel
{
    public static IReadOnlyList<HouseForce> BuildAll(
        IReadOnlyList<ArgalaFactRow> facts, IReadOnlyList<ChartKeyDetail> grahas, string ascendantSign,
        ArgalaMeaningContext? context = null) =>
        Enumerable.Range(1, 12).Select(h => Build(h, facts, grahas, ascendantSign, context)).ToList();

    public static HouseForce Build(
        int house, IReadOnlyList<ArgalaFactRow> facts, IReadOnlyList<ChartKeyDetail> grahas, string ascendantSign,
        ArgalaMeaningContext? context = null)
    {
        var ctx = context ?? ArgalaMeaningContext.Empty;
        var lagna = Enum.Parse<ZodiacName>(ascendantSign);
        var moonWaxing = MoonPhase.Of(grahas);
        var houseFacts = facts.Where(f => f.TargetKind == "House" && f.TargetHouseNumber == house).ToList();
        // Ketu in the house's own sign flips the counting (sec.10.6 note). Read from the chart, not only from the
        // facts, so a house with no occupied positions still labels its (empty) lanes with the right houses.
        var houseSign = HouseEngine.GetHouseSign(lagna, house);
        var reverse = houseFacts.Any(f => f.CountedAntiZodiacally)
                      || grahas.Any(g => g.Planet == nameof(PlanetName.Ketu) && g.Sign == houseSign.ToString());
        var dignityByPlanet = grahas
            .Where(k => k.Planet != "Ascendant" && Enum.TryParse<PlanetName>(k.Planet, out _))
            .ToDictionary(k => Enum.Parse<PlanetName>(k.Planet), k => ChartViewModel.DignityScore(k.DignityStatus));
        int Dignity(PlanetName p) => dignityByPlanet.GetValueOrDefault(p);

        IReadOnlyList<ForcePlanet> Force(IEnumerable<string> names) => names
            .Select(n => Enum.Parse<PlanetName>(n))
            .OrderBy(p => p)
            .Select(p => new ForcePlanet(p, D1PromiseEngine.NatureDirection(lagna, p, moonWaxing), Dignity(p),
                ctx.PlanetStates is not null && ctx.PlanetStates.TryGetValue(p.ToString(), out var st) ? st : PlanetStateInfo.None))
            .ToList();

        var summary = LifeMatterStatistics.BuildArgala(houseFacts);
        var lanes = summary.Pairs.Select(pair =>
        {
            var argalaHouse = HouseFrom(house, pair.ArgalaOffset, reverse);
            var obstructionHouse = HouseFrom(house, pair.ObstructionOffset, reverse);
            return new ForceLane(
                pair.ArgalaOffset, pair.ObstructionOffset,
                pair.Kind == "Malefic 3rd" ? "malefic 3rd" : ArgalaPositionRole.Label(pair.ArgalaOffset) ?? "",
                argalaHouse, obstructionHouse,
                HouseEngine.GetHouseSign(lagna, argalaHouse), HouseEngine.GetHouseSign(lagna, obstructionHouse),
                Force(pair.ArgalaPlanets), Force(pair.ObstructingPlanets),
                pair.ExceptionApplied, pair.Verdict);
        }).ToList();

        // The malefic-3rd exception turns the 3rd from an obstruction into an argala, so the 11th↔3rd lane's
        // virodhargala side is then empty and the 3rd's planets sit in their own extra lane (see BuildArgala).
        var argalaPlanets = lanes.SelectMany(l => l.Argala.Select(p => p.Planet)).ToList();
        var virodhPlanets = lanes.SelectMany(l => l.Virodhargala.Select(p => p.Planet)).ToList();
        var compare = ArgalaCalculator.Compare(argalaPlanets, virodhPlanets, Dignity);

        var outcome = (argalaPlanets.Count, virodhPlanets.Count) switch
        {
            (0, 0) => ArgalaOutcome.Quiet,
            (0, _) => ArgalaOutcome.Idle,
            _ => compare.Dominant switch
            {
                ArgalaCalculator.RelationType.Argala => ArgalaOutcome.ArgalaWins,
                ArgalaCalculator.RelationType.Virodhargala => ArgalaOutcome.VirodhargalaWins,
                _ => ArgalaOutcome.Tie
            }
        };
        var decidedByDignity = compare.ArgalaCount == compare.VirodhargalaCount
                               && outcome is ArgalaOutcome.ArgalaWins or ArgalaOutcome.VirodhargalaWins;
        var nature = NatureOf(lanes.SelectMany(l => l.Argala).ToList());
        var shell = new HouseForce(house, houseSign, reverse, lanes, argalaPlanets.Count, virodhPlanets.Count,
            compare.ArgalaDignitySum, compare.VirodhargalaDignitySum, outcome, decidedByDignity, nature,
            new ArgalaReading("", "", "", [], [], []));
        var reading = Read(house, outcome, nature, decidedByDignity, lanes, ctx, DecidingPlanets(shell));

        return new HouseForce(house, houseSign, reverse, lanes,
            argalaPlanets.Count, virodhPlanets.Count, compare.ArgalaDignitySum, compare.VirodhargalaDignitySum,
            outcome, decidedByDignity, nature, reading);
    }

    /// <summary>The selected house's contest as arrows and markers for the page chart. With a lane selected
    /// (<paramref name="selectedArgalaOffset"/>) the other lanes are dimmed.</summary>
    public static ArgalaOverlay Overlay(string chartType, HouseForce force, int? selectedArgalaOffset)
    {
        var deciding = DecidingPlanets(force);
        OverlayPlanet Tag(ForcePlanet p, int house) => ToOverlay(p, house, deciding.Contains(p.Planet));
        var links = force.Lanes
            .Where(l => l.IsActive)
            .Select(l => new ArgalaOverlayLink(
                l.ArgalaSign,
                l.Virodhargala.Count > 0 && l.ArgalaOffset != 3 ? l.ObstructionSign : null,
                l.Argala.Count > 0, NatureOf(l.Argala), l.Verdict,
                selectedArgalaOffset is not null && l.ArgalaOffset != selectedArgalaOffset,
                LaneDelivery(l.Argala),
                l.Argala.Select(p => Tag(p, l.ArgalaHouse)).ToList(),
                l.ArgalaOffset == 3 ? [] : l.Virodhargala.Select(p => Tag(p, l.ObstructionHouse)).ToList()))
            .ToList();
        return new ArgalaOverlay(chartType, force.Sign, force.House, links);
    }

    /// <summary>The strongest delivery among a lane's argala planets: one awake planet is enough to deliver, so the
    /// lane is drawn by its best planet. This is the app's own rule (PROJECT_SYNTHESIS); the levels are PVR 15.4.2.</summary>
    public static Delivery LaneDelivery(IReadOnlyList<ForcePlanet> planets)
    {
        var known = planets.Select(p => p.State.Delivery).Where(d => d != Delivery.Unknown).ToList();
        return known.Count == 0 ? Delivery.Unknown : known.Min();   // enum order: Full < Medium < Negligible
    }

    /// <summary>Planets the result rests on: those on the winning side when the margin is a single planet (or the count
    /// was level and dignity decided it) — take one away and the winner could change.</summary>
    public static IReadOnlySet<PlanetName> DecidingPlanets(HouseForce force)
    {
        if (force.Outcome is not (ArgalaOutcome.ArgalaWins or ArgalaOutcome.VirodhargalaWins))
            return new HashSet<PlanetName>();
        if (!force.DecidedByDignity && Math.Abs(force.ArgalaForce - force.VirodhargalaForce) != 1)
            return new HashSet<PlanetName>();
        var winners = force.Outcome == ArgalaOutcome.ArgalaWins
            ? force.Lanes.SelectMany(l => l.Argala)
            : force.Lanes.Where(l => l.ArgalaOffset != 3).SelectMany(l => l.Virodhargala);
        return winners.Select(p => p.Planet).ToHashSet();
    }

    private static string DeliveryCode(Delivery d) => d switch
    {
        Delivery.Full => "Jg", Delivery.Medium => "Sw", Delivery.Negligible => "Su", _ => ""
    };

    private static string DeliveryWords(Delivery d) => d switch
    {
        Delivery.Full => "awake: gives full results",
        Delivery.Medium => "dreaming: gives medium results",
        Delivery.Negligible => "asleep: its results are negligible",
        _ => "alertness not available"
    };

    private static OverlayPlanet ToOverlay(ForcePlanet p, int house, bool deciding)
    {
        var damaging = AvasthaPvrNotes.HouseResults(p.State.Damaging.Select(d => d.State), house);
        var title = $"{p.Planet}: {DeliveryWords(p.State.Delivery)}"
                    + (p.State.Damaging.Count > 0 ? $"; {string.Join(", ", p.State.Damaging.Select(d => d.State))}" : "")
                    + (deciding ? "; the result rests on this planet" : "");
        if (damaging.Count > 0) title += ". " + string.Join(" ", damaging);
        return new OverlayPlanet(p.Planet.ToString(), DeliveryCode(p.State.Delivery), p.State.Damaging.Count > 0, deciding, title);
    }

    /// <summary>The lane an obstruction column belongs to: 12th→2nd, 10th→4th, 3rd→11th (or the 3rd's own exception
    /// lane when 2+ malefics make it argala), 9th→5th.</summary>
    public static int LaneOf(int offset, bool thirdIsException) => offset switch
    {
        12 => 2, 10 => 4, 3 => thirdIsException ? 3 : 11, 9 => 5, _ => offset
    };

    /// <summary>Subhaargala vs paapaargala as D1-Promise counts it: benefic against malefic planets among those
    /// causing argala. Mixed when both are present and equal; Neutral when none cause argala.</summary>
    public static Direction NatureOf(IReadOnlyList<ForcePlanet> argala)
    {
        var good = argala.Count(p => p.Nature == Direction.Supportive);
        var bad = argala.Count(p => p.Nature == Direction.Obstructive);
        return argala.Count == 0 ? Direction.Neutral
            : good > bad ? Direction.Supportive
            : bad > good ? Direction.Obstructive
            : good > 0 ? Direction.Mixed : Direction.Neutral;
    }

    /// <summary>The Nth house from <paramref name="house"/> (1-based, house numbers 1-12 from the lagna), forward or —
    /// when Ketu holds the target sign — anti-zodiacally, as ArgalaCalculator counts it.</summary>
    public static int HouseFrom(int house, int offset, bool reverse)
    {
        var step = reverse ? -(offset - 1) : offset - 1;
        return ((house - 1 + step) % 12 + 12) % 12 + 1;
    }

    /// <summary>Share of the tug-of-war the argala side holds, 0-100 (50 when neither side has a planet).</summary>
    public static int ArgalaShare(HouseForce force) =>
        force.ArgalaForce + force.VirodhargalaForce == 0
            ? 50
            : (int)Math.Round(100.0 * force.ArgalaForce / (force.ArgalaForce + force.VirodhargalaForce), MidpointRounding.AwayFromZero);

    /// <summary>Each planet's first three significations (PVR ch.8 naisargika karakatwa), for the reading.</summary>
    public static IReadOnlyDictionary<string, string> PlanetMatters(IEnumerable<NaisargikaKarakatwaRow> details) =>
        details.GroupBy(d => d.Graha).ToDictionary(
            g => g.Key,
            g => string.Join(", ", g.OrderBy(d => d.HouseNumber).ThenBy(d => d.DisplayOrder)
                .Select(d => d.Matter.Trim().ToLowerInvariant()).Distinct().Take(3)));

    private static string Describe(ForcePlanet p, ArgalaMeaningContext ctx, int house)
    {
        var nature = p.Nature switch
        {
            Direction.Supportive => "benefic for this lagna",
            Direction.Obstructive => "malefic for this lagna",
            _ => "neutral for this lagna"
        };
        var signifies = ctx.PlanetMatters is not null && ctx.PlanetMatters.TryGetValue(p.Planet.ToString(), out var m)
            ? $"; signifies {m}" : "";
        var state = p.State.Delivery == Delivery.Unknown ? ""
            : $"; {DeliveryWords(p.State.Delivery)} ({p.State.Alertness}: {p.State.AlertnessReason})";
        var damage = p.State.Damaging.Count == 0 ? ""
            : $"; {string.Join(", ", p.State.Damaging.Select(d => $"{d.State} ({d.Reason})"))}";
        var housewise = AvasthaPvrNotes.HouseResults(p.State.Damaging.Select(d => d.State), house);
        return $"{p.Planet} ({nature}{signifies}{state}{damage})" + (housewise.Count > 0 ? " " + string.Join(" ", housewise) : "");
    }

    /// <summary>"H6 (service and employment, mental tension, injuries)": the house with its first three matters
    /// (the full list is in the House Lord table; the reading only needs enough to say what the house stands for).</summary>
    private static string HouseText(int house, ArgalaMeaningContext ctx) =>
        ctx.HouseMatters is not null && ctx.HouseMatters.TryGetValue(house, out var m)
            ? $"H{house} ({string.Join(", ", m.Split(", ", StringSplitOptions.RemoveEmptyEntries).Take(3)).ToLowerInvariant()})"
            : $"H{house}";

    private static string Names(IEnumerable<ForcePlanet> planets) => string.Join(", ", planets.Select(p => p.Planet));

    private static ArgalaReading Read(
        int house, ArgalaOutcome outcome, Direction nature, bool decidedByDignity,
        IReadOnlyList<ForceLane> lanes, ArgalaMeaningContext ctx, IReadOnlySet<PlanetName> rests)
    {
        var target = HouseText(house, ctx);
        var dignityNote = decidedByDignity ? " The planet counts were equal, so the stronger (better-placed) side decided it." : "";
        var varga = ctx.ChartType == "D1"
            ? $"Read in D1, the birth chart: house {house} is the matter itself, so its significations are taken as they are."
            : $"Read in {ctx.ChartType}{(string.IsNullOrEmpty(ctx.Domain) ? "" : " · " + ctx.Domain)}: house {house} here is that chart's own house, so what it stands for is read within the chart's area of life.";

        var (headline, summary) = outcome switch
        {
            ArgalaOutcome.Quiet => ("No intervention",
                $"Nothing from the 2nd, 4th, 5th or 11th (or from the 12th, 10th, 9th or 3rd) acts on {target}. It runs on its own lord and occupants."),
            ArgalaOutcome.Idle => ("Obstruction with nothing to obstruct",
                $"Planets stand in the obstructing houses, but nothing intervenes on {target}, so there is no argala for them to stop."),
            ArgalaOutcome.Tie => ("Contested — equal forces",
                $"Intervention on {target} meets an equal counter-force, in number and in strength. Neither side prevails; PVR gives no further rule, so the matter stays unsettled."),
            ArgalaOutcome.ArgalaWins => nature switch
            {
                Direction.Supportive => ("Subhaargala wins — help gets through",
                    $"Benefic planets decide parts of {target}, and the counter-force is too weak to stop them. The matters of the houses they stand in support it.{dignityNote}"),
                Direction.Obstructive => ("Paapaargala wins — obstruction gets through",
                    $"Malefic planets decide parts of {target} and are not held back. The matters of the houses they stand in obstruct it (PVR: paapaargala shows trouble through those matters).{dignityNote}"),
                Direction.Mixed => ("Argala wins — help and obstruction both get through",
                    $"Benefic and malefic planets both decide parts of {target}, and the counter-force does not stop them. Some of the houses' matters help it, others hinder.{dignityNote}"),
                _ => ("Argala wins — intervention gets through",
                    $"Planets of neutral standing decide parts of {target}, and the counter-force does not stop them.{dignityNote}")
            },
            _ => nature switch
            {
                Direction.Supportive => ("Virodhargala wins — help is blocked",
                    $"Benefic planets would support {target}, but the obstructing planets outnumber or outweigh them. The help does not arrive in full.{dignityNote}"),
                Direction.Obstructive => ("Virodhargala wins — obstruction is averted",
                    $"Malefic planets would obstruct {target}, but the obstructing planets outweigh them and avert the trouble, as in PVR sec.10.6's own example.{dignityNote}"),
                Direction.Mixed => ("Virodhargala wins — intervention is mostly blocked",
                    $"Both kinds of intervention on {target} are outweighed; the matter keeps to its own course.{dignityNote}"),
                _ => ("Virodhargala wins — intervention is blocked",
                    $"The intervention on {target} is outweighed by its counter-force.{dignityNote}")
            }
        };

        // Causes: the planets and houses behind the winning side first, then the side that lost.
        var causes = new List<string>();
        void ArgalaCauses()
        {
            foreach (var lane in lanes.Where(l => l.Argala.Count > 0))
            {
                var role = ArgalaPositionRole.Meaning(lane.ArgalaOffset) ?? "a contributing factor";
                foreach (var p in lane.Argala)
                {
                    var effect = p.Nature switch
                    {
                        Direction.Supportive => "subhaargala, so that house's matters help",
                        Direction.Obstructive => "paapaargala, so that house's matters obstruct",
                        _ => "argala, so that house's matters take part"
                    };
                    causes.Add($"{Describe(p, ctx, lane.ArgalaHouse)} in {HouseText(lane.ArgalaHouse, ctx)}: {effect}. It is {role}.");
                }
            }
        }
        void VirodhCauses()
        {
            foreach (var lane in lanes.Where(l => l.Virodhargala.Count > 0 && l.ArgalaOffset != 3))
            {
                var what = lane.Argala.Count > 0
                    ? $"obstruct the argala from H{lane.ArgalaHouse}"
                    : $"stand ready to obstruct the argala from H{lane.ArgalaHouse}";
                causes.Add($"{string.Join(", ", lane.Virodhargala.Select(p => Describe(p, ctx, lane.ObstructionHouse)))} in {HouseText(lane.ObstructionHouse, ctx)}: virodhargala, {what}.");
            }
        }
        if (outcome == ArgalaOutcome.VirodhargalaWins) { VirodhCauses(); ArgalaCauses(); } else { ArgalaCauses(); VirodhCauses(); }

        var laneNotes = new List<string>();
        foreach (var lane in lanes.Where(l => l.Argala.Count > 0))
        {
            var counter = $"{lane.Argala.Count}–{lane.Virodhargala.Count}";
            var source = HouseText(lane.ArgalaHouse, ctx);
            var obstructor = lane.ArgalaOffset == 3 ? "" : $" by {HouseText(lane.ObstructionHouse, ctx)}";
            laneNotes.Add(lane.Verdict switch
            {
                ArgalaVerdict.Holds => $"{source} ({Names(lane.Argala)}) holds, {counter}.",
                ArgalaVerdict.Contested => $"{source} ({Names(lane.Argala)}) is contested{obstructor}, {counter}: equal numbers, so not clearly decided.",
                _ => $"{source} ({Names(lane.Argala)}) is obstructed{obstructor}, {counter}."
            });
        }

        var caveats = new List<string>();
        var restingPlanets = lanes.SelectMany(l => l.Argala.Concat(l.ArgalaOffset == 3 ? [] : l.Virodhargala))
            .Where(p => rests.Contains(p.Planet)).DistinctBy(p => p.Planet);
        foreach (var p in restingPlanets)
        {
            if (p.State.Delivery == Delivery.Negligible)
                caveats.Add($"The result rests on {p.Planet}, who is asleep ({p.State.Alertness}): PVR says its results are negligible, so this win may deliver little.");
            if (p.State.Damaging.Count > 0)
                caveats.Add($"The result rests on {p.Planet}, who is {string.Join(" and ", p.State.Damaging.Select(d => d.State))}: PVR says that state affects the matters of its house.");
        }

        return new ArgalaReading(headline, summary, varga, causes, laneNotes, caveats);
    }
}
