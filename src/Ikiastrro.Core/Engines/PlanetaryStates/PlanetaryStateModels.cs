namespace Ikiastrro.Core.Engines.PlanetaryStates;

/// <summary>Why one avastha state was assigned: the state and the inputs that produced it, in words
/// ("3.8° from the Sun (within the 5° orb)"). Built by the same calculation that picks the state, so the
/// reason can never disagree with the result. Not persisted — recomputed from the chart when shown.</summary>
public sealed record StateReason(string System, string State, string Reason);

/// <summary>One row of dbo.tbl_Dim_PlanetaryState — the vocabulary of avastha states across every
/// avastha system (Baaladi, Jagradadi, Deeptadi, Lajjitadi, Sayanadi). Read-only reference.</summary>
public record PlanetaryStateRow(
    byte Id, string AvasthaSystem, string StateName, byte SequenceOrder, string? Meaning);

/// <summary>One row of dbo.tbl_Rule_AgeState — the within-sign degree bands (odd vs even sign)
/// and the classical effect fraction for one Baaladi state, under a given RuleSetId. StateName is
/// joined in from tbl_Dim_PlanetaryState for convenience.</summary>
public record AgeStateRuleRow(
    byte Id, byte RuleSetId, byte AvasthaStateId, string StateName,
    decimal OddSignFromDegree, decimal OddSignToDegree,
    decimal EvenSignFromDegree, decimal EvenSignToDegree,
    decimal EffectFraction);

/// <summary>One row of dbo.tbl_Rule_WakefulnessState — maps a DignityStatus value to a Jagradadi
/// state (Jagrat / Swapna / Sushupti) under a given RuleSetId. StateName joined in from
/// tbl_Dim_PlanetaryState.</summary>
public record WakefulnessStateRuleRow(
    byte Id, byte RuleSetId, string DignityStatus, byte AvasthaStateId, string StateName);

/// <summary>One row of dbo.tbl_Rule_DeeptadiState — maps a DignityStatus value to one of Dīptādi's
/// 6 dignity-tier states (Deepta/Swastha/Mudita/Saanta/Deena/Duhkhita, PVR sec 15.4.3) under a given
/// RuleSetId. PVR names only 6 tiers; this project's DignityStatus has 9 values, folded the same
/// shape tbl_Rule_WakefulnessState already uses (Moolatrikona -> Swastha alongside Own Sign; Great
/// Enemy/Debilitated -> Duhkhita alongside Enemy) — see the row's own CalculationNarrative.
/// Dīptādi's other 3 states (Vikala/Khala/Kopita) are affliction flags, not dignity-tier, so they
/// have no rule row here — DeeptadiStateCalculator computes them directly, same reasoning as
/// Sayanaadi having no per-state rule row.</summary>
public record DeeptadiStateRuleRow(
    byte Id, byte RuleSetId, string DignityStatus, byte AvasthaStateId, string StateName);

/// <summary>Everything a chart's planetary-state computation needs from the Rule/Dim layer for one
/// RuleSetId — loaded once by PlanetaryStateRuleRepository, handed to PlanetaryStateComputer.
/// <see cref="PostureStatesBySequence"/> is keyed by the Sayanaadi index (1-12,
/// tbl_Dim_PlanetaryState.SequenceOrder) — no separate Rule row per state (unlike AgeState /
/// WakefulnessState): PostureStateCalculator computes the index, this just names it.
/// <see cref="DeeptadiStatesByName"/> / <see cref="LajjitadiStatesByName"/> are the Dim vocabulary
/// for those two systems keyed by StateName, for DeeptadiStateCalculator/LajjitadiStateCalculator to
/// resolve their directly-computed (non-Rule-table) states.</summary>
public record PlanetaryStateRuleSet(
    byte RuleSetId,
    IReadOnlyList<AgeStateRuleRow> AgeBands,
    IReadOnlyDictionary<string, WakefulnessStateRuleRow> WakefulnessByDignity,
    IReadOnlyDictionary<byte, PlanetaryStateRow> PostureStatesBySequence,
    IReadOnlyDictionary<string, DeeptadiStateRuleRow> DeeptadiByDignity,
    IReadOnlyDictionary<string, PlanetaryStateRow> DeeptadiStatesByName,
    IReadOnlyDictionary<string, PlanetaryStateRow> LajjitadiStatesByName);

/// <summary>One row of dbo.tbl_Fact_PlanetaryState — the computed avastha states for one planet in
/// one chart. Ascendant excluded. The age state is D1-only (needs within-sign degree); the
/// wakefulness state is populated for every chart type. RuleSetId records which rule rows produced
/// this fact.</summary>
public class PlanetaryStateFact
{
    public int Id { get; set; }
    public int ChartResultId { get; set; }
    public string Planet { get; set; } = string.Empty;
    /// <summary>FK to tbl_Planets for Planet.</summary>
    public byte? PlanetId { get; set; }
    public byte RuleSetId { get; set; }

    /// <summary>FK to tbl_Dim_PlanetaryState (AvasthaSystem = 'Baaladi'). D1 only — null otherwise.</summary>
    public byte? AgeStateId { get; set; }
    /// <summary>Classical strength fraction for the Baaladi state (Baala .25 / Kumara .50 / Yuva 1 / Vriddha .125 / Mrita 0). D1 only.</summary>
    public decimal? AgeEffectFraction { get; set; }

    /// <summary>FK to tbl_Dim_PlanetaryState (AvasthaSystem = 'Jagradadi'). Populated for every chart type. Null only if DignityStatus was absent.</summary>
    public byte? WakefulnessStateId { get; set; }

    /// <summary>FK to tbl_Dim_PlanetaryState (AvasthaSystem = 'Sayanadi'). D1 only — needs a
    /// continuous within-sign degree (for the navamsa index) and the birth-moment Janma Ghatis,
    /// same D1-only reasoning as AgeStateId.</summary>
    public byte? PostureStateId { get; set; }

    /// <summary>tbl_Dim_PlanetaryState Ids (AvasthaSystem = 'Deeptadi') matched for this planet —
    /// 1 dignity tier plus 0-3 affliction flags (Vikala/Khala/Kopita), PVR sec 15.4.3. The book gives
    /// no precedence between these, so every match is kept, not just one. Populated for every chart
    /// type (dignity-based, like Wakefulness). Flattened into tbl_Fact_PlanetaryStateFlag rows at
    /// persistence time — not itself a DB column.</summary>
    public List<byte> DeeptadiStateIds { get; set; } = new();

    /// <summary>tbl_Dim_PlanetaryState Ids (AvasthaSystem = 'Lajjitadi') matched for this planet —
    /// 0-6 independent condition flags, PVR sec 15.4.3's unnamed 6-state group. Same
    /// keep-every-match reasoning as <see cref="DeeptadiStateIds"/>.</summary>
    public List<byte> LajjitadiStateIds { get; set; } = new();

    /// <summary>How each state above was reached (Baaladi, Jagradadi, Sayanadi, Deeptadi, Lajjitadi), filled by
    /// PlanetaryStateComputer on every run. Not a DB column — stored facts carry only the ids.</summary>
    public List<StateReason> Reasons { get; set; } = new();
}
