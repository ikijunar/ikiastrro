using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Houses;

namespace Ikiastrro.Core.LifeMatters.Promise;

/// <summary>
/// Phase 2 of docs/architecture/key_inference_promise.md: the D1 promise for one matter. It follows
/// PVR's analytical order (docs/cli/reading/PVR_read_horoscope.md) — target, lord and its placement,
/// karakas, occupants and aspects, conjunctions, dignity and dispositor, nakshatra refinement,
/// strength as capacity, Argala and Virodhargala, functional nature, bādhaka, yogas — and turns each
/// step into <see cref="Testimony"/> rows. Strength never creates a direction: a planet's Ṣaḍbala only
/// sets the capacity of what its placement already says. The verdict counts one testimony per
/// independence family (see <see cref="PromiseFamilies"/>), so one underlying condition is not
/// counted twice. The verdict thresholds are a project heuristic (SRC_IKIASTRRO_SYNTHESIS). Pure.
/// </summary>
public static class D1PromiseEngine
{
    private const string Pvr = "SRC_PVR_INTEGRATED";

    public static LifeMatterPromise Read(MatterPromiseInput input)
    {
        var testimonies = new List<Testimony>();
        var missing = new List<string>();
        var refinements = new List<string>();
        var ctx = new Ctx(input, testimonies, missing, refinements);

        var lord = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(input.Target));
        var karakas = (input.Karakas ?? []).Where(k => k != lord).Distinct().ToList();

