using Ikiastrro.Core.Engines.Astronomy;
using Ikiastrro.Core.Engines.Strength;

namespace Ikiastrro.Core.LifeMatters.Promise;

// Phase 1 of docs/architecture/key_inference_promise.md: the vocabulary. Direction and capacity are
// separate properties of every finding — strength says how powerfully a testimony operates, never
// whether it helps — and the verdict is categorical, with confidence kept apart from it.

/// <summary>Which way a finding points for the matter.</summary>
public enum Direction { Supportive, Obstructive, Mixed, Neutral }

/// <summary>How powerfully a finding operates (<see cref="StrengthBands"/>); never converted to a
/// <see cref="Direction"/>. A strong malefic is strongly obstructive.</summary>
public enum Capacity { Strong, Moderate, Weak, Unknown }

/// <summary>The categorical natal promise. Not a probability.</summary>
public enum PromiseVerdict { StrongPositive, PositiveConditional, Mixed, WeakLimited, Adverse, Indeterminate }

/// <summary>How firmly the evidence agrees. Independent of the verdict: Mixed is a firm reading that
/// promise and denial coexist; Low means the evidence is sparse or missing.</summary>
public enum Confidence { High, Medium, Low }

/// <summary>The part a testimony plays. Target, Lord and Karaka are the principal roles.</summary>
public enum TestimonyRole { Target, Lord, Karaka, Influence, Intervention, Context, Yoga, Varga }

/// <summary>One finding about a matter, with its source and the independence family it belongs to.
/// The verdict counts one testimony per <see cref="Family"/>; every testimony stays visible.</summary>
/// <param name="SourceCode">SRC_* code of the rule that produced it.</param>
/// <param name="Chart">D1 or the matter's varga.</param>
/// <param name="Reference">What houses are counted from: Lagna, AL, a planet.</param>
/// <param name="Subject">The house, pada, planet or yoga involved.</param>
public sealed record Testimony(
    string SourceCode, string RuleSetVersion,
    string Chart, string Reference,
    TestimonyRole Role, string Subject,
    Direction Direction, Capacity Capacity,
    string Family, string Explanation)
{
    public bool IsPrincipal => Role is TestimonyRole.Target or TestimonyRole.Lord or TestimonyRole.Karaka;
}

/// <summary>Two principal roles that point opposite ways.</summary>
public sealed record Contradiction(TestimonyRole RoleA, TestimonyRole RoleB, string Description);

/// <summary>The D1 foundation: the gate every other layer is read against.</summary>
public sealed record D1Foundation(
    PromiseVerdict Verdict,
    Confidence Confidence,
    IReadOnlyDictionary<TestimonyRole, Direction> RoleDirections,
    IReadOnlyList<Testimony> Testimonies);

// Placeholders for phases 3 and 4. The shapes are fixed here so the page can be wired once.
public sealed record DomainConfirmation(string Chart, PromiseVerdict Verdict, string Inference);
public sealed record ManifestationLens(string Code, string Label, string Policy, Direction Direction, string Note);

public sealed record LifeMatterPromise(
    string MatterCode,
    D1Foundation D1,
    DomainConfirmation? Domain,
    IReadOnlyList<ManifestationLens> Lenses,
    IReadOnlyList<Testimony> Positive,
    IReadOnlyList<Testimony> Negative,
    IReadOnlyList<Contradiction> Contradictions,
    PromiseVerdict Verdict,
    Confidence Confidence,
    IReadOnlyList<string> MissingEvidence,
    IReadOnlyList<string> Refinements,
    string DominantSupport,
    string DominantObstruction,
    int? TechnicalSupportIndex);

/// <summary>One planet as the engine needs it, in the chart being read. Capacity is the planet's
/// Ṣaḍbala band; Rāhu and Ketu legitimately have none.</summary>
/// <param name="Dignity">DignityEngine's status: Exalted, Moolatrikona, Own Sign, Great Friend, Friend,
/// Neutral, Enemy, Great Enemy or Debilitated.</param>
public sealed record PlanetFact(
    ZodiacName Sign, string? Dignity = null, bool IsCombust = false,
    Capacity Capacity = Capacity.Unknown, PlanetName? NakshatraLord = null, double? LongitudeDegrees = null);

/// <summary>One Argala pair on the target (PVR 10.5-10.6): the planets intervening from
/// <see cref="ArgalaOffset"/> and the planets obstructing them from <see cref="ObstructionOffset"/>.
/// <see cref="ExceptionApplied"/> marks the 2+ malefics in the 3rd, which cause argala instead.</summary>
public sealed record ArgalaLink(
    int ArgalaOffset, int ObstructionOffset,
    IReadOnlyList<PlanetName> ArgalaPlanets, IReadOnlyList<PlanetName> ObstructingPlanets,
    bool ExceptionApplied = false);

/// <summary>A matter a graha naturally signifies and the house it is read from (PVR ch. 8 Table 12).</summary>
public sealed record PlanetSignification(int House, string Matter);

public sealed record YogaFact(string Name, Direction Direction, IReadOnlyList<PlanetName> Planets);

/// <summary>Everything the D1 engine reads for one matter. The caller resolves the target, karakas
/// and bands (<see cref="StrengthBands"/>); the engine touches no database.</summary>
/// <param name="TargetLabel">The house or pada being read, for the explanations.</param>
/// <param name="TargetCapacity">Independent Bhava Bala band of the target house.</param>
/// <param name="SavBand">Sarvāṣṭakavarga band of the target sign.</param>
/// <param name="LordBavBand">The lord's own Bhinnāṣṭakavarga band in the target sign.</param>
/// <param name="ArgalaPlanets">Planets whose Argala on the target holds or is contested.</param>
/// <param name="VirodhargalaPlanets">Planets obstructing that Argala.</param>
public sealed record MatterPromiseInput(
    string MatterCode, ZodiacName Lagna, string TargetLabel, ZodiacName Target,
    IReadOnlyDictionary<PlanetName, PlanetFact> Planets,
    IReadOnlyList<PlanetName>? Karakas = null,
    Capacity TargetCapacity = Capacity.Unknown,
    Capacity SavBand = Capacity.Unknown,
    Capacity LordBavBand = Capacity.Unknown,
    IReadOnlyList<PlanetName>? ArgalaPlanets = null,
    IReadOnlyList<PlanetName>? VirodhargalaPlanets = null,
    IReadOnlyList<YogaFact>? Yogas = null,
    int? TechnicalSupportIndex = null,
    string Chart = "D1",
    string RuleSetVersion = "PVR-1",
    IReadOnlyList<ArgalaLink>? ArgalaLinks = null,
    IReadOnlyDictionary<PlanetName, IReadOnlyList<PlanetSignification>>? Significations = null,
    IReadOnlyDictionary<int, string>? HouseMatters = null);

public static class CapacityExtensions
{
    public static Capacity ToCapacity(this StrengthTier tier) => tier switch
    {
        StrengthTier.Strong => Capacity.Strong,
        StrengthTier.Moderate => Capacity.Moderate,
        StrengthTier.Weak => Capacity.Weak,
        _ => Capacity.Unknown,
    };

    /// <summary>One step down, e.g. for a combust planet: strong becomes moderate, moderate weak.</summary>
    public static Capacity Lowered(this Capacity c) => c switch
    {
        Capacity.Strong => Capacity.Moderate,
        Capacity.Moderate => Capacity.Weak,
        _ => c,
    };
}
