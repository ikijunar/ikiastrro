namespace Ikiastrro.Core.Engines.PlanetaryStates;

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

/// <summary>Everything a chart's planetary-state computation needs from the Rule/Dim layer for one
/// RuleSetId — loaded once by PlanetaryStateRuleRepository, handed to PlanetaryStateComputer.</summary>
public record PlanetaryStateRuleSet(
    byte RuleSetId,
    IReadOnlyList<AgeStateRuleRow> AgeBands,
    IReadOnlyDictionary<string, WakefulnessStateRuleRow> WakefulnessByDignity);

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
}