        // 1. The target itself: only its own capacity is read here, so it carries no direction.
        testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Lagna", TestimonyRole.Target,
            input.TargetLabel, Direction.Neutral, input.TargetCapacity, PromiseFamilies.Target,
            $"{input.TargetLabel} ({Disp(input.Target)}); house capacity {Word(input.TargetCapacity)}."));

        // 2–3, 6–9. The lord and the karakas: placement, dignity, conjunctions, refinements.
        var lordMissing = !input.Planets.ContainsKey(lord);
        ReadPlanet(ctx, lord, TestimonyRole.Lord, $"lord of {input.TargetLabel}", (input.Karakas ?? []).Contains(lord));
        foreach (var k in karakas) ReadPlanet(ctx, k, TestimonyRole.Karaka, "karaka of the matter", false);

        // 4–5, 11. Planets acting on the target that are neither its lord nor a karaka.
        ReadInfluences(ctx, lord);

        // 10. Argala and Virodhargala.
        ReadIntervention(ctx);

        // 9. Ashtakavarga: PVR reads the sign's own bindus as favourable or not.
        ReadAshtakavarga(ctx);

        // 11. Yogas.
        ReadYogas(ctx, lord, karakas);

        return Synthesise(input, lord, lordMissing, testimonies, missing, refinements);
    }

    // ---------------------------------------------------------------- planets

    private static void ReadPlanet(Ctx c, PlanetName planet, TestimonyRole role, string roleText, bool alsoKaraka)
    {
        var input = c.Input;
        if (!input.Planets.TryGetValue(planet, out var fact))
        {
            c.Missing.Add($"{planet} ({roleText}) is not placed in {input.Chart}");
            return;
        }

        var house = AstroMath.CountFromSignToSign(input.Target, fact.Sign);
        var capacity = fact.IsCombust ? fact.Capacity.Lowered() : fact.Capacity;
        var combust = fact.IsCombust ? "; combust, so its capacity is lowered" : "";
        var text = alsoKaraka ? $"{roleText} and a karaka" : roleText;
        var acts = ActsOnTarget(input, planet, fact);

        c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Target", role, planet.ToString(),
            PlacementDirection(house), capacity, PromiseFamilies.LordshipPlacement(planet),
            $"{planet}, {text}, sits {Ordinal(house)} from {input.TargetLabel} ({ClassesOf(house)}){acts}{combust}."));

        if (fact.Dignity is { Length: > 0 } dignity)
            c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Lagna", role, planet.ToString(),
                DignityDirection(dignity), capacity, PromiseFamilies.Placement(planet),
                $"{planet} is {dignity.ToLowerInvariant()} in {Disp(fact.Sign)}."));

        foreach (var (other, otherFact) in input.Planets.Where(p => p.Key != planet && p.Value.Sign == fact.Sign))
        {
            var d = NatureDirection(input.Lagna, other, MoonWaxing(input));
            if (d == Direction.Neutral) continue;
            c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Lagna", role, planet.ToString(),
                d, otherFact.Capacity, PromiseFamilies.Conjunction(planet),
                $"{other} ({NatureWord(input.Lagna, other, MoonWaxing(input))}) is conjunct {planet}."));
        }

        // Refinements: they colour how the planet delivers; they never create or flip a direction.
        var dispositor = Enum.Parse<PlanetName>(HouseEngine.GetSignLord(fact.Sign));
        if (dispositor != planet && input.Planets.TryGetValue(dispositor, out var dispFact)
            && dispFact.Dignity == "Debilitated")
            c.Refinements.Add($"{planet}'s dispositor {dispositor} is debilitated.");
        if (fact.NakshatraLord is { } star && star != planet && NatureDirection(input.Lagna, star, MoonWaxing(input)) == Direction.Obstructive)
            c.Refinements.Add($"{planet} is in the nakshatra of {star}, a malefic for this Lagna.");
    }

    private static string ActsOnTarget(MatterPromiseInput input, PlanetName planet, PlanetFact fact)
    {
        var links = new List<string>();
        if (RelationshipEngineAspects(planet, fact.Sign, input.Target)) links.Add("aspects it");
        if (fact.Sign != input.Target && RasiDrishtiCalculator.Aspects(fact.Sign, input.Target)) links.Add("casts rāśi dṛṣṭi on it");
        return links.Count == 0 ? "" : "; also " + string.Join(" and ", links);
    }

    private static bool RelationshipEngineAspects(PlanetName planet, ZodiacName from, ZodiacName to) =>
        from != to && Engines.Relationships.RelationshipEngine.AspectsSign(planet.ToString(), from, to);

    // ------------------------------------------------------------- influences

    private static void ReadInfluences(Ctx c, PlanetName lord)
    {
        var input = c.Input;
        if (input.Planets.Count < 9)
        {
            c.Missing.Add("Planets acting on the target need all nine placements");
            return;
        }

        var placements = input.Planets.ToDictionary(p => p.Key, p => p.Value.Sign);
        var reading = TargetInfluences.Read(input.Lagna, placements, input.Target, input.Karakas,
            input.ArgalaPlanets, input.VirodhargalaPlanets, MoonWaxing(input));
        var targetHouseFromLagna = AstroMath.CountFromSignToSign(input.Lagna, input.Target);
        var karakaSet = (input.Karakas ?? []).ToHashSet();

        foreach (var p in reading.Planets)
        {
            const InfluenceLink acts = InfluenceLink.Occupies | InfluenceLink.GrahaDrishti | InfluenceLink.RasiDrishti;
            if ((p.Links & acts) == 0 || p.Planet == lord || karakaSet.Contains(p.Planet)) continue;

            // PVR step 5 and 13.2 together: what the planet is (functional nature) and where it stands from
            // the target (quadrant, trine and upachaya sustain; dusthana obstructs). They agree, or one is
            // silent, or the result is Mixed. A planet in the target itself has no position from it, so
            // only its nature counts; the bādhaka always obstructs.
            var direction = p.IsBaadhaka || p.InBaadhakaSthaana
                ? Direction.Obstructive
                : Combine(NatureDirection(input.Lagna, p.Planet, MoonWaxing(input)),
                    p.Links.HasFlag(InfluenceLink.Occupies) ? Direction.Neutral : LeanDirection(p.Lean));
            // PVR 13.2: a functional malefic in the 3rd or a dusthana is good, it spoils a house that
            // should be spoiled. Applied only to a planet standing in the target itself.
            if (direction == Direction.Obstructive && p.Links.HasFlag(InfluenceLink.Occupies)
                && p.Functional == FunctionalNature.Malefic && targetHouseFromLagna is 3 or 6 or 8 or 12)
                direction = Direction.Supportive;

            var how = new List<string>();
            if (p.Links.HasFlag(InfluenceLink.Occupies)) how.Add("occupies");
            if (p.Links.HasFlag(InfluenceLink.GrahaDrishti)) how.Add("aspects (graha dṛṣṭi)");
            if (p.Links.HasFlag(InfluenceLink.RasiDrishti)) how.Add("aspects (rāśi dṛṣṭi)");
            var baadhaka = p.IsBaadhaka ? "; it is the target's bādhaka" : p.InBaadhakaSthaana ? "; it stands in the bādhaka sthāna" : "";
            c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Target", TestimonyRole.Influence,
                p.Planet.ToString(), direction, input.Planets[p.Planet].Capacity, PromiseFamilies.Influence(p.Planet),
                $"{p.Planet} ({NatureWord(input.Lagna, p.Planet, MoonWaxing(input))}) {string.Join(" and ", how)} {input.TargetLabel}{baadhaka}."));
        }
    }

    private static void ReadIntervention(Ctx c)
    {
        var input = c.Input;
        if (input.ArgalaLinks is { Count: > 0 } links) { ReadInterventionLinks(c, links); return; }

        var argala = (input.ArgalaPlanets ?? []).Distinct().ToList();
        var virodha = (input.VirodhargalaPlanets ?? []).Distinct().ToList();
        if (argala.Count == 0) return;

        var benefic = argala.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Supportive);
        var malefic = argala.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Obstructive);
        Direction direction;
        string text;
        if (virodha.Count >= argala.Count)
        {
            direction = Direction.Neutral;
            text = $"Argala by {string.Join(", ", argala)} is blocked by virodhargala from {string.Join(", ", virodha)}.";
        }
        else
        {
            direction = benefic > malefic ? Direction.Supportive
                : malefic > benefic ? Direction.Obstructive
                : benefic > 0 ? Direction.Mixed : Direction.Neutral;
            text = $"Argala by {string.Join(", ", argala)} on {input.TargetLabel}"
                 + (virodha.Count > 0 ? $", partly obstructed by {string.Join(", ", virodha)}." : ".");
        }

        c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Target", TestimonyRole.Intervention,
            input.TargetLabel, direction, virodha.Count > 0 ? Capacity.Weak : Capacity.Moderate,
            PromiseFamilies.Intervention, text));
    }

    /// <summary>PVR 10.5-10.6 pair by pair: is each argala good (subhaargala) or bad (paapaargala), does
    /// the virodhargala from its paired house hold it back, and what the argala planet brings to the
    /// target from its significations (PVR ch. 8) — as the matter's own karaka, or by the matters it
    /// naturally signifies, above all one read from the target house itself.</summary>
    private static void ReadInterventionLinks(Ctx c, IReadOnlyList<ArgalaLink> links)
    {
        var input = c.Input;
        var karakas = (input.Karakas ?? []).ToHashSet();
        var targetHouse = AstroMath.CountFromSignToSign(input.Lagna, input.Target);
        var active = new List<PlanetName>();
        var anyObstruction = false;
        var lines = new List<string>();

        foreach (var link in links.Where(l => l.ArgalaPlanets.Count > 0))
        {
            var planets = link.ArgalaPlanets.Distinct().ToList();
            var obstructing = link.ObstructingPlanets.Distinct().ToList();
            var guard = Ordinal(link.ObstructionOffset);

            var good = planets.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Supportive);
            var bad = planets.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Obstructive);
            var kind = good > bad ? "subhaargala (good)" : bad > good ? "paapaargala (bad)" : good > 0 ? "mixed argala" : "neutral argala";

            string virodha;
            var blocked = obstructing.Count >= planets.Count;
            if (link.ExceptionApplied)
                virodha = $"the {guard} holds {string.Join(", ", obstructing)}: 2+ malefics there cause argala instead of obstructing (PVR 10.6)";
            else if (obstructing.Count == 0)
                virodha = $"no virodhargala from the {guard}, so it stands";
            else if (blocked)
                virodha = $"virodhargala from the {guard} ({string.Join(", ", obstructing)}) is as strong or stronger, so the argala is blocked";
            else
                virodha = $"virodhargala from the {guard} ({string.Join(", ", obstructing)}) answers it only in part";
            if (obstructing.Count > 0 && !link.ExceptionApplied) anyObstruction = true;

            var brings = planets.Select(p => Brings(input, p, karakas, targetHouse, good >= bad)).ToList();
            // The house the argala planets stand in, from lagna, and what that house signifies (PVR ch. 7).
            var argalaHouse = (targetHouse + link.ArgalaOffset - 2) % 12 + 1;
            if (input.HouseMatters is not null && input.HouseMatters.TryGetValue(argalaHouse, out var houseMatters))
                brings.Add($"They stand in the {Ordinal(argalaHouse)} house, which signifies {houseMatters}.");
            var role = ArgalaPositionRole.Meaning(link.ArgalaOffset);
            var roleText = role is null ? "" : $" ({role})";
            lines.Add($"{kind} from the {Ordinal(link.ArgalaOffset)}{roleText} by {string.Join(", ", planets)} — {virodha}. {string.Join(" ", brings)}");

            if (!blocked || link.ExceptionApplied) active.AddRange(planets);
        }
        if (lines.Count == 0) return;

        var benefic = active.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Supportive);
        var malefic = active.Count(p => NatureDirection(input.Lagna, p, MoonWaxing(input)) == Direction.Obstructive);
        var direction = active.Count == 0 ? Direction.Neutral
            : benefic > malefic ? Direction.Supportive
            : malefic > benefic ? Direction.Obstructive
            : benefic > 0 ? Direction.Mixed : Direction.Neutral;

        c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Target", TestimonyRole.Intervention,
            input.TargetLabel, direction, anyObstruction ? Capacity.Weak : Capacity.Moderate,
            PromiseFamilies.Intervention, string.Join(" | ", lines)));
    }

    private static string Brings(MatterPromiseInput input, PlanetName planet, HashSet<PlanetName> karakas, int targetHouse, bool good)
    {
        var effect = good ? "supports" : "strains";
        var signifies = input.Significations is not null && input.Significations.TryGetValue(planet, out var list) ? list : [];
        var onTarget = signifies.Where(s => s.House == targetHouse).Select(s => s.Matter).ToList();
        var others = signifies.Where(s => s.House != targetHouse).Select(s => $"{s.Matter} ({Ordinal(s.House)})").Take(3).ToList();

        var parts = new List<string>();
        if (karakas.Contains(planet))
            parts.Add($"{planet} is the matter's own karaka, so its argala {effect} {input.TargetLabel} directly");
        if (onTarget.Count > 0)
            parts.Add($"{planet} naturally signifies {string.Join(", ", onTarget)}, read from this very house, so it {effect} what the house stands for");
        if (parts.Count == 0 && others.Count > 0)
            parts.Add($"{planet} brings its own significations ({string.Join(", ", others)}) to bear on {input.TargetLabel}");
        if (parts.Count == 0)
            parts.Add($"{planet} has no natural signification tied to {input.TargetLabel}");
        return string.Join("; ", parts) + ".";
    }

    private static void ReadAshtakavarga(Ctx c)
    {
        var input = c.Input;
        void Add(Capacity band, string what)
        {
            if (band is Capacity.Moderate or Capacity.Unknown) return;
            c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Target", TestimonyRole.Context,
                input.TargetLabel, band == Capacity.Strong ? Direction.Supportive : Direction.Obstructive, band,
                PromiseFamilies.Ashtakavarga, $"{what} is {Word(band)}."));
        }
        Add(input.SavBand, $"Sarvāṣṭakavarga of {Disp(input.Target)}");
        Add(input.LordBavBand, $"The lord's own Bhinnāṣṭakavarga in {Disp(input.Target)}");
    }

    private static void ReadYogas(Ctx c, PlanetName lord, IReadOnlyList<PlanetName> karakas)
    {
        var input = c.Input;
        foreach (var yoga in input.Yogas ?? [])
        {
            // A yoga formed by the very conjunction already read on the lord or a karaka is the same
            // evidence, so it joins that conjunction's family instead of counting again.
            var placed = yoga.Planets.Where(input.Planets.ContainsKey).ToList();
            PlanetName? principal = new[] { lord }.Concat(karakas).Cast<PlanetName?>().FirstOrDefault(p => placed.Contains(p!.Value));
            var conjunction = principal is not null && placed.Count >= 2
                              && placed.Select(p => input.Planets[p].Sign).Distinct().Count() == 1;
            var family = conjunction ? PromiseFamilies.Conjunction(principal!.Value) : PromiseFamilies.Yoga(yoga.Name);
            var capacity = placed.Select(p => input.Planets[p].Capacity).Where(x => x != Capacity.Unknown)
                .DefaultIfEmpty(Capacity.Unknown).OrderByDescending(x => (int)x).First();
            c.Testimonies.Add(new Testimony(Pvr, input.RuleSetVersion, input.Chart, "Lagna", TestimonyRole.Yoga,
                yoga.Name, yoga.Direction, capacity, family,
                $"{yoga.Name} ({string.Join(", ", yoga.Planets)}){(conjunction ? ", the conjunction already read" : "")}."));
        }
    }

    // --------------------------------------------------------------- verdict

    private sealed record FamilyResult(string Key, bool Principal, bool HasLord, Direction Direction, Capacity Capacity);

    private static LifeMatterPromise Synthesise(
        MatterPromiseInput input, PlanetName lord, bool lordMissing,
        List<Testimony> testimonies, List<string> missing, List<string> refinements)
    {
        var families = testimonies.GroupBy(t => t.Family).Select(g => new FamilyResult(
            g.Key,
            g.Any(t => t.IsPrincipal),
            g.Any(t => t.Role == TestimonyRole.Lord),
            PromiseFamilies.Resolve(g.Select(t => t.Direction)),
            PromiseFamilies.Weakest(g.Select(t => t.Capacity)))).ToList();

        int Count(IEnumerable<FamilyResult> f, Direction d) => f.Count(x => x.Direction == d);
        var principal = families.Where(f => f.Principal).ToList();
        var s = Count(families, Direction.Supportive);
        var o = Count(families, Direction.Obstructive);
        var m = Count(families, Direction.Mixed);
        var ps = Count(principal, Direction.Supportive);
        var po = Count(principal, Direction.Obstructive);
        var lordFamilies = families.Where(f => f.HasLord).ToList();
        var lordSup = Count(lordFamilies, Direction.Supportive);
        var lordObs = Count(lordFamilies, Direction.Obstructive);
        var principalSupportive = principal.Where(f => f.Direction == Direction.Supportive).ToList();
        var anyStrongEnough = principalSupportive.Any(f => f.Capacity != Capacity.Weak);
        var weakCapacity = principalSupportive.Any(f => f.Capacity == Capacity.Weak);

        PromiseVerdict verdict;
        if (lordMissing || s + o + m == 0) verdict = PromiseVerdict.Indeterminate;
        else if (ps >= 3 && po == 0 && lordObs == 0 && !weakCapacity) verdict = PromiseVerdict.StrongPositive;
        else if ((ps == 0 && po > 0 && o > s) || (o >= 2 && o > s && lordObs > lordSup)) verdict = PromiseVerdict.Adverse;
        else if (ps == 0) verdict = s > 0 || o > 0 || po > 0 ? PromiseVerdict.WeakLimited : PromiseVerdict.Mixed;
        else if (po == 0 && !anyStrongEnough) verdict = PromiseVerdict.WeakLimited;
        else if (po > 0 && Math.Abs(ps - po) <= 1) verdict = PromiseVerdict.Mixed;
        else if (ps > po) verdict = PromiseVerdict.PositiveConditional;
        else verdict = PromiseVerdict.Mixed;

        // Role directions drive confidence and the contradiction list: the lord, the karakas, and
        // everything else acting on the target (influences, Argala, Ashtakavarga, yogas).
        var roleDirections = new Dictionary<TestimonyRole, Direction>
        {
            [TestimonyRole.Lord] = RoleDirection(testimonies, t => t.Role == TestimonyRole.Lord),
            [TestimonyRole.Karaka] = RoleDirection(testimonies, t => t.Role == TestimonyRole.Karaka),
            [TestimonyRole.Influence] = RoleDirection(testimonies, t => t.Role is TestimonyRole.Influence
                or TestimonyRole.Intervention or TestimonyRole.Context or TestimonyRole.Yoga),
        };
        var directional = roleDirections.Where(r => r.Value != Direction.Neutral).ToList();

        var contradictions = new List<Contradiction>();
        foreach (var (a, b) in new[] { (TestimonyRole.Lord, TestimonyRole.Karaka), (TestimonyRole.Lord, TestimonyRole.Influence), (TestimonyRole.Karaka, TestimonyRole.Influence) })
            if (roleDirections[a] != Direction.Neutral && roleDirections[b] != Direction.Neutral
                && roleDirections[a] != Direction.Mixed && roleDirections[b] != Direction.Mixed
                && roleDirections[a] != roleDirections[b])
                contradictions.Add(new Contradiction(a, b,
                    $"{RoleLabel(a)} points {roleDirections[a].ToString().ToLowerInvariant()}, {RoleLabel(b)} {roleDirections[b].ToString().ToLowerInvariant()}."));

        Confidence d1Confidence;
        if (lordMissing || missing.Count > 0 || directional.Count < 2) d1Confidence = Confidence.Low;
        else if (directional.Count == 3 && directional.All(r => r.Value == directional[0].Value && r.Value != Direction.Mixed))
            d1Confidence = Confidence.High;
        else d1Confidence = Confidence.Medium;

        // No varga has confirmed the promise yet, so confidence cannot reach High on D1 alone.
        var confidence = d1Confidence == Confidence.High ? Confidence.Medium : d1Confidence;

        var positive = testimonies.Where(t => t.Direction is Direction.Supportive or Direction.Mixed).ToList();
        var negative = testimonies.Where(t => t.Direction is Direction.Obstructive or Direction.Mixed).ToList();

        return new LifeMatterPromise(
            input.MatterCode,
            new D1Foundation(verdict, d1Confidence, roleDirections, testimonies),
            Domain: null, Lenses: [], positive, negative, contradictions, verdict, confidence,
            missing, refinements,
            Dominant(positive, Direction.Supportive), Dominant(negative, Direction.Obstructive),
            input.TechnicalSupportIndex);
    }

    private static Direction RoleDirection(IEnumerable<Testimony> all, Func<Testimony, bool> inRole)
    {
        var families = all.Where(inRole).GroupBy(t => t.Family)
            .Select(g => PromiseFamilies.Resolve(g.Select(t => t.Direction))).ToList();
        return PromiseFamilies.Resolve(families);
    }

    private static string Dominant(IEnumerable<Testimony> list, Direction direction) =>
        list.Where(t => t.Direction == direction)
            .OrderByDescending(t => t.IsPrincipal)
            .ThenBy(t => t.Capacity == Capacity.Unknown ? 3 : (int)t.Capacity)
            .Select(t => t.Explanation).FirstOrDefault() ?? "";

    // --------------------------------------------------------------- helpers

    private sealed record Ctx(MatterPromiseInput Input, List<Testimony> Testimonies, List<string> Missing, List<string> Refinements);

    /// <summary>PVR step 5: from the target, quadrants sustain, trines let it prosper, upachayas let it
    /// grow, dusthanas obstruct; a house in both a helping and a hindering class (6th) reads Mixed.</summary>
    public static Direction PlacementDirection(int houseFromTarget)
    {
        var position = TargetInfluences.PositionOf(houseFromTarget);
        var helps = (position & (TargetPosition.Quadrant | TargetPosition.Trine | TargetPosition.Upachaya)) != 0;
        var hinders = (position & TargetPosition.Dusthana) != 0;
        return helps && hinders ? Direction.Mixed : helps ? Direction.Supportive : hinders ? Direction.Obstructive : Direction.Neutral;
    }

    private static Direction LeanDirection(InfluenceLean lean) => lean switch
    {
        InfluenceLean.Supports => Direction.Supportive,
        InfluenceLean.Obstructs => Direction.Obstructive,
        InfluenceLean.Mixed => Direction.Mixed,
        _ => Direction.Neutral,
    };

    private static Direction Combine(Direction nature, Direction position) =>
        position == Direction.Neutral ? nature
        : nature == Direction.Neutral ? position
        : nature == position ? nature
        : Direction.Mixed;

    public static Direction DignityDirection(string? dignity) => dignity switch
    {
        "Exalted" or "Moolatrikona" or "Own Sign" or "Great Friend" or "Friend" => Direction.Supportive,
        "Debilitated" or "Enemy" or "Great Enemy" => Direction.Obstructive,
        _ => Direction.Neutral,
    };

    /// <summary>Functional nature for the Lagna: benefic and yogakaraka support, malefic obstructs,
    /// neutral is neutral. Rāhu and Ketu own no sign, so they read as natural malefics.</summary>
    public static Direction NatureDirection(ZodiacName lagna, PlanetName planet, bool? moonWaxing = null)
    {
        if (planet is PlanetName.Rahu or PlanetName.Ketu) return Direction.Obstructive;
        return LagnaFunctionalNature.For(lagna, planet, moonWaxing).Nature switch
        {
            FunctionalNature.Benefic or FunctionalNature.Yogakaraka => Direction.Supportive,
            FunctionalNature.Malefic => Direction.Obstructive,
            _ => Direction.Neutral,
        };
    }

    /// <summary>The birth Moon's phase for PVR 13.2: exact from longitudes when every planet fact carries one,
    /// otherwise from the signs.</summary>
    private static bool? MoonWaxing(MatterPromiseInput input) =>
        input.Planets.TryGetValue(PlanetName.Sun, out var sun) && input.Planets.TryGetValue(PlanetName.Moon, out var moon)
            ? sun.LongitudeDegrees is { } sl && moon.LongitudeDegrees is { } ml
                ? MoonPhase.IsWaxing(sl, ml)
                : MoonPhase.IsWaxing(sun.Sign, moon.Sign)
            : null;

    private static string NatureWord(ZodiacName lagna, PlanetName planet, bool? moonWaxing = null) =>
        planet is PlanetName.Rahu or PlanetName.Ketu
            ? "natural malefic"
            : $"functional {LagnaFunctionalNature.For(lagna, planet, moonWaxing).Nature.ToString().ToLowerInvariant()}";

    private static string ClassesOf(int house)
    {
        var p = TargetInfluences.PositionOf(house);
        var names = new List<string>();
        if (p.HasFlag(TargetPosition.Quadrant)) names.Add("quadrant");
        if (p.HasFlag(TargetPosition.Trine)) names.Add("trine");
        if (p.HasFlag(TargetPosition.Upachaya)) names.Add("upachaya");
        if (p.HasFlag(TargetPosition.Dusthana)) names.Add("dusthana");
        return names.Count == 0 ? "no helping or hindering class" : string.Join(" and ", names);
    }

    private static string RoleLabel(TestimonyRole r) => r switch
    {
        TestimonyRole.Lord => "the lord",
        TestimonyRole.Karaka => "the karakas",
        _ => "the planets acting on the target",
    };

    private static string Word(Capacity c) => c.ToString().ToLowerInvariant();

    private static string Ordinal(int n) =>
        n + (n % 100 is 11 or 12 or 13 ? "th" : (n % 10) switch { 1 => "st", 2 => "nd", 3 => "rd", _ => "th" });

    private static string Disp(ZodiacName s) => s == ZodiacName.Capricornus ? "Capricorn" : s.ToString();
}
